using Get_Pc_Info;

// RAM
Console.WriteLine($"Total ram: {GetPcInfo.TotalRam / 1024 / 1024 / 1024} GB");
Console.WriteLine($"Available ram: {GetPcInfo.AvailableRam / 1024 / 1024 / 1024} GB");

// OS
Console.WriteLine($"OS: {GetPcInfo.OSVersion}");

// Disk
Console.WriteLine($"Disk type: {GetPcInfo.DiskType}");
Console.WriteLine($"Disk size: {GetPcInfo.DiskSize / 1024 / 1024 / 1024} GB");
Console.WriteLine($"Disk available: {GetPcInfo.DiskFree / 1024 / 1024 / 1024} GB");

// GPUs
foreach (var gpu in GetPcInfo.Gpus)
{
    Console.WriteLine($"GPU: {gpu.Name}, VRAM: {gpu.VRAM / 1024 / 1024} MB");
}

// All CPUs (multi CPU systems)
foreach (var cpu in GetPcInfo.CpuList)
{
    Console.WriteLine($"CPU in list: {cpu.Name}, Logical: {cpu.LogicalCores}, Physical: {cpu.PhysicalCores}");
}

Console.ReadLine();

