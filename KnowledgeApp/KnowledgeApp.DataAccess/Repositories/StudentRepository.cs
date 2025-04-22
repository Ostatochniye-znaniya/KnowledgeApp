using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class StudentRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public StudentRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<StudentModel> CreateStudent(StudentModel studentModel)
        {
            var groupid = await _context.Students.SingleOrDefaultAsync(f => f.GroupId == studentModel.GroupId);
            var userid = await _context.Students.SingleOrDefaultAsync(f => f.UserId == studentModel.UserId);
            if (groupid == null || userid == null) throw new Exception("Студента с таким groupid или userid не существует");
            var studentEntity = new Student
            {
                UserId = studentModel.UserId,
                Id = studentModel.Id,
                GroupId = studentModel.GroupId
            };

            await _context.Students.AddAsync(studentEntity);
            await _context.SaveChangesAsync();

            StudentModel createdStudent = new StudentModel(studentEntity.Id, studentEntity.UserId, studentEntity.GroupId);
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
                        studentEntity.UserId,
                        studentEntity.GroupId);

                    return studentModel;
                })
                .ToList();

            return students;
        }
        public async Task<StudentModel> GetStudentById(int studentId)
        {
            var studentEntity = await _context.Students.SingleOrDefaultAsync(d => d.Id == studentId);
            if (studentEntity == null) throw new Exception("Student с таким id не существует");
            StudentModel student = new StudentModel(studentEntity.Id, studentEntity.UserId, studentEntity.GroupId);
            return student;
        }
        public async Task<StudentModel> UpdateStudent(StudentModel studentModel)
        {
            var studentEntity = await _context.Students.SingleOrDefaultAsync(d => d.Id == studentModel.Id);
            if (studentEntity == null) throw new Exception("Student с таким id не существует");

            var student_groupEntity = await _context.StudyGroups.SingleOrDefaultAsync(f => f.Id == studentModel.GroupId);
            if (student_groupEntity == null) throw new Exception("Группы с таким id не существует");

            studentEntity.UserId = studentModel.UserId;
            studentEntity.GroupId = studentEntity.GroupId;
            _context.SaveChanges();
            StudentModel student = new StudentModel(studentEntity.Id, studentEntity.UserId, studentEntity.GroupId);
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
}
