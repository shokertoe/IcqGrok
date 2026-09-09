using System.Text;
using ICQ.Server.Data;
using ICQ.Server.Hubs;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ─── Configuration defaults (dev-friendly; production must set Jwt:Key) ──
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Issuer"] = builder.Configuration["Jwt:Issuer"] ?? "ICQ.Server",
    ["Jwt:Audience"] = builder.Configuration["Jwt:Audience"] ?? "ICQ.Client",
    ["Jwt:AccessTokenMinutes"] = builder.Configuration["Jwt:AccessTokenMinutes"] ?? "120",
    ["Jwt:RefreshTokenDays"] = builder.Configuration["Jwt:RefreshTokenDays"] ?? "30",
    ["ConnectionStrings:Default"] = builder.Configuration.GetConnectionString("Default")
        ?? "Data Source=icq.db",
    ["FileStorage:Path"] = builder.Configuration["FileStorage:Path"]
        ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"),
    ["FileStorage:BaseUrl"] = builder.Configuration["FileStorage:BaseUrl"] ?? "/uploads"
});

var connStr = builder.Configuration.GetConnectionString("Default")!;
var usePostgres = connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase)
                  || connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (usePostgres)
        opt.UseNpgsql(connStr);
    else
        opt.UseSqlite(connStr);
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<PushService>();
builder.Services.AddScoped<WebPushService>();
builder.Services.AddHttpClient();

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
{
    if (builder.Environment.IsDevelopment())
    {
        jwtKey = "DEV_ONLY_ICQ_JWT_Key_Min_32_chars!!";
        Console.WriteLine("WARN: Using development JWT key. Set Jwt:Key for real deployments.");
    }
    else
    {
        throw new InvalidOperationException(
            "Jwt:Key must be configured with at least 32 characters in non-Development environments.");
    }
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = System.Security.Claims.ClaimTypes.Name,
            RoleClaimType = System.Security.Claims.ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value!.Errors
                        .Select(e => string.IsNullOrEmpty(e.ErrorMessage)
                            ? "Некорректное значение"
                            : e.ErrorMessage)
                        .ToArray());

            var problem = new
            {
                type = "https://tools.ietf.org/html/rfc9110#section-15.5.1",
                title = "Ошибка валидации",
                status = StatusCodes.Status400BadRequest,
                errors,
                traceId = context.HttpContext.TraceIdentifier
            };

            return new BadRequestObjectResult(problem)
            {
                ContentTypes = { "application/problem+json" }
            };
        };
    });

builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var origins = builder.Configuration["Cors:Origins"];
        if (!string.IsNullOrWhiteSpace(origins))
        {
            policy.WithOrigins(origins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else if (builder.Environment.IsDevelopment())
        {
            policy.SetIsOriginAllowed(_ => true)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        }
        else
        {
            policy.AllowAnyHeader()
                  .AllowAnyMethod()
                  .SetIsOriginAllowed(_ => false);
        }
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ICQ Messenger API",
        Version = "v1",
        Description = "REST + SignalR API для мессенджера ICQGrok (аутентификация, чаты, файлы, E2E-ключи, push)."
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Пример: `Bearer {access_token}`",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });

    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // MVP bootstrap. Prefer EF migrations for production schema evolution.
    db.Database.EnsureCreated();
}

var uploadPath = builder.Configuration["FileStorage:Path"]
    ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
Directory.CreateDirectory(uploadPath);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "web");
if (Directory.Exists(webRoot))
{
    app.UseDefaultFiles(new DefaultFilesOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot),
        RequestPath = "/web",
        DefaultFileNames = new List<string> { "index.html" }
    });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(webRoot),
        RequestPath = "/web"
    });
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(uploadPath),
    RequestPath = "/uploads"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hubs/chat");

app.MapGet("/", () => Results.Ok(new
{
    name = "ICQ Messenger Server",
    version = "1.1.1",
    database = usePostgres ? "PostgreSQL" : "SQLite",
    status = "running",
    hubs = new[] { "/hubs/chat" },
    api = "/swagger",
    features = new[] { "auth", "chats", "contacts", "signalr", "file-upload", "push", "webrtc-signaling", "e2e-keys" }
}));

app.Run();
