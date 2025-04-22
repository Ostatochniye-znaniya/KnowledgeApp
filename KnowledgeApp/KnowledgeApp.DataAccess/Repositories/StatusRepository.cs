using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class StatusRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public StatusRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<StatusModel> CreateStatus(StatusModel statusModel)
        {
            var status = await _context.Statuses.SingleOrDefaultAsync(f => f.StatusName == statusModel.StatusName);
            if (status == null) throw new Exception("Такого status не существует");
            var statusEntity = new Status
            {
                StatusName = statusModel.StatusName,
                Id = statusModel.Id
            };

            await _context.Statuses.AddAsync(statusEntity);
            await _context.SaveChangesAsync();

            StatusModel createdStatus = new StatusModel(statusEntity.Id, statusEntity.StatusName);
            return createdStatus;
        }
        public async Task<List<StatusModel>> GetAllStatuses()
        {
            //достаем данные из бд
            var statusEntities = await _context.Statuses
                .AsNoTracking()
                .ToListAsync();

            // преобразуем entities в models
            var statuses = statusEntities
                .Select(statusEntity =>
                {
                    var statusModel = new StatusModel(
                        statusEntity.Id,
                        statusEntity.StatusName);

                    return statusModel;
                })
                .ToList();

            return statuses;
        }
        public async Task<StatusModel> GetStatusById(int statusId)
        {
            var statusEntity = await _context.Statuses.SingleOrDefaultAsync(d => d.Id == statusId);
            if (statusEntity == null) throw new Exception("Status с таким id не существует");
            StatusModel status = new StatusModel(statusEntity.Id, statusEntity.StatusName);
            return status;
        }
        public async Task<StatusModel> UpdateStatus(StatusModel statusModel)
        {
            var statusEntity = await _context.Statuses.SingleOrDefaultAsync(d => d.Id == statusModel.Id);
            if (statusEntity == null) throw new Exception("Status с таким id не существует");

            statusEntity.StatusName = statusModel.StatusName;
            _context.SaveChanges();
            StatusModel status = new StatusModel(statusEntity.Id, statusEntity.StatusName);
            return status;
        }
        public async Task<bool> DeleteStatus(int statusId)
        {
            var statusEntity = await _context.Statuses.SingleOrDefaultAsync(d => d.Id == statusId);
            if (statusEntity == null) throw new Exception("Status с таким id не существует");
            _context.Remove(statusEntity);
            _context.SaveChanges();

            var status = await _context.Statuses.SingleOrDefaultAsync(d => d.Id == statusId);
            if (status == null) return true;
            else return false;
        }
    }
}
