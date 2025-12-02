using System;

namespace KnowledgeApp.Core.Models
{
    public class TestingOrderModel
    {
        public TestingOrderModel() { }
        public TestingOrderModel(
            int id,
            DateOnly orderDate,
            int number,
            string educationControlEmployeeName,
            string educationMethodEmployeeName,
            DateOnly testingSummaryReportUpTo,
            DateOnly questionnaireSummaryReportUpTo,
            DateOnly paperReportUpTo,
            DateOnly digitalReportUpTo,
            int semesterId)
        {
            Id = id;
            OrderDate = orderDate;
            Number = number;
            EducationControlEmployeeName = educationControlEmployeeName;
            EducationMethodEmployeeName = educationMethodEmployeeName;
            TestingSummaryReportUpTo = testingSummaryReportUpTo;
            QuestionnaireSummaryReportUpTo = questionnaireSummaryReportUpTo;
            PaperReportUpTo = paperReportUpTo;
            DigitalReportUpTo = digitalReportUpTo;
            SemesterId = semesterId;
        }

        public int Id { get; set; }
        public DateOnly OrderDate { get; set; }
        public int Number { get; set; }
        public string EducationControlEmployeeName { get; set; }
        public string EducationMethodEmployeeName { get; set; }
        public DateOnly TestingSummaryReportUpTo { get; set; }
        public DateOnly QuestionnaireSummaryReportUpTo { get; set; }
        public DateOnly PaperReportUpTo { get; set; }
        public DateOnly DigitalReportUpTo { get; set; }
        public int SemesterId { get; set; }
    }
}