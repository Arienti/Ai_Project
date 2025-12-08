using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ai_Project.DTO
{
    public class PcInfo
    {
        public string CpuName { get; private set; } = string.Empty;

        public int CpuCores { get; private set; } = 0;

        public ulong TotalRam { get; private set; } = 0;

        public ulong AvailableRam { get; private set; } = 0;

        public string GpuName { get; private set; } = string.Empty;

        public ulong GpuVram { get; private set; } = 0;

        public string DiskType { get; private set; } = string.Empty;

        public ulong DiskSize { get; private set; } = 0;

        public ulong DiskFree { get; private set; } = 0;

        public string OSVersion { get; private set; } = string.Empty;
    }
}
