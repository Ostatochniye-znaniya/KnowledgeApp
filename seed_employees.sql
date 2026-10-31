SET NAMES utf8mb4;

INSERT INTO knowledge_db.users (id, name, email, password) VALUES
(2, 'Смирнова Елена Викторовна', 'e.smirnova@mospolytech.ru', ''),
(3, 'Кузнецов Андрей Сергеевич', 'a.kuznetsov@mospolytech.ru', ''),
(4, 'Васильева Ольга Николаевна', 'o.vasilieva@mospolytech.ru', ''),
(5, 'Попов Сергей Владимирович', 's.popov@mospolytech.ru', ''),
(6, 'Морозова Анна Михайловна', 'a.morozova@mospolytech.ru', ''),
(7, 'Новиков Игорь Павлович', 'i.novikov@mospolytech.ru', ''),
(8, 'Федорова Татьяна Александровна', 't.fedorova@mospolytech.ru', ''),
(9, 'Соколов Максим Дмитриевич', 'm.sokolov@mospolytech.ru', ''),
(10, 'Ковалева Наталья Юрьевна', 'n.kovaleva@mospolytech.ru', ''),
(11, 'Михайлов Артем Константинович', 'a.mikhaylov@mospolytech.ru', ''),
(12, 'Лебедева Светлана Игоревна', 's.lebedeva@mospolytech.ru', '')
ON DUPLICATE KEY UPDATE name=VALUES(name);

INSERT INTO knowledge_db.user_role (user_id, role_id) VALUES
(2, 2),
(3, 3),
(4, 4),
(5, 5),
(6, 6),
(7, 4),
(8, 3),
(9, 7),
(10, 4),
(11, 6),
(12, 7);
