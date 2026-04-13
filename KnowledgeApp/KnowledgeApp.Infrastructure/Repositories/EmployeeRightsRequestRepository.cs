using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

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
                UserId = r.UserId,
                StructuralDivision = r.StructuralDivision,
                CategoryName = r.CategoryName,
                FullName = r.FullName,
                IsActive = r.IsActive,
                JobEnd = r.JobEnd,
                JobStart = r.JobStart,
                JobName = r.JobName,
                UpdatedAt = r.UpdatedAt,
                CreatedAt = r.CreatedAt
            }
            ).ToListAsync();
    }
    public async Task<List<EmployeeRightsRequestModel>> GetAllByDivision(string division, int page = 1, int pageSize = 50, int? userId = null)
    {
        var query = _context.EmployeeRightsRequests.Where(e => e.StructuralDivision == division);

        if (userId.HasValue)
        {
            query = query.Where(e => e.UserId == userId.Value);
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new EmployeeRightsRequestModel()
            {
                Id = r.Id,
                UserId = r.UserId,
                StructuralDivision = r.StructuralDivision,
                CategoryName = r.CategoryName,
                FullName = r.FullName,
                IsActive = r.IsActive,
                JobEnd = r.JobEnd,
                JobStart = r.JobStart,
                JobName = r.JobName,
                UpdatedAt = r.UpdatedAt,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<EmployeeRightsRequestModel>> GetAllActive(int page = 1, int pageSize = 50, int? userId = null)
    {
        var query = _context.EmployeeRightsRequests.Where(u => u.IsActive == true);

        if (userId.HasValue)
        {
            query = query.Where(u => u.UserId == userId.Value);
        }

        return await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new EmployeeRightsRequestModel()
            {
                Id = r.Id,
                UserId = r.UserId,
                StructuralDivision = r.StructuralDivision,
                CategoryName = r.CategoryName,
                FullName = r.FullName,
                IsActive = r.IsActive,
                JobEnd = r.JobEnd,
                JobStart = r.JobStart,
                JobName = r.JobName,
                UpdatedAt = r.UpdatedAt,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<List<EmployeeRightsRequestModel>> GetAllByUserId(int userId, int page = 1, int pageSize = 50)
    {
        return await _context.EmployeeRightsRequests.Where(r=> r.UserId == userId).Skip((page - 1) * pageSize).Take(pageSize).Select(r =>
            new EmployeeRightsRequestModel()
            {
                Id = r.Id,
                UserId = r.UserId,
                StructuralDivision = r.StructuralDivision,
                CategoryName = r.CategoryName,
                FullName = r.FullName,
                IsActive = r.IsActive,
                JobEnd = r.JobEnd,
                JobStart = r.JobStart,
                JobName = r.JobName,
                UpdatedAt = r.UpdatedAt,
                CreatedAt = r.CreatedAt
            }
            ).ToListAsync();
    }
    public async Task<EmployeeRightsRequestModel> GetById(int id)
    {
        var r = await _context.EmployeeRightsRequests.FirstOrDefaultAsync(r => r.Id == id);
        if (r == null) throw new Exception("EmployeeRightsRequest с таким id не существует");
        return new EmployeeRightsRequestModel()
        {
            Id = r.Id,
            UserId = r.UserId,
            StructuralDivision = r.StructuralDivision,
            CategoryName = r.CategoryName,
            FullName = r.FullName,
            IsActive = r.IsActive,
            JobEnd = r.JobEnd,
            JobStart = r.JobStart,
            JobName = r.JobName,
            UpdatedAt = r.UpdatedAt,
            CreatedAt = r.CreatedAt
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
            UserId = emp.UserId,
            JobEnd = emp.JobEnd,
            JobStart = emp.JobStart,
            JobName = emp.JobName,
            IsActive = emp.IsActive,
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
        req.UserId = emp.UserId;
        req.JobEnd = emp.JobEnd;
        req.JobStart = emp.JobStart;
        req.JobName = emp.JobName;
        req.IsActive = emp.IsActive;
        req.StructuralDivision = emp.StructuralDivision;
        req.CategoryName = emp.CategoryName;
        req.FullName = emp.FullName;
        req.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }
}
