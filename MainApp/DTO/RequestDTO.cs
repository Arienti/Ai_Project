using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.DTO
{
    public class RequestDTO
    {
        public string prompt { get; set; } = string.Empty;
        public string model { get; set; } = string.Empty;
        public bool stream { get; set; } = false;
        public uint max_new_tokens { get; set; }
    }
}
