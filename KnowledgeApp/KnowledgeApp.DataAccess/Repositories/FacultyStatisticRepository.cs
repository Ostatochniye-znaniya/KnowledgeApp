using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class FacultyStatisticRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public FacultyStatisticRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<FacultyStatisticModel> CreateFacultyStatistic(FacultyStatisticModel facultystatisticModel)
        {
            var facultystatisticEntity = new FacultyStatistic
            {
                FacultyName = facultystatisticModel.FacultyName,
                GroupCountMust = facultystatisticModel.GroupCountMust,
                GroupCountFact = facultystatisticModel.GroupCountFact,
                DisciplineCountMust = facultystatisticModel.DisciplineCountMust,
                DisciplineCountFact = facultystatisticModel.DisciplineCountFact,
                EReportDoneCount = facultystatisticModel.EReportDoneCount,
                EReportRevCount = facultystatisticModel.EReportRevCount,
                PapReportDoneCount = facultystatisticModel.PapReportDoneCount,
                PapReportRevCount = facultystatisticModel.PapReportRevCount,
                AllDone = facultystatisticModel.AllDone,
            };

            await _context.FacultyStatistics.AddAsync(facultystatisticEntity);
            await _context.SaveChangesAsync();

            FacultyStatisticModel createdFacultyStatistic = new FacultyStatisticModel(facultystatisticEntity.Id, facultystatisticEntity.FacultyName,
                facultystatisticEntity.GroupCountFact, facultystatisticEntity.GroupCountMust, facultystatisticEntity.DisciplineCountMust, facultystatisticEntity.GroupCountMust,
                facultystatisticEntity.EReportDoneCount, facultystatisticEntity.EReportRevCount, facultystatisticEntity.PapReportDoneCount, facultystatisticEntity.PapReportRevCount,
                facultystatisticEntity.AllDone);
            return createdFacultyStatistic;
        }

        public async Task<List<FacultyStatisticModel>> GetAllFacultyStatistics()
        {
            //достаем данные из бд
            var facultystatisticEntities = await _context.FacultyStatistics
                .AsNoTracking()
                .ToListAsync();

            // преобразуем entities в models
            var facultystatistics = facultystatisticEntities
                .Select(facultystatisticEntity =>
                {
                    var facultystatisticModel = new FacultyStatisticModel(facultystatisticEntity.Id, facultystatisticEntity.FacultyName,
                        facultystatisticEntity.GroupCountFact, facultystatisticEntity.GroupCountMust, facultystatisticEntity.DisciplineCountMust, facultystatisticEntity.GroupCountMust,
                        facultystatisticEntity.EReportDoneCount, facultystatisticEntity.EReportRevCount, facultystatisticEntity.PapReportDoneCount, facultystatisticEntity.PapReportRevCount,
                        facultystatisticEntity.AllDone);

                    return facultystatisticModel;
                })
                .ToList();

            return facultystatistics;
        }

        public async Task<FacultyStatisticModel> GetFacultyStatisticById(int facultystatisticId)
        {
            var facultystatisticEntity = await _context.FacultyStatistics.SingleOrDefaultAsync(d => d.Id == facultystatisticId);
            if (facultystatisticEntity == null) throw new Exception("FacultyStatistic с таким id не существует");
            FacultyStatisticModel facultystatistic = new FacultyStatisticModel(facultystatisticEntity.Id, facultystatisticEntity.FacultyName,
                        facultystatisticEntity.GroupCountFact, facultystatisticEntity.GroupCountMust, facultystatisticEntity.DisciplineCountMust, facultystatisticEntity.GroupCountMust,
                        facultystatisticEntity.EReportDoneCount, facultystatisticEntity.EReportRevCount, facultystatisticEntity.PapReportDoneCount, facultystatisticEntity.PapReportRevCount,
                        facultystatisticEntity.AllDone);
            return facultystatistic;
        }

        public async Task<FacultyStatisticModel> UpdateFacultyStatistic(FacultyStatisticModel facultystatisticModel)
        {
            var facultystatisticEntity = await _context.FacultyStatistics.SingleOrDefaultAsync(d => d.Id == facultystatisticModel.Id);
            if (facultystatisticEntity == null) throw new Exception("FacultyStatistic с таким id не существует");

            facultystatisticEntity.FacultyName = facultystatisticModel.FacultyName;
            _context.SaveChanges();
            var facultystatistic = new FacultyStatisticModel(facultystatisticEntity.Id, facultystatisticEntity.FacultyName,
                        facultystatisticEntity.GroupCountFact, facultystatisticEntity.GroupCountMust, facultystatisticEntity.DisciplineCountMust, facultystatisticEntity.GroupCountMust,
                        facultystatisticEntity.EReportDoneCount, facultystatisticEntity.EReportRevCount, facultystatisticEntity.PapReportDoneCount, facultystatisticEntity.PapReportRevCount,
                        facultystatisticEntity.AllDone);
            return facultystatistic;
        }

        public async Task<bool> DeleteFacultyStatistic(int facultystatisticId)
        {
            var facultystatisticEntity = await _context.FacultyStatistics.SingleOrDefaultAsync(d => d.Id == facultystatisticId);
            if (facultystatisticEntity == null) throw new Exception("FacultyStatistic с таким id не существует");
            _context.Remove(facultystatisticEntity);
            _context.SaveChanges();

            var facultystatistic = await _context.Faculties.SingleOrDefaultAsync(d => d.Id == facultystatisticId);
            if (facultystatistic == null) return true;
            else return false;
        }
    }
}
