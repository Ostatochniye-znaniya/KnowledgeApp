using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class RecommendationHistoryRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public RecommendationHistoryRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task AddRecommendationAsync(RecommendationHistoryModel model)
        {
            await _context.RecommendationHistory.AddAsync(new Database.Entities.RecommendationHistory()
            {
                RecommendedAt = model.RecommendedAt,
                UserId = model.RecommendedById,
                SemesterId = model.SemesterId,
                StudyGroupId = model.StudyGroupId
            });
            await _context.SaveChangesAsync();
        }
        public async Task DeleteRecommendationAsync(int recommendationId)
        {
            var res = await _context.RecommendationHistory.FirstOrDefaultAsync(r => r.Id == recommendationId);
            if (res == null) throw new Exception("Не найдена запись рекоммендации с таким Id");
            _context.RecommendationHistory.Remove(res);
            await _context.SaveChangesAsync();
        }
        public async Task<RecommendationHistoryModel> GetById(int recommendationId)
        {
            var res = await _context.RecommendationHistory.Include(u => u.StudyGroup).Include(u => u.Semester).Include(r=>r.User).FirstOrDefaultAsync(r => r.Id == recommendationId);
            if (res == null) throw new Exception("Не найдена запись рекоммендации с таким Id");
            return new RecommendationHistoryModel()
            {
                Id = recommendationId,
                RecommendedAt = res.RecommendedAt,
                RecommendedBy = res.User?.Name,
                SemesterId = res.SemesterId,
                SemesterPart = res.Semester?.SemesterPart,
                SemesterYear = res.Semester?.SemesterYear,
                StudyGroupId = res.StudyGroupId,
                StudyGroupName = res.StudyGroup?.GroupNumber,
                IsChosenForTesting = res.IsChosenForTesting
            };
        }
        public async Task UpdateRecommendationAsync(RecommendationHistoryModel model)
        {
            var existing = await _context.RecommendationHistory
                .FirstOrDefaultAsync(r => r.Id == model.Id);

            if (existing == null)
                throw new Exception("Не найдена запись рекоммендации с таким Id");

            existing.RecommendedAt = model.RecommendedAt;
            existing.SemesterId = model.SemesterId;
            existing.StudyGroupId = model.StudyGroupId;
            existing.UserId = model.RecommendedById;
            existing.IsChosenForTesting = model.IsChosenForTesting;

            await _context.SaveChangesAsync();
        }
        public async Task UpdateIsChosenGroup(int recId, bool isChosen)
        {
            var existing = await _context.RecommendationHistory
                .FirstOrDefaultAsync(r => r.Id == recId);

            if (existing == null)
                throw new Exception("Не найдена запись рекоммендации с таким Id");
            existing.IsChosenForTesting = isChosen;
            await _context.SaveChangesAsync();
        }


        public async Task<List<RecommendationHistoryModel>> GetAllAsync(
            int? semesterId = null,
            int? studyGroupId = null,
            int? userId = null)
        {
            var query = _context.RecommendationHistory
                .Include(u => u.StudyGroup)
                .Include(u => u.Semester)
                .Include(u=> u.User)
                .AsQueryable();

            if (semesterId.HasValue)
            {
                query = query.Where(r => r.SemesterId == semesterId.Value);
            }

            if (userId.HasValue)
            {
                query = query.Where(r => r.UserId == userId.Value);
            }

            if (studyGroupId.HasValue)
            {
                query = query.Where(r => r.StudyGroupId == studyGroupId.Value);
            }

            var results = await query
                .OrderByDescending(r => r.RecommendedAt)
                .ToListAsync();

            return results.Select(res => new RecommendationHistoryModel()
            {
                Id = res.Id,
                RecommendedAt = res.RecommendedAt,
                RecommendedBy = res.User?.Name,
                SemesterId = res.SemesterId,
                SemesterPart = res.Semester?.SemesterPart,
                SemesterYear = res.Semester?.SemesterYear,
                StudyGroupId = res.StudyGroupId,
                StudyGroupName = res.StudyGroup?.GroupNumber,
                IsChosenForTesting = res.IsChosenForTesting
               
            }).ToList();
        }
    }
}
