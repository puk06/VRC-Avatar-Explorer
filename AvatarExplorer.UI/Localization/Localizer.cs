using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.UI.Localization;

public class Localizer : INotifyPropertyChanged
{
    private readonly List<Dictionary<string, string>> _map;
    private int _selectedLanguageIndex = -1;
    private bool IsValidIndex => _selectedLanguageIndex >= 0 && _selectedLanguageIndex < _map.Count;

    public static Localizer Instance { get; private set; } = new Localizer();
    public event Action? LanguageChanged;

    public int CurrentLanguageIndex
    {
        get => _selectedLanguageIndex;
        private set
        {
            _selectedLanguageIndex = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
            LanguageChanged?.Invoke();
        }
    }

    public int LanguageCount => _map.Count;

    public event PropertyChangedEventHandler? PropertyChanged;

    private Localizer()
    {
        _map = [];
    }

    public void LoadFromFolder(string path)
    {
        if (!Directory.Exists(path)) return;

        var languageMaps = new List<Dictionary<string, string>>();

        foreach (var filePath in FileSystemService.EnumerateFiles(path))
        {
            var deserializeResult = FileSystemService.DeserializeClass<Dictionary<string, string>>(filePath);
            if (deserializeResult.IsError)
            {
                ErrorManager.Instance.PostInternalError($"Failed to load language: '{Path.GetFileName(filePath)}'.", tag: deserializeResult.Errors.ToErrorString());
            }
            else if (!deserializeResult.Value.ContainsKey(Loc.LanguageName))
            {
                ErrorManager.Instance.PostInternalError($"Failed to load language: '{Path.GetFileName(filePath)}'.", tag: "No language name was defined.");
            }
            else
            {
                languageMaps.Add(deserializeResult.Value);
            }
        }

        _map.Clear();
        var sortedMaps = languageMaps.OrderBy(i => ValueParser.Int(i.TryGetValue(Loc.LanguagePriority, out string? value) ? value : string.Empty, int.MaxValue)).ToList();
        _map.AddRange(sortedMaps);
    }

    public string[] GetLanguageList() => _map.Select(i => i[Loc.LanguageName]).ToArray();

    public void SetLanguage(int index) => CurrentLanguageIndex = GetValidLanguageIndex(index);

    private int GetValidLanguageIndex(int index)
    {
        if (_map.Count == 0) return -1;
        return Math.Clamp(index, 0, _map.Count - 1);
    }

    public string Get(string localizationKey) => this[localizationKey];
    public string Get(string localizationKey, string arg)
    {
        if (!IsValidIndex) return localizationKey;
        var localizedText = _map[_selectedLanguageIndex].TryGetValue(localizationKey, out var value) ? value : localizationKey;
        return string.Format(localizedText, arg);
    }
    public string Get(string localizationKey, string[] args)
    {
        if (!IsValidIndex) return localizationKey;
        var localizedText = _map[_selectedLanguageIndex].TryGetValue(localizationKey, out var value) ? value : localizationKey;
        return args.Length > 0 ? string.Format(localizedText, args) : localizedText;
    }

    public string this[string key]
    {
        get
        {
            if (!IsValidIndex) return key;
            return _map[_selectedLanguageIndex].TryGetValue(key, out string? value) ? value : key;
        }
    }

    public string? GetKey(string displayName)
    {
        if (!IsValidIndex) return null;
        return _map[_selectedLanguageIndex].FirstOrDefault(i => i.Value == displayName).Key;
    }
}
