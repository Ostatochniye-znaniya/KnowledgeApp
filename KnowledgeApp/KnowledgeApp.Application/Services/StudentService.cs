using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class StudentService
    {
        private readonly StudentRepository _studentRepository;

        public StudentService(StudentRepository studentRepository)
        {
            _studentRepository = studentRepository;
        }

        public async Task<List<StudentModel>> GetAll()
        {
            List<StudentModel> students = await _studentRepository.GetAllStudents();
            return students;
        }

        public async Task<StudentModel> GetStudentById(int studentId)
        {
            StudentModel student = await _studentRepository.GetStudentById(studentId);
            return student;
        }

        public async Task<StudentModel> CreateStudent(StudentModel studentModel)
        {
            StudentModel createdStudentId = await _studentRepository.CreateStudent(studentModel);

            return createdStudentId;
        }

        public async Task<StudentModel> UpdateStudent(StudentModel studentModel)
        {
            StudentModel updatedStudentModel = await _studentRepository.UpdateStudent(studentModel);
            return updatedStudentModel;
        }

        public async Task<bool> DeleteStudent(int studentId)
        {
            bool result = await _studentRepository.DeleteStudent(studentId);
            return result;
        }
    }
}
