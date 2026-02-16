using BotConstructor.Core.Interfaces;
using BotConstructor.Infrastructure.Data;
using BotConstructor.Infrastructure.Repositories;
using BotConstructor.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(12);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    // Temporary external auth cookie used during the OAuth callback round-trip
    .AddCookie("Cookies.External", options =>
    {
        options.Cookie.Name = ".BotConstructor.External";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
        options.Cookie.SameSite = SameSiteMode.Lax;
    })
    // Google OAuth
    .AddGoogle(options =>
    {
        options.SignInScheme = "Cookies.External";
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? string.Empty;
        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = false;
    })
    // VKontakte OAuth (manual AddOAuth — no extra package required)
    .AddOAuth("VKontakte", options =>
    {
        options.SignInScheme = "Cookies.External";
        options.ClientId = builder.Configuration["Authentication:VK:ClientId"] ?? string.Empty;
        options.ClientSecret = builder.Configuration["Authentication:VK:ClientSecret"] ?? string.Empty;
        options.CallbackPath = "/signin-vkontakte";
        options.AuthorizationEndpoint = "https://oauth.vk.com/authorize";
        options.TokenEndpoint = "https://oauth.vk.com/access_token";
        options.Scope.Add("email");
        options.SaveTokens = false;

        options.Events = new OAuthEvents
        {
            OnCreatingTicket = async context =>
            {
                // VK returns user_id and email directly in the access-token response
                var tokenResponse = context.TokenResponse.Response?.RootElement;

                var userId = tokenResponse?.TryGetProperty("user_id", out var uid) == true
                    ? uid.ToString()
                    : string.Empty;

                var email = tokenResponse?.TryGetProperty("email", out var emailProp) == true
                    ? emailProp.GetString()
                    : null;

                if (!string.IsNullOrEmpty(userId))
                    context.Identity!.AddClaim(new Claim(ClaimTypes.NameIdentifier, userId));

                if (!string.IsNullOrEmpty(email))
                    context.Identity!.AddClaim(new Claim(ClaimTypes.Email, email));

                // Fetch first/last name from the VK API
                if (!string.IsNullOrEmpty(userId) && !string.IsNullOrEmpty(context.AccessToken))
                {
                    var apiUrl = $"https://api.vk.com/method/users.get?user_ids={userId}" +
                                 $"&fields=first_name,last_name" +
                                 $"&access_token={context.AccessToken}&v=5.131";

                    var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
                    var response = await context.Backchannel.SendAsync(
                        request, context.HttpContext.RequestAborted);

                    if (response.IsSuccessStatusCode)
                    {
                        var json = await response.Content.ReadAsStringAsync();
                        using var doc = JsonDocument.Parse(json);
                        if (doc.RootElement.TryGetProperty("response", out var arr)
                            && arr.GetArrayLength() > 0)
                        {
                            var info = arr[0];
                            if (info.TryGetProperty("first_name", out var fn))
                                context.Identity!.AddClaim(
                                    new Claim(ClaimTypes.GivenName, fn.GetString() ?? string.Empty));
                            if (info.TryGetProperty("last_name", out var ln))
                                context.Identity!.AddClaim(
                                    new Claim(ClaimTypes.Surname, ln.GetString() ?? string.Empty));
                        }
                    }
                }
            }
        };
    });

builder.Services.AddAuthorization();

// Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register repositories and services
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IProfileService, ProfileService>();
builder.Services.AddSingleton<IEmailService, EmailService>();

var app = builder.Build();

// Run migrations automatically (for development)
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

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();

public partial class Program { }
