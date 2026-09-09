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

// ─── Configuration defaults (overridden by env / appsettings / docker) ──
builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Key"] = builder.Configuration["Jwt:Key"] ?? "ICQ_Super_Secret_Key_At_Least_32_Chars_Long_2024!",
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

// ─── Database: PostgreSQL if connection string looks like it, else SQLite ──
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

// ─── Services ───────────────────────────────────────────────────
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<PushService>();
builder.Services.AddScoped<WebPushService>();
builder.Services.AddHttpClient();

// ─── Auth ───────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]!;
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
            ClockSkew = TimeSpan.FromMinutes(1)
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
        // Единый JSON при ошибках валидации моделей ([Required], [MaxLength], …)
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

// DataAnnotations: неявная валидация через [ApiController] + ModelState
builder.Services.AddSignalR();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()
              .SetIsOriginAllowed(_ => true);
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

    // XML-комментарии из сборки (GenerateDocumentationFile в csproj)
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
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

// Default files + static for PWA web client
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
    version = "1.1.0",
    database = usePostgres ? "PostgreSQL" : "SQLite",
    status = "running",
    hubs = new[] { "/hubs/chat" },
    api = "/swagger",
    features = new[] { "auth", "chats", "contacts", "signalr", "file-upload", "push" }
}));

app.Run();
