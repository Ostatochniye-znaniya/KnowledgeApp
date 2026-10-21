namespace KnowledgeApp.Application.DTOs.MockAuth;

/// <summary>
/// Опции конфигурации MOCK авторизации, загружаемые из .env / appsettings.json.
/// </summary>
public class MockAuthOptions
{
    /// <summary>
    /// Флаг включения MOCK-авторизации (env: MOCK_AUTH_ENABLED или ENABLE_MOCK_AUTH).
    /// Если false — MOCK авторизация отключена, применяется стандартный режим.
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Роль по умолчанию, если в запросе не передана конкретная роль (env: MOCK_AUTH_DEFAULT_ROLE).
    /// По умолчанию: Admin.
    /// </summary>
    public string DefaultRole { get; set; } = "Admin";

    /// <summary>
    /// Заголовок для передачи роли (env: MOCK_AUTH_HEADER_NAME).
    /// По умолчанию: X-Mock-Role.
    /// </summary>
    public string HeaderName { get; set; } = "X-Mock-Role";
}
