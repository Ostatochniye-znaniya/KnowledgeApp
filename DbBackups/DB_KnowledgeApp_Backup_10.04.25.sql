-- MySQL Workbench Forward Engineering

SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0;
SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0;
SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='ONLY_FULL_GROUP_BY,STRICT_TRANS_TABLES,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION';

-- -----------------------------------------------------
-- Schema mydb
-- -----------------------------------------------------
-- -----------------------------------------------------
-- Schema knowledge_test_db
-- -----------------------------------------------------

-- -----------------------------------------------------
-- Schema knowledge_test_db
-- -----------------------------------------------------
CREATE SCHEMA IF NOT EXISTS `knowledge_test_db` DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci ;
USE `knowledge_test_db` ;

-- -----------------------------------------------------
-- Table `knowledge_test_db`.`faculties`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`faculties` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `faculty_name` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`departments`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`departments` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(255) NOT NULL,
  `faculty_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `faculty_id` (`faculty_id` ASC) VISIBLE,
  CONSTRAINT `departments_ibfk_1`
    FOREIGN KEY (`faculty_id`)
    REFERENCES `knowledge_test_db`.`faculties` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 11
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`disciplines`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`disciplines` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(255) NOT NULL,
  `department_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `department_id` (`department_id` ASC) VISIBLE,
  CONSTRAINT `disciplines_ibfk_1`
    FOREIGN KEY (`department_id`)
    REFERENCES `knowledge_test_db`.`departments` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 11
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`statuses`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`statuses` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `status_name` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 5
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`users`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`users` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(255) NOT NULL,
  `email` VARCHAR(255) NOT NULL,
  `password` VARCHAR(255) NOT NULL,
  `status_id` INT NULL DEFAULT NULL,
  `faculty_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  UNIQUE INDEX `email` (`email` ASC) VISIBLE,
  INDEX `status_id` (`status_id` ASC) VISIBLE,
  INDEX `faculty_id` (`faculty_id` ASC) VISIBLE,
  CONSTRAINT `users_ibfk_1`
    FOREIGN KEY (`status_id`)
    REFERENCES `knowledge_test_db`.`statuses` (`id`),
  CONSTRAINT `users_ibfk_2`
    FOREIGN KEY (`faculty_id`)
    REFERENCES `knowledge_test_db`.`faculties` (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 11
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`discipline_teacher`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`discipline_teacher` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `discipline_id` INT NULL DEFAULT NULL,
  `responsible_teacher_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `discipline_id` (`discipline_id` ASC) VISIBLE,
  INDEX `responsible_teacher_id` (`responsible_teacher_id` ASC) VISIBLE,
  CONSTRAINT `discipline_teacher_ibfk_1`
    FOREIGN KEY (`discipline_id`)
    REFERENCES `knowledge_test_db`.`disciplines` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `discipline_teacher_ibfk_2`
    FOREIGN KEY (`responsible_teacher_id`)
    REFERENCES `knowledge_test_db`.`users` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`semesters`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`semesters` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `semester_name` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`reports`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`reports` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `discipline_id` INT NULL DEFAULT NULL,
  `teacher_id` INT NULL DEFAULT NULL,
  `file_path` VARCHAR(255) NULL DEFAULT NULL,
  `is_correct` TINYINT(1) NULL DEFAULT NULL,
  `done_in_paper_form` TINYINT(1) NULL DEFAULT NULL,
  `done_in_electronic_form` TINYINT(1) NULL DEFAULT NULL,
  `all_done` TINYINT(1) NULL DEFAULT NULL,
  `semester_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `discipline_id` (`discipline_id` ASC) VISIBLE,
  INDEX `teacher_id` (`teacher_id` ASC) VISIBLE,
  INDEX `semester_id` (`semester_id` ASC) VISIBLE,
  CONSTRAINT `reports_ibfk_1`
    FOREIGN KEY (`discipline_id`)
    REFERENCES `knowledge_test_db`.`disciplines` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `reports_ibfk_2`
    FOREIGN KEY (`teacher_id`)
    REFERENCES `knowledge_test_db`.`users` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `reports_ibfk_3`
    FOREIGN KEY (`semester_id`)
    REFERENCES `knowledge_test_db`.`semesters` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`roles`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`roles` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `role_name` VARCHAR(255) NOT NULL,
  PRIMARY KEY (`id`))
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`study_programs`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`study_programs` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `name` VARCHAR(255) NOT NULL,
  `department_id` INT NULL DEFAULT NULL,
  `cypher_of_the_direction` VARCHAR(255) NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `department_id` (`department_id` ASC) VISIBLE,
  CONSTRAINT `study_programs_ibfk_1`
    FOREIGN KEY (`department_id`)
    REFERENCES `knowledge_test_db`.`departments` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 8
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`study_groups`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`study_groups` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `group_number` VARCHAR(50) NOT NULL,
  `study_program_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `study_program_id` (`study_program_id` ASC) VISIBLE,
  CONSTRAINT `study_groups_ibfk_1`
    FOREIGN KEY (`study_program_id`)
    REFERENCES `knowledge_test_db`.`study_programs` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 6
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`testing`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`testing` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `group_id` INT NULL DEFAULT NULL,
  `discipline_id` INT NULL DEFAULT NULL,
  `scheduled_date` DATE NULL DEFAULT NULL,
  `scheduled_time` TIME NULL DEFAULT NULL,
  `status` VARCHAR(50) NULL DEFAULT NULL,
  `result_of_testing` TEXT NULL DEFAULT NULL,
  `report_id` INT NULL DEFAULT NULL,
  `semester_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `group_id` (`group_id` ASC) VISIBLE,
  INDEX `discipline_id` (`discipline_id` ASC) VISIBLE,
  INDEX `report_id` (`report_id` ASC) VISIBLE,
  INDEX `semester_id` (`semester_id` ASC) VISIBLE,
  CONSTRAINT `testing_ibfk_1`
    FOREIGN KEY (`group_id`)
    REFERENCES `knowledge_test_db`.`study_groups` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `testing_ibfk_2`
    FOREIGN KEY (`discipline_id`)
    REFERENCES `knowledge_test_db`.`disciplines` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `testing_ibfk_3`
    FOREIGN KEY (`report_id`)
    REFERENCES `knowledge_test_db`.`reports` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `testing_ibfk_4`
    FOREIGN KEY (`semester_id`)
    REFERENCES `knowledge_test_db`.`semesters` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 3
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


-- -----------------------------------------------------
-- Table `knowledge_test_db`.`user_role`
-- -----------------------------------------------------
CREATE TABLE IF NOT EXISTS `knowledge_test_db`.`user_role` (
  `id` INT NOT NULL AUTO_INCREMENT,
  `user_id` INT NULL DEFAULT NULL,
  `role_id` INT NULL DEFAULT NULL,
  PRIMARY KEY (`id`),
  INDEX `user_id` (`user_id` ASC) VISIBLE,
  INDEX `role_id` (`role_id` ASC) VISIBLE,
  CONSTRAINT `user_role_ibfk_1`
    FOREIGN KEY (`user_id`)
    REFERENCES `knowledge_test_db`.`users` (`id`)
    ON DELETE CASCADE,
  CONSTRAINT `user_role_ibfk_2`
    FOREIGN KEY (`role_id`)
    REFERENCES `knowledge_test_db`.`roles` (`id`)
    ON DELETE CASCADE)
ENGINE = InnoDB
AUTO_INCREMENT = 12
DEFAULT CHARACTER SET = utf8mb4
COLLATE = utf8mb4_0900_ai_ci;


SET SQL_MODE=@OLD_SQL_MODE;
SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS;
SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS;
