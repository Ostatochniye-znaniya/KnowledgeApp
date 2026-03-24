using KnowledgeApp.Application.Services;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

Env.Load();
var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?.Split(',') ?? Array.Empty<string>();

services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.AddServer(new OpenApiServer { Url = "/csh/api" });
});

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

//app.UseHttpsRedirection();

app.UseCors("AllowSpecificOrigin");
//app.UseAuthorization();

app.UsePathBase("/csh/api");
app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();
