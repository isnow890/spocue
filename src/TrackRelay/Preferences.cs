using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Input;

namespace TrackRelay;

internal enum ShortcutAction
{
    TogglePlayback,
    NextTrack,
    PreviousTrack,
    ShowTrack,
    SpotifyVolumeUp,
    SpotifyVolumeDown,
    CopyTrack
}

[Flags]
internal enum Modifiers : uint
{
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8
}

internal readonly record struct Shortcut(Modifiers Modifiers, Key Key)
{
    public override string ToString()
    {
        var modifierValue = Modifiers;
        var keyValue = Key;
        var modifiers = Preferences.ModifierOptions.First(option => option.Value == modifierValue);
        var key = Preferences.KeyOptions.First(option => option.Value == keyValue);
        return $"{modifiers.Label} + {key.Label}";
    }
}

internal sealed record Option<T>(T Value, string Label)
{
    public override string ToString() => Label;
}

internal sealed record Preferences(bool ShowTrackChanges, Dictionary<ShortcutAction, Shortcut?> Shortcuts,
    bool VolumeFunctionKeys = false, AppLanguage Language = AppLanguage.System)
{
    public static readonly ShortcutAction[] Actions = Enum.GetValues<ShortcutAction>();

    public static readonly Option<Modifiers>[] ModifierOptions =
    [
        new(Modifiers.Control | Modifiers.Shift, "Ctrl + Shift"),
        new(Modifiers.Control | Modifiers.Alt, "Ctrl + Alt"),
        new(Modifiers.Alt | Modifiers.Shift, "Alt + Shift"),
        new(Modifiers.Control | Modifiers.Alt | Modifiers.Shift, "Ctrl + Alt + Shift"),
        new(Modifiers.Windows | Modifiers.Shift, "Win + Shift")
    ];

    public static readonly Option<Key>[] KeyOptions =
    [
        new(Key.None, "Off"),
        .. Enumerable.Range((int)Key.F1, 11).Select(value => new Option<Key>((Key)value, ((Key)value).ToString())),
        new(Key.Left, "Left"), new(Key.Right, "Right"), new(Key.Up, "Up"), new(Key.Down, "Down"),
        .. Enumerable.Range((int)Key.A, 26).Select(value => new Option<Key>((Key)value, ((Key)value).ToString())),
        .. Enumerable.Range(0, 10).Select(value => new Option<Key>(Key.D0 + value, value.ToString()))
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static Preferences Default => new(true, new()
    {
        [ShortcutAction.TogglePlayback] = new(Modifiers.Control | Modifiers.Shift, Key.F8),
        [ShortcutAction.NextTrack] = new(Modifiers.Control | Modifiers.Shift, Key.F9),
        [ShortcutAction.PreviousTrack] = new(Modifiers.Control | Modifiers.Shift, Key.F7),
        [ShortcutAction.ShowTrack] = new(Modifiers.Control | Modifiers.Shift, Key.F10),
        [ShortcutAction.SpotifyVolumeUp] = new(Modifiers.Control | Modifiers.Shift, Key.Up),
        [ShortcutAction.SpotifyVolumeDown] = new(Modifiers.Control | Modifiers.Shift, Key.Down),
        [ShortcutAction.CopyTrack] = new(Modifiers.Control | Modifiers.Shift, Key.C)
    });

    public static string FilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpoCue", "settings.json");

    private static readonly string LegacyFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TrackRelay", "settings.json");

    public static Preferences Load(out bool firstRun, out string? notice)
    {
        firstRun = false;
        notice = null;
        try
        {
            var preferences = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(File.Exists(FilePath) ? FilePath : LegacyFilePath), JsonOptions);
            if (preferences?.Shortcuts is null)
            {
                throw new JsonException("Missing shortcuts.");
            }

            var defaults = Default;
            foreach (var action in Actions)
            {
                if (!preferences.Shortcuts.TryGetValue(action, out var shortcut) ||
                    (shortcut is { } binding &&
                     (binding.Key == Key.None || !KeyOptions.Any(option => option.Value == binding.Key) ||
                      !ModifierOptions.Any(option => option.Value == binding.Modifiers))))
                {
                    preferences.Shortcuts[action] = defaults.Shortcuts[action];
                }
            }
            return new(preferences.ShowTrackChanges,
                Actions.ToDictionary(action => action, action => preferences.Shortcuts[action]),
                preferences.VolumeFunctionKeys,
                Enum.IsDefined(preferences.Language) ? preferences.Language : AppLanguage.System);
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException)
        {
            firstRun = true;
            return Default;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            notice = Strings.Get("ReadSettingsError");
            return Default;
        }
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        var temporaryFile = FilePath + ".tmp";
        File.WriteAllText(temporaryFile, JsonSerializer.Serialize(this, JsonOptions));
        File.Move(temporaryFile, FilePath, overwrite: true);
    }

    public static string Label(ShortcutAction action) => action switch
    {
        ShortcutAction.TogglePlayback => Strings.Get("ActionPlayPause"),
        ShortcutAction.NextTrack => Strings.Get("ActionNext"),
        ShortcutAction.PreviousTrack => Strings.Get("ActionPrevious"),
        ShortcutAction.ShowTrack => Strings.Get("ActionShowTrack"),
        ShortcutAction.SpotifyVolumeUp => Strings.Get("ActionSpotifyVolumeUp"),
        ShortcutAction.SpotifyVolumeDown => Strings.Get("ActionSpotifyVolumeDown"),
        ShortcutAction.CopyTrack => Strings.Get("ActionCopyTrack"),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
