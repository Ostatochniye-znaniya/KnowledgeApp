using KnowledgeApp.Application.Services;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);
var services = builder.Services;

// CORS политика
Env.Load();
var envCandidates = new[]
{
    Path.Combine(builder.Environment.ContentRootPath, ".env"),
    Path.Combine(builder.Environment.ContentRootPath, "..", "..", ".env"),
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
};
foreach (var path in envCandidates)
{
    var full = Path.GetFullPath(path);
    if (File.Exists(full))
    {
        Env.Load(full);
        break;
    }
}

var allowedOrigins = Environment.GetEnvironmentVariable("CORS_ALLOWED_ORIGINS")
    ?.Split(',') ?? Array.Empty<string>();

services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin",
        policy => policy.WithOrigins(allowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod());
});

// Добавление контроллеров и Swagger
services.AddControllers();
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.AddServer(new OpenApiServer { Url = "/csh/api" });
});

// Добавление поддержки MVC Views
services.AddControllersWithViews();

// Регистрация DbContext
services.AddScoped<KnowledgeTestDbContext>();

// Регистрация сервисов и репозиториев
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

// Регистрация PDF сервиса
services.AddScoped<IPdfGeneratorService, PdfGeneratorService>();

var app = builder.Build();

// Миграции базы данных
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeTestDbContext>();
    dbContext.Database.Migrate();
}

// Настройка конвейера запросов
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

// app.UseHttpsRedirection(); // закомментировано для использования HTTP

app.UseCors("AllowAll");

// app.UseAuthorization(); // раскомментировать если нужно

app.UseStaticFiles();

// Маршрутизация для MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Schedule}/{action=Index}/{id?}");

// Маршрутизация для API контроллеров
app.MapControllers();

app.Run();