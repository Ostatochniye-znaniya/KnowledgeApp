using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class EmployeeRightsRequestRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public EmployeeRightsRequestRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<List<EmployeeRightsRequestModel>> GetAll(int page = 1, int pageSize = 50)
        {
            return await _context.EmployeeRightsRequests.Skip((page-1)* pageSize).Take(pageSize).Select(r=>
                new EmployeeRightsRequestModel()
                {
                    Id = r.Id,
                    StructuralDivision = r.StructuralDivision,
                    CategoryName = r.CategoryName,
                    FullName = r.FullName,
                    IsActive = r.IsActive,
                    JobEnd = r.JobEnd,
                    JobStart = r.JobStart,
                    JobName = r.JobName,
                    UpdatedAt = r.UpdatedAt,
                }
                ).ToListAsync();
        }
        public async Task<EmployeeRightsRequestModel> GetById(int id)
        {
            var r = await _context.EmployeeRightsRequests.FirstOrDefaultAsync(r=>r.Id == id);
            return new EmployeeRightsRequestModel()
            {
                Id = r.Id,
                StructuralDivision = r.StructuralDivision,
                CategoryName = r.CategoryName,
                FullName = r.FullName,
                IsActive = r.IsActive,
                JobEnd = r.JobEnd,
                JobStart = r.JobStart,
                JobName = r.JobName,
                UpdatedAt = r.UpdatedAt,
            };
        }
        public async Task DeleteById(int id)
        {
            var req = await _context.EmployeeRightsRequests.FirstOrDefaultAsync(r => r.Id == id);
            if (req == null) throw new Exception("Запроса с таким id не существует");
            _context.EmployeeRightsRequests.Remove(req);
            await _context.SaveChangesAsync();
        }
        public async Task AddEmployee(EmployeeRightsRequestModel emp)
        {
            await _context.EmployeeRightsRequests.AddAsync(new EmployeeRightsRequest()
            {
                JobEnd = emp.JobEnd,
                JobStart = emp.JobStart,
                JobName = emp.JobName,
                IsActive = emp.IsActive,
                Id = emp.Id,
                StructuralDivision = emp.StructuralDivision,
                CategoryName= emp.CategoryName,
                FullName = emp.FullName,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt= DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
        
        public async Task UpdateEmplpyee(EmployeeRightsRequestModel emp)
        {
            var req = await _context.EmployeeRightsRequests.FirstOrDefaultAsync(r => r.Id == emp.Id);
            if (req == null) throw new Exception("Запроса с таким id не существует");
            emp.JobEnd = req.JobEnd;
            emp.JobStart = req.JobStart;
            emp.JobName = req.JobName;
            emp.IsActive = req.IsActive;
            emp.StructuralDivision = req.StructuralDivision;
            emp.CategoryName = req.CategoryName;
            emp.FullName = req.FullName;
            emp.UpdatedAt = req.UpdatedAt;
            await _context.SaveChangesAsync();
        }
    }
}
