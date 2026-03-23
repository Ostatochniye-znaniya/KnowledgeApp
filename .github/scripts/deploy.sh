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
        "${!server_user}@${!server_host}" << EOF
            set -e
            
            # Создаем директорию проекта если её нет
            mkdir -p "$dir_value"
            
            # Копируем файлы из временной директории
            cp -r "$temp_dir"/* "$dir_value/"
            
            # Переходим в директорию проекта
            cd "$dir_value"
            
            # Бэкап существующего .env файла
            if [ -f ".env" ] && [ ! -f ".env.backup" ]; then
                cp .env .env.backup.\$(date +%Y%m%d_%H%M%S)
                log_info "Создан бэкап .env файла"
            fi
            
            # Восстанавливаем .env если он был скопирован
            if [ -f "$temp_dir/.env" ]; then
                mv "$temp_dir/.env" .env
            fi
            
            # Останавливаем старые контейнеры
            log_info "Остановка старых контейнеров..."
            docker compose $compose_value down || true
            
            # Загрузка свежих образов
            log_info "Загрузка образов..."
            docker compose $compose_value pull
            
            # Сборка и запуск
            log_info "Сборка и запуск контейнеров..."
            docker network create app-network 2>/dev/null || true  # || true игнорирует ошибку, если сеть уже существует
            docker compose $compose_value up -d --build
            
            # Ожидание готовности контейнеров
            sleep 10
            
            # Проверка статуса
            log_info "Статус контейнеров:"
            docker compose $compose_value ps
            
            # Проверка здоровья (если есть healthcheck)
            if docker compose $compose_value ps | grep -q "unhealthy"; then
                log_error "Обнаружены нездоровые контейнеры!"
                docker compose $compose_value logs --tail=50
                exit 1
            fi
            
            # Очистка старых образов
            log_info "Очистка старых образов..."
            docker image prune -f
            
            # Удаляем временную директорию
            rm -rf "$temp_dir"
            
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