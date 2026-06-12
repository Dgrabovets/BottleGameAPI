using BottleGame.API.Middleware;
using BottleGame.API.Options;
using BottleGame.Application;
using BottleGame.Infrastructure;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

#region Логирование

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Transaction", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Infrastructure", LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File("Logs/log-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();
builder.Host.UseSerilog();

#endregion

#region Dependency Injection

// Application слой
builder.Services.AddApplication();

// Infrastructure слой
builder.Services.AddInfrastructure(builder.Configuration);

// FluentValidation автоматическая валидация
builder.Services.AddFluentValidationAutoValidation();

#endregion

#region Аутентификация и авторизация

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("JWT"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var jwtKey = builder.Configuration["JWT:Key"] ?? string.Empty;
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "BottleAppAPI";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "BottleGame";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuerSigningKey = true,
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Admin", policy => policy.RequireRole("Admin"))
    .AddPolicy("Moderator", policy => policy.RequireRole("Moderator"))
    .AddPolicy("Player", policy => policy.RequireRole("Player"));

#endregion

#region MVC и Swagger

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Bottle Game API",
        Version = "v1",
        Description = "API для игры Bottle Game с интеграцией Telegram и платёжной системой PayShark",
    });

    c.TagActionsBy(api => [api.GroupName ?? api.ActionDescriptor.RouteValues["controller"]]);
    c.DocInclusionPredicate((name, api) => true);

    // XML-документация из всех проектов
    var xmlFiles = new[]
    {
        $"{Assembly.GetExecutingAssembly().GetName().Name}.xml",
        "BottleGame.Application.xml",
        "BottleGame.Domain.xml"
    };
    foreach (var xmlFile in xmlFiles)
    {
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
    }

    // JWT в Swagger (Swashbuckle 10 / Microsoft.OpenApi 2.x)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "JWT Authorization header using the Bearer scheme"
    });

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

#endregion

#region CORS

var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>()
    ?? ["https://admin.bottledevadmin.ru"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOrigins", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

#endregion

#region Rate Limiting

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Глобально: 750 запросов в минуту на IP
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIpAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 750,
                Window = TimeSpan.FromMinutes(1),
            }));

    // auth (логин, Telegram): 25 запросов в минуту на IP
    options.AddPolicy("auth", context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIpAddress(context),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 25,
                Window = TimeSpan.FromMinutes(1),
            }));

    // game (раунды): 250 запросов за 30 секунд на IP
    options.AddPolicy("game", context =>
        RateLimitPartition.GetSlidingWindowLimiter(
            partitionKey: GetClientIpAddress(context),
            factory: _ => new SlidingWindowRateLimiterOptions
            {
                AutoReplenishment = true,
                PermitLimit = 250,
                Window = TimeSpan.FromSeconds(30),
                SegmentsPerWindow = 3,
            }));
});

#endregion

var app = builder.Build();

// Middleware pipeline
app.UseGlobalExceptionHandler();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bottle Game API v1");
});

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowedOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

static string GetClientIpAddress(HttpContext context)
{
    var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwardedFor))
    {
        return forwardedFor.Split(',')[0].Trim();
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
