using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Database.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class TestingOrderRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public TestingOrderRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<TestingOrderModel> CreateTestingOrder(TestingOrderModel testingOrderModel)
        {
            var testingOrderEntity = new TestingOrder
            {
                OrderDate = testingOrderModel.OrderDate,
                Number = testingOrderModel.Number,
                EducationControlEmployeeName = testingOrderModel.EducationControlEmployeeName,
                EducationMethodEmployeeName = testingOrderModel.EducationMethodEmployeeName,
                TestingSummaryReportUpTo = testingOrderModel.TestingSummaryReportUpTo,
                QuestionnaireSummaryReportUpTo = testingOrderModel.QuestionnaireSummaryReportUpTo,
                PaperReportUpTo = testingOrderModel.PaperReportUpTo,
                DigitalReportUpTo = testingOrderModel.DigitalReportUpTo,
                SemesterId = testingOrderModel.SemesterId
            };

            await _context.TestingOrders.AddAsync(testingOrderEntity);
            await _context.SaveChangesAsync();

            TestingOrderModel createdTestingOrder = new TestingOrderModel(
                testingOrderEntity.Id,
                testingOrderEntity.OrderDate,
                testingOrderEntity.Number,
                testingOrderEntity.EducationControlEmployeeName,
                testingOrderEntity.EducationMethodEmployeeName,
                testingOrderEntity.TestingSummaryReportUpTo,
                testingOrderEntity.QuestionnaireSummaryReportUpTo,
                testingOrderEntity.PaperReportUpTo,
                testingOrderEntity.DigitalReportUpTo,
                testingOrderEntity.SemesterId);

            return createdTestingOrder;
        }

        public async Task<List<TestingOrderModel>> GetAllTestingOrders(int pageNumber = 1, int pageSize = 20)
        {
            var testingOrderEntities = await _context.TestingOrders
                .AsNoTracking()
                .OrderBy(to => to.Id) 
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();


            var testingOrders = testingOrderEntities
                .Select(testingOrderEntity => new TestingOrderModel(
                    testingOrderEntity.Id,
                    testingOrderEntity.OrderDate,
                    testingOrderEntity.Number,
                    testingOrderEntity.EducationControlEmployeeName,
                    testingOrderEntity.EducationMethodEmployeeName,
                    testingOrderEntity.TestingSummaryReportUpTo,
                    testingOrderEntity.QuestionnaireSummaryReportUpTo,
                    testingOrderEntity.PaperReportUpTo,
                    testingOrderEntity.DigitalReportUpTo,
                    testingOrderEntity.SemesterId))
                .ToList();
            return testingOrders;
        }


        public async Task<List<TestingOrderModel>> GetAllTestingOrdersWithoutPagination()
        {
            var testingOrderEntities = await _context.TestingOrders
                .AsNoTracking()
                .ToListAsync();

            var testingOrders = testingOrderEntities
                .Select(testingOrderEntity => new TestingOrderModel(
                    testingOrderEntity.Id,
                    testingOrderEntity.OrderDate,
                    testingOrderEntity.Number,
                    testingOrderEntity.EducationControlEmployeeName,
                    testingOrderEntity.EducationMethodEmployeeName,
                    testingOrderEntity.TestingSummaryReportUpTo,
                    testingOrderEntity.QuestionnaireSummaryReportUpTo,
                    testingOrderEntity.PaperReportUpTo,
                    testingOrderEntity.DigitalReportUpTo,
                    testingOrderEntity.SemesterId))
                .ToList();

            return testingOrders;
        }

        public async Task<TestingOrderModel> GetTestingOrderById(int testingOrderId)
        {
            var testingOrderEntity = await _context.TestingOrders
                .SingleOrDefaultAsync(to => to.Id == testingOrderId);

            if (testingOrderEntity == null)
                throw new Exception("TestingOrder с таким id не существует");

            TestingOrderModel testingOrder = new TestingOrderModel(
                testingOrderEntity.Id,
                testingOrderEntity.OrderDate,
                testingOrderEntity.Number,
                testingOrderEntity.EducationControlEmployeeName,
                testingOrderEntity.EducationMethodEmployeeName,
                testingOrderEntity.TestingSummaryReportUpTo,
                testingOrderEntity.QuestionnaireSummaryReportUpTo,
                testingOrderEntity.PaperReportUpTo,
                testingOrderEntity.DigitalReportUpTo,
                testingOrderEntity.SemesterId);

            return testingOrder;
        }

        public async Task<TestingOrderModel> UpdateTestingOrder(TestingOrderModel testingOrderModel)
        {
            var testingOrderEntity = await _context.TestingOrders
                .SingleOrDefaultAsync(to => to.Id == testingOrderModel.Id);

            if (testingOrderEntity == null)
                throw new Exception("TestingOrder с таким id не существует");

            testingOrderEntity.OrderDate = testingOrderModel.OrderDate;
            testingOrderEntity.Number = testingOrderModel.Number;
            testingOrderEntity.EducationControlEmployeeName = testingOrderModel.EducationControlEmployeeName;
            testingOrderEntity.EducationMethodEmployeeName = testingOrderModel.EducationMethodEmployeeName;
            testingOrderEntity.TestingSummaryReportUpTo = testingOrderModel.TestingSummaryReportUpTo;
            testingOrderEntity.QuestionnaireSummaryReportUpTo = testingOrderModel.QuestionnaireSummaryReportUpTo;
            testingOrderEntity.PaperReportUpTo = testingOrderModel.PaperReportUpTo;
            testingOrderEntity.DigitalReportUpTo = testingOrderModel.DigitalReportUpTo;
            testingOrderEntity.SemesterId = testingOrderModel.SemesterId;

            await _context.SaveChangesAsync();

            var updatedTestingOrder = new TestingOrderModel(
                testingOrderEntity.Id,
                testingOrderEntity.OrderDate,
                testingOrderEntity.Number,
                testingOrderEntity.EducationControlEmployeeName,
                testingOrderEntity.EducationMethodEmployeeName,
                testingOrderEntity.TestingSummaryReportUpTo,
                testingOrderEntity.QuestionnaireSummaryReportUpTo,
                testingOrderEntity.PaperReportUpTo,
                testingOrderEntity.DigitalReportUpTo,
                testingOrderEntity.SemesterId);

            return updatedTestingOrder;
        }

        public async Task<bool> DeleteTestingOrder(int testingOrderId)
        {
            var testingOrderEntity = await _context.TestingOrders
                .SingleOrDefaultAsync(to => to.Id == testingOrderId);

            if (testingOrderEntity == null)
                throw new Exception("TestingOrder с таким id не существует");

            _context.Remove(testingOrderEntity);
            await _context.SaveChangesAsync();

            var testingOrder = await _context.TestingOrders
                .SingleOrDefaultAsync(to => to.Id == testingOrderId);

            return testingOrder == null;
        }
    }
}