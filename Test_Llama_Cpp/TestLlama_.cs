
using Run_Llama_cpp;
using Run_Llama_cpp.Tools;
using System.Text;
// See https://aka.ms/new-console-template for more information

RunLlamaCpp runLlamaCpp = new();
string modelPath = @"C:\Users\Wizard\Downloads\Qwen3-8B-Q4_K_M.gguf";

// --- Read model metadata ---
var meta = GgufMetadata.ReadFromFile(modelPath);
uint reservedTokensForResponse = 1500; // Tokens reserved for AI to generate

var conversation = new List<(string Role, string Message)>();

Console.WriteLine("Chat with AI. Type 'exit' to quit.");

while (true)
{
    // Get user input
    Console.Write("User: ");
    string? userInput = Console.ReadLine();
    if (string.IsNullOrEmpty(userInput)) continue;
    if (userInput.Trim().ToLower() == "exit") break;
    // Add user message to conversation
    conversation.Add(("User", userInput));

    // --- Build prompt while respecting context ---
    var promptBuilder = new StringBuilder();
    uint currentTokens = 0;

    // We'll approximate tokens as words (can adjust with better tokenizer later)
    foreach (var msg in conversation.AsEnumerable().Reverse())
    {
        string line = $"{msg.Role}: {msg.Message}\n";
        uint lineTokens = (uint)line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        if (currentTokens + lineTokens + reservedTokensForResponse > meta.NCtx)
            break; // Stop adding older messages
        line = line.Replace("*", "")
                .Replace("\"", "'")   // double quotes to single quotes
                .Replace("*", "")     // remove asterisks
                .Replace("`", "'")    // backticks
                .Replace("�", "");    // remove unknown characters; // Remove asterisks if any
        promptBuilder.Insert(0, line); // Insert at the beginning
        currentTokens += lineTokens;
    }

    promptBuilder.Append("AI: "); // Signal AI to respond
    
    string prompt = promptBuilder.ToString();

    // Run model
    string aiResponse = await runLlamaCpp.RunLlama(modelPath, prompt);

    // Clean AI response
    aiResponse = aiResponse.Replace("[end of text]", "").Trim();

    // Add AI response to conversation
    conversation.Add(("AI", aiResponse));

    // Show AI response
    Console.WriteLine($"AI: {aiResponse}");
}