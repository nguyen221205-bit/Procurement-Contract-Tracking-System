using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ProcurementSystem.API.Middlewares;
using ProcurementSystem.Core.Interfaces;
using ProcurementSystem.Infrastructure.Data;
using ProcurementSystem.Infrastructure.Repositories;
using ProcurementSystem.Infrastructure.Seeders;
using ProcurementSystem.Infrastructure.Services;
using ProcurementSystem.API.BackgroundServices;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// 1. DATABASE - Microsoft SQL Server + EF Core
// ==========================================
var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// ==========================================
// 2. DEPENDENCY INJECTION
// ==========================================
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Memory Cache
builder.Services.AddMemoryCache();

// Auth Services
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, OtpService>();

// Contractor Services
builder.Services.AddHttpClient<ITaxLookupService, TaxLookupService>();
builder.Services.AddScoped<IPdfSecurityService, PdfSecurityService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();
builder.Services.AddScoped<IContractorAuthService, ContractorAuthService>();
builder.Services.AddScoped<IContractorService, ContractorService>();
builder.Services.AddScoped<IProcuringEntityAuthService, ProcuringEntityAuthService>();
builder.Services.AddScoped<IProcuringEntityService, ProcuringEntityService>();

// User Management Services
builder.Services.AddScoped<IUserService, UserService>();

// Bid Package Services
builder.Services.AddScoped<IBidPackageService, BidPackageService>();

// Bid Submission Services
builder.Services.AddScoped<IBidSubmissionService, BidSubmissionService>();

// Evaluation & Scoring Services
builder.Services.AddScoped<IEvaluationService, EvaluationService>();
builder.Services.AddScoped<IEvaluatorProposalService, EvaluatorProposalService>();

// Contract Services
builder.Services.AddScoped<IContractService, ContractService>();

// Management & Reporting Services
builder.Services.AddScoped<IReportService, ReportService>();

// Audit & Notification Services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Background Workers (Cronjob cảnh báo hạn 1 giờ/lần + startup)
builder.Services.AddHostedService<ContractExpiryNotificationWorker>();

// ==========================================
// 3. AUTHENTICATION - JWT
// ==========================================
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
    ?? jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userIdClaim = context.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out int userId))
            {
                var dbContext = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                var user = await dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || !user.IsActive)
                {
                    context.Fail("Tài khoản người dùng đã bị khóa hoặc không còn tồn tại trên hệ thống.");
                }
            }
            else
            {
                context.Fail("Token không chứa định danh người dùng hợp lệ.");
            }
        }
    };
});

builder.Services.AddAuthorization();

// ==========================================
// 4. CONTROLLERS + JSON
// ==========================================
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// ==========================================
// 5. SWAGGER / API DOCS
// ==========================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Procurement & Contract Tracking System API",
        Version = "v1",
        Description = "Hệ thống Quản lý Đấu Thầu & Theo Dõi Hợp Đồng"
    });

    // JWT Bearer token in Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token. Ví dụ: eyJhbGciOiJIUzI1NiIsInR..."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            Array.Empty<string>()
        }
    });

    // Include XML Documentation Comments for Swagger
    var apiXmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var apiXmlPath = Path.Combine(AppContext.BaseDirectory, apiXmlFile);
    if (File.Exists(apiXmlPath))
    {
        options.IncludeXmlComments(apiXmlPath, includeControllerXmlComments: true);
    }

    var coreXmlPath = Path.Combine(AppContext.BaseDirectory, "ProcurementSystem.Core.xml");
    if (File.Exists(coreXmlPath))
    {
        options.IncludeXmlComments(coreXmlPath);
    }
});

// ==========================================
// 6. CORS
// ==========================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==========================================
// 7. AUTOMAPPER
// ==========================================
builder.Services.AddAutoMapper(AppDomain.CurrentDomain.GetAssemblies());

// ==========================================
// BUILD APP
// ==========================================
var app = builder.Build();

// ==========================================
// MIDDLEWARE PIPELINE
// ==========================================

// Global Exception Handler (đặt đầu tiên)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Swagger (chỉ ở Development)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Procurement System API v1");
        c.RoutePrefix = string.Empty; // Swagger ở root URL
    });
}

// Static files (cho Frontend)
app.UseStaticFiles();

// Đảm bảo thư mục uploads tồn tại trong hệ thống lưu trữ (không phục vụ static công khai để bảo vệ Sealed-Bid)
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
if (!Directory.Exists(uploadsPath)) Directory.CreateDirectory(uploadsPath);

// CORS
app.UseCors("AllowAll");

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map Controllers
app.MapControllers();

// ==========================================
// SEED DATA
// ==========================================
await DataSeeder.SeedAsync(app.Services);

// ==========================================
// RUN
// ==========================================
app.Run();
