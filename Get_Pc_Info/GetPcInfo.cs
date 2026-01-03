using NickStrupat;
using SharpDX;
using SharpDX.DXGI;
using System.Management;
using System.Runtime.InteropServices;


namespace Get_Pc_Info
{
    public class GetPcInfo
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct DISPLAY_DEVICE
        {
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        // CPU
        public static List<(string Name, int LogicalCores, int PhysicalCores)> CpuList { get; private set; } = new List<(string, int, int)>();

        // RAM
        public static ulong TotalRam { get; private set; } = 0;
        public static ulong AvailableRam { get; private set; } = 0;

        // GPU
        public static List<(string Name, ulong VRAM)> Gpus { get; private set; } = new List<(string, ulong)>();

        // Disk
        public static string DiskType { get; private set; } = string.Empty;
        public static ulong DiskSize { get; private set; } = 0;
        public static ulong DiskFree { get; private set; } = 0;

        // OS
        public static string OSVersion { get; private set; } = string.Empty;

        static GetPcInfo()
        {
            LoadCpuInfo();
            GetRam();
            LoadGpuInfo();
            LoadDiskInfo();
            GetOSVersion();
        }

        private static void LoadCpuInfo()
        {
            CpuList.Clear();

            int totalLogical = 0;
            int totalPhysical = 0;

            try
            {
                var searcher = new ManagementObjectSearcher(
                    "select Name, NumberOfLogicalProcessors, NumberOfCores from Win32_Processor");

                foreach (var o in searcher.Get())
                {
                    string name = o["Name"]?.ToString() ?? "Unknown CPU";

                    int logical = Convert.ToInt32(o["NumberOfLogicalProcessors"] ?? 0);
                    int physical = Convert.ToInt32(o["NumberOfCores"] ?? 0);

                    CpuList.Add((name, logical, physical));

                    // SUM for final CPU score
                    totalLogical += logical;
                    totalPhysical += physical;
                }
            }
            catch
            {
                string name = Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER") ?? "Unknown CPU";
                int cores = Environment.ProcessorCount;

                CpuList.Add((name, cores, 0));

                totalLogical += cores;
                totalPhysical += 0;
            }
        }

        private static void GetRam()
        {
            try
            {
                var pc = new ComputerInfo();
                TotalRam = pc.TotalPhysicalMemory;
                AvailableRam = pc.AvailablePhysicalMemory;
            }
            catch
            {
                TotalRam = 0;
                AvailableRam = 0;
            }
        }


        private static void LoadGpuInfo()
        {
            Gpus.Clear();

            try
            {
                var searcher = new ManagementObjectSearcher(
                    "SELECT Name, PNPDeviceID FROM Win32_VideoController");

                var list = new List<(string Name, ulong DedicatedVRAM, string Id)>();

                // Get DXGI adapters info
                using var factory = new Factory1();
                var dxgiAdapters = factory.Adapters1;

                foreach (ManagementObject mo in searcher.Get())
                {
                    if (mo == null)
                        continue;

                    string name = "Unknown GPU";
                    string id = Guid.NewGuid().ToString(); // fallback unique ID
                    ulong dedicatedVRAM = 0;

                    try { name = mo["Name"]?.ToString() ?? "Unknown GPU"; } catch { }
                    try { id = mo["PNPDeviceID"]?.ToString() ?? id; } catch { }

                    // Try to match WMI GPU with DXGI adapter by name
                    var dxgiAdapter = dxgiAdapters.FirstOrDefault(a => a.Description.Description.Contains(name));
                    if (dxgiAdapter != null)
                    {
                        PointerSize vramPointer = dxgiAdapter.Description.DedicatedVideoMemory;
                        string s = vramPointer.ToString();
                        if (ulong.TryParse(s, out ulong vram))
                            dedicatedVRAM = vram;
                    }

                    list.Add((name, dedicatedVRAM, id));
                }

                // Remove duplicates by PNPDeviceID
                var unique = list
                    .GroupBy(g => g.Id)
                    .Select(g => (Name: g.First().Name, DedicatedVRAM: g.First().DedicatedVRAM))
                    .ToList();

                foreach (var gpu in unique)
                    Gpus.Add(gpu);
            }
            catch
            {
                // WMI/DXGI error fallback
            }

            // Ensure at least one placeholder GPU exists
            if (Gpus.Count == 0)
                Gpus.Add(("Unknown GPU", 0));
        }




        private static void LoadDiskInfo()
        {
            try
            {
                string? appDriveLetter = Path.GetPathRoot(AppContext.BaseDirectory);
                if (string.IsNullOrEmpty(appDriveLetter))
                {
                    DiskType = "Unknown";
                    DiskSize = 0;
                    DiskFree = 0;
                    return;
                }

                DriveInfo drive = new DriveInfo(appDriveLetter);
                if (drive.IsReady)
                {
                    DiskType = drive.DriveType.ToString();
                    DiskSize = (ulong)drive.TotalSize;
                    DiskFree = (ulong)drive.AvailableFreeSpace;
                }
                else
                {
                    DiskType = "Unknown";
                    DiskSize = 0;
                    DiskFree = 0;
                }
            }
            catch
            {
                DiskType = "Unknown";
                DiskSize = 0;
                DiskFree = 0;
            }
        }


        public static double GetRamBandwidth()
        {
            try
            {
                var searcher = new ManagementObjectSearcher("SELECT Speed, ConfiguredClockSpeed, DataWidth FROM Win32_PhysicalMemory");
                double totalBandwidthGBs = 0;
                int modules = 0;

                foreach (var mo in searcher.Get())
                {
                    int speed = Convert.ToInt32(mo["Speed"] ?? mo["ConfiguredClockSpeed"] ?? 0); // MHz
                    int width = Convert.ToInt32(mo["DataWidth"] ?? 64); // bits

                    if (speed > 0 && width > 0)
                    {
                        // DDR effective: multiply by 2 (DDR), convert bits to bytes
                        double moduleBandwidthMBs = speed * 2 * width / 8.0; // MB/s
                        double moduleBandwidthGBs = moduleBandwidthMBs / 1024.0; // GB/s
                        totalBandwidthGBs += moduleBandwidthGBs;
                        modules++;
                    }
                }

                return modules > 0 ? totalBandwidthGBs : 0;
            }
            catch
            {
                return 0;
            }
        }


        private static void GetOSVersion()
        {
            OSVersion = RuntimeInformation.OSDescription;
        }
    }
}