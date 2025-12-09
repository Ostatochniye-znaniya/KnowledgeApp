using KnowledgeApp.DataAccess.Repositories;
using KnowledgeApp.Core.Models;

namespace KnowledgeApp.Application.Services
{
    public class SemesterService
    {
        private readonly SemesterRepository _semesterRepository;

        public SemesterService(SemesterRepository semesterRepository)
        {
            _semesterRepository = semesterRepository;
        }

        public async Task<List<SemesterModel>> GetAll()
        {
            List<SemesterModel> semesters = await _semesterRepository.GetAllSemesters();
            return semesters;
        }

        public async Task<SemesterModel> GetSemesterById(int semesterId)
        {
            SemesterModel semester = await _semesterRepository.GetSemesterById(semesterId);
            return semester;
        }
        public async Task<SemesterModel> Get—urrentSemester()
        {
            SemesterModel semester = await _semesterRepository.GetCurrentSemesterAsync();
            return semester;
        }
        public async Task<List<SemesterModel>> GetSemestersTimeline(int fromYear, int fromPart, int toYear, int toPart)
        {
            List<SemesterModel> raw = await _semesterRepository
                .GetSemestersTimeline(fromYear, fromPart, toYear, toPart);

            raw = raw
                .GroupBy(s => new { s.SemesterYear, s.SemesterPart })
                .Select(g => g.First())
                .OrderBy(s => s.SemesterYear)
                .ThenBy(s => s.SemesterPart)
                .ToList();

            List<SemesterModel> result = new();

            int y = fromYear;
            int p = fromPart;

            int index = 0;

            while (y < toYear || (y == toYear && p <= toPart))
            {
                SemesterModel? existing = null;
                if (index < raw.Count &&
                    raw[index].SemesterYear == y &&
                    raw[index].SemesterPart == p)
                {
                    existing = raw[index];
                    index++;
                }

                if (existing != null)
                    result.Add(existing);
                else
                    result.Add(new SemesterModel(0, y, p));
                if (p == 1)
                    p = 2;
                else
                {
                    p = 1;
                    y++;
                }
            }

            return result;
        }



        public async Task<SemesterModel> CreateSemester(SemesterModel semesterModel)
        {
            SemesterModel createdSemesterId = await _semesterRepository.CreateSemester(semesterModel);
            return createdSemesterId;
        }

        public async Task<SemesterModel> UpdateSemester(SemesterModel semesterModel)
        {
            SemesterModel updatedSemesterModel = await _semesterRepository.UpdateSemester(semesterModel);
            return updatedSemesterModel;
        }

        public async Task<bool> DeleteSemester(int semesterId)
        {
            bool result = await _semesterRepository.DeleteSemester(semesterId);
            return result;
        }
    }
}