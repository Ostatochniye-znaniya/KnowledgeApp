using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Infrastructure.Context;

public partial class KnowledgeTestDbContext
{
    public virtual DbSet<TestingPlanningState> TestingPlanningStates { get; set; }

    private static void ConfigureTestingPlanning(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecommendationHistory>(entity =>
        {
            entity.ToTable("recommendation_history", t => t.HasCheckConstraint(
                "CK_recommendation_history_author", "`author` IN ('automatic', 'manual')"));
            entity.Property(e => e.Author).HasColumnName("author").HasMaxLength(16).HasDefaultValue("manual").IsRequired();
            entity.Property(e => e.RecommendedAt).HasColumnName("recommended_at").HasColumnType("datetime(6)");
            entity.HasIndex(e => new { e.StudyGroupId, e.SemesterId }).IsUnique()
                .HasDatabaseName("UX_recommendation_history_group_semester");
            entity.HasOne(e => e.Semester).WithMany().HasForeignKey(e => e.SemesterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.StudyGroup).WithMany().HasForeignKey(e => e.StudyGroupId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.User).WithMany().HasForeignKey(e => e.UserId).OnDelete(DeleteBehavior.SetNull);
        });
        modelBuilder.Entity<Testing>(entity =>
        {
            entity.Property(e => e.RecommendationHistoryId).HasColumnName("recommendation_history_id");
            entity.HasOne(e => e.RecommendationHistory).WithMany(e => e.Testings)
                .HasForeignKey(e => e.RecommendationHistoryId).OnDelete(DeleteBehavior.Restrict);
        });
        modelBuilder.Entity<TestingPlanningState>(entity =>
        {
            entity.ToTable("testing_planning_state", t => t.HasCheckConstraint(
                "CK_testing_planning_state_singleton", "`id` = 1"));
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
            entity.Property(e => e.CurrentSemesterId).HasColumnName("current_semester_id");
            entity.Property(e => e.InitializedAt).HasColumnName("initialized_at").HasColumnType("datetime(6)");
            entity.Property(e => e.Version).HasColumnName("version").IsConcurrencyToken();
            entity.HasOne<Semester>().WithMany().HasForeignKey(e => e.CurrentSemesterId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasData(new TestingPlanningState { Id = 1, Version = 0 });
        });
    }
}
