using Microsoft.SemanticKernel.Embeddings;

namespace Day03.EmbeddingsAndSearch.Services;

public class EmbeddingService
{
    private readonly ITextEmbeddingGenerationService _embedder;

    public EmbeddingService(ITextEmbeddingGenerationService embedder)
    {
        _embedder = embedder;
    }

    // Convert a single text to a float array
    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        var result = await _embedder.GenerateEmbeddingAsync(text);
        return result.ToArray();
    }

    // Convert multiple texts at once (more efficient)
    public async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
    {
        var results = new List<float[]>();
        foreach (var text in texts)
        {
            var embedding = await GetEmbeddingAsync(text);
            results.Add(embedding);
        }
        return results;
    }

    // Measure similarity between two texts (0.0 to 1.0)
    public async Task<double> GetSimilarityAsync(string text1, string text2)
    {
        var emb1 = await GetEmbeddingAsync(text1);
        var emb2 = await GetEmbeddingAsync(text2);
        return CosineSimilarity(emb1, emb2);
    }

    // Cosine similarity calculation
    public static double CosineSimilarity(float[] a, float[] b)
    {
        double dot = 0, magA = 0, magB = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            magA += a[i] * a[i];
            magB += b[i] * b[i];
        }
        return dot / (Math.Sqrt(magA) * Math.Sqrt(magB));
    }
}