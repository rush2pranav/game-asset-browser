using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using GameAssetBrowser.Commands;
using GameAssetBrowser.Models;
using GameAssetBrowser.Services;

namespace GameAssetBrowser.ViewModels
{
    public class MainViewModel : BaseViewModel
    {
        private readonly AssetScannerService _scanner;
        private List<GameAsset> _allAssets = new();
        // constructor
        public MainViewModel()
        {
            _scanner = new AssetScannerService();
            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
            ClearSearchCommand = new RelayCommand(_ => ClearSearch());
            ClearFiltersCommand = new RelayCommand(_ => ClearFilters());
            FilteredAssets = new ObservableCollection<GameAsset>();
            AvailableCategories = new ObservableCollection<string> { "All" };
            AvailableTags = new ObservableCollection<string>();
            SortOptions = new ObservableCollection<string>
            {
                "Name (A-Z)", "Name (Z-A)",
                "Size (Smallest)", "Size (Largest)",
                "Date (Newest)", "Date (Oldest)",
                "Category"
            };
            SelectedSort = "Name (A-Z)";

            StatusText = "Select a folder to browse game assets";
        }

        // commands which are bound to buttons in the view
        public ICommand BrowseFolderCommand { get; }
        public ICommand ClearSearchCommand { get; }
        public ICommand ClearFiltersCommand { get; }

        // collections
        public ObservableCollection<GameAsset> FilteredAssets { get; }
        public ObservableCollection<string> AvailableCategories { get; }
        public ObservableCollection<string> AvailableTags { get; }
        public ObservableCollection<string> SortOptions { get; }

        // Each property calls SetProperty which triggers UI updates
        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                    ApplyFilters();
            }
        }

        private string _selectedCategory = "All";
        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                    ApplyFilters();
            }
        }

        private string? _selectedTag;
        public string? SelectedTag
        {
            get => _selectedTag;
            set
            {
                if (SetProperty(ref _selectedTag, value))
                    ApplyFilters();
            }
        }

        private string _selectedSort = "Name (A-Z)";
        public string SelectedSort
        {
            get => _selectedSort;
            set
            {
                if (SetProperty(ref _selectedSort, value))
                    ApplyFilters();
            }
        }

        private GameAsset? _selectedAsset;
        public GameAsset? SelectedAsset
        {
            get => _selectedAsset;
            set
            {
                if (SetProperty(ref _selectedAsset, value))
                {
                    OnPropertyChanged(nameof(IsAssetSelected));
                    OnPropertyChanged(nameof(PreviewImagePath));
                    OnPropertyChanged(nameof(IsImageAsset));
                }
            }
        }

        private string _statusText = string.Empty;
        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        private string _currentFolderPath = string.Empty;
        public string CurrentFolderPath
        {
            get => _currentFolderPath;
            set => SetProperty(ref _currentFolderPath, value);
        }

        // Summary stats
        private int _totalAssetCount;
        public int TotalAssetCount
        {
            get => _totalAssetCount;
            set => SetProperty(ref _totalAssetCount, value);
        }

        private string _totalSizeDisplay = "0 KB";
        public string TotalSizeDisplay
        {
            get => _totalSizeDisplay;
            set => SetProperty(ref _totalSizeDisplay, value);
        }

        private int _imageCount;
        public int ImageCount
        {
            get => _imageCount;
            set => SetProperty(ref _imageCount, value);
        }

        private int _audioCount;
        public int AudioCount
        {
            get => _audioCount;
            set => SetProperty(ref _audioCount, value);
        }

        private int _modelCount;
        public int ModelCount
        {
            get => _modelCount;
            set => SetProperty(ref _modelCount, value);
        }

        private int _configCount;
        public int ConfigCount
        {
            get => _configCount;
            set => SetProperty(ref _configCount, value);
        }

        // Computed properties for the View
        public bool IsAssetSelected => SelectedAsset != null;
        public bool IsImageAsset => SelectedAsset?.Category == "Image";
        public string? PreviewImagePath => IsImageAsset ? SelectedAsset?.FullPath : null;

        // methods

        private void BrowseFolder()
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Select Game Assets Folder"
            };

            if (dialog.ShowDialog() == true)
            {
                LoadAssets(dialog.FolderName);
            }
        }

        // scans the directory and loads all the discovered assets
        private void LoadAssets(string folderPath)
        {
            CurrentFolderPath = folderPath;
            StatusText = "Scanning folder...";

            _allAssets = _scanner.ScanDirectory(folderPath);

            // Update summary
            var summary = _scanner.GetSummary(_allAssets);
            TotalAssetCount = summary.TotalAssets;
            TotalSizeDisplay = summary.TotalSizeDisplay;
            ImageCount = summary.ImageCount;
            AudioCount = summary.AudioCount;
            ModelCount = summary.ModelCount;
            ConfigCount = summary.ConfigCount;

            // Populate category filter
            AvailableCategories.Clear();
            AvailableCategories.Add("All");
            foreach (var cat in _allAssets.Select(a => a.Category).Distinct().OrderBy(c => c))
                AvailableCategories.Add(cat);

            // Populate tag filter
            AvailableTags.Clear();
            foreach (var tag in _allAssets.SelectMany(a => a.Tags).Distinct().OrderBy(t => t))
                AvailableTags.Add(tag);

            // Reset filters and apply
            _selectedCategory = "All";
            OnPropertyChanged(nameof(SelectedCategory));
            _searchText = string.Empty;
            OnPropertyChanged(nameof(SearchText));

            ApplyFilters();

            StatusText = $"Loaded {_allAssets.Count} assets from {folderPath}";
        }
        private void ApplyFilters()
        {
            var filtered = _allAssets.AsEnumerable();

            // Category filter
            if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "All")
                filtered = filtered.Where(a => a.Category == SelectedCategory);

            // Tag filter
            if (!string.IsNullOrEmpty(SelectedTag))
                filtered = filtered.Where(a => a.Tags.Contains(SelectedTag));

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var search = SearchText.ToLower();
                filtered = filtered.Where(a =>
                    a.Name.ToLower().Contains(search) ||
                    a.RelativePath.ToLower().Contains(search) ||
                    a.Extension.ToLower().Contains(search) ||
                    a.Tags.Any(t => t.ToLower().Contains(search))
                );
            }

            // Sort
            filtered = SelectedSort switch
            {
                "Name (A-Z)" => filtered.OrderBy(a => a.Name),
                "Name (Z-A)" => filtered.OrderByDescending(a => a.Name),
                "Size (Smallest)" => filtered.OrderBy(a => a.FileSizeBytes),
                "Size (Largest)" => filtered.OrderByDescending(a => a.FileSizeBytes),
                "Date (Newest)" => filtered.OrderByDescending(a => a.DateModified),
                "Date (Oldest)" => filtered.OrderBy(a => a.DateModified),
                "Category" => filtered.OrderBy(a => a.Category).ThenBy(a => a.Name),
                _ => filtered.OrderBy(a => a.Name)
            };

            // Update the observable collection
            FilteredAssets.Clear();
            foreach (var asset in filtered)
                FilteredAssets.Add(asset);

            StatusText = $"Showing {FilteredAssets.Count} of {_allAssets.Count} assets";
        }

        private void ClearSearch()
        {
            SearchText = string.Empty;
        }

        private void ClearFilters()
        {
            SearchText = string.Empty;
            SelectedCategory = "All";
            SelectedTag = null;
            SelectedSort = "Name (A-Z)";
        }
    }
}