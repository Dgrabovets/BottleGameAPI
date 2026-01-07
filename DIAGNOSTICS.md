# Диагностика проблем с подключением к PostgreSQL

## Проблема
Приложение не может подключиться к PostgreSQL на `45.139.76.236:5432` (Connection refused).

## Что проверить на сервере БД (45.139.76.236)

### 1. Проверить, запущен ли PostgreSQL

```bash
# Проверить статус службы
sudo systemctl status postgresql
# или
sudo systemctl status postgresql@14-main  # для версии 14
# или
sudo service postgresql status

# Если не запущен, запустить:
sudo systemctl start postgresql
# или
sudo service postgresql start
```

### 2. Проверить, слушает ли PostgreSQL на внешнем интерфейсе

```bash
# Проверить, на каких интерфейсах слушает PostgreSQL
sudo netstat -tlnp | grep 5432
# или
sudo ss -tlnp | grep 5432

# Должно быть что-то вроде:
# 0.0.0.0:5432 или :::5432 (слушает на всех интерфейсах)
# Если только 127.0.0.1:5432 - PostgreSQL слушает только локально
```

### 3. Проверить настройки listen_addresses

```bash
# Найти файл postgresql.conf
sudo find /etc -name postgresql.conf
# Обычно находится в: /etc/postgresql/14/main/postgresql.conf

# Открыть файл
sudo nano /etc/postgresql/14/main/postgresql.conf

# Найти строку:
# listen_addresses = 'localhost'

# Изменить на:
listen_addresses = '*'  # или '0.0.0.0'

# Сохранить и перезапустить PostgreSQL
sudo systemctl restart postgresql
```

### 4. Проверить настройки pg_hba.conf (разрешения на подключение)

```bash
# Найти файл pg_hba.conf
sudo find /etc -name pg_hba.conf
# Обычно находится в: /etc/postgresql/14/main/pg_hba.conf

# Открыть файл
sudo nano /etc/postgresql/14/main/pg_hba.conf

# Добавить строку для разрешения подключения с IP вашего приложения:
# host    bottle_game_db    bottle_game_db_user    IP_ВАШЕГО_СЕРВЕРА_ПРИЛОЖЕНИЯ/32    md5

# Или для всех IP (менее безопасно, но для теста):
host    bottle_game_db    bottle_game_db_user    0.0.0.0/0    md5

# Сохранить и перезагрузить конфигурацию PostgreSQL
sudo systemctl reload postgresql
```

### 5. Проверить firewall (брандмауэр)

```bash
# Проверить статус firewall
sudo ufw status
# или
sudo firewall-cmd --list-all  # для firewalld

# Если firewall активен, разрешить порт 5432:
sudo ufw allow 5432/tcp
# или
sudo firewall-cmd --permanent --add-port=5432/tcp
sudo firewall-cmd --reload
```

### 6. Проверить, что пользователь и база данных существуют

```bash
# Подключиться к PostgreSQL локально
sudo -u postgres psql

# Проверить пользователя
\du bottle_game_db_user

# Проверить базу данных
\l bottle_game_db

# Если не существует, создать:
CREATE USER bottle_game_db_user WITH PASSWORD 'ltw5AF7g_9ei5Snb2z8T';
CREATE DATABASE bottle_game_db OWNER bottle_game_db_user;
GRANT ALL PRIVILEGES ON DATABASE bottle_game_db TO bottle_game_db_user;

# Выйти
\q
```

### 7. Проверить подключение с сервера приложения

```bash
# С сервера, где запущено приложение, проверить подключение:
psql -h 45.139.76.236 -p 5432 -U bottle_game_db_user -d bottle_game_db

# Или через telnet (проверить доступность порта):
telnet 45.139.76.236 5432
```

## Быстрая проверка всех пунктов одной командой

```bash
# На сервере БД выполнить:
echo "=== Статус PostgreSQL ==="
sudo systemctl status postgresql | head -5

echo "=== Слушает ли на порту 5432 ==="
sudo netstat -tlnp | grep 5432

echo "=== Настройки listen_addresses ==="
sudo grep listen_addresses /etc/postgresql/*/main/postgresql.conf

echo "=== Правила pg_hba.conf ==="
sudo grep -v "^#" /etc/postgresql/*/main/pg_hba.conf | grep -v "^$"

echo "=== Статус firewall ==="
sudo ufw status 2>/dev/null || sudo firewall-cmd --list-all 2>/dev/null || echo "Firewall не найден"
```

## После исправления

1. Перезапустить PostgreSQL:
```bash
sudo systemctl restart postgresql
```

2. Проверить логи приложения - должно исчезнуть сообщение "Connection refused"

3. Проверить подключение с сервера приложения

## Дополнительная информация

- **Connection refused** означает, что порт закрыт или сервис не слушает на этом интерфейсе
- **Connection timeout** означает, что порт заблокирован firewall
- **Authentication failed** означает, что подключение установлено, но неверные учетные данные

