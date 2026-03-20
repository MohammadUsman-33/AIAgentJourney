using Day02.StructuredOutputAPI.Services;
using Microsoft.SemanticKernel;


var builder = WebApplication.CreateBuilder(args);

// ── Semantic Kernel + Ollama ──────────────────────────────────────────────────
builder.Services.AddKernel()
    .AddOllamaChatCompletion(
        modelId:  "phi3:mini",
        endpoint: new Uri("http://localhost:11434"));

// ── Register Services ─────────────────────────────────────────────────────────
builder.Services.AddScoped<StructuredAIService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

// ── Middleware ────────────────────────────────────────────────────────────────
app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();