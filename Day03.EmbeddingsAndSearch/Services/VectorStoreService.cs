using Day03.EmbeddingsAndSearch.Models;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Day03.EmbeddingsAndSearch.Services;

public class VectorStoreService
{
    private readonly QdrantClient _qdrant;
    private readonly EmbeddingService _embedder;
    private const string CollectionName = "day03_texts";
    private const int VectorSize = 768; // nomic-embed-text dimension

    public VectorStoreService(QdrantClient qdrant, EmbeddingService embedder)
    {
        _qdrant = qdrant;
        _embedder = embedder;
    }

    // ── Create collection if it doesn't exist ─────────────────────────────────
    public async Task EnsureCollectionExistsAsync()
    {
        var collections = await _qdrant.ListCollectionsAsync();
        if (collections.Any(c => c == CollectionName))
        {
            Console.WriteLine($"  Collection '{CollectionName}' already exists.");
            return;
        }

        await _qdrant.CreateCollectionAsync(CollectionName,
            new VectorParams
            {
                Size = VectorSize,
                Distance = Distance.Cosine  // use cosine similarity for text
            });

        Console.WriteLine($"  Collection '{CollectionName}' created.");
    }

    // ── Store a text entry ────────────────────────────────────────────────────
    public async Task StoreAsync(TextEntry entry)
    {
        var embedding = await _embedder.GetEmbeddingAsync(entry.Text);

        var point = new PointStruct
        {
            Id = new PointId { Uuid = entry.Id.ToString() },
            Vectors = embedding,
            Payload =
            {
                ["text"]     = entry.Text,
                ["source"]   = entry.Source,
                ["category"] = entry.Category,
            }
        };

        await _qdrant.UpsertAsync(CollectionName, new[] { point });
        Console.WriteLine($"  Stored: \"{entry.Text[..Math.Min(50, entry.Text.Length)]}...\"");
    }

    // ── Search for similar texts ──────────────────────────────────────────────
    public async Task<List<SearchResult>> SearchAsync(string query, int topK = 5)
    {
        var queryEmbedding = await _embedder.GetEmbeddingAsync(query);

        var results = await _qdrant.SearchAsync(
            collectionName: CollectionName,
            vector: queryEmbedding,
            limit: (ulong)topK,
            scoreThreshold: 0.5f  // only return results above 50% similarity
        );

        return results.Select(r => new SearchResult
        {
            Text = r.Payload["text"].StringValue,
            Source = r.Payload["source"].StringValue,
            Category = r.Payload["category"].StringValue,
            Score = Math.Round(r.Score, 4),
        }).ToList();
    }
}