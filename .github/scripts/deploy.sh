#!/bin/bash


set -e  # Выход при ошибке

# Цвета для вывода
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m' # No Color

# Функция для логирования
log_info() {
    echo -e "${GREEN}[INFO]${NC} $1"
}

log_warn() {
    echo -e "${YELLOW}[WARN]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

# Функция проверки переменных
check_vars() {
    local vars=("$@")
    for var in "${vars[@]}"; do
        if [ -z "${!var}" ]; then
            log_error "Переменная $var не установлена"
            exit 1
        fi
    done
}

# Функция создания .env файла из secrets
create_env_file() {
    local env_file="$1"
    local env_prefix="$2"
    
    log_info "Создание .env файла..."
    
    # Создаем пустой файл
    > "$env_file"
    
    # Получаем все переменные окружения с префиксом
    env | grep "^${env_prefix}" | while read -r line; do
        # Убираем префикс и записываем в файл
        key=$(echo "$line" | cut -d'=' -f1 | sed "s/^${env_prefix}//")
        value=$(echo "$line" | cut -d'=' -f2-)
        echo "$key=$value" >> "$env_file"
    done
    
    log_info ".env файл создан: $env_file"
}

# Функция деплоя
deploy() {
    local env="$1"
    local project_name="$2"
    
    log_info "=== Начинаем деплой $project_name в окружение $env ==="
    
    # Определяем переменные в зависимости от окружения
    local server_host="${env}_SERVER_HOST"
    local server_user="${env}_SERVER_USER"
    local server_port="${env}_SERVER_PORT"
    local project_dir="${env}_PROJECT_DIR"
    local compose_files="${env}_COMPOSE_FILES"
    
    # Проверяем обязательные переменные
    check_vars "$server_host" "$server_user"
    
    # Устанавливаем значения по умолчанию
    local port_value="${!server_port:-22}"
    local dir_value="${!project_dir:-/opt/$project_name}"
    local compose_value="${!compose_files:--f docker-compose.yml}"
    
    log_info "Сервер: ${!server_user}@${!server_host}:$port_value"
    log_info "Директория: $dir_value"
    log_info "Docker Compose файлы: $compose_value"
    
    # Создаем временную директорию для файлов
    local temp_dir="/tmp/deploy_${project_name}_${env}_$(date +%s)"
    mkdir -p "$temp_dir"
    
    # Копируем необходимые файлы
    log_info "Копирование файлов на сервер..."
    
    # Копируем docker-compose файлы
    scp -P "$port_value" \
        -o StrictHostKeyChecking=no \
        -o UserKnownHostsFile=/dev/null \
        docker-compose*.yml \
        "${!server_user}@${!server_host}:$temp_dir/"
    
    # Если есть .env файл в репозитории
    if [ -f ".env.example" ]; then
        scp -P "$port_value" \
            -o StrictHostKeyChecking=no \
            .env.example \
            "${!server_user}@${!server_host}:$temp_dir/"
    fi
    
    # Создаем и копируем .env файл если есть переменные
    if [ -n "${env}_ENV_PREFIX" ]; then
        local env_prefix="${!env}_ENV_PREFIX"
        create_env_file "$temp_dir/.env" "$env_prefix"
        scp -P "$port_value" \
            -o StrictHostKeyChecking=no \
            "$temp_dir/.env" \
            "${!server_user}@${!server_host}:$temp_dir/"
    fi
    
    # Выполняем деплой на сервере
    log_info "Выполнение команд на сервере..."
    
    ssh -p "$port_value" \
        -o StrictHostKeyChecking=no \
        "${!server_user}@${!server_host}" << 'EOF'
            set -e
            
            # Функция для логирования
            log_info() {
                echo "[INFO] $(date '+%Y-%m-%d %H:%M:%S') - $1"
            }
            
            log_error() {
                echo "[ERROR] $(date '+%Y-%m-%d %H:%M:%S') - $1" >&2
            }
            
            # Переменные (будут подставлены извне)
            DIR_VALUE="{{dir_value}}"
            TEMP_DIR="{{temp_dir}}"
            COMPOSE_VALUE="{{compose_value}}"
            
            # Создаем директорию проекта если её нет
            mkdir -p "$DIR_VALUE"
            
            # Копируем файлы из временной директории
            cp -r "$TEMP_DIR"/* "$DIR_VALUE/"
            
            # Переходим в директорию проекта
            cd "$DIR_VALUE"
            
            # Бэкап существующего .env файла
            if [ -f ".env" ] && [ ! -f ".env.backup" ]; then
                BACKUP_NAME=".env.backup.$(date +%Y%m%d_%H%M%S)"
                cp .env "$BACKUP_NAME"
                log_info "Создан бэкап .env файла: $BACKUP_NAME"
            fi
            
            # Восстанавливаем .env если он был скопирован
            if [ -f "$TEMP_DIR/.env" ]; then
                mv "$TEMP_DIR/.env" .env
                log_info ".env файл восстановлен"
            fi
            
            # ========== ВАЖНО: СЕТЬ СОЗДАЕТСЯ ДО ОСТАНОВКИ КОНТЕЙНЕРОВ ==========
            log_info "Проверка/создание сети app-network..."
            if ! docker network inspect app-network >/dev/null 2>&1; then
                log_info "Создание сети app-network..."
                docker network create app-network
                log_info "Сеть app-network успешно создана"
            else
                log_info "Сеть app-network уже существует"
            fi
            
            # Проверяем существование сети
            docker network ls | grep app-network || {
                log_error "Сеть app-network не найдена!"
                exit 1
            }
            
            # Останавливаем старые контейнеры
            log_info "Остановка старых контейнеров..."
            docker compose $COMPOSE_VALUE down --remove-orphans || true
            
            # Загрузка свежих образов
            log_info "Загрузка образов..."
            docker compose $COMPOSE_VALUE pull || {
                log_error "Ошибка при загрузке образов"
                exit 1
            }
            
            # Сборка и запуск
            log_info "Сборка и запуск контейнеров..."
            docker compose $COMPOSE_VALUE up -d --build || {
                log_error "Ошибка при сборке/запуске контейнеров"
                exit 1
            }
            
            # Ожидание готовности контейнеров
            log_info "Ожидание готовности контейнеров (10 секунд)..."
            sleep 10
            
            # Проверка статуса
            log_info "Статус контейнеров:"
            docker compose $COMPOSE_VALUE ps
            
            # Проверка здоровья (если есть healthcheck)
            if docker compose $COMPOSE_VALUE ps | grep -q "unhealthy"; then
                log_error "Обнаружены нездоровые контейнеры!"
                docker compose $COMPOSE_VALUE logs --tail=50
                exit 1
            fi
            
            # Проверка что все контейнеры запущены
            RUNNING_COUNT=$(docker compose $COMPOSE_VALUE ps --filter "status=running" | grep -c "Up" || true)
            TOTAL_COUNT=$(docker compose $COMPOSE_VALUE ps --format json | grep -c "Name" || true)
            
            if [ "$RUNNING_COUNT" -lt "$TOTAL_COUNT" ]; then
                log_error "Не все контейнеры запущены! Запущено: $RUNNING_COUNT из $TOTAL_COUNT"
                docker compose $COMPOSE_VALUE logs --tail=50
                exit 1
            fi
            
            # Очистка старых образов
            log_info "Очистка старых образов..."
            docker image prune -f
            
            # Удаляем временную директорию
            if [ -d "$TEMP_DIR" ]; then
                rm -rf "$TEMP_DIR"
                log_info "Временная директория удалена: $TEMP_DIR"
            fi
            
            log_info "✅ Деплой успешно завершен!"
EOF
    
    # Проверка результата
    if [ $? -eq 0 ]; then
        log_info "=== Деплой $project_name в $env успешно выполнен ==="
    else
        log_error "=== Ошибка при деплое $project_name в $env ==="
        exit 1
    fi
}

# Функция отката
rollback() {
    local env="$1"
    local project_name="$2"
    
    log_warn "Выполнение отката для $project_name в $env"
    
    local server_host="${env}_SERVER_HOST"
    local server_user="${env}_SERVER_USER"
    local server_port="${env}_SERVER_PORT"
    local project_dir="${env}_PROJECT_DIR"
    local compose_files="${env}_COMPOSE_FILES"
    
    local port_value="${!server_port:-22}"
    local dir_value="${!project_dir:-/opt/$project_name}"
    local compose_value="${!compose_files:--f docker-compose.yml}"
    
    ssh -p "$port_value" \
        -o StrictHostKeyChecking=no \
        "${!server_user}@${!server_host}" << EOF
            cd "$dir_value"
            
            # Откат к предыдущей версии
            if [ -f ".env.backup" ]; then
                mv .env.backup .env
            fi
            
            # Перезапуск с предыдущей версией
            docker compose $compose_value down
            docker compose $compose_value up -d
            
            log_info "Откат выполнен"
EOF
}

# Основная логика
main() {
    local env="${1:-dev}"
    local action="${2:-deploy}"
    local project_name="${3:-${GITHUB_REPOSITORY##*/}}"
    
    case "$action" in
        deploy)
            deploy "$env" "$project_name"
            ;;
        rollback)
            rollback "$env" "$project_name"
            ;;
        *)
            log_error "Неизвестное действие: $action"
            log_info "Использование: $0 [dev|prod] [deploy|rollback] [project_name]"
            exit 1
            ;;
    esac
}

# Запуск
main "$@"