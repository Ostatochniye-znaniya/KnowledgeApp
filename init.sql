-- init.sql
ALTER USER 'root'@'localhost' IDENTIFIED WITH mysql_native_password BY 'admin';
CREATE USER IF NOT EXISTS 'root'@'%' IDENTIFIED WITH mysql_native_password BY 'admin';
GRANT ALL PRIVILEGES ON *.* TO 'root'@'%' WITH GRANT OPTION;
FLUSH PRIVILEGES;