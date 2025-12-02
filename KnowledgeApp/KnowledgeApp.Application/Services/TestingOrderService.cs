using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace KnowledgeApp.Application.Services
{
    public class TestingOrderService
    {
        private readonly TestingOrderRepository _testingOrderRepository;

        public TestingOrderService(TestingOrderRepository testingOrderRepository)
        {
            _testingOrderRepository = testingOrderRepository;
        }

        public async Task<List<TestingOrderModel>> GetAll(int pageNumber = 1, int pageSize = 10)
        {
            List<TestingOrderModel> testingOrders = await _testingOrderRepository
                .GetAllTestingOrders(pageNumber, pageSize);
            return testingOrders;
        }

        public async Task<List<TestingOrderModel>> GetAllWithoutPagination()
        {
            List<TestingOrderModel> testingOrders = await _testingOrderRepository
                .GetAllTestingOrdersWithoutPagination();
            return testingOrders;
        }

        public async Task<TestingOrderModel> GetTestingOrderById(int testingOrderId)
        {
            TestingOrderModel testingOrder = await _testingOrderRepository
                .GetTestingOrderById(testingOrderId);
            return testingOrder;
        }

        public async Task<TestingOrderModel> CreateTestingOrder(TestingOrderModel testingOrderModel)
        {


            TestingOrderModel createdTestingOrder = await _testingOrderRepository
                .CreateTestingOrder(testingOrderModel);
            return createdTestingOrder;
        }

        public async Task<TestingOrderModel> UpdateTestingOrder(TestingOrderModel testingOrderModel)
        {
            var existingOrder = await _testingOrderRepository
                .GetTestingOrderById(testingOrderModel.Id);

            TestingOrderModel updatedTestingOrder = await _testingOrderRepository
                .UpdateTestingOrder(testingOrderModel);
            return updatedTestingOrder;
        }

        public async Task<bool> DeleteTestingOrder(int testingOrderId)
        {
            var existingOrder = await _testingOrderRepository
                .GetTestingOrderById(testingOrderId);

            bool result = await _testingOrderRepository.DeleteTestingOrder(testingOrderId);
            return result;
        }

        // Дополнительные методы с бизнес-логикой

        public async Task<List<TestingOrderModel>> GetTestingOrdersBySemesterId(int semesterId)
        {
            var allOrders = await _testingOrderRepository.GetAllTestingOrdersWithoutPagination();
            var filteredOrders = allOrders.Where(o => o.SemesterId == semesterId).ToList();
            return filteredOrders;
        }

        public async Task<List<TestingOrderModel>> GetActiveTestingOrders(DateOnly currentDate)
        {
            var allOrders = await _testingOrderRepository.GetAllTestingOrdersWithoutPagination();

            var activeOrders = allOrders.Where(o =>
                currentDate <= o.TestingSummaryReportUpTo ||
                currentDate <= o.QuestionnaireSummaryReportUpTo ||
                currentDate <= o.PaperReportUpTo ||
                currentDate <= o.DigitalReportUpTo
            ).ToList();

            return activeOrders;
        }

        public async Task<List<TestingOrderModel>> GetTestingOrdersByEmployeeName(string employeeName)
        {
            var allOrders = await _testingOrderRepository.GetAllTestingOrdersWithoutPagination();

            var filteredOrders = allOrders.Where(o =>
                o.EducationControlEmployeeName.Contains(employeeName, StringComparison.OrdinalIgnoreCase) ||
                o.EducationMethodEmployeeName.Contains(employeeName, StringComparison.OrdinalIgnoreCase)
            ).ToList();

            return filteredOrders;
        }

        public async Task<bool> ValidateTestingOrder(TestingOrderModel testingOrderModel)
        {
            if (testingOrderModel.OrderDate == default)
                return false;

            if (testingOrderModel.Number <= 0)
                return false;

            if (string.IsNullOrWhiteSpace(testingOrderModel.EducationControlEmployeeName))
                return false;

            if (string.IsNullOrWhiteSpace(testingOrderModel.EducationMethodEmployeeName))
                return false;

            // Проверка, что даты отчетов не раньше даты приказа
            if (testingOrderModel.TestingSummaryReportUpTo < testingOrderModel.OrderDate ||
                testingOrderModel.QuestionnaireSummaryReportUpTo < testingOrderModel.OrderDate ||
                testingOrderModel.PaperReportUpTo < testingOrderModel.OrderDate ||
                testingOrderModel.DigitalReportUpTo < testingOrderModel.OrderDate)
            {
                return false;
            }

            return true;
        }
    }
}