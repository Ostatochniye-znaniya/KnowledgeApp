using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using KnowledgeApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Repositories;

public class UserRepository
{
    private readonly KnowledgeTestDbContext _context;

    public UserRepository(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<User>> GetAllAsync()
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Faculty)
            .ToListAsync();
    }

    public async Task<bool> SetUserRolesAsync(int userId, List<int> roleIds)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null) return false;

        _context.UserRoles.RemoveRange(user.UserRoles);

        foreach (var roleId in roleIds.Distinct())
        {
            await _context.UserRoles.AddAsync(new UserRole
            {
                UserId = userId,
                RoleId = roleId
            });
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.Faculty)
            .FirstOrDefaultAsync(u => u.Id == id);
    }

    public async Task AddAsync(User user)
    {
        await _context.Set<User>().AddAsync(user);
        await _context.SaveChangesAsync();
    }

    public async Task<User> Create(UserModel model)
    {
        var entity = new User
        {
            Id = model.Id,
            Name = model.Name,
            Email = model.Email,
            Password = model.Password,
            StatusId = model.StatusId,
            FacultyId = model.FacultyId
        };

        await _context.Users.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }
    
    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public void Delete(User user)
    {
        _context.Users.Remove(user);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
