using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class RoleService
    {
        private readonly RoleRepository _roleRepository;

        public RoleService(RoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }

        public async Task<List<RoleModel>> GetAll()
        {
            List<RoleModel> roles = await _roleRepository.GetAllRoles();
            return roles;
        }

        public async Task<RoleModel> GetRoleById(int roleId)
        {
            RoleModel role = await _roleRepository.GetRoleById(roleId);
            return role;
        }

        public async Task<RoleModel> CreateRole(RoleModel roleModel)
        {
            RoleModel createdRoleId = await _roleRepository.CreateRole(roleModel);

            return createdRoleId;
        }

        public async Task<RoleModel> UpdateRole(RoleModel roleModel)
        {
            RoleModel updatedRoleModel = await _roleRepository.UpdateRole(roleModel);
            return updatedRoleModel;
        }

        public async Task<bool> DeleteRole(int roleId)
        {
            bool result = await _roleRepository.DeleteRole(roleId);
            return result;
        }
    }
}