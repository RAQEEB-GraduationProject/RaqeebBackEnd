using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RAQEEB.Common;
using RAQEEB.Data;
using RAQEEB.DTOs.Common;
using RAQEEB.Entities;
using RAQEEB.Services.Auth;
using RAQEEB.Services.Diagnostics;
using RAQEEB.Services.Patients;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

//builder.Services.AddControllers();
builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(error =>
                    new ApiError
                    {
                        Field = x.Key,
                        Message = string.IsNullOrEmpty(error.ErrorMessage)
                            ? "Invalid value."
                            : error.ErrorMessage
                    }))
                .ToList();

            var response = ApiResponse<object>.FailureResponse(
                "VALIDATION_ERROR",
                "One or more validation errors occurred.",
                errors
            );

            return new BadRequestObjectResult(response);
        };
    });

// Secrets are NOT stored in appsettings.json (it is pushed to GitHub).
// Development: put them in appsettings.Development.json (git-ignored).
// Server:      use environment variables ConnectionStrings__DefaultConnection and Jwt__Key.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is not set. " +
        "Add it to appsettings.Development.json (git-ignored) or the " +
        "ConnectionStrings__DefaultConnection environment variable.");
}

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key is missing or shorter than 32 characters. " +
        "Add a long random key to appsettings.Development.json (git-ignored) " +
        "or the Jwt__Key environment variable.");
}

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Lock an account for 5 minutes after 5 wrong passwords in a row
        // (stops password-guessing attacks on the login endpoint).
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Any unhandled exception -> ApiResponse JSON (409 for duplicate values, 500 otherwise).
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUser, CurrentUser>();

builder.Services.AddScoped<IAuthService, AuthService>();

builder.Services.AddScoped<IPatientAccessService, PatientAccessService>();

builder.Services.AddScoped<IPatientOverviewService, PatientOverviewService>();

builder.Services.AddScoped<IPatientMedicalHistoryService, PatientMedicalHistoryService>();

builder.Services.AddSingleton<IDiagnosticFileStorage, DiagnosticFileStorage>();

builder.Services.AddScoped<IPatientDiagnosticsService, PatientDiagnosticsService>();

builder.Services.AddScoped<IPatientJournalService, PatientJournalService>();

builder.Services.AddScoped<IPatientAppointmentsService, PatientAppointmentsService>();

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

        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],

        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtKey)
        )
    };

    // A token stays valid for 2 hours, so check on EVERY request that the
    // account still exists and is still active. A deactivated user is
    // blocked immediately instead of when their token expires.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var userId = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            var db = context.HttpContext.RequestServices
                .GetRequiredService<ApplicationDbContext>();

            var isActive = userId != null && await db.Users
                .AsNoTracking()
                .AnyAsync(u => u.Id == userId && u.IsActive);

            if (!isActive)
            {
                context.Fail("This account is inactive or no longer exists.");
            }
        }
    };
});
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();

// Swagger 
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// add cors to allow requests from the frontend
// Allowed frontend URLs come from "Cors:AllowedOrigins" in appsettings.json
// (add your deployed frontend URL there when you deploy).
var allowedOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>();

if (allowedOrigins == null || allowedOrigins.Length == 0)
{
    allowedOrigins = new[] { "http://localhost:5173" };
}

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .WithExposedHeaders("Content-Disposition");
    });
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    var context = services
        .GetRequiredService<ApplicationDbContext>();

    var userManager = services
        .GetRequiredService<UserManager<ApplicationUser>>();

    var roleManager = services
        .GetRequiredService<RoleManager<IdentityRole>>();

    // The first admin's password comes from configuration ("AdminSeed:Password"),
    // never from the source code. Only needed when no admin exists yet.
    await DbSeeder.SeedAsync(
        context,
        userManager,
        roleManager,
        builder.Configuration["AdminSeed:Password"]);

    // Fake demo users, patients, labs, imaging and PDFs - Development only.
    if (app.Environment.IsDevelopment())
    {
        await DbSeeder.SeedDevelopmentDataAsync(
            context,
            userManager,
            services.GetRequiredService<IDiagnosticFileStorage>(),
            builder.Configuration["DemoSeed:Password"] ?? "Demo@12345");
    }
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    // app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Use CORS policy
app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
