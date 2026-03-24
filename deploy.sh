#!/bin/bash

set -e

# Цвета для вывода
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
    
    # Проверка Docker
    if ! command -v docker &> /dev/null; then
        log_error "Docker не установлен"
        exit 1
    fi
    log_info "Docker установлен: $(docker --version)"
    
    # Определяем команду docker-compose
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

# Проверка файлов и директорий
check_files() {
    log_step "Проверка файлов и директорий..."
    
    # Проверяем наличие compose файла
    if [ -f "docker-compose.dev.yml" ]; then
        COMPOSE_FILE="docker-compose.dev.yml"
        log_info "Найден compose файл: $COMPOSE_FILE"
    elif [ -f "docker-compose.yml" ]; then
        COMPOSE_FILE="docker-compose.yml"
        log_warn "docker-compose.dev.yml не найден, используем docker-compose.yml"
    else
        log_error "Файл docker-compose.yml или docker-compose.dev.yml не найден"
        exit 1
    fi
    
    # Проверяем наличие папки frontend
    if [ ! -d "frontend" ]; then
        log_error "Папка frontend не найдена"
        log_info "Текущая директория: $(pwd)"
        log_info "Содержимое: $(ls -la)"
        exit 1
    fi
    log_info "Папка frontend найдена"
    
    # Проверяем наличие Dockerfile
    if [ ! -f "frontend/Dockerfile" ]; then
        log_error "Dockerfile не найден в папке frontend"
        log_info "Содержимое frontend:"
        ls -la frontend/
        exit 1
    fi
    log_info "Dockerfile в frontend найден"
}

# Создание Docker сети (СИНХРОНИЗИРОВАННАЯ ВЕРСИЯ)
create_network() {
    local network_name="$1"
    
    log_step "Проверка Docker сети: $network_name"


    # Сначала проверяем существование сети
    if docker network inspect "$network_name" >/dev/null 2>&1; then
        log_info "✅ Сеть $network_name уже существует"
        docker network ls --filter "name=^${network_name}$" --format "table {{.Name}}\t{{.Driver}}\t{{.Scope}}"
        
        # Дополнительная проверка, что сеть действительно работает
        if ! docker network inspect "$network_name" >/dev/null 2>&1; then
            log_error "Сеть $network_name существует, но недоступна"
            return 1
        fi
        return 0
    fi
    
    # Создаем сеть, если её нет
    log_info "Создание сети $network_name..."
    
    # Пробуем создать сеть
    if docker network create "$network_name" --driver bridge 2>/dev/null; then
        log_info "✅ Сеть $network_name успешно создана"
        
        # Проверяем, что сеть создалась и готова к использованию
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
        
        # Показываем существующие сети для диагностики
        log_info "Существующие сети:"
        docker network ls
        return 1
    fi
}

# Остановка и очистка
cleanup_containers() {
    log_step "Очистка старых контейнеров..."
    
    # Проверяем, есть ли контейнеры для остановки
    if $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps --quiet 2>/dev/null | grep -q .; then
        log_info "Остановка существующих контейнеров..."
        $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" down --remove-orphans --volumes 2>/dev/null || {
            log_warn "Не удалось полностью остановить контейнеры"
            # Принудительная остановка
            $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" kill 2>/dev/null || true
            $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" rm -f 2>/dev/null || true
        }
        log_info "Контейнеры остановлены и удалены"
    else
        log_info "Нет запущенных контейнеров"
    fi
    
    # Очистка неиспользуемых ресурсов (опционально)
    if [ "${CLEAN_IMAGES:-false}" = "true" ]; then
        log_info "Очистка неиспользуемых образов..."
        docker image prune -f 2>/dev/null || true
        
        log_info "Очистка неиспользуемых томов..."
        docker volume prune -f 2>/dev/null || true
    fi
}

# Запуск контейнеров
start_containers() {
    log_step "Запуск контейнеров..."
    
    # Проверяем существование сети перед запуском
    if ! docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
        log_error "Сеть $NETWORK_NAME не существует! Запуск невозможен."
        return 1
    fi
    
    # Пробуем собрать и запустить
    if $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" up -d --build --force-recreate --remove-orphans; then
        log_info "Контейнеры успешно запущены"
    else
        log_error "Не удалось запустить контейнеры"
        
        # Выводим логи для диагностики
        log_info "Логи ошибок:"
        $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" logs --tail=50
        return 1
    fi
    
    # Ожидание запуска с проверкой
    local wait_time="${WAIT_TIME:-15}"
    log_info "Ожидание запуска контейнеров (${wait_time} секунд)..."
    
    local i=0
    while [ $i -lt $wait_time ]; do
        # Проверяем, что контейнеры не упали
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
    
    # Вывод статуса
    echo ""
    $DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps
    echo ""
    
    # Получаем список контейнеров
    local containers=$($DOCKER_COMPOSE_CMD -f "$COMPOSE_FILE" ps -q 2>/dev/null | wc -l)
    
    if [ "$containers" -eq 0 ]; then
        log_error "Нет запущенных контейнеров"
        return 1
    fi
    
    # Проверяем статус каждого контейнера
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
    
    # Проверка Docker демона
    check_docker_daemon
    
    # Проверка зависимостей
    check_dependencies
    
    # Проверка файлов
    check_files
    
    # Получаем имя сети из переменной или используем по умолчанию
    NETWORK_NAME="${NETWORK_NAME:-app-network}"
    log_info "Имя сети: $NETWORK_NAME"
    
    # КРИТИЧЕСКИ ВАЖНО: Создаем сеть ДО вызова Docker Compose
    if ! create_network "$NETWORK_NAME"; then
        log_error "Не удалось создать/проверить сеть $NETWORK_NAME"
        exit 1
    fi
    
    # Экспортируем переменную для Docker Compose
    export NETWORK_NAME="$NETWORK_NAME"
    
    # Проверяем, что compose файл использует внешнюю сеть
    if grep -q "external:" "$COMPOSE_FILE" && ! grep -q "external: false" "$COMPOSE_FILE"; then
        log_info "Compose файл использует внешнюю сеть: $NETWORK_NAME"
        log_info "Убеждаемся, что сеть существует..."
        
        # Дополнительная проверка перед запуском
        if ! docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
            log_error "Сеть $NETWORK_NAME не найдена, хотя compose файл ожидает её"
            exit 1
        fi
    fi
    
    # Очистка старых контейнеров
    cleanup_containers
    
    # Запуск контейнеров
    if ! start_containers; then
        log_error "❌ Деплой завершился с ошибками при запуске контейнеров"
        exit 1
    fi
    
    # Проверка статуса
    if ! check_status; then
        log_error "❌ Деплой завершился с ошибками при проверке статуса"
        exit 1
    fi
    
    # Вывод информации о сети
    show_network_info "$NETWORK_NAME"
    
    log_info "✅ Деплой успешно завершен!"
    log_info "Время завершения: $(date)"
    
    # Вывод полезной информации
    log_info "Полезные команды:"
    echo "  docker-compose -f $COMPOSE_FILE logs -f    # Просмотр логов"
    echo "  docker-compose -f $COMPOSE_FILE ps         # Статус контейнеров"
    echo "  docker network inspect $NETWORK_NAME       # Информация о сети"
}

# Запуск основной функции
main "$@"