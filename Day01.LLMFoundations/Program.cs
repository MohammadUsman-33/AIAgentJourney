using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// ── CONFIG ───────────────────────────────────────────────────────────────────
// Make sure Ollama is running: open a terminal and run "ollama serve"
var ollamaEndpoint = new Uri("http://localhost:11434");
var modelId = "phi3:mini";

var kernel = Kernel.CreateBuilder()
    .AddOllamaChatCompletion(
        modelId: modelId,
        endpoint: ollamaEndpoint)
    .Build();

var chat = kernel.GetRequiredService<IChatCompletionService>();

Console.WriteLine("🤖 Connecting to Ollama (phi3:mini)...\n");

// ── EXPERIMENT 1: Temperature ─────────────────────────────────────────────────
Console.WriteLine("━━━ EXPERIMENT 1: Temperature ━━━");
Console.WriteLine("Same prompt, 3 temperatures. Watch how output changes.\n");

string prompt = "Write one sentence describing what an AI agent is.";

foreach (var temp in new[] { 0.0f, 0.5f, 0.9f })
{
    var settings = new PromptExecutionSettings
    {
        ExtensionData = new Dictionary<string, object>
        {
            ["temperature"] = (double)temp
        }
    };

    var history = new ChatHistory();
    history.AddUserMessage(prompt);

    Console.Write($"  Temp {temp:F1} → ");
    var response = await chat.GetChatMessageContentAsync(history, settings, kernel);
    Console.WriteLine(response.Content);
}

// ── EXPERIMENT 2: System Prompts ──────────────────────────────────────────────
Console.WriteLine("\n━━━ EXPERIMENT 2: System Prompts ━━━");
Console.WriteLine("Same question, 3 different personas. Watch tone change.\n");

var personas = new[]
{
    ("You are a teacher explaining to a curious 10-year-old. Use very simple words.",  "Simple"),
    ("You are a senior software engineer writing precise technical documentation.",    "Technical"),
    ("You are an enthusiastic startup founder who loves technology.",                  "Excited"),
};

foreach (var (systemPrompt, label) in personas)
{
    var history = new ChatHistory(systemPrompt);
    history.AddUserMessage("Explain machine learning in 2 sentences.");

    Console.WriteLine($"  [{label}]");
    var response = await chat.GetChatMessageContentAsync(history, kernel: kernel);
    Console.WriteLine($"  {response.Content}\n");
}

// ── EXPERIMENT 3: Prompt Templates ───────────────────────────────────────────
Console.WriteLine("━━━ EXPERIMENT 3: Prompt Templates ━━━");
Console.WriteLine("Variables injected into a reusable prompt structure.\n");

string BuildPrompt(string topic, string audience, int count) =>
    $"""
    You are a content strategist.
    Generate exactly {count} blog title ideas about: {topic}
    Target audience: {audience}
    Return ONLY a numbered list. No extra text. No explanations.
    """;

var topics = new[]
{
    ("C# and AI Agents", "senior .NET developers", 3),
    ("Home automation",  "complete beginners",      3),
};

foreach (var (topic, audience, count) in topics)
{
    var result = await kernel.InvokePromptAsync(BuildPrompt(topic, audience, count));
    Console.WriteLine($"  Topic: {topic}");
    Console.WriteLine($"  {result}\n");
}

// ── DELIVERABLE: Interactive Topic Generator ──────────────────────────────────
Console.WriteLine("━━━ YOUR AI CONTENT GENERATOR ━━━\n");
Console.Write("Enter any topic: ");
var userTopic = Console.ReadLine() ?? "artificial intelligence";

var styles = new[]
{
    ("Simple",      "Explain this to a curious 12-year-old in 3 sentences."),
    ("Technical",   "Write a technical summary for an experienced engineer in 3 sentences."),
    ("Persuasive",  "Write a compelling argument for why this topic matters in 3 sentences."),
};

foreach (var (style, instruction) in styles)
{
    var history = new ChatHistory(
        "You are a skilled writer who adapts tone and style precisely to instructions.");
    history.AddUserMessage($"Topic: {userTopic}\nTask: {instruction}");

    Console.WriteLine($"\n  ── {style.ToUpper()} ──────────────────────");
    var response = await chat.GetChatMessageContentAsync(history, kernel: kernel);
    Console.WriteLine($"  {response.Content}");
}

Console.WriteLine("\n✅ Day 1 complete!");
Console.ReadKey(); // keeps window open in Visual Studio