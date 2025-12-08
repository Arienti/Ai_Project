using System.Threading.Tasks;

namespace Run_LlamaSharp
{
    public class SelectLlama : RunLlamaSharp
    {
        public override async Task InitializeAsync(string model)
        {
            // Here you can implement custom logic to select different models
            // Based on user input or configuration settings
            // We'll just call the base implementation
            await base.InitializeAsync(model);
        }
    }
}
