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
        /// Получить все запросы прав сотрудников с пагинацией для определнного пользователя
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns>Список запросов прав сотрудников</returns>
        public async Task<List<EmployeeRightsRequestModel>> GetAllEmployeeRightsRequestsByUserId(int userId,int page = 1, int pageSize = 50)
        {
            return await _employeeRightsRequestRepository.GetAllByUserId(userId, page, pageSize);
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
            var allRequests = await _employeeRightsRequestRepository.GetAllActive(page, pageSize);
            return allRequests;
        }

        /// <summary>
        /// Получить активные запросы прав сотрудников для определнного пользователя
        /// </summary>
        /// <param name="page">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <returns>Список активных запросов</returns>
        public async Task<List<EmployeeRightsRequestModel>> GetActiveEmployeeRightsRequestsByUser(int userId, int page = 1, int pageSize = 50)
        {
            var allRequests = await _employeeRightsRequestRepository.GetAllActive(page, pageSize, userId);
            return allRequests;
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
            var allRequests = await _employeeRightsRequestRepository.GetAllByDivision(structuralDivision,page, pageSize);
            return allRequests;
        }
        public async Task<List<EmployeeRightsRequestModel>> GetEmployeeRightsRequestsByDivisionAndUserId(string structuralDivision,int userId, int page = 1, int pageSize = 50)
        {
            var allRequests = await _employeeRightsRequestRepository.GetAllByDivision(structuralDivision, page, pageSize,userId);
            return allRequests;
        }
    }
}