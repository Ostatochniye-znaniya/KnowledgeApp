using System;
using System.Collections.Generic;
using KnowledgeApp.DataAccess.Entities;
using Microsoft.EntityFrameworkCore;
using Pomelo.EntityFrameworkCore.MySql.Scaffolding.Internal;

namespace KnowledgeApp.DataAccess.Context;

public partial class KnowledgeTestDbContext : DbContext
{
    public KnowledgeTestDbContext()
    {
    }

    public KnowledgeTestDbContext(DbContextOptions<KnowledgeTestDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Department> Departments { get; set; }

    public virtual DbSet<Discipline> Disciplines { get; set; }

    public virtual DbSet<DisciplineTeacher> DisciplineTeachers { get; set; }

    public virtual DbSet<Faculty> Faculties { get; set; }

    public virtual DbSet<Report> Reports { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Semester> Semesters { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudyGroup> StudyGroups { get; set; }

    public virtual DbSet<StudyProgram> StudyPrograms { get; set; }

    public virtual DbSet<Testing> Testings { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see https://go.microsoft.com/fwlink/?LinkId=723263.
        => optionsBuilder.UseMySql("server=localhost;database=knowledge_test_db;user=root;password=admin", Microsoft.EntityFrameworkCore.ServerVersion.Parse("9.1.0-mysql"));

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        modelBuilder.Entity<Department>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("departments");

            entity.HasIndex(e => e.FacultyId, "faculty_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FacultyId).HasColumnName("faculty_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.Faculty).WithMany(p => p.Departments)
                .HasForeignKey(d => d.FacultyId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("departments_ibfk_1");
        });

        modelBuilder.Entity<Discipline>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("disciplines");

            entity.HasIndex(e => e.DepartmentId, "department_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.Department).WithMany(p => p.Disciplines)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("disciplines_ibfk_1");
        });

        modelBuilder.Entity<DisciplineTeacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("discipline_teacher");

            entity.HasIndex(e => e.DisciplineId, "discipline_id");

            entity.HasIndex(e => e.ResponsibleTeacherId, "responsible_teacher_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.Property(e => e.ResponsibleTeacherId).HasColumnName("responsible_teacher_id");

            entity.HasOne(d => d.Discipline).WithMany(p => p.DisciplineTeachers)
                .HasForeignKey(d => d.DisciplineId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("discipline_teacher_ibfk_1");

            entity.HasOne(d => d.ResponsibleTeacher).WithMany(p => p.DisciplineTeachers)
                .HasForeignKey(d => d.ResponsibleTeacherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("discipline_teacher_ibfk_2");
        });

        modelBuilder.Entity<Faculty>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("faculties");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.FacultyName)
                .HasMaxLength(255)
                .HasColumnName("faculty_name");
        });

        modelBuilder.Entity<Report>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("reports");

            entity.HasIndex(e => e.DisciplineId, "discipline_id");

            entity.HasIndex(e => e.TeacherId, "teacher_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AllDone).HasColumnName("all_done");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.Property(e => e.DoneInElectronicForm).HasColumnName("done_in_electronic_form");
            entity.Property(e => e.DoneInPaperForm).HasColumnName("done_in_paper_form");
            entity.Property(e => e.FilePath)
                .HasMaxLength(255)
                .HasColumnName("file_path");
            entity.Property(e => e.IsCorrect).HasColumnName("is_correct");
            entity.Property(e => e.ResultOfAttestation)
                .HasColumnType("text")
                .HasColumnName("result_of_attestation");
            entity.Property(e => e.TeacherId).HasColumnName("teacher_id");

            entity.HasOne(d => d.Discipline).WithMany(p => p.Reports)
                .HasForeignKey(d => d.DisciplineId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("reports_ibfk_1");

            entity.HasOne(d => d.Teacher).WithMany(p => p.Reports)
                .HasForeignKey(d => d.TeacherId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("reports_ibfk_2");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(255)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Semester>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("semesters");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.SemesterName)
                .HasMaxLength(255)
                .HasColumnName("semester_name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("statuses");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.StatusName)
                .HasMaxLength(255)
                .HasColumnName("status_name");
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("students");

            entity.HasIndex(e => e.GroupId, "group_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Group).WithMany(p => p.Students)
                .HasForeignKey(d => d.GroupId)
                .HasConstraintName("students_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.Students)
                .HasForeignKey(d => d.UserId)
                .HasConstraintName("students_ibfk_1");
        });

        modelBuilder.Entity<StudyGroup>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("study_groups");

            entity.HasIndex(e => e.StudyProgramId, "study_program_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GroupNumber)
                .HasMaxLength(50)
                .HasColumnName("group_number");
            entity.Property(e => e.StudyProgramId).HasColumnName("study_program_id");

            entity.HasOne(d => d.StudyProgram).WithMany(p => p.StudyGroups)
                .HasForeignKey(d => d.StudyProgramId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("study_groups_ibfk_1");
        });

        modelBuilder.Entity<StudyProgram>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("study_programs");

            entity.HasIndex(e => e.DepartmentId, "department_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CypherOfTheDirection)
                .HasMaxLength(255)
                .HasColumnName("cypher_of_the_direction");
            entity.Property(e => e.DepartmentId).HasColumnName("department_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");

            entity.HasOne(d => d.Department).WithMany(p => p.StudyPrograms)
                .HasForeignKey(d => d.DepartmentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("study_programs_ibfk_1");
        });

        modelBuilder.Entity<Testing>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("testing");

            entity.HasIndex(e => e.DisciplineId, "discipline_id");

            entity.HasIndex(e => e.GroupId, "group_id");

            entity.HasIndex(e => e.ReportId, "report_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DisciplineId).HasColumnName("discipline_id");
            entity.Property(e => e.GroupId).HasColumnName("group_id");
            entity.Property(e => e.ReportId).HasColumnName("report_id");
            entity.Property(e => e.ResultOfTesting)
                .HasColumnType("text")
                .HasColumnName("result_of_testing");
            entity.Property(e => e.ScheduledDate).HasColumnName("scheduled_date");
            entity.Property(e => e.ScheduledTime)
                .HasColumnType("time")
                .HasColumnName("scheduled_time");
            entity.Property(e => e.Status)
                .HasMaxLength(50)
                .HasColumnName("status");

            entity.HasOne(d => d.Discipline).WithMany(p => p.Testings)
                .HasForeignKey(d => d.DisciplineId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("testing_ibfk_2");

            entity.HasOne(d => d.Group).WithMany(p => p.Testings)
                .HasForeignKey(d => d.GroupId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("testing_ibfk_1");

            entity.HasOne(d => d.Report).WithMany(p => p.Testings)
                .HasForeignKey(d => d.ReportId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("testing_ibfk_3");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.Email, "email").IsUnique();

            entity.HasIndex(e => e.FacultyId, "faculty_id");

            entity.HasIndex(e => e.StatusId, "status_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.FacultyId).HasColumnName("faculty_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.Password)
                .HasMaxLength(255)
                .HasColumnName("password");
            entity.Property(e => e.StatusId).HasColumnName("status_id");

            entity.HasOne(d => d.Faculty).WithMany(p => p.Users)
                .HasForeignKey(d => d.FacultyId)
                .HasConstraintName("users_ibfk_2");

            entity.HasOne(d => d.Status).WithMany(p => p.Users)
                .HasForeignKey(d => d.StatusId)
                .HasConstraintName("users_ibfk_1");
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("user_role");

            entity.HasIndex(e => e.RoleId, "role_id");

            entity.HasIndex(e => e.UserId, "user_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Role).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_role_ibfk_2");

            entity.HasOne(d => d.User).WithMany(p => p.UserRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("user_role_ibfk_1");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
