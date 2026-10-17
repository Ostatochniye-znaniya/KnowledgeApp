#!/bin/bash

set -e

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1" >&2
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_step() {
    echo -e "${BLUE}[STEP]${NC} $1"
}

# Проверка наличия необходимых утилит
check_dependencies() {
    log_step "Проверка зависимостей..."

    if ! command -v docker &> /dev/null; then
        log_error "Docker не установлен"
        exit 1
    fi
    log_info "Docker установлен: $(docker --version)"

    if command -v docker-compose &> /dev/null; then
        DOCKER_COMPOSE_CMD="docker-compose"
        log_info "Найден docker-compose (standalone)"
    elif docker compose version &> /dev/null 2>&1; then
        DOCKER_COMPOSE_CMD="docker compose"
        log_info "Найден docker compose (plugin)"
    else
        log_error "Docker Compose не установлен"
        exit 1
    fi
}

# Выбор compose-файла: приоритет у DEPLOY_COMPOSE_FILE (его задаёт workflow).
# Без переменной - старое поведение (dev-файл, если есть).
check_files() {
    log_step "Проверка файлов и директорий..."

    COMPOSE_FILE="${DEPLOY_COMPOSE_FILE:-}"

    if [ -z "$COMPOSE_FILE" ]; then
        if [ -f "docker-compose.dev.yml" ]; then
            COMPOSE_FILE="docker-compose.dev.yml"
        elif [ -f "docker-compose.yml" ]; then
            COMPOSE_FILE="docker-compose.yml"
        else
            log_error "Файл docker-compose.yml или docker-compose.dev.yml не найден"
            exit 1
        fi
        log_warn "DEPLOY_COMPOSE_FILE не задан, выбран файл: $COMPOSE_FILE"
    fi

    if [ ! -f "$COMPOSE_FILE" ]; then
        log_error "Compose файл не найден: $COMPOSE_FILE"
        exit 1
    fi

    log_info "Compose файл: $COMPOSE_FILE"
}

# Создание Docker сети
create_network() {
    local network_name="$1"

    log_step "Проверка Docker сети: $network_name"

    if docker network inspect "$network_name" >/dev/null 2>&1; then
        log_info "✅ Сеть $network_name уже существует"
        docker network ls --filter "name=^${network_name}$" --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}"
        return 0
    fi

    log_info "Создание сети $network_name..."

    if docker network create "$network_name" --driver bridge 2>/dev/null; then
        log_info "✅ Сеть $network_name успешно создана"
        sleep 2
        if docker network inspect "$network_name" >/dev/null 2>&1; then
            log_info "✅ Сеть $network_name готова к использованию"
            return 0
        else
            log_error "❌ Сеть $network_name создана, но недоступна"
            return 1
        fi
    else
        log_error "❌ Не удалось создать сеть $network_name"
        log_info "Существующие сети:"
        docker network ls
        return 1
    fi
}

# Остановка контейнеров ТОЛЬКО текущего проекта (name: в compose-файле).
# Тома (БД, MinIO) НЕ удаляются.
cleanup_containers() {
    log_step "Очистка старых контейнеров..."

    if $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps --quiet 2>/dev/null | grep -q .; then
        log_info "Остановка существующих контейнеров..."
        $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" down --remove-orphans 2>/dev/null || {
            log_warn "Не удалось полностью остановить контейнеры"
            $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" kill 2>/dev/null || true
            $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" rm -f 2>/dev/null || true
        }
        log_info "Контейнеры остановлены и удалены (тома сохранены)"
    else
        log_info "Нет запущенных контейнеров"
    fi

    # Только неиспользуемые образы; тома не трогаем никогда
    if [ "${CLEAN_IMAGES:-false}" = "true" ]; then
        log_info "Очистка неиспользуемых образов..."
        docker image prune -f 2>/dev/null || true
    fi
}

# Запуск контейнеров
start_containers() {
    log_step "Запуск контейнеров..."

    if ! docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
        log_error "Сеть $NETWORK_NAME не существует! Запуск невозможен."
        return 1
    fi

    if $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" up -d --build --force-recreate --remove-orphans; then
        log_info "Контейнеры успешно запущены"
    else
        log_error "Не удалось запустить контейнеры"
        log_info "Логи ошибок:"
        $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" logs --tail=50
        return 1
    fi

    local wait_time="${WAIT_TIME:-15}"
    log_info "Ожидание запуска контейнеров (${wait_time} секунд)..."

    local i=0
    while [ $i -lt $wait_time ]; do
        if ! $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps --quiet 2>/dev/null | grep -q .; then
            log_error "Контейнеры не запустились или упали сразу после запуска"
            $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" logs --tail=50
            return 1
        fi
        printf "."
        sleep 1
        i=$((i + 1))
    done
    echo ""

    return 0
}

# Проверка статуса контейнеров
check_status() {
    log_step "Проверка статуса контейнеров..."

    echo ""
    $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps
    echo ""

    local containers=$($DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps -q 2>/dev/null | wc -l)

    if [ "$containers" -eq 0 ]; then
        log_error "Нет запущенных контейнеров"
        return 1
    fi

    local unhealthy=0
    local running=0

    for container in $($DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps -q 2>/dev/null); do
        local status=$(docker inspect --format='{{.State.Status}}' "$container" 2>/dev/null)
        local health=$(docker inspect --format='{{.State.Health.Status}}' "$container" 2>/dev/null)

        if [ "$status" = "running" ]; then
            running=$((running + 1))
            if [ "$health" = "unhealthy" ]; then
                unhealthy=$((unhealthy + 1))
                log_error "Контейнер $container нездоров (unhealthy)"
                docker logs --tail=20 "$container" 2>/dev/null || true
            fi
        else
            log_warn "Контейнер $container не запущен (статус: $status)"
        fi
    done

    if [ "$unhealthy" -gt 0 ]; then
        log_error "Обнаружено $unhealthy нездоровых контейнеров"
        return 1
    fi

    if [ "$running" -eq 0 ]; then
        log_error "Нет запущенных контейнеров"
        return 1
    fi

    log_info "✅ $running контейнеров успешно запущено"
    return 0
}

# Вывод информации о сети
show_network_info() {
    local network_name="$1"

    log_step "Информация о сети $network_name:"

    if docker network inspect "$network_name" 2>/dev/null | grep -A 20 "Containers" | grep -v "\[\]" | head -20; then
        log_info "Контейнеры в сети:"
        docker network inspect "$network_name" | grep -E "Name|IPv4Address" | grep -v "\[\]" | sed 's/^[[:space:]]*//' | head -10
    else
        log_warn "Нет контейнеров в сети $network_name или сеть пуста"
    fi
}

# Проверка Docker демона
check_docker_daemon() {
    log_step "Проверка Docker демона..."

    if ! docker info &>/dev/null; then
        log_error "Docker демон не запущен"
        exit 1
    fi
    log_info "Docker демон работает"
}

# Основная функция
main() {
    log_step "=== Начало деплоя ==="
    log_info "Время запуска: $(date)"
    log_info "Текущая директория: $(pwd)"
    log_info "Пользователь: $(whoami)"

    check_docker_daemon
    check_dependencies
    check_files

    # Имя сети по умолчанию зависит от окружения (должно совпадать с default в compose)
    if [ "$COMPOSE_FILE" = "docker-compose.yml" ]; then
        DEFAULT_NETWORK="app-network-prod"
    else
        DEFAULT_NETWORK="app-network-dev"
    fi
    NETWORK_NAME="${NETWORK_NAME:-$DEFAULT_NETWORK}"
    log_info "Имя сети: $NETWORK_NAME"

    # Сеть должна существовать ДО вызова Docker Compose (external: true)
    if ! create_network "$NETWORK_NAME"; then
        log_error "Не удалось создать/проверить сеть $NETWORK_NAME"
        exit 1
    fi

    export NETWORK_NAME="$NETWORK_NAME"

    if grep -q "external:" "$COMPOSE_FILE" && ! grep -q "external: false" "$COMPOSE_FILE"; then
        log_info "Compose файл использует внешнюю сеть: $NETWORK_NAME"
        if ! docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
            log_error "Сеть $NETWORK_NAME не найдена, хотя compose файл ожидает её"
            exit 1
        fi
    fi

    cleanup_containers

    if ! start_containers; then
        log_error "❌ Деплой завершился с ошибками при запуске контейнеров"
        exit 1
    fi

    if ! check_status; then
        log_error "❌ Деплой завершился с ошибками при проверке статуса"
        exit 1
    fi

    show_network_info "$NETWORK_NAME"

    log_info "✅ Деплой успешно завершен!"
    log_info "Время завершения: $(date)"

    log_info "Полезные команды:"
    echo "  $DOCKER_COMPOSE_CMD -f $COMPOSE_FILE logs -f    # Просмотр логов"
    echo "  $DOCKER_COMPOSE_CMD -f $COMPOSE_FILE ps         # Статус контейнеров"
    echo "  docker network inspect $NETWORK_NAME            # Информация о сети"
}

main "$@"