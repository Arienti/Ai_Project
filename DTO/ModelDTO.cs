using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.DTO
{
    public class ModelDTO
    {
        public string modelId { get; set; }
        public string pipeline_tag { get; set; }
        public string createdAt { get; set; }
        public string[] tags { get; set; }
        public string CardData { get; set; }
        public string license { get;set; }
    }
}
