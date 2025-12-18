using Run_LlamaSharp;
using System.Text;
using System.Text.RegularExpressions;

class TestLlamaSharp
{
    // Conversation memory
    static List<(string role, string content)> ChatHistory = new();
    static protected RunLlamaSharp runLlamaCpp;

    static async Task Main()
    {
        string modelPath = @"C:\Users\Wizard\Downloads\Ministral-3-14B-Instruct-2512-Q4_K_M.gguf";
        Console.WriteLine("Loading model...");
        runLlamaCpp = new();
        await runLlamaCpp.InitializeAsync(modelPath);
        Console.WriteLine("Chat started. Type 'exit' to quit.");

        while (true)
        {
            Console.Write("\nEnter your question: ");
            string? userInput = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(userInput)) continue;
            if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

            // Add user message to history
            ChatHistory.Add(("user", userInput));

            // string prompt = BuildPrompt("You are a helpful assistant. Do NOT generate <think> blocks, notes, meta comments, analysis, or reasoning explanations. Only answer the user.", false);

            string aiResponse = await runLlamaCpp.GenerateResponse(ChatHistory);

            string cleanedResponse = Regex.Replace(aiResponse.ToString(), "<think>.*?</think>", "", RegexOptions.Singleline).Trim();

            Console.WriteLine(cleanedResponse);

            ChatHistory.Add(("assistant", cleanedResponse));
        }

        Console.WriteLine("\nChat ended.");
    }


    static string BuildPrompt(string systemInstruction, bool isInstructor = false)
    {
        if (isInstructor)
        {
            var sb = new StringBuilder();

            // Add system instruction (only once)
            sb.AppendLine("[INST] <<SYS>>");
            sb.AppendLine(systemInstruction);
            sb.AppendLine("<</SYS>>");

            // Add all previous conversation as separate [INST] blocks
            foreach (var msg in ChatHistory)
            {
                if (msg.role == "user")
                {
                    sb.AppendLine($"[INST] {msg.content} [/INST]");
                }
                else if (msg.role == "assistant")
                {
                    sb.AppendLine(msg.content); // assistant text goes directly after user block
                }
            }

            // Start new input block for the next user question
            sb.Append("[INST] ");

            return sb.ToString();
        }

        else
        {
            // Chat-style template for reasoning/chat models
            var sb = new StringBuilder();

            sb.Append($"<|im_start|>system\n{systemInstruction}<|im_end|>\n");

            foreach (var msg in ChatHistory)
            {
                sb.Append($"<|im_start|>{msg.role}\n{msg.content}<|im_end|>\n");
            }

            sb.Append("<|im_start|>assistant\n");

            return sb.ToString();
        }
    }
}