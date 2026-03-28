using System.Text;
using System.Text.Json;
using Day04.RAGPipeline.Models;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using Qdrant.Client;
using Qdrant.Client.Grpc;

namespace Day04.RAGPipeline.Services;

public class RAGService
{
    private readonly ITextEmbeddingGenerationService _embedder;
    private readonly IChatCompletionService _chat;
    private readonly Kernel _kernel;
    private readonly QdrantClient _qdrant;
    private readonly DocumentChunkerService _chunker;
    private readonly ILogger<RAGService> _logger;

    private const string CollectionName = "rag_documents";
    private const int VectorSize = 768;

    public RAGService(
        Kernel kernel,
        QdrantClient qdrant,
        DocumentChunkerService chunker,
        ILogger<RAGService> logger)
    {
        _kernel = kernel;
        _qdrant = qdrant;
        _chunker = chunker;
        _logger = logger;
        _embedder = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        _chat = kernel.GetRequiredService<IChatCompletionService>();
    }

    // ── INGEST: chunk → embed → store ────────────────────────────────────────
    public async Task<IngestResponse> IngestAsync(IngestRequest request)
    {
        _logger.LogInformation("Ingesting document: {File}", request.FileName);

        await EnsureCollectionExistsAsync();

        // 1. Chunk the document
        var chunks = _chunker.ChunkDocument(request.Text, request.FileName);
        _logger.LogInformation("Created {Count} chunks", chunks.Count);

        // 2. Embed and store each chunk
        foreach (var chunk in chunks)
        {
            var embedding = await _embedder.GenerateEmbeddingAsync(chunk.Text);

            var point = new PointStruct
            {
                Id = new PointId { Uuid = chunk.Id.ToString() },
                Vectors = embedding.ToArray(),
                Payload =
                {
                    ["text"]        = chunk.Text,
                    ["fileName"]    = chunk.FileName,
                    ["chunkIndex"]  = chunk.ChunkIndex,
                    ["totalChunks"] = chunk.TotalChunks,
                    ["preview"]     = chunk.Text[..Math.Min(100, chunk.Text.Length)],
                }
            };

            await _qdrant.UpsertAsync(CollectionName, new[] { point });
            _logger.LogInformation("Stored chunk {Index}/{Total}", chunk.ChunkIndex + 1, chunk.TotalChunks);
        }

        return new IngestResponse
        {
            FileName = request.FileName,
            TotalChunks = chunks.Count,
            Message = $"Successfully ingested {chunks.Count} chunks from {request.FileName}",
        };
    }

    // ── QUERY: embed → retrieve → augment → generate ─────────────────────────
    public async Task<Models.QueryResponse> QueryAsync(QueryRequest request)
    {
        _logger.LogInformation("Query: {Question}", request.Question);

        // 1. Embed the question
        var queryEmbedding = await _embedder.GenerateEmbeddingAsync(request.Question);

        // 2. Search Qdrant for most relevant chunks
        var searchResults = await _qdrant.SearchAsync(
            collectionName: CollectionName,
            vector: queryEmbedding.ToArray(),
            limit: (ulong)request.MaxChunks,
            scoreThreshold: 0.5f
        );

        // 3. Check if we found any relevant context
        if (!searchResults.Any())
        {
            _logger.LogWarning("No relevant context found for query");
            return new Models.QueryResponse
            {
                Answer = "I could not find relevant information in the uploaded documents to answer this question.",
                Sources = new List<Source>(),
                HasContext = false,
            };
        }

        // 4. Build context from retrieved chunks
        var contextBuilder = new StringBuilder();
        var sources = new List<Source>();

        foreach (var result in searchResults)
        {
            var text = result.Payload["text"].StringValue;
            var fileName = result.Payload["fileName"].StringValue;
            var chunkIndex = (int)result.Payload["chunkIndex"].IntegerValue;
            var preview = result.Payload["preview"].StringValue;

            contextBuilder.AppendLine($"[Source: {fileName}, Chunk: {chunkIndex + 1}]");
            contextBuilder.AppendLine(text);
            contextBuilder.AppendLine();

            sources.Add(new Source
            {
                FileName = fileName,
                ChunkIndex = chunkIndex,
                Score = Math.Round(result.Score, 3),
                Preview = preview,
            });
        }

        // 5. Build the augmented prompt
        var systemPrompt =
            """
            You are a helpful assistant that answers questions based ONLY on the provided context.
            Rules:
            - Answer using ONLY information from the context below
            - If the answer is not in the context, say "This information is not available in the provided documents"
            - Always be specific and cite which section your answer comes from
            - Keep answers clear and concise
            """;

        var userPrompt =
            $"""
            Context from documents:
            {contextBuilder}

            Question: {request.Question}

            Answer based only on the context above:
            """;

        // 6. Generate the answer
        var history = new ChatHistory(systemPrompt);
        history.AddUserMessage(userPrompt);

        var settings = new PromptExecutionSettings
        {
            ExtensionData = new Dictionary<string, object> { ["temperature"] = 0.1 }
        };

        var response = await _chat.GetChatMessageContentAsync(history, settings, _kernel);

        return new Models.QueryResponse
        {
            Answer = response.Content ?? "No answer generated",
            Sources = sources,
            HasContext = true,
        };
    }

    // ── Helper ────────────────────────────────────────────────────────────────
    private async Task EnsureCollectionExistsAsync()
    {
        var collections = await _qdrant.ListCollectionsAsync();
        if (collections.Any(c => c == CollectionName)) return;

        await _qdrant.CreateCollectionAsync(CollectionName,
            new VectorParams { Size = VectorSize, Distance = Distance.Cosine });

        _logger.LogInformation("Created Qdrant collection: {Name}", CollectionName);
    }
}