# BotConstructor Platform

Платформа для создания чат-ботов для мессенджеров без программирования.

## Реализованный функционал (MVP 1.1)

### Email/Password Аутентификация ✅

Полностью реализована система аутентификации пользователей согласно ТЗ (раздел 18.1, пункт 1.1):

#### Основные возможности:
- ✅ **Регистрация** с валидацией пароля (минимум 8 символов, сложность)
- ✅ **Вход в систему** с защитой от брутфорса
- ✅ **Подтверждение email** через токен
- ✅ **Восстановление пароля** через email
- ✅ **Хеширование паролей** с использованием BCrypt (cost factor 12)
- ✅ **Логирование попыток входа** (успешных и неудачных)
- ✅ **Блокировка аккаунта** после 5 неудачных попыток на 15 минут
- ✅ **Ролевая модель** (SuperAdmin, Admin, User, Developer, Affiliate)

#### Технические детали:
- **Backend:** ASP.NET Core MVC 9.0
- **Database:** Entity Framework Core 9.0 + MySQL 8.0
- **Authentication:** Cookie-based с поддержкой "Запомнить меня"
- **Security:** BCrypt для хеширования паролей, Anti-CSRF токены
- **Email:** Сервис отправки email (заглушка для dev, готов к интеграции SMTP)

## Структура проекта

```
BotConstructor.Platform/
├── src/
│   ├── Core/                          # Бизнес-логика
│   │   ├── Entities/                  # Модели данных
│   │   │   ├── User.cs
│   │   │   ├── Role.cs
│   │   │   ├── UserRole.cs
│   │   │   ├── LoginAttempt.cs
│   │   │   ├── PasswordResetToken.cs
│   │   │   └── EmailConfirmationToken.cs
│   │   ├── Interfaces/                # Интерфейсы
│   │   │   ├── IAuthService.cs
│   │   │   ├── IEmailService.cs
│   │   │   └── IUserRepository.cs
│   │   └── DTOs/                      # Data Transfer Objects
│   │       ├── RegisterDto.cs
│   │       ├── LoginDto.cs
│   │       ├── ForgotPasswordDto.cs
│   │       └── ResetPasswordDto.cs
│   │
│   ├── Infrastructure/                # Реализация инфраструктуры
│   │   ├── Data/
│   │   │   └── ApplicationDbContext.cs
│   │   ├── Repositories/
│   │   │   └── UserRepository.cs
│   │   └── Services/
│   │       ├── AuthService.cs
│   │       └── EmailService.cs
│   │
│   └── Web/                           # MVC приложение
│       ├── Controllers/
│       │   └── AuthController.cs
│       ├── Views/
│       │   └── Auth/
│       │       ├── Register.cshtml
│       │       ├── Login.cshtml
│       │       ├── ConfirmEmail.cshtml
│       │       ├── ForgotPassword.cshtml
│       │       ├── ForgotPasswordConfirmation.cshtml
│       │       └── ResetPassword.cshtml
│       └── Program.cs
│
├── TZ_BotConstructor_FULL_1_.md       # Техническое задание
└── README.md                          # Этот файл
```

## Требования

- .NET 9.0 SDK
- MySQL 8.0+
- Linux/Windows/macOS

## Установка и запуск

### 1. Установка зависимостей

```bash
cd src
dotnet restore
```

### 2. Настройка базы данных

Создайте базу данных MySQL:

```sql
CREATE DATABASE botconstructor CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
```

### 3. Конфигурация

Отредактируйте `src/Web/appsettings.json` и укажите корректные данные для подключения к MySQL:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "server=localhost;port=3306;database=botconstructor;user=root;password=YOUR_PASSWORD;charset=utf8mb4;"
  }
}
```

### 4. Создание миграций и обновление БД

Миграции применяются автоматически при первом запуске приложения благодаря `db.Database.Migrate()` в `Program.cs`.

Либо можно применить миграции вручную:

```bash
cd src/Web
dotnet ef migrations add InitialCreate --project ../Infrastructure/BotConstructor.Infrastructure.csproj --startup-project BotConstructor.Web.csproj
dotnet ef database update --project ../Infrastructure/BotConstructor.Infrastructure.csproj --startup-project BotConstructor.Web.csproj
```

### 5. Запуск приложения

```bash
cd src/Web
dotnet run
```

Приложение будет доступно по адресу: `https://localhost:5001` или `http://localhost:5000`

## Использование

### Регистрация нового пользователя

1. Откройте `https://localhost:5001/Auth/Register`
2. Заполните форму регистрации:
   - Email
   - Пароль (минимум 8 символов, включая заглавные, строчные буквы, цифры и спецсимволы)
   - Подтверждение пароля
3. После регистрации на email будет отправлено письмо с ссылкой подтверждения (в dev режиме смотрите логи консоли)
4. Перейдите по ссылке подтверждения: `/Auth/ConfirmEmail?token=YOUR_TOKEN`

### Вход в систему

1. Откройте `https://localhost:5001/Auth/Login`
2. Введите email и пароль
3. Опционально: поставьте галочку "Запомнить меня" для автоматического входа на 30 дней

### Восстановление пароля

1. Откройте `https://localhost:5001/Auth/ForgotPassword`
2. Введите email
3. Перейдите по ссылке из письма (в dev режиме смотрите логи консоли)
4. Введите новый пароль

## Безопасность

### Реализованные меры безопасности:

- ✅ **BCrypt hashing** с cost factor 12 для паролей
- ✅ **Rate limiting** - блокировка на 15 минут после 5 неудачных попыток входа
- ✅ **Email confirmation** - обязательное подтверждение email перед входом
- ✅ **Secure cookies** - HttpOnly, Secure, SameSite=Lax
- ✅ **CSRF protection** - Anti-forgery tokens на всех формах
- ✅ **Password complexity** - валидация сложности пароля
- ✅ **Audit logging** - логирование всех попыток входа

### Токены:

- **Email confirmation token** - действителен 24 часа
- **Password reset token** - действителен 1 час
- **Session cookie** - 12 часов (или 30 дней с "Запомнить меня")

## Роли пользователей

Система поддерживает 5 ролей (seed данные создаются автоматически):

1. **SuperAdmin** - полный доступ к платформе
2. **Admin** - администратор
3. **User** - обычный пользователь (назначается по умолчанию при регистрации)
4. **Developer** - разработчик шаблонов
5. **Affiliate** - реферальщик

## Email в режиме разработки

В режиме разработки все email выводятся в консоль (логи).

Пример лога подтверждения email:

```
=== EMAIL SENT ===
To: user@example.com
Subject: Подтверждение email - BotConstructor
Body: [HTML content with confirmation link]
==================
```

Для продакшена необходимо настроить SMTP в `EmailService.cs`.

## Следующие шаги (по ТЗ)

Согласно плану разработки MVP (раздел 18.1), следующие пункты к реализации:

- [ ] **1.2** - Админка для управления пользователями
- [ ] **1.3** - Личный кабинет пользователя
- [ ] **1.4** - Тестирование модуля аутентификации
- [ ] **2.x** - Система тарифов и биллинг
- [ ] **3.x** - Конструктор ботов
- [ ] **4.x** - Telegram интеграция
- [ ] **5.x** - Базовая аналитика

## Технологии

- **Backend:** ASP.NET Core MVC 9.0, C# 12
- **ORM:** Entity Framework Core 9.0
- **Database:** MySQL 8.0+ (Pomelo.EntityFrameworkCore.MySql)
- **Authentication:** Cookie Authentication
- **Password Hashing:** BCrypt.Net-Next (cost 12)
- **Frontend:** Bootstrap 5, Razor Views
- **Validation:** Data Annotations + Model State

## Автор

Проект разработан согласно техническому заданию "BotConstructor Platform v1.0"

## Лицензия

Proprietary
