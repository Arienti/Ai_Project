using System;
using System.Runtime.InteropServices;
using System.Text;

class Program
{
    // ---------------- DLL Imports ----------------
    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr llama_model_load_from_file([MarshalAs(UnmanagedType.LPStr)] string path);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void llama_model_free(IntPtr model);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr llama_context_create(IntPtr model);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern void llama_context_free(IntPtr ctx);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int llama_tokenize(IntPtr ctx,
                                     [MarshalAs(UnmanagedType.LPStr)] string text,
                                     [Out] int[] tokens,
                                     int maxTokens,
                                     bool add_bos,
                                     bool parse_special);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int llama_eval(IntPtr ctx,
                                 [In] int[] tokens,
                                 int n_tokens);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern int llama_sample_token(IntPtr ctx);

    [DllImport(@"C:\Users\Wizard\Downloads\llama-b7358-bin-win-cpu-x64\llama.dll", CallingConvention = CallingConvention.Cdecl)]
    static extern IntPtr llama_detokenize(IntPtr ctx,
                                          [In] int[] tokens,
                                          int n_tokens);

    // ---------------- Helper for detokenized string ----------------
    static string PtrToStringUtf8(IntPtr ptr)
    {
        if (ptr == IntPtr.Zero) return string.Empty;
        return Marshal.PtrToStringAnsi(ptr);
    }

    // ---------------- Main ----------------
    static void Main()
    {
        string modelPath = @"C:\Users\Wizard\Downloads\Ministral-3-14B-Instruct-2512-Q4_K_M.gguf";
        string prompt = "Hello, can you answer as an AI assistant?";

        // Load model
        IntPtr model = llama_model_load_from_file(modelPath);
        if (model == IntPtr.Zero)
        {
            Console.WriteLine("Failed to load model.");
            return;
        }

        // Create context
        IntPtr ctx = llama_context_create(model);
        if (ctx == IntPtr.Zero)
        {
            Console.WriteLine("Failed to create context.");
            llama_model_free(model);
            return;
        }

        // Tokenize prompt
        int maxTokens = 512;
        int[] tokens = new int[maxTokens];
        int nTokens = llama_tokenize(ctx, prompt, tokens, maxTokens, true, false);
        Console.WriteLine($"Tokenized {nTokens} tokens.");

        // Evaluate tokens (feed them into model)
        int evalResult = llama_eval(ctx, tokens, nTokens);
        if (evalResult != 0)
        {
            Console.WriteLine("Error during evaluation.");
        }

        // Generate new tokens
        int maxGen = 128;
        int[] genTokens = new int[maxGen];
        int genCount = 0;

        for (int i = 0; i < maxGen; i++)
        {
            int token = llama_sample_token(ctx); // generates next token
            if (token < 0) break; // stop if invalid
            genTokens[genCount++] = token;
        }

        // Detokenize generated tokens
        IntPtr outPtr = llama_detokenize(ctx, genTokens, genCount);
        string output = PtrToStringUtf8(outPtr);

        Console.WriteLine("AI Response:");
        Console.WriteLine(output);

        // Cleanup
        llama_context_free(ctx);
        llama_model_free(model);
    }
}
