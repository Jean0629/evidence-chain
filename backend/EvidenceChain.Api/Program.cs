using EvidenceChain.Api.Filters;
using EvidenceChain.Application.Auth;
using EvidenceChain.Application.Custodians;
using EvidenceChain.Application.Evidence;
using EvidenceChain.Application.Transfers;
using EvidenceChain.Domain.Exceptions;
using EvidenceChain.Infrastructure;
using EvidenceChain.Infrastructure.Auth;
using EvidenceChain.Infrastructure.Queries;
using EvidenceChain.Infrastructure.Transfers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Security.Claims;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando el esquema Bearer. Ejemplo: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    options.OperationFilter<AuthorizeCheckOperationFilter>();
});


builder.Services.AddDbContext<EvidenceChainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IEvidenceQueries, EvidenceQueries>();
builder.Services.AddScoped<IChainVerification, ChainVerification>();
builder.Services.AddScoped<ICustodyTransferService, CustodyTransferService>();
builder.Services.AddScoped<ICustodianQueries, CustodianQueries>();

var jwtSecret = builder.Configuration["Jwt:Secret"]!;

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 100,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    options.AddPolicy("chain-verify", context =>
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";

        return RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: userId,
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            });
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.ContentType = "application/problem+json";
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            title = "Demasiadas solicitudes. Intenta de nuevo en unos segundos.",
            status = StatusCodes.Status429TooManyRequests
        }, cancellationToken: token);
    };
});

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy => policy
        .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173", "https://evidencechainweb.vercel.app"])
        .AllowAnyHeader()
        .AllowAnyMethod()
        .WithExposedHeaders("ETag")));

var app = builder.Build();

app.UseRateLimiter();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapGet("/", () => Results.Redirect("/swagger"))
        .ExcludeFromDescription();
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.UseExceptionHandler(errApp =>
{
    errApp.Run(async context =>
    {
        var ex = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        context.Response.ContentType = "application/problem+json";

        switch (ex)
        {
            case KeyNotFoundException:
                context.Response.StatusCode = 404;
                await context.Response.WriteAsJsonAsync(new { title = ex.Message, status = 404 });
                break;
            case InvalidTransitionException ite:
                context.Response.StatusCode = 409;
                await context.Response.WriteAsJsonAsync(new { type = "invalid-transition", title = ite.Message, status = 409, currentStatus = ite.CurrentStatus });
                break;
            case ConcurrencyConflictException cce:
                context.Response.StatusCode = 409;
                await context.Response.WriteAsJsonAsync(new { type = "concurrency-conflict", title = cce.Message, status = 409, currentStatus = cce.CurrentStatus });
                break;
            case InvalidOperationException ioe:
                context.Response.StatusCode = 400;
                await context.Response.WriteAsJsonAsync(new { title = ioe.Message, status = 400 });
                break;
            case ForbiddenTransferActionException fte:
                context.Response.StatusCode = 403;
                await context.Response.WriteAsJsonAsync(new { title = fte.Message, status = 403 });
                break;
            default:
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { title = "Error interno", status = 500 });
                break;
        }
    });
});

app.Run();
