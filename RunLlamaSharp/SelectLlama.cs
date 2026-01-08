using ModelsDTO;
using Run_LlamaSharp.DTOs;
using System;
using System.Threading.Tasks;

namespace Run_LlamaSharp
{
    public class SelectLlama : RunLlamaSharp
    {
        public override async Task<ResultDTO> InitializeAsync(ModelDTO model)
        {
            return await base.InitializeAsync(model);
        }

        public void UnLoadModel()
        {
            base.UnloadModel();
        }
    }
}
