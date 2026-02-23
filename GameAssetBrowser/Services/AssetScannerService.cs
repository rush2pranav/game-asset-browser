using GameAssetBrowser.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameAssetBrowser.Services
{
    public class AssetScannerService
    {
        private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            // These are for the images and the textures
            ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".tga", ".tiff", ".dds", ".svg",
            // these are for the audios
            ".wav", ".mp3", ".ogg", ".flac", ".aiff",
            // these are for the 3D Models
            ".fbx", ".obj", ".blend", ".max", ".dae", ".gltf", ".glb", ".stl",
            // these are for the configs or the data
            ".json", ".xml", ".yaml", ".yml", ".ini", ".cfg", ".toml", ".csv",
            // these are for the scripts
            ".cs", ".lua", ".py", ".shader", ".hlsl", ".glsl",
            // these are for any other common game files
            ".txt", ".md", ".pdf"
        };
        // in order to scan the directory and return all the recognized game assets
        public List<GameAsset> ScanDirectory(string rootPath)
        {
            var assets = new List<GameAsset>();

            if (!Directory.Exists(rootPath))
                return assets;

            try
            {
                var files = Directory.EnumerateFiles(rootPath, "*.*", SearchOption.AllDirectories);

                foreach (var filePath in files)
                {
                    try
                    {
                        var ext = Path.GetExtension(filePath);
                        if (!SupportedExtensions.Contains(ext))
                            continue;

                        var fileInfo = new FileInfo(filePath);
                        var asset = new GameAsset
                        {
                            Name = Path.GetFileNameWithoutExtension(filePath),
                            FullPath = filePath,
                            Extension = ext.ToLower(),
                            Category = GameAsset.GetCategory(ext),
                            FileSizeBytes = fileInfo.Length,
                            DateModified = fileInfo.LastWriteTime,
                            RelativePath = Path.GetRelativePath(rootPath, filePath),
                            Tags = GenerateAutoTags(filePath, rootPath)
                        };

                        assets.Add(asset);
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // skip the files that we cannot access
                    }
                    catch (IOException)
                    {
                        // skip files with the io issues
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error scanning directory: {ex.Message}");
            }

            return assets;
        }

        // this is to auto generate the tags based on the file's folder name and the structure
        private List<string> GenerateAutoTags(string filePath, string rootPath)
        {
            var tags = new List<string>();
            var relativePath = Path.GetRelativePath(rootPath, filePath);
            var directories = Path.GetDirectoryName(relativePath)?.Split(Path.DirectorySeparatorChar);

            if (directories != null)
            {
                foreach (var dir in directories)
                {
                    if (!string.IsNullOrWhiteSpace(dir))
                        tags.Add(dir);
                }
            }

            return tags;
        }

        // return the stats for the collection of assets
        public AssetSummary GetSummary(List<GameAsset> assets)
        {
            return new AssetSummary
            {
                TotalAssets = assets.Count,
                TotalSizeBytes = assets.Sum(a => a.FileSizeBytes),
                ImageCount = assets.Count(a => a.Category == "Image"),
                AudioCount = assets.Count(a => a.Category == "Audio"),
                ModelCount = assets.Count(a => a.Category == "Model"),
                ConfigCount = assets.Count(a => a.Category == "Config"),
                OtherCount = assets.Count(a => a.Category == "Other"),
                UniqueExtensions = assets.Select(a => a.Extension).Distinct().Count()
            };
        }
    }

    // summmary stats for the asset set
    public class AssetSummary
    {
        public int TotalAssets { get; set; }
        public long TotalSizeBytes { get; set; }
        public int ImageCount { get; set; }
        public int AudioCount { get; set; }
        public int ModelCount { get; set; }
        public int ConfigCount { get; set; }
        public int OtherCount { get; set; }
        public int UniqueExtensions { get; set; }

        public string TotalSizeDisplay
        {
            get
            {
                if (TotalSizeBytes < 1024 * 1024) return $"{TotalSizeBytes / 1024.0:F1} KB";
                if (TotalSizeBytes < 1024L * 1024 * 1024) return $"{TotalSizeBytes / (1024.0 * 1024):F1} MB";
                return $"{TotalSizeBytes / (1024.0 * 1024 * 1024):F2} GB";
            }
        }
    }
}
