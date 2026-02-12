using BotConstructor.Core.Interfaces;
using BotConstructor.Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace BotConstructor.Tests.Integration;

/// <summary>
/// Фабрика для создания тестового веб-приложения
/// Используется в Integration-тестах для имитации реального окружения
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Удаление реальной базы данных
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Добавление InMemory базы данных для тестов
            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase("TestDatabase");
            });

            // Замена EmailService на mock, чтобы не отправлять реальные email при тестах
            var emailServiceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(IEmailService));

            if (emailServiceDescriptor != null)
            {
                services.Remove(emailServiceDescriptor);
            }

            // Добавление mock для IEmailService
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(x => x.SendEmailConfirmationAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            mockEmailService
                .Setup(x => x.SendWelcomeEmailAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            mockEmailService
                .Setup(x => x.SendPasswordResetAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>()))
                .ReturnsAsync(true);

            services.AddSingleton(mockEmailService.Object);

            // Инициализация базы данных
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();

            db.Database.EnsureCreated();
        });
    }
}
