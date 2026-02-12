# Инструкции по созданию миграций для личного кабинета

## Новые таблицы
Добавлены следующие сущности:
1. **UserSessions** - активные сессии пользователей
2. **UserSettings** - настройки уведомлений и региональные параметры

## Команда для создания миграции

```bash
cd src/Infrastructure
dotnet ef migrations add AddUserProfileTables --startup-project ../Web
```

## Применение миграции

```bash
cd src/Infrastructure
dotnet ef database update --startup-project ../Web
```

## Альтернативный способ
Миграция будет применена автоматически при запуске приложения благодаря коду в Program.cs:

```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    try
    {
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
    }
}
```

## Структура таблиц

### UserSessions
- Id (int, PK)
- UserId (int, FK to Users)
- SessionId (string, unique)
- IpAddress (string)
- UserAgent (string)
- LastActivityAt (DateTime)
- ExpiresAt (DateTime)
- IsActive (bool)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)

### UserSettings
- Id (int, PK)
- UserId (int, FK to Users, unique)
- EmailNotificationsEnabled (bool)
- BotEventsNotifications (bool)
- PaymentNotifications (bool)
- NewsletterNotifications (bool)
- SecurityNotifications (bool)
- Language (string)
- TimeZone (string)
- CreatedAt (DateTime)
- UpdatedAt (DateTime)
