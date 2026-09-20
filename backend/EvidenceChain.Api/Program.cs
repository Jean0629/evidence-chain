using EvidenceChain.Application.Evidence;
using EvidenceChain.Application.Transfers;
using EvidenceChain.Domain.Exceptions;
using EvidenceChain.Infrastructure;
using EvidenceChain.Infrastructure.Queries;
using EvidenceChain.Infrastructure.Transfers;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<EvidenceChainDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddScoped<IEvidenceQueries, EvidenceQueries>();
builder.Services.AddScoped<IChainVerification, ChainVerification>();
builder.Services.AddScoped<ICustodyTransferService, CustodyTransferService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

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
            default:
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { title = "Error interno", status = 500 });
                break;
        }
    });
});

app.Run();
