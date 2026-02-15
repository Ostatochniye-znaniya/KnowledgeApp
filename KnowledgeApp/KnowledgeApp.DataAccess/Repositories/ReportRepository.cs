using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.Core.Models;
using Microsoft.EntityFrameworkCore;
using System.Xml.Linq;

namespace KnowledgeApp.DataAccess.Repositories
{
    public class ReportRepository
    {
        private readonly KnowledgeTestDbContext _context;

        public ReportRepository(KnowledgeTestDbContext context)
        {
            _context = context;
        }

        public async Task<ReportModel> CreateReport(ReportModel reportModel)
        {
            Report reportEntity = new Report
            {
                DisciplineId = reportModel.DisciplineId,
                TeacherId = reportModel.TeacherId,
                FilePath = reportModel.FilePath,
                IsCorrect = reportModel.IsCorrect,
                DoneInPaperForm = reportModel.DoneInPaperForm,
                DoneInElectronicForm = reportModel.DoneInElectronicForm,
                ResultOfAttestation = reportModel.ResultOfAttestation,
                AllDone = reportModel.AllDone
            };

            await _context.Reports.AddAsync(reportEntity);
            await _context.SaveChangesAsync();

            ReportModel createdReport = new ReportModel(reportEntity.DisciplineId, reportEntity.TeacherId, reportEntity.FilePath, reportEntity.IsCorrect, reportEntity.ResultOfAttestation, reportEntity.DoneInPaperForm, reportEntity.DoneInElectronicForm, reportEntity.AllDone);
            return createdReport;
        }
        public async Task<List<ReportModel>> GetAllReports()
        {
            var reportEntities = await _context.Reports
                .AsNoTracking()
                .ToListAsync();

            var reports = reportEntities
                .Select(reportEntity =>
                {
                    var reportModel = new ReportModel(
                        reportEntity.Id,
                        reportEntity.DisciplineId,
                        reportEntity.TeacherId,
                        reportEntity.FilePath,
                        reportEntity.IsCorrect,
                        reportEntity.ResultOfAttestation,
                        reportEntity.DoneInPaperForm,
                        reportEntity.DoneInElectronicForm,
                        reportEntity.AllDone);

                    return reportModel;
                })
                .ToList();

            return reports;
        }

        public async Task<ReportModel> GetReportById(int reportId)
        {
            var reportEntity = await _context.Reports.SingleOrDefaultAsync(r => r.Id == reportId);
            if (reportEntity == null) throw new Exception("Report с таким id не существует");
            ReportModel report = new ReportModel(
                        reportEntity.Id,
                        reportEntity.DisciplineId,
                        reportEntity.TeacherId,
                        reportEntity.FilePath,
                        reportEntity.IsCorrect,
                        reportEntity.ResultOfAttestation,
                        reportEntity.DoneInPaperForm,
                        reportEntity.DoneInElectronicForm,
                        reportEntity.AllDone);
            
            return report;
        }

        public async Task<ReportModel> UpdateReport(ReportModel reportModel)
        {
            var reportEntity = await _context.Reports.SingleOrDefaultAsync(d => d.Id == reportModel.Id);
            if (reportEntity == null) throw new Exception("Report с таким id не существует");

            //var discipline = await _context.Disciplines.SingleOrDefaultAsync(d => d.Id == reportModel.DisciplineId);
            //var teacher = await _context.Users.SingleOrDefaultAsync(t => t.Id == reportModel.TeacherId);

            //if (discipline == null) throw new Exception("Дисциплины с таким id не существует");
            //if (teacher == null) throw new Exception("Преподавателя с таким id не существует");

            reportEntity.DisciplineId = reportModel.DisciplineId.GetValueOrDefault();
            reportEntity.TeacherId = reportEntity.TeacherId;
            reportEntity.FilePath = reportModel.FilePath;
            reportEntity.IsCorrect = reportModel.IsCorrect;
            reportEntity.ResultOfAttestation = reportModel.ResultOfAttestation;
            reportEntity.DoneInPaperForm = reportModel.DoneInPaperForm;
            reportEntity.DoneInElectronicForm = reportModel.DoneInElectronicForm;
            reportEntity.AllDone = reportModel.AllDone;

            await _context.SaveChangesAsync();
            var report = new ReportModel(
                        reportEntity.Id,
                        reportEntity.DisciplineId,
                        reportEntity.TeacherId,
                        reportEntity.FilePath,
                        reportEntity.IsCorrect,
                        reportEntity.ResultOfAttestation,
                        reportEntity.DoneInPaperForm,
                        reportEntity.DoneInElectronicForm,
                        reportEntity.AllDone);
            return report;
        }

        public async Task<bool> DeleteReport(int reportId)
        {
            var reportEntity = await _context.Reports.SingleOrDefaultAsync(d => d.Id == reportId);
            if (reportEntity == null) throw new Exception("Report с таким id не существует");
            _context.Remove(reportEntity);
            _context.SaveChanges();

            var report = await _context.Reports.SingleOrDefaultAsync(d => d.Id == reportId);
            if (report == null) return true;
            else return false;
        }
    }
}

