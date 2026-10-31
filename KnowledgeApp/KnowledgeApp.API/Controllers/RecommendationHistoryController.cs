using KnowledgeApp.API.Contracts;
using KnowledgeApp.Application.Services;
using KnowledgeApp.Domain.Entities;
using KnowledgeApp.Domain.Planning;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;

namespace KnowledgeApp.API.Controllers;

public class RecommendationHistoryController : BaseController
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
            RecommendedAt = r.RecommendedAt, Author = r.Author, RecommendedById = r.RecommendedById,
            RecommendedBy = r.RecommendedBy,
            SemesterId = r.SemesterId,
            SemesterName = r.SemesterName,
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
                RecommendedAt = recommendation.RecommendedAt, Author = recommendation.Author, RecommendedById = recommendation.RecommendedById,
                RecommendedBy = recommendation.RecommendedBy,
                SemesterId = recommendation.SemesterId,
                SemesterName = recommendation.SemesterName,
                StudyGroupId = recommendation.StudyGroupId,
                StudyGroupName = recommendation.StudyGroupName
            };

            return Ok(response);
        }
        catch (PlanningRuleException ex) { return Conflict(ex.Message); }
        catch (DbUpdateException) { return Conflict("Назначение уже существует или связано с другими данными"); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<RecommendationHistoryResponse>> Create(
        [FromBody] CreateRecommendationHistoryRequest request)
    {
        try
        {
            var model = new RecommendationHistoryModel
            {
                RecommendedAt = request.RecommendedAt, Author = request.Author,
                RecommendedById = request.RecommendedById,
                SemesterId = request.SemesterId,
                StudyGroupId = request.StudyGroupId
            };
    
            await _recommendationHistoryService.AddRecommendationAsync(model);
    
            var createdRecommendation = await _recommendationHistoryService.GetByIdAsync(model.Id);
    
            var response = new RecommendationHistoryResponse
            {
                Id = createdRecommendation.Id,
                RecommendedAt = createdRecommendation.RecommendedAt, Author = createdRecommendation.Author, RecommendedById = createdRecommendation.RecommendedById,
                RecommendedBy = createdRecommendation.RecommendedBy,
                SemesterId = createdRecommendation.SemesterId,
                SemesterName = createdRecommendation.SemesterName,
                StudyGroupId = createdRecommendation.StudyGroupId,
                StudyGroupName = createdRecommendation.StudyGroupName
            };
    
            return CreatedAtAction(nameof(GetById), new { id = response.Id }, response);
        }
        catch (PlanningRuleException ex) { return Conflict(ex.Message); }
        catch (DbUpdateException) { return Conflict("Назначение уже существует или связано с другими данными"); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }

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
                RecommendedAt = request.RecommendedAt, Author = request.Author,
                RecommendedBy = request.RecommendedBy, RecommendedById = request.RecommendedById,
                SemesterId = request.SemesterId,
                StudyGroupId = request.StudyGroupId
            };

            await _recommendationHistoryService.UpdateRecommendationAsync(model);
            return NoContent();
        }
        catch (PlanningRuleException ex) { return Conflict(ex.Message); }
        catch (DbUpdateException) { return Conflict("Назначение уже существует или связано с другими данными"); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex)
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
        catch (PlanningRuleException ex) { return Conflict(ex.Message); }
        catch (DbUpdateException) { return Conflict("Назначение уже существует или связано с другими данными"); }
        catch (ArgumentException ex) { return BadRequest(ex.Message); }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
    }
}
