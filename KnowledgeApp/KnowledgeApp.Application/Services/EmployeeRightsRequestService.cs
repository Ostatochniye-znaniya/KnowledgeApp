using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Repositories;

namespace KnowledgeApp.Application.Services
{
    public class EmployeeRightsRequestService
    {
        private readonly EmployeeRightsRequestRepository _employeeRightsRequestRepository;

        public EmployeeRightsRequestService(EmployeeRightsRequestRepository employeeRightsRequestRepository)
        {
            _employeeRightsRequestRepository = employeeRightsRequestRepository;
        }

        /// <summary>
        /// Получить все запросы прав сотрудников с пагинацией
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns>Список запросов прав сотрудников</returns>
        public async Task<List<EmployeeRightsRequestModel>> GetAllEmployeeRightsRequests(int page = 1, int pageSize = 50)
        {
            return await _employeeRightsRequestRepository.GetAll(page, pageSize);
        }

        /// <summary>
        /// Получить запрос прав сотрудника по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор запроса</param>
        /// <returns>Модель запроса прав сотрудника</returns>
        public async Task<EmployeeRightsRequestModel> GetEmployeeRightsRequestById(int id)
        {
            return await _employeeRightsRequestRepository.GetById(id);
        }

        /// <summary>
        /// Создать новый запрос прав сотрудника
        /// </summary>
        /// <param name="employeeRequest">Модель запроса прав сотрудника</param>
        /// <returns></returns>
        public async Task CreateEmployeeRightsRequest(EmployeeRightsRequestModel employeeRequest)
        {
            if (employeeRequest == null)
                throw new ArgumentNullException(nameof(employeeRequest), "Запрос не может быть null");

            await _employeeRightsRequestRepository.AddEmployee(employeeRequest);
        }

        /// <summary>
        /// Обновить существующий запрос прав сотрудника
        /// </summary>
        /// <param name="employeeRequest">Модель запроса прав сотрудника</param>
        /// <returns></returns>
        public async Task UpdateEmployeeRightsRequest(EmployeeRightsRequestModel employeeRequest)
        {
            if (employeeRequest == null)
                throw new ArgumentNullException(nameof(employeeRequest), "Запрос не может быть null");

            await _employeeRightsRequestRepository.UpdateEmplpyee(employeeRequest);
        }

        /// <summary>
        /// Удалить запрос прав сотрудника по идентификатору
        /// </summary>
        /// <param name="id">Идентификатор запроса</param>
        /// <returns></returns>
        public async Task DeleteEmployeeRightsRequest(int id)
        {
            await _employeeRightsRequestRepository.DeleteById(id);
        }

        /// <summary>
        /// Получить активные запросы прав сотрудников
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns>Список активных запросов</returns>
        public async Task<List<EmployeeRightsRequestModel>> GetActiveEmployeeRightsRequests(int page = 1, int pageSize = 50)
        {
            var allRequests = await _employeeRightsRequestRepository.GetAll(page, pageSize);
            return allRequests.Where(r => r.IsActive).ToList();
        }

        /// <summary>
        /// Получить запросы по структурному подразделению
        /// </summary>
        /// <param name="structuralDivision">Структурное подразделение</param>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns>Список запросов по подразделению</returns>
        public async Task<List<EmployeeRightsRequestModel>> GetEmployeeRightsRequestsByDivision(string structuralDivision, int page = 1, int pageSize = 50)
        {
            var allRequests = await _employeeRightsRequestRepository.GetAll(page, pageSize);
            return allRequests.Where(r => r.StructuralDivision == structuralDivision).ToList();
        }
    }
}