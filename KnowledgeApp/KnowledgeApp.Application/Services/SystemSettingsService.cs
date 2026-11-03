using System.Net.Mail;
using KnowledgeApp.Application.DTOs.Admin;
using KnowledgeApp.Application.Exceptions;
using KnowledgeApp.Infrastructure.Context;
using KnowledgeApp.Infrastructure.Entities;
using Microsoft.EntityFrameworkCore;

namespace KnowledgeApp.Application.Services;

public sealed class SystemSettingsService
{
    private const int SettingsId = 1;
    private const int MaxTextLength = 4000;
    private const string DefaultWelcomeText =
        "Добро пожаловать в систему проверки остаточных знаний. Доступ к разделам выдаёт администратор.";

    private readonly KnowledgeTestDbContext _context;

    public SystemSettingsService(KnowledgeTestDbContext context)
    {
        _context = context;
    }

    public async Task<SystemSettingsDto> Get(CancellationToken cancellationToken)
    {
        var settings = await _context.SystemSettings.AsNoTracking()
            .SingleOrDefaultAsync(s => s.Id == SettingsId, cancellationToken);
        return settings is null
            ? new SystemSettingsDto(null, null, DefaultWelcomeText, null, null, null, null)
            : ToDto(settings);
    }

    public async Task<PublicSettingsDto> GetPublic(CancellationToken cancellationToken)
    {
        var settings = await Get(cancellationToken);
        return new PublicSettingsDto(
            settings.SupportEmail,
            settings.SupportPhone,
            settings.WelcomeText,
            settings.AccessRequestInstructions);
    }

    public async Task<SystemSettingsDto> Update(
        UpdateSystemSettingsCommand command,
        AdminActor actor,
        CancellationToken cancellationToken)
    {
        var supportEmail = Normalize(command.SupportEmail);
        if (supportEmail is null)
            throw new DocumentValidationException("Укажите почту техподдержки.");
        if (supportEmail.Length > 255 ||
            !MailAddress.TryCreate(supportEmail, out var address) ||
            address.Address != supportEmail)
            throw new DocumentValidationException("Укажите корректную почту техподдержки.");

        var supportPhone = Normalize(command.SupportPhone);
        if (supportPhone?.Length > 32)
            throw new DocumentValidationException("Телефон техподдержки длиннее 32 символов.");

        var welcomeText = Text(command.WelcomeText, "Текст для новых пользователей");
        var instructions = Text(command.AccessRequestInstructions, "Инструкция по запросу доступа");
        var template = Text(command.NotificationTemplate, "Шаблон уведомления");
        var actorName = await AdminActors.NameOf(_context, actor, cancellationToken);

        await _context.InTransactionAsync(async () =>
        {
            var settings = await _context.SystemSettings.SingleOrDefaultAsync(s => s.Id == SettingsId, cancellationToken);
            var previous = settings is null
                ? new SystemSettingsDto(null, null, DefaultWelcomeText, null, null, null, null)
                : ToDto(settings);
            if (settings is null)
            {
                settings = new SystemSettings { Id = SettingsId };
                _context.SystemSettings.Add(settings);
            }

            settings.SupportEmail = supportEmail;
            settings.SupportPhone = supportPhone;
            settings.WelcomeText = welcomeText;
            settings.AccessRequestInstructions = instructions;
            settings.NotificationTemplate = template;
            settings.UpdatedAt = DateTime.UtcNow;
            settings.UpdatedByUserId = actor.UserId;
            settings.UpdatedByName = actorName;

            var changes = Changes(previous, ToDto(settings));
            if (changes.Count > 0)
            {
                await AuditTrail.Add(
                    _context,
                    actor,
                    AuditCatalog.SettingsUpdate,
                    AuditCatalog.EntitySettings,
                    SettingsId,
                    "Общие настройки",
                    string.Join("; ", changes.Select(c => $"{c.Field}: {c.Previous ?? "—"}")),
                    string.Join("; ", changes.Select(c => $"{c.Field}: {c.Current ?? "—"}")),
                    null,
                    cancellationToken);
            }
            await _context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return await Get(cancellationToken);
    }

    private static List<(string Field, string? Previous, string? Current)> Changes(
        SystemSettingsDto previous,
        SystemSettingsDto current)
    {
        var fields = new (string Field, string? Previous, string? Current)[]
        {
            ("Почта техподдержки", previous.SupportEmail, current.SupportEmail),
            ("Телефон техподдержки", previous.SupportPhone, current.SupportPhone),
            ("Текст для новых пользователей", previous.WelcomeText, current.WelcomeText),
            ("Инструкция по запросу доступа", previous.AccessRequestInstructions, current.AccessRequestInstructions),
            ("Шаблон уведомления", previous.NotificationTemplate, current.NotificationTemplate)
        };
        return fields.Where(f => f.Previous != f.Current).ToList();
    }

    private static SystemSettingsDto ToDto(SystemSettings settings) =>
        new(
            settings.SupportEmail,
            settings.SupportPhone,
            settings.WelcomeText,
            settings.AccessRequestInstructions,
            settings.NotificationTemplate,
            settings.UpdatedAt == default ? null : DocumentStatusInfo.AsUtc(settings.UpdatedAt),
            settings.UpdatedByName);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Text(string? value, string field)
    {
        var normalized = Normalize(value);
        if (normalized?.Length > MaxTextLength)
            throw new DocumentValidationException($"{field}: не длиннее {MaxTextLength} символов.");
        return normalized;
    }
}
