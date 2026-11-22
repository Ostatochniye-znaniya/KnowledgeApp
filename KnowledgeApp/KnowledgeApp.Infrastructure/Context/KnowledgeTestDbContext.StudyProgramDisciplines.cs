using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Context;

public partial class KnowledgeTestDbContext
{
    public virtual DbSet<StudyProgramDiscipline> StudyProgramDisciplines { get; set; }

    private static void ConfigureStudyProgramDisciplines(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<StudyProgramDiscipline>(entity =>
        {
            entity.ToTable("study_program_disciplines");
            entity.HasKey(e => new { e.StudyProgramId, e.DisciplineId });
            entity.Property(e => e.StudyProgramId).HasColumnName("study_program_id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.HasOne(e => e.StudyProgram).WithMany(e => e.StudyProgramDisciplines)
                .HasForeignKey(e => e.StudyProgramId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Discipline).WithMany(e => e.StudyProgramDisciplines)
                .HasForeignKey(e => e.DisciplineId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
