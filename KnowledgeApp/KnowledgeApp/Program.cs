using KnowledgeApp.Application.Services;
using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<KnowledgeTestDbContext>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<DepartmentRepository>();
builder.Services.AddScoped<DisciplineRepository>();
builder.Services.AddScoped<DisciplineService>();
builder.Services.AddScoped<ReportRepository>();
builder.Services.AddScoped<ReportService>();
builder.Services.AddScoped<RoleRepository>();
builder.Services.AddScoped<RoleService>();
builder.Services.AddScoped<StatusService>();
builder.Services.AddScoped<StatusRepository>();
builder.Services.AddScoped<StudentRepository>();
builder.Services.AddScoped<StudentService>();
builder.Services.AddScoped<SemesterRepository>();
builder.Services.AddScoped<SemesterService>();
builder.Services.AddScoped<StudyGroupRepository>();
builder.Services.AddScoped<StudyProgramRepository>();
builder.Services.AddScoped<StudyGroupService>();
builder.Services.AddScoped<StudyProgramService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
