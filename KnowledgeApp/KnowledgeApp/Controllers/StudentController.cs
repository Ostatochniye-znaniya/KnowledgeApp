using Microsoft.AspNetCore.Mvc;
using KnowledgeApp.Application.Services;
using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class StudentController : ControllerBase
    {
        private readonly StudentService _studentService;
        public StudentController(StudentService studentService)
        {
            _studentService = studentService;
        }

        [HttpGet]
        public async Task<IResult> GetStudents()
        {
            try
            {
                List<StudentModel> students = await _studentService.GetAll();
                return Results.Json(students);

            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpGet]
        public async Task<IResult> GetStudentById(int studentId)
        {
            try
            {
                StudentModel student = await _studentService.GetStudentById(studentId);
                return Results.Json(student);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPost]
        public async Task<IResult> CreateStudent(StudentRequest studentRequest)
        {
            try
            {
                var newStudentModel = new StudentModel(studentRequest.UserId, studentRequest.GroupId);
                StudentModel newStudentId = await _studentService.CreateStudent(newStudentModel);
                return Results.Json(newStudentId);

            } catch(Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpPut]
        public async Task<IResult> UpdateStudent(int studentId, StudentRequest studentRequest)
        {
            try
            {
                var updatedStudentModel = new StudentModel(studentId, studentRequest.UserId, studentRequest.GroupId);
                var updatedStudent = await _studentService.UpdateStudent(updatedStudentModel);
                return Results.Json(updatedStudent);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }

        [HttpDelete]
        public async Task<IResult> DeleteStudent(int studentId)
        {
            try
            {
                var result = await _studentService.DeleteStudent(studentId);
                return Results.Json(result);
            }
            catch (Exception e)
            {
                return Results.Problem(e.Message);
            }
        }
    }
}
