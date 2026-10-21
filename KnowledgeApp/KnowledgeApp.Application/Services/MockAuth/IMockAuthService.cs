using System.Security.Claims;
using KnowledgeApp.Application.DTOs.MockAuth;

namespace KnowledgeApp.Application.Services.MockAuth;

/// <summary>
/// Интерфейс сервиса MOCK-авторизации для всех ролей.
/// </summary>
public interface IMockAuthService
{
    /// <summary>
    /// Включена ли MOCK-авторизация в текущей конфигурации (.env).
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Роль по умолчанию из конфигурации.
    /// </summary>
    string DefaultRole { get; }

    /// <summary>
    /// Имя заголовка для передачи MOCK роли.
    /// </summary>
    string HeaderName { get; }

    /// <summary>
    /// Получить список всех доступных MOCK-пользователей для всех ролей.
    /// </summary>
    IReadOnlyList<MockUserProfile> GetAllMockUsers();

    /// <summary>
    /// Найти MOCK-пользователя по роли (принимает slug, русское или английское название, или ID роли).
    /// </summary>
    MockUserProfile? GetMockUser(string? roleIdentifier);

    /// <summary>
    /// Найти MOCK-пользователя по ID.
    /// </summary>
    MockUserProfile? GetMockUserById(int id);

    /// <summary>
    /// Создать ClaimsPrincipal для MOCK-пользователя с полным набором ролевых и профильных claims.
    /// </summary>
    ClaimsPrincipal CreatePrincipal(MockUserProfile user);
}
