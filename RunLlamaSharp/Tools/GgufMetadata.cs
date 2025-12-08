using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Run_LlamaSharp.Tools
{
    public class GgufMetadata
    {
        public string ModelName { get; set; } = "";
        public uint NCtx { get; set; } = 4096;       // context size
        public uint NLayers { get; set; } = 0;       // number of transformer layers
        public uint Dim { get; set; } = 0;           // embedding dimension
        public string FType { get; set; } = "";      // quantization type (Q4_0, F16, etc.)
        public double ModelSizeB { get; private set; } = 0; // size in billions (B)
        public Dictionary<string, object> RawMetadata { get; set; } = new();

        public static GgufMetadata ReadFromFile(string modelPath)
        {
            var meta = new GgufMetadata();
            meta.ModelName = Path.GetFileName(modelPath);

            using var fs = new FileStream(modelPath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // Verify GGUF header
            var magic = br.ReadBytes(4);
            if (Encoding.ASCII.GetString(magic) != "GGUF")
                throw new Exception("Not a GGUF file");

            br.BaseStream.Seek(4, SeekOrigin.Current); // Skip version/reserved
            uint kvCount = br.ReadUInt32();

            for (uint i = 0; i < kvCount; i++)
            {
                byte keyLen = br.ReadByte();
                string key = Encoding.UTF8.GetString(br.ReadBytes(keyLen));
                byte valType = br.ReadByte();

                object? val = valType switch
                {
                    0 => br.ReadUInt32(),
                    1 => br.ReadSingle(),
                    2 => ReadString(br),
                    _ => null
                };

                if (val != null)
                    meta.RawMetadata[key] = val;

                switch (key)
                {
                    case "n_ctx": meta.NCtx = Convert.ToUInt32(val); break;
                    case "n_layers": meta.NLayers = Convert.ToUInt32(val); break;
                    case "dim": meta.Dim = Convert.ToUInt32(val); break;
                    case "ftype": meta.FType = val?.ToString() ?? ""; break;
                }
            }

            // Fill NLayers, Dim, FType, and ModelSizeB from filename if missing
            FillFromModelName(meta);

            return meta;
        }

        private static string ReadString(BinaryReader br)
        {
            byte len = br.ReadByte();
            return Encoding.UTF8.GetString(br.ReadBytes(len));
        }

        private static void FillFromModelName(GgufMetadata meta)
        {
            var name = meta.ModelName.ToLower();

            // Extract FType from filename if missing
            if (string.IsNullOrEmpty(meta.FType))
            {
                var ftypeMatch = Regex.Match(name, @"q\d+_[a-z0-9_]+|f16|f32|bf16", RegexOptions.IgnoreCase);
                if (ftypeMatch.Success)
                    meta.FType = ftypeMatch.Value.ToUpper();
            }

            // Extract model size (#B) from filename
            var sizeMatch = Regex.Match(name, @"(\d+(\.\d+)?)b", RegexOptions.IgnoreCase);
            if (sizeMatch.Success)
            {
                double sizeB = double.Parse(sizeMatch.Groups[1].Value);
                meta.ModelSizeB = sizeB; // set the new property

                if (meta.NLayers == 0 || meta.Dim == 0)
                    SetLayersDimBySize(meta, sizeB);
            }
        }

        private static void SetLayersDimBySize(GgufMetadata meta, double sizeB)
        {
            var table = new Dictionary<double, (uint NLayers, uint Dim)>()
            {
                { 3,  (24, 2560) },
                { 7,  (32, 4096) },
                { 13, (40, 5120) },
                { 30, (60, 8192) },
                { 33, (64, 8192) },
                { 65, (80, 12288) },
                { 70, (80, 12288) },
                { 120, (96, 16384) },
                { 150, (96, 16384) }
            };

            double nearest = 3;
            foreach (var key in table.Keys)
            {
                if (key <= sizeB && key > nearest)
                    nearest = key;
            }

            meta.NLayers = table[nearest].NLayers;
            meta.Dim = table[nearest].Dim;
        }

        public override string ToString()
        {
            return $"ModelName: {ModelName}, NCtx: {NCtx}, NLayers: {NLayers}, Dim: {Dim}, FType: {FType}, ModelSizeB: {ModelSizeB}";
        }
    }
}