using System.Text;
using ICQ.Server.Data;
using ICQ.Server.Hubs;
using ICQ.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
{
    ["Jwt:Key"] = builder.Configuration["Jwt:Key"] ?? "ICQ_Super_Secret_Key_At_Least_32_Chars_Long_2024!",
    ["Jwt:Issuer"] = builder.Configuration["Jwt:Issuer"] ?? "ICQ.Server",
    ["Jwt:Audience"] = builder.Configuration["Jwt:Audience"] ?? "ICQ.Client",
    ["Jwt:AccessTokenMinutes"] = builder.Configuration["Jwt:AccessTokenMinutes"] ?? "120",
    ["Jwt:RefreshTokenDays"] = builder.Configuration["Jwt:RefreshTokenDays"] ?? "30",
    ["ConnectionStrings:Default"] = builder.Configuration.GetConnectionString("Default") ?? "Data Source=icq.db",
    ["FileStorage:Path"] = builder.Configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads"),
    ["FileStorage:BaseUrl"] = builder.Configuration["FileStorage:BaseUrl"] ?? "/uploads"
});

var connStr = builder.Configuration.GetConnectionString("Default")!;
var usePostgres = connStr.Contains("Host=", StringComparison.OrdinalIgnoreCase) || connStr.Contains("Server=", StringComparison.OrdinalIgnoreCase);

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (usePostgres) opt.UseNpgsql(connStr);
    else opt.UseSqlite(connStr);
});

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddSingleton<FileStorageService>();
builder.Services.AddScoped<PushService>();
builder.Services.AddScoped<WebPushService>();
builder.Services.AddHttpClient();

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
builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().AllowCredentials().SetIsOriginAllowed(_ => true);
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "ICQ Messenger API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

var uploadPath = builder.Configuration["FileStorage:Path"] ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
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
    version = "1.1.0",
    database = usePostgres ? "PostgreSQL" : "SQLite",
    status = "running",
    hubs = new[] { "/hubs/chat" },
    api = "/swagger",
    features = new[] { "auth", "chats", "contacts", "signalr", "file-upload", "push" }
}));
app.Run();
