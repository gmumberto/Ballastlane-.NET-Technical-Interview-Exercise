using TaskManagement.Bootstrap;
using TaskManagement.API.Swagger;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TaskManagement.API.Filters;
using TaskManagement.API.Middleware;
using TaskManagement.Application.Validators;

var builder = WebApplication.CreateBuilder(args);

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT Issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT Audience is not configured.");

var jwtKeyBase64 = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT Key is not configured.");

byte[] jwtKey;

try
{
    jwtKey = Convert.FromBase64String(jwtKeyBase64);
}
catch (FormatException ex)
{
    throw new InvalidOperationException(
        "JWT Key must be a valid Base64 string.", ex);
}

if (jwtKey.Length < 32)
{
    throw new InvalidOperationException(
        "JWT Key must contain at least 32 bytes.");
}

// Composition is isolated from the HTTP host.
builder.Services.AddTaskManagementCore(builder.Configuration);

builder.Services.AddControllers(options =>
{
    options.Filters.Add<ValidationFilter>();
});

builder.Services
    .AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(jwtKey),

                ValidateLifetime = true,
                RequireExpirationTime = true,
                RequireSignedTokens = true,

                ValidAlgorithms =
                [
                    SecurityAlgorithms.HmacSha256
                ],

                NameClaimType = "sub",

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaskManagement API",
        Version = "v1",
        Description = """
            RESTful Task Management API built with
            ASP.NET Core 10 and Clean Architecture.

            Features:
            - JWT Bearer authentication
            - User-specific task management
            - Task CRUD operations
            - Pagination and filtering
            - Dynamic sorting
            - FluentValidation
            - Standardized ProblemDetails responses

            Architecture:
            - API
            - Application
            - Domain
            - Infrastructure
            """
    });

    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Description = """
                JWT authentication using the Bearer scheme.

                1. Register or log in using the Auth endpoints.
                2. Copy the accessToken returned by the API.
                3. Click Authorize.
                4. Paste the JWT token without the Bearer prefix.
                5. Execute an authenticated endpoint.
                """,
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });

    options.OperationFilter<SwaggerAuthorizationOperationFilter>();

    // XML documentation
    var xmlFile =
        $"{typeof(Program).Assembly.GetName().Name}.xml";

    var xmlPath = Path.Combine(
        AppContext.BaseDirectory,
        xmlFile);

    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    await DevelopmentSeeder.SeedDevelopmentAsync(app.Services, app.Configuration);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "TaskManagement API v1");

        options.RoutePrefix = "swagger";

        options.DocumentTitle =
            "TaskManagement API - Documentation";

        options.DisplayRequestDuration();
    });
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }