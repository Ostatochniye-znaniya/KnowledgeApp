using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class RoleRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public RoleRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<RoleModel> CreateRole(RoleModel roleModel)
        {
            var rolename = await _context.Roles.SingleOrDefaultAsync(f => f.RoleName == roleModel.RoleName);
            if (rolename == null) throw new Exception("Такой роли не существует");
            var roleEntity = new Role
            {
                RoleName = roleModel.RoleName,
                Id = roleModel.Id,
            };

            await _context.Roles.AddAsync(roleEntity);
            await _context.SaveChangesAsync();

            RoleModel createdRole = new RoleModel(roleEntity.Id, roleEntity.RoleName);
            return createdRole;
        }
        public async Task<List<RoleModel>> GetAllRoles()
        {
            //достаем данные из бд
            var roleEntities = await _context.Roles
                .AsNoTracking()
                .ToListAsync();

            // преобразуем entities в models
            var roles = roleEntities
                .Select(roleEntity =>
                {
                    var roleModel = new RoleModel(
                        roleEntity.Id,
                        roleEntity.RoleName);

                    return roleModel;
                })
                .ToList();

            return roles;
        }
        public async Task<RoleModel> GetRoleById(int roleId)
        {
            var roleEntity = await _context.Roles.SingleOrDefaultAsync(d => d.Id == roleId);
            if (roleEntity == null) throw new Exception("Role с таким id не существует");
            RoleModel role = new RoleModel(roleEntity.Id, roleEntity.RoleName);
            return role;
        }
        public async Task<RoleModel> UpdateRole(RoleModel roleModel)
        {
            var roleEntity = await _context.Roles.SingleOrDefaultAsync(d => d.Id == roleModel.Id);
            if (roleEntity == null) throw new Exception("Role с таким id не существует");

            roleEntity.RoleName = roleModel.RoleName;
            _context.SaveChanges();
            RoleModel role = new RoleModel(roleEntity.Id, roleEntity.RoleName);
            return role;
        }
        public async Task<bool> DeleteRole(int roleId)
        {
            var roleEntity = await _context.Roles.SingleOrDefaultAsync(d => d.Id == roleId);
            if (roleEntity == null) throw new Exception("Role с таким id не существует");
            _context.Remove(roleEntity);
            _context.SaveChanges();

            var role = await _context.Roles.SingleOrDefaultAsync(d => d.Id == roleId);
            if (role == null) return true;
            else return false;
        }
    }
}
