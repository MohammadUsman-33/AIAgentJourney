using Day04.RAGPipeline.Services;
using Microsoft.SemanticKernel;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);
// ── Register named HttpClient with 10 minute timeout ─────────────────────
builder.Services.AddHttpClient("ollama")
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromMinutes(10);
        client.BaseAddress = new Uri("http://localhost:11434");
    });
// ── Semantic Kernel: Chat + Embeddings ────────────────────────────────────────
builder.Services.AddKernel()
    .AddOllamaChatCompletion(
        modelId: "phi3:mini",
        endpoint: new Uri("http://localhost:11434"));
builder.Services.AddKernel()
    .AddOllamaTextEmbeddingGeneration(
        modelId: "nomic-embed-text",
        endpoint: new Uri("http://localhost:11434"));

// ── Qdrant ────────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<QdrantClient>(_ =>
    new QdrantClient("localhost", 6334));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddSingleton<DocumentChunkerService>();
builder.Services.AddScoped<RAGService>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ── Logging ───────────────────────────────────────────────────────────────────
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Add this BEFORE builder.Build()
builder.Services.AddHttpClient();
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(10);
    options.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(10);
});
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();

app.Run();