using System.Reflection;
using System.Text;
using BottleGameServer.Authorization;
using BottleGameServer.Data;
using BottleGameServer.Processing;
using BottleGameServer.Processing.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Логгирование
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Transaction", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

// Подключение базы данных
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgresConnection")));

// Добавляем сервисы аутентификации
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["JWT:Issuer"],
            ValidateAudience = true,
            ValidAudience = builder.Configuration["JWT:Audience"],
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Key"]!)),
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireRole("Admin"));
    options.AddPolicy("Moderator", policy => policy.RequireRole("Moderator"));
    options.AddPolicy("Player", policy => policy.RequireRole("Player"));
});

// Сервисы
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<ITelegramAuthService, TelegramAuthService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();

builder.Services.Configure<ProcessingOptions>(
    builder.Configuration.GetSection("Processing"));
builder.Services.AddHttpClient<IProcessingService, ProcessingService>();

// MVC + Контроллеры
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Bottle Game API", Version = "v1" });

    // Безопасное включение XML-комментариев
    try
    {
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);

        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
        else
        {
            // Логируем предупреждение, но не падаем
            Console.WriteLine($"XML documentation file not found: {xmlPath}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error loading XML documentation: {ex.Message}");
    }

    // Настройка Swagger для JWT
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Настройка CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowSpecificOrigins", policy =>
//    {
//        policy.WithOrigins(
//                "https://138.124.115.47",
//                "http://138.124.115.47",
//                "https://138.124.115.47:5001",
//                "http://138.124.115.47:5001",
//                "https://upione.github.io",
//                "http://upione.github.io",
//                "https://adminbottle.ru",
//                "http://adminbottle.ru",
//                "https://api.adminbottle.ru",
//                "http://api.adminbottle.ru",
//                "https://api.adminbottle.ru:5001",
//                "http://api.adminbottle.ru:5000",
//                "https://admin.adminbottle.ru", // добавлено
//                "http://admin.adminbottle.ru",  // добавлено
//                "https://luckybottlex.com",
//                "http://luckybottlex.com",
//                "https://admin.luckybottlex.com",
//                "http://admin.luckybottlex.com",
//                "https://api.admin.luckybottlex.com",
//                "http://api.admin.luckybottlex.com",
//                "http://localhost:3000",
//                "https://localhost:3000",
//                "http://localhost:5001",
//                "https://localhost:5001",
//                "https://nicepay.io",
//                "http://nicepay.io",
//                "https://121.127.46.166",
//                "http://121.127.46.166")
//            .AllowAnyHeader()
//            .AllowAnyMethod()
//            .AllowCredentials();
//    });
//});

// Настройка CORS
//builder.Services.AddCors(options =>
//{
//    options.AddPolicy("AllowSpecificOrigins", policy =>
//    {
//        policy.SetIsOriginAllowed(origin =>
//        {
//            // Разрешаем все локальные адреса
//            if (origin.StartsWith("http://localhost") ||
//                origin.StartsWith("https://localhost") ||
//                origin.StartsWith("http://127.0.0.1") ||
//                origin.StartsWith("https://127.0.0.1"))
//                return true;

//            // Разрешаем ваши домены и IP-адреса
//            var allowedDomains = new[]
//            {
//                "138.124.115.47",
//                "upione.github.io",
//                "adminbottle.ru",
//                "api.adminbottle.ru",
//                "admin.adminbottle.ru",
//                "luckybottlex.com",
//                "admin.luckybottlex.com",
//                "api.admin.luckybottlex.com",
//                "nicepay.io",
//                "121.127.46.166"
//            };

//            return allowedDomains.Any(domain => origin.Contains(domain));
//        })
//        .AllowAnyHeader()
//        .AllowAnyMethod()
//        .AllowCredentials();
//    });
//});

// Настройка CORS - разрешить все
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bottle Game API v1");
    });
}

app.UseHttpsRedirection();

app.UseRouting();

//app.UseCors("AllowSpecificOrigins");
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
