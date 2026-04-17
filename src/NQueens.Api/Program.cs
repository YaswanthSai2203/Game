using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using NQueens.Api.Models;
using NQueens.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<GameSessionStore>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:4200"];

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();

var games = app.MapGroup("/api/games");

games.MapPost("/", (StartGameRequest body, GameSessionStore store) =>
{
    try
    {
        var session = store.Start(body.N);
        return Results.Ok(session.GetState());
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return Results.BadRequest(new ErrorResponse(ex.Message));
    }
});

games.MapGet("/{id:guid}", Results<Ok<GameStateResponse>, NotFound<ErrorResponse>> (Guid id, GameSessionStore store) =>
{
    if (!store.TryGet(id, out var session))
        return TypedResults.NotFound(new ErrorResponse("Game not found."));

    return TypedResults.Ok(session.GetState());
});

games.MapPost("/{id:guid}/toggle", Results<Ok<GameStateResponse>, NotFound<ErrorResponse>, BadRequest<ErrorResponse>> (
    Guid id,
    ToggleCellRequest body,
    GameSessionStore store) =>
{
    if (!store.TryGet(id, out var session))
        return TypedResults.NotFound(new ErrorResponse("Game not found."));

    try
    {
        return TypedResults.Ok(session.Toggle(body.Row, body.Col));
    }
    catch (ArgumentOutOfRangeException ex)
    {
        return TypedResults.BadRequest(new ErrorResponse(ex.Message));
    }
    catch (InvalidOperationException ex)
    {
        return TypedResults.BadRequest(new ErrorResponse(ex.Message));
    }
});

games.MapDelete("/{id:guid}", (Guid id, GameSessionStore store) =>
{
    store.Remove(id);
    return Results.NoContent();
});

app.MapFallbackToFile("index.html");

app.Run();
