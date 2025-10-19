using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace KnowledgeApp.DataAccess.Database.Entities
{
    [Table("EmployeeRigthsRequests")]
    public class EmployeeRightsRequest
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string StructuralDivision { get; set; }
        public string JobName { get; set; }
        public DateOnly JobStart { get; set; }
        public DateOnly JobEnd { get; set; }
        public bool IsActive { get; set; }
        public string CategoryName { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime CreatedAt { get; set; }

    }
}
