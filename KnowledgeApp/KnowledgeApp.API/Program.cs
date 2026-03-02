using KnowledgeApp.Application.Services;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// Когда донастроим фронт
// services.AddCors(options =>
// {
//     options.AddPolicy("AllowSpecificOrigin",
//         policy => policy.WithOrigins("http://localhost:3000")
//                         .AllowAnyHeader()
//                         .AllowAnyMethod());
// });

// После донастройки убрать
services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        policy => policy.AllowAnyOrigin()
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen();

services.AddScoped<KnowledgeTestDbContext>();
services.AddScoped<DepartmentService>();
services.AddScoped<DepartmentRepository>();
services.AddScoped<UserService>();
services.AddScoped<UserRepository>();
services.AddScoped<TestingService>();
services.AddScoped<TestingRepository>();
services.AddScoped<DisciplineRepository>();
services.AddScoped<DisciplineService>();
services.AddScoped<FacultyRepository>();
services.AddScoped<FacultyService>();
services.AddScoped<ReportRepository>();
services.AddScoped<ReportService>();
services.AddScoped<RoleRepository>();
services.AddScoped<RoleService>();
services.AddScoped<StatusService>();
services.AddScoped<StatusRepository>();
services.AddScoped<StudentRepository>();
services.AddScoped<StudentService>();
services.AddScoped<SemesterRepository>();
services.AddScoped<SemesterService>();
services.AddScoped<StudyGroupRepository>();
services.AddScoped<StudyProgramRepository>();
services.AddScoped<StudyGroupService>();
services.AddScoped<StudyProgramService>();
services.AddScoped<EmployeeRightsRequestRepository>();
services.AddScoped<EmployeeRightsRequestService>();
services.AddScoped<RecommendationHistoryRepository>();
services.AddScoped<RecommendationHistoryService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
	var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeTestDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseCors("AllowAll");
//app.UseAuthorization();
app.MapControllers();

app.Run();
