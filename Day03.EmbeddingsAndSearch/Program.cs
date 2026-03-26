using Day03.EmbeddingsAndSearch.Models;
using Day03.EmbeddingsAndSearch.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Qdrant.Client;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.WriteLine("🔍 Day 3 — Embeddings & Semantic Search\n");

// ── Setup ─────────────────────────────────────────────────────────────────────
var services = new ServiceCollection();

services.AddKernel()
    .AddOllamaTextEmbeddingGeneration(
        modelId: "nomic-embed-text",
        endpoint: new Uri("http://localhost:11434"));

services.AddSingleton<QdrantClient>(_ => new QdrantClient("localhost", 6334));
services.AddSingleton<EmbeddingService>();
services.AddSingleton<VectorStoreService>();

var provider = services.BuildServiceProvider();
var embedder = provider.GetRequiredService<EmbeddingService>();
var store = provider.GetRequiredService<VectorStoreService>();

// ── EXPERIMENT 1: Cosine Similarity ───────────────────────────────────────────
Console.WriteLine("━━━ EXPERIMENT 1: Cosine Similarity ━━━\n");
Console.WriteLine("How similar are these sentence pairs?\n");

var pairs = new[]
{
    ("I love programming in C#",         "I enjoy writing code in dotnet"),
    ("The weather is nice today",        "It is a beautiful sunny day"),
    ("Machine learning is fascinating",  "AI and deep learning are interesting"),
    ("I love programming in C#",         "The cat sat on the mat"),
    ("Pizza is my favorite food",        "I enjoy coding late at night"),
};

foreach (var (text1, text2) in pairs)
{
    var score = await embedder.GetSimilarityAsync(text1, text2);
    var bar = new string('█', (int)(score * 20)).PadRight(20);
    Console.WriteLine($"  [{bar}] {score:F3}");
    Console.WriteLine($"   A: {text1}");
    Console.WriteLine($"   B: {text2}\n");
}

// ── EXPERIMENT 2: Store & Search ──────────────────────────────────────────────
Console.WriteLine("━━━ EXPERIMENT 2: Semantic Search ━━━\n");

await store.EnsureCollectionExistsAsync();
Console.WriteLine();

// Sample knowledge base
var entries = new List<TextEntry>
{
    new() { Text = "C# is a modern object-oriented programming language by Microsoft",  Category = "tech" },
    new() { Text = "ASP.NET Core is a framework for building web APIs and applications", Category = "tech" },
    new() { Text = "Machine learning models learn patterns from large datasets",         Category = "ai"   },
    new() { Text = "Neural networks are inspired by the human brain structure",          Category = "ai"   },
    new() { Text = "Vector databases store and search high-dimensional embeddings",      Category = "ai"   },
    new() { Text = "SQL Server is a relational database management system by Microsoft", Category = "tech" },
    new() { Text = "Docker containers package applications with their dependencies",     Category = "devops"},
    new() { Text = "Kubernetes orchestrates containerized applications at scale",        Category = "devops"},
    new() { Text = "The stock market experienced high volatility this quarter",          Category = "finance"},
    new() { Text = "Interest rates affect mortgage payments and loan costs",             Category = "finance"},
};

Console.WriteLine("Storing 10 entries in Qdrant...\n");
foreach (var entry in entries)
    await store.StoreAsync(entry);

// Run semantic searches
var queries = new[]
{
    "How do I build a web application?",
    "What is artificial intelligence?",
    "Tell me about containers and deployment",
};

foreach (var query in queries)
{
    Console.WriteLine($"\n  Query: \"{query}\"");
    Console.WriteLine($"  {"─",40}");

    var results = await store.SearchAsync(query, topK: 3);
    foreach (var r in results)
        Console.WriteLine($"  [{r.Score:F3}] {r.Text}");
}

// ── EXPERIMENT 3: Keyword vs Semantic ─────────────────────────────────────────
Console.WriteLine("\n\n━━━ EXPERIMENT 3: Keyword vs Semantic Search ━━━\n");

string semanticQuery = "programming language for backend";
Console.WriteLine($"  Query: \"{semanticQuery}\"\n");

PerformKeywordSearch(entries,null);

Console.WriteLine("\n  SEMANTIC SEARCH (meaning-based):");
var semanticResults = await store.SearchAsync(semanticQuery, topK: 3);
foreach (var r in semanticResults)
    Console.WriteLine($"  [{r.Score:F3}] → {r.Text}");

// ── Interactive Search ─────────────────────────────────────────────────────────
Console.WriteLine("\n━━━ YOUR TURN: Interactive Search ━━━\n");
while (true)
{
    Console.Write("  Enter search query (or 'quit' to exit): ");
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input) || input == "quit") break;

    var results = await store.SearchAsync(input, topK: 3);
    if (!results.Any())
    {
        Console.WriteLine("  No results above similarity threshold.\n");
        continue;
    }


    PerformKeywordSearch(null,input);

    Console.WriteLine("\n  SEMANTIC SEARCH (meaning-based):");
    foreach (var r in results)
        Console.WriteLine($"  [{r.Score:F3}] {r.Text}");
    Console.WriteLine();
}

Console.WriteLine("✅ Day 3 complete!");
Console.ReadKey();
static void PerformKeywordSearch(
    List<TextEntry>? entries = null,
    object? input = null)
{
    Console.WriteLine("  KEYWORD SEARCH (simple contains):");

    // Handle missing entries
    if (entries == null || !entries.Any())
    {
        Console.WriteLine("  No data to search.\n");
        return;
    }

    // Normalize input → keywords
    IEnumerable<string> keywords = input switch
    {
        string str => str.Split(' ', StringSplitOptions.RemoveEmptyEntries),
        IEnumerable<string> list => list,
        null => Enumerable.Empty<string>(),
        _ => throw new ArgumentException("Invalid input type")
    };

    // Handle missing keywords
    if (!keywords.Any())
    {
        Console.WriteLine("  No keywords provided.\n");
        return;
    }

    // Perform search
    var results = entries
        .Where(e => keywords.Any(k =>
            e.Text.Contains(k, StringComparison.OrdinalIgnoreCase)))
        .ToList();

    // Output results
    if (!results.Any())
    {
        Console.WriteLine("  No matches found.\n");
        return;
    }

    foreach (var r in results)
        Console.WriteLine($"  → {r.Text}");

    Console.WriteLine();
}
//static void PerformKeywordSearch(List<TextEntry>? entries, params string[]? keywords)
//{
//    Console.WriteLine("  KEYWORD SEARCH (simple contains):");

//    if (keywords == null || keywords.Length == 0)
//    {
//        Console.WriteLine("  No keywords provided.");
//        return;
//    }

//    foreach (var e in entries)
//    {
//        if (keywords.Any(k =>
//            e.Text.Contains(k, StringComparison.OrdinalIgnoreCase)))
//        {
//            Console.WriteLine($"  → {e.Text}");
//        }
//    }
//}