using Run_LlamaSharp.DTOs;
using System.Threading.Tasks;

namespace Run_LlamaSharp
{
    public class SelectLlama : RunLlamaSharp
    {
        public override async Task InitializeAsync(ModelDTO model)
        {
            await base.InitializeAsync(model);
        }

        public void UnLoadModel()
        {
            base.UnloadModel();
        }
    }
}
