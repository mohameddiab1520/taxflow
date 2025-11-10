using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using TaxFlow.Infrastructure.Data;
using TaxFlow.Infrastructure.Repositories;
using TaxFlow.Infrastructure.Services.ETA;
using TaxFlow.Infrastructure.Services.Security;
using TaxFlow.Infrastructure.Services.Auth;
using TaxFlow.Infrastructure.Services.Notifications;
using TaxFlow.Infrastructure.Services.Processing;
using TaxFlow.Infrastructure.Services.Reporting;
using TaxFlow.Application.Services;
using TaxFlow.Core.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/taxflow-api-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Configure JWT Authentication
var jwtSecret = builder.Configuration["JWT:Secret"] ?? "TaxFlowSecretKeyMinimum32CharactersLong!@#";
var jwtIssuer = builder.Configuration["JWT:Issuer"] ?? "TaxFlow";
var jwtAudience = builder.Configuration["JWT:Audience"] ?? "TaxFlowUsers";

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
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
    };
});

builder.Services.AddAuthorization();

// Configure Database
builder.Services.AddDbContext<TaxFlowDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("SQLite") ?? "Data Source=taxflow.db";
    options.UseSqlite(connectionString);
});

builder.Services.AddDbContext<AnalyticsDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("PostgreSQL");
    if (!string.IsNullOrEmpty(connectionString))
    {
        options.UseNpgsql(connectionString);
    }
});

// Register repositories
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
builder.Services.AddScoped<IReceiptRepository, ReceiptRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Register services
builder.Services.AddHttpClient<IEtaAuthenticationService, EtaAuthenticationService>();
builder.Services.AddHttpClient<IEtaSubmissionService, EtaSubmissionService>();
builder.Services.AddScoped<IDigitalSignatureService, DigitalSignatureService>();
builder.Services.AddScoped<ICertificateService, CertificateService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IAuthorizationService, AuthorizationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMfaService, MfaService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IBatchProcessingService, BatchProcessingService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddScoped<TaxCalculationService>();

// Add Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<TaxFlowDbContext>("database")
    .AddCheck("eta-service", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy("ETA service is reachable"));

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaxFlow Enterprise API",
        Version = "v1.0",
        Description = "Egyptian Tax Invoice System - REST API for B2B invoices and B2C receipts with ETA integration",
        Contact = new OpenApiContact
        {
            Name = "TaxFlow Support",
            Email = "support@taxflow.com",
            Url = new Uri("https://taxflow.com")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Seed database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TaxFlowDbContext>();
    dbContext.Database.EnsureCreated();
    await SeedData.SeedAsync(dbContext);
}

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "TaxFlow API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging();

app.MapControllers();

// Health check endpoint
app.MapHealthChecks("/health");

// Welcome endpoint
app.MapGet("/", () => new
{
    Name = "TaxFlow Enterprise API",
    Version = "1.0.0",
    Status = "Running",
    Documentation = "/swagger",
    Health = "/health"
});

try
{
    Log.Information("Starting TaxFlow API...");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
