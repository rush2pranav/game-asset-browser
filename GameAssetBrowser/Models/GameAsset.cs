using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAssetBrowser.Models
{
    public class GameAsset
    {
        public string Name { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public string Extension { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime DateModified { get; set; }
        public string RelativePath { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new();
        public string FileSizeDisplay
        {
            get
            {
                if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
                if (FileSizeBytes < 1024 * 1024) return $"{FileSizeBytes / 1024.0:F1} KB";
                if (FileSizeBytes < 1024 * 1024 * 1024) return $"{FileSizeBytes / (1024.0 * 1024):F1} MB";
                return $"{FileSizeBytes / (1024.0 * 1024 * 1024):F2} GB";
            }
        }
        // Icon representation based on the file category
        public string CategoryIcon => Category switch
        {
            "Image" => "🖼️",
            "Audio" => "🔊",
            "Model" => "🧊",
            "Config" => "⚙️",
            _ => "📄"
        };
        // To determine the asset category based on the file extension
        public static string GetCategory(string extension)
        {
            return extension.ToLower() switch
            {
                ".png" or ".jpg" or ".jpeg" or ".bmp" or ".gif" or ".tga" or ".tiff" or ".dds" or ".svg" => "Image",
                ".wav" or ".mp3" or ".ogg" or ".flac" or ".aiff" => "Audio",
                ".fbx" or ".obj" or ".blend" or ".max" or ".dae" or ".gltf" or ".glb" or ".stl" => "Model",
                ".json" or ".xml" or ".yaml" or ".yml" or ".ini" or ".cfg" or ".toml" or ".csv" => "Config",
                _ => "Other"
            };
        }
    }
}
