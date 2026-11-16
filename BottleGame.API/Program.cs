using BottleGame.API.Middleware;
using BottleGame.API.Options;
using BottleGame.Application;
using BottleGame.Infrastructure;
using BottleGame.Infrastructure.External.Processing;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// За обратным прокси клиентский IP берётся из X-Forwarded-For только от доверенных прокси.
// Без списка прокси заголовок используется как раньше, но его может подделать любой клиент.
var knownProxies = builder.Configuration.GetSection("ForwardedHeaders:KnownProxies").Get<string[]>() ?? [];
var trustForwardedForHeader = knownProxies.Length == 0;

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
        Description = "API для игры Bottle Game с интеграцией Telegram и платёжной системой ЮKassa",
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
    ?? [];

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

#region Forwarded headers

if (!trustForwardedForHeader)
{
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        foreach (var proxy in knownProxies)
        {
            options.KnownProxies.Add(IPAddress.Parse(proxy));
        }
    });
}

#endregion

var app = builder.Build();

if (trustForwardedForHeader)
{
    app.Logger.LogWarning("ForwardedHeaders:KnownProxies is not set: X-Forwarded-For is trusted as is, so clients can spoof the address used for rate limiting.");
}

if (app.Services.GetRequiredService<IOptions<ProcessingOptions>>().Value.Mode == ProcessingMode.Simulated)
{
    app.Logger.LogWarning("Processing runs in Simulated mode: deposits and withdrawals complete locally without real payments. Set Processing__Mode=Live to use the payment processor.");
}

// Middleware pipeline
app.UseGlobalExceptionHandler();

if (!trustForwardedForHeader)
{
    app.UseForwardedHeaders();
}

if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Bottle Game API v1");
    });
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowedOrigins");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

string GetClientIpAddress(HttpContext context)
{
    if (trustForwardedForHeader)
    {
        var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }
    }

    return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
