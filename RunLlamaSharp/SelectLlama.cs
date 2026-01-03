using System.Threading.Tasks;

namespace Run_LlamaSharp
{
    public class SelectLlama : RunLlamaSharp
    {
        public override async Task InitializeAsync(string model)
        {
            await base.InitializeAsync(model);
        }
    }
}
