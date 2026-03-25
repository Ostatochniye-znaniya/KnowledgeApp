-- init.sql
-- Удаляем существующую запись, если есть
DELETE FROM mysql.user WHERE User='root' AND Host='%';
-- Добавляем новую
CREATE USER 'root'@'%' IDENTIFIED BY 'admin';
-- Даём права
GRANT ALL PRIVILEGES ON *.* TO 'root'@'%' WITH GRANT OPTION;
FLUSH PRIVILEGES;