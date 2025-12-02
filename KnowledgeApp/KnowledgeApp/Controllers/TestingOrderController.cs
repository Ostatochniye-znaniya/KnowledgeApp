using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Core.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class TestingOrderController : ControllerBase
    {
        private readonly TestingOrderService _testingOrderService;

        public TestingOrderController(TestingOrderService testingOrderService)
        {
            _testingOrderService = testingOrderService;
        }

        [HttpGet]
        public async Task<ActionResult<List<TestingOrderResponse>>> GetAll()
        {
            try
            {
                var testingOrders = await _testingOrderService.GetAllWithoutPagination();

                var response = testingOrders.Select(o => new TestingOrderResponse
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    Number = o.Number,
                    EducationControlEmployeeName = o.EducationControlEmployeeName,
                    EducationMethodEmployeeName = o.EducationMethodEmployeeName,
                    TestingSummaryReportUpTo = o.TestingSummaryReportUpTo,
                    QuestionnaireSummaryReportUpTo = o.QuestionnaireSummaryReportUpTo,
                    PaperReportUpTo = o.PaperReportUpTo,
                    DigitalReportUpTo = o.DigitalReportUpTo,
                    SemesterId = o.SemesterId
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<TestingOrderResponse>> GetById(int id)
        {
            try
            {
                var testingOrder = await _testingOrderService.GetTestingOrderById(id);

                var response = new TestingOrderResponse
                {
                    Id = testingOrder.Id,
                    OrderDate = testingOrder.OrderDate,
                    Number = testingOrder.Number,
                    EducationControlEmployeeName = testingOrder.EducationControlEmployeeName,
                    EducationMethodEmployeeName = testingOrder.EducationMethodEmployeeName,
                    TestingSummaryReportUpTo = testingOrder.TestingSummaryReportUpTo,
                    QuestionnaireSummaryReportUpTo = testingOrder.QuestionnaireSummaryReportUpTo,
                    PaperReportUpTo = testingOrder.PaperReportUpTo,
                    DigitalReportUpTo = testingOrder.DigitalReportUpTo,
                    SemesterId = testingOrder.SemesterId
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<TestingOrderResponse>> Create(
            [FromBody] CreateTestingOrderRequest request)
        {
            try
            {
                var model = new TestingOrderModel
                {
                    OrderDate = request.OrderDate,
                    Number = request.Number,
                    EducationControlEmployeeName = request.EducationControlEmployeeName,
                    EducationMethodEmployeeName = request.EducationMethodEmployeeName,
                    TestingSummaryReportUpTo = request.TestingSummaryReportUpTo,
                    QuestionnaireSummaryReportUpTo = request.QuestionnaireSummaryReportUpTo,
                    PaperReportUpTo = request.PaperReportUpTo,
                    DigitalReportUpTo = request.DigitalReportUpTo,
                    SemesterId = request.SemesterId
                };

                var createdTestingOrder = await _testingOrderService.CreateTestingOrder(model);

                var response = new TestingOrderResponse
                {
                    Id = createdTestingOrder.Id,
                    OrderDate = createdTestingOrder.OrderDate,
                    Number = createdTestingOrder.Number,
                    EducationControlEmployeeName = createdTestingOrder.EducationControlEmployeeName,
                    EducationMethodEmployeeName = createdTestingOrder.EducationMethodEmployeeName,
                    TestingSummaryReportUpTo = createdTestingOrder.TestingSummaryReportUpTo,
                    QuestionnaireSummaryReportUpTo = createdTestingOrder.QuestionnaireSummaryReportUpTo,
                    PaperReportUpTo = createdTestingOrder.PaperReportUpTo,
                    DigitalReportUpTo = createdTestingOrder.DigitalReportUpTo,
                    SemesterId = createdTestingOrder.SemesterId
                };

                return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(
            [FromBody] UpdateTestingOrderRequest request)
        {
            try
            {
                var model = new TestingOrderModel
                {
                    Id = request.Id,
                    OrderDate = request.OrderDate,
                    Number = request.Number,
                    EducationControlEmployeeName = request.EducationControlEmployeeName,
                    EducationMethodEmployeeName = request.EducationMethodEmployeeName,
                    TestingSummaryReportUpTo = request.TestingSummaryReportUpTo,
                    QuestionnaireSummaryReportUpTo = request.QuestionnaireSummaryReportUpTo,
                    PaperReportUpTo = request.PaperReportUpTo,
                    DigitalReportUpTo = request.DigitalReportUpTo,
                    SemesterId = request.SemesterId
                };

                await _testingOrderService.UpdateTestingOrder(model);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _testingOrderService.DeleteTestingOrder(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("semester/{semesterId}")]
        public async Task<ActionResult<List<TestingOrderResponse>>> GetBySemesterId(int semesterId)
        {
            try
            {
                var testingOrders = await _testingOrderService.GetTestingOrdersBySemesterId(semesterId);

                var response = testingOrders.Select(o => new TestingOrderResponse
                {
                    Id = o.Id,
                    OrderDate = o.OrderDate,
                    Number = o.Number,
                    EducationControlEmployeeName = o.EducationControlEmployeeName,
                    EducationMethodEmployeeName = o.EducationMethodEmployeeName,
                    TestingSummaryReportUpTo = o.TestingSummaryReportUpTo,
                    QuestionnaireSummaryReportUpTo = o.QuestionnaireSummaryReportUpTo,
                    PaperReportUpTo = o.PaperReportUpTo,
                    DigitalReportUpTo = o.DigitalReportUpTo,
                    SemesterId = o.SemesterId
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}