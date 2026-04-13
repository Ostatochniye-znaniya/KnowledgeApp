using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class StudentRepository
{
    private readonly KnowledgeTestDbContext _context;

    public StudentRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<StudentModel> CreateStudent(StudentModel studentModel)
    {
        var group = await _context.StudyGroups.SingleOrDefaultAsync(f => f.Id == studentModel.GroupId);
        if (group == null) throw new Exception("Группы с таким id не существует");
        
        var studentEntity = new Student
        {
            Name = studentModel.Name,
            Year = studentModel.Year,
            GroupId = studentModel.GroupId,
            Status = studentModel.Status
        };

        await _context.Students.AddAsync(studentEntity);
        await _context.SaveChangesAsync();

        StudentModel createdStudent = new StudentModel(studentEntity.Id, studentEntity.Name, studentEntity.Year, studentEntity.GroupId, studentEntity.Status);
        return createdStudent;
    }
    public async Task<List<StudentModel>> GetAllStudents()
    {
        //достаем данные из бд
        var studentEntities = await _context.Students
            .AsNoTracking()
            .ToListAsync();

        // преобразуем entities в models
        var students = studentEntities
            .Select(studentEntity =>
            {
                var studentModel = new StudentModel(
                    studentEntity.Id,
                    studentEntity.Name,
                    studentEntity.Year,
                    studentEntity.GroupId,
                    studentEntity.Status);

                return studentModel;
            })
            .ToList();

        return students;
    }
    public async Task<StudentModel> GetStudentById(int studentId)
    {
        var studentEntity = await _context.Students.SingleOrDefaultAsync(d => d.Id == studentId);
        if (studentEntity == null) throw new Exception("Student с таким id не существует");
        StudentModel student = new StudentModel(studentEntity.Id, studentEntity.Name, studentEntity.Year, studentEntity.GroupId, studentEntity.Status);
        return student;
    }
    public async Task<StudentModel> UpdateStudent(StudentModel studentModel)
    {
        var studentEntity = await _context.Students.SingleOrDefaultAsync(d => d.Id == studentModel.Id);
        if (studentEntity == null) throw new Exception("Student с таким id не существует");

        var student_groupEntity = await _context.StudyGroups.SingleOrDefaultAsync(f => f.Id == studentModel.GroupId);
        if (student_groupEntity == null) throw new Exception("Группы с таким id не существует");

        studentEntity.Name = studentModel.Name;
        studentEntity.Year = studentModel.Year;
        studentEntity.GroupId = studentModel.GroupId;
        studentEntity.Status = studentModel.Status;
        
        await _context.SaveChangesAsync();
        StudentModel student = new StudentModel(studentEntity.Id, studentEntity.Name, studentEntity.Year, studentEntity.GroupId, studentEntity.Status);
        return student;
    }
    public async Task<bool> DeleteStudent(int studentId)
    {
        var studentEntity = await _context.Students.SingleOrDefaultAsync(d => d.Id == studentId);
        if (studentEntity == null) throw new Exception("Student с таким id не существует");
        _context.Remove(studentEntity);
        _context.SaveChanges();

        var student = await _context.Students.SingleOrDefaultAsync(d => d.Id == studentId);
        if (student == null) return true;
        else return false;
    }
}