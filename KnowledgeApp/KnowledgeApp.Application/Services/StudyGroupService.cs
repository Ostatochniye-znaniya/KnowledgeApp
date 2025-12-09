using KnowledgeApp.API.Contracts;
using KnowledgeApp.Core.Models;
using KnowledgeApp.DataAccess.Database.Entities;
using KnowledgeApp.DataAccess.Entities;
using KnowledgeApp.DataAccess.Repositories;

namespace KnowledgeApp.Application.Services
{
    public class StudyGroupService
    {
        private readonly StudyGroupRepository _repository;
        private readonly SemesterRepository _semesterRepository;

        public StudyGroupService(StudyGroupRepository repository, SemesterRepository semesterRepository)
        {
            _repository = repository;
            _semesterRepository = semesterRepository;
        }

        public async Task<List<StudyGroupFilteredRecommResponse>> GetGroupsWithTimelineAsync(
            int? facultyId,
            int? studyProgrammId)
        {
            var currentSemester = await _semesterRepository.GetCurrentSemesterAsync();
            var (currentYear, currentPart) = (currentSemester.SemesterYear, currentSemester.SemesterPart);
            var groups = await _repository.GetAllFilteredAsync(facultyId, studyProgrammId);
            var result = new List<StudyGroupFilteredRecommResponse>();

            foreach (var group in groups)
            {
                var recDict = group.RecommendationHistory
                    .GroupBy(r => new { r.Semester.SemesterYear, r.Semester.SemesterPart })
                    .ToDictionary(g => (g.Key.SemesterYear, g.Key.SemesterPart), g => g.First());

                var response = new StudyGroupFilteredRecommResponse
                {
                    Id = group.Id,
                    GroupNumber = group.GroupNumber,
                    Testings = GetSemesterRange(recDict, currentYear, currentPart, -1, 3),
                    TestingNow = GetSemesterTesting(recDict, currentYear, currentPart),
                    TestingsPlanned = GetSemesterRange(recDict, currentYear, currentPart, 1, 2)
                };

                result.Add(response);
            }

            return result;
        }

        private TestingForRecommendationResponse? GetSemesterTesting(
            Dictionary<(int Year, int Part), RecommendationHistory> dict,
            int year, int part)
        {
            if (dict.TryGetValue((year, part), out var rec))
            {
                return new TestingForRecommendationResponse
                {
                    Id = rec.Id,
                    SemesterYear = year,
                    SemesterPart = part,
                    IsChosen = rec.IsChosenForTesting
                    
                };
            }

            return null;
        }

        private List<TestingForRecommendationResponse> GetSemesterRange(
            Dictionary<(int Year, int Part), RecommendationHistory> dict,
            int startYear, int startPart, int direction, int count)
        {
            var result = new List<TestingForRecommendationResponse>();
            var (year, part) = (startYear, startPart);

            for (int i = 0; i < count; i++)
            {
                if (direction == -1) 
                    (year, part) = part == 2 ? (year, 1) : (year - 1, 2);
                else 
                    (year, part) = part == 2 ? (year + 1, 1) : (year, 2);

                result.Add(GetSemesterTesting(dict, year, part));
            }

            return result;
        }

        public async Task<IEnumerable<StudyGroup>> GetAllGroupsAsync()
        {
            return await _repository.GetAllAsync();
        }
        public async Task<IEnumerable<StudyGroup>> GetAllChosenGroupsAsync(int semId)
        {
            return await _repository.GetAllChosenAsync(semId);
        }
        public async Task<IEnumerable<StudyGroup>> GetAllRecommendedAsync(int semId, int? facultyId, int? studyProgrammId)
        {
            return await _repository.GetAllRecommendedAsync(semId, facultyId, studyProgrammId);
        }

        public async Task<StudyGroup?> GetGroupByIdAsync(int id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task CreateGroupAsync(StudyGroup studyGroup)
        {
            await _repository.AddAsync(studyGroup);
        }

        public async Task UpdateGroupAsync(StudyGroup studyGroup)
        {
            await _repository.UpdateAsync(studyGroup);
        }

        public async Task DeleteGroupAsync(int id)
        {
            await _repository.DeleteAsync(id);
        }

        public async Task<StudyGroupModel> CreateGroupAsync(StudyGroupModel studyGroupModel)
        {
            StudyGroupModel createdStudyGroupId = await _repository.CreateStudyGroup(studyGroupModel);

            return createdStudyGroupId;
        }
    }
}
