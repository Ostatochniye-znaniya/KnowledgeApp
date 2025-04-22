using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class StatusService
    {
        private readonly StatusRepository _statusRepository;

        public StatusService(StatusRepository statusRepository)
        {
            _statusRepository = statusRepository;
        }

        public async Task<List<StatusModel>> GetAll()
        {
            List<StatusModel> statuses = await _statusRepository.GetAllStatuses();
            return statuses;
        }

        public async Task<StatusModel> GetStatusById(int statusId)
        {
            StatusModel status = await _statusRepository.GetStatusById(statusId);
            return status;
        }

        public async Task<StatusModel> CreateStatus(StatusModel statusModel)
        {
            StatusModel createdStatusId = await _statusRepository.CreateStatus(statusModel);

            return createdStatusId;
        }

        public async Task<StatusModel> UpdateStatus(StatusModel statusModel)
        {
            StatusModel updatedStatusModel = await _statusRepository.UpdateStatus(statusModel);
            return updatedStatusModel;
        }

        public async Task<bool> DeleteStatus(int statusId)
        {
            bool result = await _statusRepository.DeleteStatus(statusId);
            return result;
        }
    }
}