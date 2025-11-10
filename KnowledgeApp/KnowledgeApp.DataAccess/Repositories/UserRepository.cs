using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class UserRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public UserRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetAllAsync()
        {
            return await _context.Set<User>().ToListAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Set<User>().FindAsync(id);
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
}
