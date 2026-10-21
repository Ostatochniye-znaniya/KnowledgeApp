using KnowledgeApp.Application.Services;
using KnowledgeApp.Application.Interfaces;
using KnowledgeApp.Application.DTOs.MockAuth;
using KnowledgeApp.Application.Services.MockAuth;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Repositories;
using KnowledgeApp.API.Authentication;
using Microsoft.AspNetCore.Authentication;
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
    Path.Combine(builder.Environment.ContentRootPath, "..", ".env"),
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
    ?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries) ?? Array.Empty<string>();

services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigin", policy =>
    {
        if (allowedOrigins.Length > 0 && allowedOrigins[0] != "*")
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
    });
});

// Конфигурация MOCK авторизации из .env
var mockAuthEnvVal = Environment.GetEnvironmentVariable("MOCK_AUTH_ENABLED") 
    ?? Environment.GetEnvironmentVariable("ENABLE_MOCK_AUTH") 
    ?? builder.Configuration["MockAuth:Enabled"];

var isMockAuthEnabled = (mockAuthEnvVal ?? "false")
    .Equals("true", StringComparison.OrdinalIgnoreCase);

var defaultMockRole = Environment.GetEnvironmentVariable("MOCK_AUTH_DEFAULT_ROLE")
    ?? builder.Configuration["MockAuth:DefaultRole"]
    ?? "Admin";

var mockHeaderName = Environment.GetEnvironmentVariable("MOCK_AUTH_HEADER_NAME")
    ?? "X-Mock-Role";

var mockAuthOptions = new MockAuthOptions
{
    Enabled = isMockAuthEnabled,
    DefaultRole = defaultMockRole,
    HeaderName = mockHeaderName
};

services.AddSingleton(Microsoft.Extensions.Options.Options.Create(mockAuthOptions));
services.AddSingleton<IMockAuthService>(new MockAuthService(mockAuthOptions));

// Настройка схемы аутентификации и авторизации
services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = MockAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = MockAuthenticationDefaults.AuthenticationScheme;
})
.AddScheme<AuthenticationSchemeOptions, MockAuthenticationHandler>(
    MockAuthenticationDefaults.AuthenticationScheme, _ => { });

services.AddAuthorization();

// Добавление контроллеров и Swagger
services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

services.AddHttpClient(); // Для авторизации надо
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
try
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<KnowledgeTestDbContext>();
    dbContext.Database.Migrate();
}
catch (Exception ex)
{
    app.Logger.LogWarning(ex, "Не удалось применить миграции базы данных при запуске приложения.");
}

// Настройка конвейера запросов
app.UsePathBase("/csh/api");
app.UseRouting();

app.UseCors("AllowSpecificOrigin");

app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseStaticFiles();

// Маршрутизация для MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Schedule}/{action=Index}/{id?}");

// Маршрутизация для API контроллеров
app.MapControllers();

app.Run();