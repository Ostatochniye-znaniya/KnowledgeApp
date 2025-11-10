using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.DataAccess.Repositories;

namespace KnowledgeApp.Application.Services
{
    public class UserService
    {
        private readonly UserRepository _userRepository;

        public UserService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<IEnumerable<User>> GetAll()
        {
            return await _userRepository.GetAllAsync();
        }

        public async Task<User?> GetByIdAsync(int id)
		{
			return await _userRepository.GetByIdAsync(id);
        }

        public async Task<UserModel> CreateAsync(UserModel model)
        {
            var entity = new User
            {
                Name = model.Name,
                Email = model.Email,
                Password = model.Password,
                StatusId = model.StatusId,
                FacultyId = model.FacultyId
            };

            await _userRepository.AddAsync(entity);
            await _userRepository.SaveChangesAsync();

            model.Id = entity.Id;
            return model;
        }

        public async Task<bool> UpdateAsync(int id, UserModel model)
        {
            var existing = await _userRepository.GetByIdAsync(id);
            if (existing == null) return false;

            existing.Name = model.Name;
            existing.Email = model.Email;
            existing.Password = model.Password;
            existing.StatusId = model.StatusId;
            existing.FacultyId = model.FacultyId;

            _userRepository.Update(existing);
            await _userRepository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var existing = await _userRepository.GetByIdAsync(id);
            if (existing == null) return false;

            _userRepository.Delete(existing);
            await _userRepository.SaveChangesAsync();

            return true;
        }
    }
}
