using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Data;

public static class SampleDataSeeder
{
    public static async Task SeedAsync(KnowledgeTestDbContext context)
    {
        if (await context.Faculties.AnyAsync())
        {
            return;
        }

        var faculties = new[]
        {
            new Faculty { FacultyName = "Факультет информатики" },
            new Faculty { FacultyName = "Факультет экономики и управления" }
        };

        context.Faculties.AddRange(faculties);
        await context.SaveChangesAsync();

        var departments = new[]
        {
            new Department { Name = "Кафедра программирования", FacultyId = faculties[0].Id },
            new Department { Name = "Кафедра информационных систем", FacultyId = faculties[0].Id },
            new Department { Name = "Кафедра менеджмента", FacultyId = faculties[1].Id }
        };

        context.Departments.AddRange(departments);
        await context.SaveChangesAsync();

        var programs = new[]
        {
            new StudyProgram { Name = "09.03.01 Информатика и вычислительная техника", DepartmentId = departments[0].Id, CypherOfTheDirection = "09.03.01" },
            new StudyProgram { Name = "09.03.02 Информационные системы и технологии", DepartmentId = departments[1].Id, CypherOfTheDirection = "09.03.02" },
            new StudyProgram { Name = "38.03.02 Менеджмент", DepartmentId = departments[2].Id, CypherOfTheDirection = "38.03.02" }
        };

        context.StudyPrograms.AddRange(programs);
        await context.SaveChangesAsync();

        var users = new[]
        {
            new User { Name = "Иванов Алексей Петрович", Email = "ivanov@edu.local", Password = "123456", FacultyId = faculties[0].Id },
            new User { Name = "Петрова Ольга Николаевна", Email = "petrova@edu.local", Password = "123456", FacultyId = faculties[0].Id },
            new User { Name = "Сидоров Сергей Владимирович", Email = "sidorov@edu.local", Password = "123456", FacultyId = faculties[1].Id },
            new User { Name = "Кузнецова Дарья Игоревна", Email = "kuznetsova@edu.local", Password = "123456", FacultyId = faculties[1].Id },
            new User { Name = "Морозов Дмитрий Сергеевич", Email = "morozov@edu.local", Password = "123456", FacultyId = faculties[0].Id }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();

        var groups = new[]
        {
            new StudyGroup { GroupNumber = "ИВТ-101", StudyProgramId = programs[0].Id },
            new StudyGroup { GroupNumber = "ИСТ-202", StudyProgramId = programs[1].Id },
            new StudyGroup { GroupNumber = "МН-303", StudyProgramId = programs[2].Id }
        };

        context.StudyGroups.AddRange(groups);
        await context.SaveChangesAsync();

        var disciplines = new[]
        {
            new Discipline { Name = "Основы программирования", DepartmentId = departments[0].Id },
            new Discipline { Name = "Базы данных", DepartmentId = departments[1].Id },
            new Discipline { Name = "Управление проектами", DepartmentId = departments[2].Id }
        };

        context.Disciplines.AddRange(disciplines);
        await context.SaveChangesAsync();

        var disciplineTeachers = new[]
        {
            new DisciplineTeacher { DisciplineId = disciplines[0].Id, ResponsibleTeacherId = users[0].Id },
            new DisciplineTeacher { DisciplineId = disciplines[1].Id, ResponsibleTeacherId = users[1].Id },
            new DisciplineTeacher { DisciplineId = disciplines[2].Id, ResponsibleTeacherId = users[2].Id }
        };

        context.DisciplineTeachers.AddRange(disciplineTeachers);
        await context.SaveChangesAsync();

        var reports = new[]
        {
            new Report { DisciplineId = disciplines[0].Id, TeacherId = users[0].Id, AllDone = true, IsCorrect = true, ResultOfAttestation = "Допущен" },
            new Report { DisciplineId = disciplines[1].Id, TeacherId = users[1].Id, AllDone = true, IsCorrect = true, ResultOfAttestation = "Допущен" },
            new Report { DisciplineId = disciplines[2].Id, TeacherId = users[2].Id, AllDone = true, IsCorrect = true, ResultOfAttestation = "Допущен" }
        };

        context.Reports.AddRange(reports);
        await context.SaveChangesAsync();

        var semesters = new[]
        {
            new Semester { SemesterYear = 2025, SemesterPart = 1 },
            new Semester { SemesterYear = 2025, SemesterPart = 2 }
        };

        context.Semesters.AddRange(semesters);
        await context.SaveChangesAsync();

        var testings = new[]
        {
            new Testing
            {
                GroupId = groups[0].Id,
                DisciplineId = disciplines[0].Id,
                SemesterId = semesters[0].Id,
                ScheduledDate = new DateTime(2025, 03, 10),
                ScheduledTime = new TimeSpan(9, 0, 0),
                Status = "Scheduled",
                ResultOfTesting = "Ожидает",
                ReportId = reports[0].Id
            },
            new Testing
            {
                GroupId = groups[1].Id,
                DisciplineId = disciplines[1].Id,
                SemesterId = semesters[0].Id,
                ScheduledDate = new DateTime(2025, 03, 12),
                ScheduledTime = new TimeSpan(11, 30, 0),
                Status = "Scheduled",
                ResultOfTesting = "Ожидает",
                ReportId = reports[1].Id
            },
            new Testing
            {
                GroupId = groups[2].Id,
                DisciplineId = disciplines[2].Id,
                SemesterId = semesters[1].Id,
                ScheduledDate = new DateTime(2025, 09, 02),
                ScheduledTime = new TimeSpan(13, 0, 0),
                Status = "Scheduled",
                ResultOfTesting = "Ожидает",
                ReportId = reports[2].Id
            }
        };

        context.Testings.AddRange(testings);
        await context.SaveChangesAsync();

        var rightsRequests = new[]
        {
            new EmployeeRightsRequest
            {
                FullName = "Иванов Алексей Петрович",
                StructuralDivision = "Кафедра программирования",
                JobName = "Заведующий кафедрой",
                JobStart = new DateOnly(2024, 01, 01),
                JobEnd = new DateOnly(2026, 12, 31),
                IsActive = true,
                CategoryName = "Подписывающее лицо",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                UserId = users[0].Id
            },
            new EmployeeRightsRequest
            {
                FullName = "Петрова Ольга Николаевна",
                StructuralDivision = "Кафедра информационных систем",
                JobName = "Заместитель заведующего",
                JobStart = new DateOnly(2023, 09, 01),
                JobEnd = new DateOnly(2027, 08, 31),
                IsActive = true,
                CategoryName = "Подписывающее лицо",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                UserId = users[1].Id
            },
            new EmployeeRightsRequest
            {
                FullName = "Сидоров Сергей Владимирович",
                StructuralDivision = "Кафедра менеджмента",
                JobName = "Деканат",
                JobStart = new DateOnly(2022, 02, 01),
                JobEnd = new DateOnly(2026, 02, 28),
                IsActive = true,
                CategoryName = "Подписывающее лицо",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                UserId = users[2].Id
            }
        };

        context.EmployeeRightsRequests.AddRange(rightsRequests);
        await context.SaveChangesAsync();
    }
}
