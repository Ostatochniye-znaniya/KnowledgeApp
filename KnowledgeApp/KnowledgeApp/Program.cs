using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using KnowledgeApp.Application.Services;
using KnowledgeApp.DataAccess.Context;
using KnowledgeApp.DataAccess.Repositories;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddAuthorization();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            // указывает, будет ли валидироваться издатель при валидации токена
            ValidateIssuer = true,
            // строка, представляющая издателя
            ValidIssuer = AuthOptions.ISSUER,
            // будет ли валидироваться потребитель токена
            ValidateAudience = true,
            // установка потребителя токена
            ValidAudience = AuthOptions.AUDIENCE,
            // будет ли валидироваться время существования
            ValidateLifetime = true,
            // установка ключа безопасности
            IssuerSigningKey = AuthOptions.GetSymmetricSecurityKey(),
            // валидация ключа безопасности
            ValidateIssuerSigningKey = true,
         };
});
// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<KnowledgeTestDbContext>();
builder.Services.AddScoped<DepartmentService>();
builder.Services.AddScoped<DepartmentRepository>();
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<UserRepository>();
builder.Services.AddScoped<TestingService>();
builder.Services.AddScoped<TestingRepository>();
builder.Services.AddScoped<DisciplineRepository>();
builder.Services.AddScoped<DisciplineService>();
builder.Services.AddScoped<FacultyRepository>();
builder.Services.AddScoped<FacultyService>();
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
builder.Services.AddScoped<EmployeeRightsRequestRepository>();
builder.Services.AddScoped<EmployeeRightsRequestService>();
builder.Services.AddScoped<RecommendationHistoryRepository>();
builder.Services.AddScoped<RecommendationHistoryService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// app.Map("/login/{username}", (string username) => 
// {
//     var claims = new List<Claim>
// 	{
// 		new Claim(ClaimTypes.NameIdentifier, username),
// 		new Claim("departmentId", user)
// 	};
//     var jwt = new JwtSecurityToken(
//             issuer: AuthOptions.ISSUER,
//             audience: AuthOptions.AUDIENCE,
//             claims: claims,
//             expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(2)), // время действия 2 минуты
//             signingCredentials: new SigningCredentials(AuthOptions.GetSymmetricSecurityKey(), SecurityAlgorithms.HmacSha256));
            
//     return new JwtSecurityTokenHandler().WriteToken(jwt);
// });

app.Run();
// app.Use()
public class AuthOptions
{
    public const string ISSUER = "MyAuthServer"; // издатель токена
    public const string AUDIENCE = "MyAuthClient"; // потребитель токена
    const string KEY = "mysupersecret_secretsecretsecretkey!123";   // ключ для шифрации
    public static SymmetricSecurityKey GetSymmetricSecurityKey() => 
        new SymmetricSecurityKey(Encoding.UTF8.GetBytes(KEY));
}
