using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers
{
    [ApiController]
    [Route("[controller]/[action]")]
    public class RecommendationHistoryController : ControllerBase
    {
        private readonly RecommendationHistoryService _recommendationHistoryService;

        public RecommendationHistoryController(RecommendationHistoryService recommendationHistoryService)
        {
            _recommendationHistoryService = recommendationHistoryService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<RecommendationHistoryResponse>>> GetAll(
            [FromQuery] GetRecommendationsRequest request)
        {
            var recommendations = await _recommendationHistoryService.GetAllAsync(
                request.SemesterId,
                request.StudyGroupId);

            var response = recommendations.Select(r => new RecommendationHistoryResponse
            {
                Id = r.Id,
                RecommendedAt = r.RecommendedAt,
                RecommendedBy = r.RecommendedBy,
                SemesterId = r.SemesterId,
                SemesterPart = r.SemesterPart,
                SemesterYear = r.SemesterYear,
                StudyGroupId = r.StudyGroupId,
                StudyGroupName = r.StudyGroupName
            });

            return Ok(response);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<RecommendationHistoryResponse>> GetById(int id)
        {
            try
            {
                var recommendation = await _recommendationHistoryService.GetByIdAsync(id);

                var response = new RecommendationHistoryResponse
                {
                    Id = recommendation.Id,
                    RecommendedAt = recommendation.RecommendedAt,
                    RecommendedBy = recommendation.RecommendedBy,
                    SemesterId = recommendation.SemesterId,
                    SemesterPart = recommendation.SemesterPart,
                    SemesterYear = recommendation.SemesterYear,
                    StudyGroupId = recommendation.StudyGroupId,
                    StudyGroupName = recommendation.StudyGroupName
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpPost]
        public async Task<ActionResult<RecommendationHistoryResponse>> Create(
            [FromBody] CreateRecommendationHistoryRequest request)
        {
            var model = new RecommendationHistoryModel
            {
                RecommendedAt = request.RecommendedAt,
                RecommendedById = request.RecommendedById,
                SemesterId = request.SemesterId,
                StudyGroupId = request.StudyGroupId
            };

            await _recommendationHistoryService.AddRecommendationAsync(model);

            // Получаем созданную запись для возврата
            var recommendations = await _recommendationHistoryService.GetAllAsync();
            var createdRecommendation = recommendations
                .OrderByDescending(r => r.Id)
                .First();

            var response = new RecommendationHistoryResponse
            {
                Id = createdRecommendation.Id,
                RecommendedAt = createdRecommendation.RecommendedAt,
                RecommendedBy = createdRecommendation.RecommendedBy,
                SemesterId = createdRecommendation.SemesterId,
                SemesterPart = createdRecommendation.SemesterPart,
                SemesterYear = createdRecommendation.SemesterYear,
                StudyGroupId = createdRecommendation.StudyGroupId,
                StudyGroupName = createdRecommendation.StudyGroupName
            };

            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<ActionResult> Update(
            int id,
            [FromBody] UpdateRecommendationHistoryRequest request)
        {
            try
            {
                var model = new RecommendationHistoryModel
                {
                    Id = id,
                    RecommendedAt = request.RecommendedAt,
                    RecommendedById = request.RecommendedById,
                    SemesterId = request.SemesterId,
                    StudyGroupId = request.StudyGroupId
                };

                await _recommendationHistoryService.UpdateRecommendationAsync(model);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
        [HttpPut]
        public async Task<ActionResult> UpdateChosenFLag(
            [FromBody] UpdateChosenFlagInRecHistory request)
        {
            try
            {
                await _recommendationHistoryService.UpdateIsChosenFlag(request.recId, request.isChosen);
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
                await _recommendationHistoryService.DeleteRecommendationAsync(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(ex.Message);
            }
        }
    }
}