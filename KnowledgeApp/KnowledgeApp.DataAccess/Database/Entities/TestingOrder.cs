using KnowledgeApp.DataAccess.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KnowledgeApp.DataAccess.Database.Entities
{
    public class TestingOrder
    {
        public int Id { get; set; }
        public DateOnly OrderDate { get; set; }
        public int Number {  get; set; }
        public string EducationControlEmployeeName { get; set; }
        public string EducationMethodEmployeeName { get; set; }
        public DateOnly TestingSummaryReportUpTo { get; set; }
        public DateOnly QuestionnaireSummaryReportUpTo { get; set; }
        public DateOnly PaperReportUpTo { get; set; }
        public DateOnly DigitalReportUpTo { get; set; }

        public int SemesterId { get; set; }
        public Semester Semester { get; set; }
    }
}
