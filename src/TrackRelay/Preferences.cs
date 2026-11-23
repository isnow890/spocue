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

internal enum ToastCorner
{
    BottomRight,
    TopRight,
    BottomLeft,
    TopLeft
}

internal sealed record Preferences(bool ShowTrackChanges, Dictionary<ShortcutAction, Shortcut?> Shortcuts,
    AppLanguage Language = AppLanguage.System, bool CheckUpdates = true,
    ToastCorner ToastCorner = ToastCorner.BottomRight)
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
        .. Enumerable.Range((int)Key.F1, 12).Select(value => new Option<Key>((Key)value, ((Key)value).ToString())),
        new(Key.Left, "Left"), new(Key.Right, "Right"), new(Key.Up, "Up"), new(Key.Down, "Down"),
        .. Enumerable.Range((int)Key.A, 26).Select(value => new Option<Key>((Key)value, ((Key)value).ToString())),
        .. Enumerable.Range(0, 10).Select(value => new Option<Key>(Key.D0 + value, value.ToString()))
    ];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static readonly Dictionary<ShortcutAction, Shortcut> DefaultBindings = new()
    {
        [ShortcutAction.TogglePlayback] = new(Modifiers.Control | Modifiers.Shift, Key.F8),
        [ShortcutAction.NextTrack] = new(Modifiers.Control | Modifiers.Shift, Key.F9),
        [ShortcutAction.PreviousTrack] = new(Modifiers.Control | Modifiers.Shift, Key.F7),
        [ShortcutAction.ShowTrack] = new(Modifiers.Control | Modifiers.Shift, Key.F10),
        [ShortcutAction.SpotifyVolumeUp] = new(Modifiers.Control | Modifiers.Shift, Key.Up),
        [ShortcutAction.SpotifyVolumeDown] = new(Modifiers.Control | Modifiers.Shift, Key.Down),
        [ShortcutAction.CopyTrack] = new(Modifiers.Control | Modifiers.Shift, Key.C)
    };

    public static Preferences Default => new(true,
        Actions.ToDictionary(action => action, action => (Shortcut?)DefaultBindings[action]));

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

            foreach (var action in Actions)
            {
                if (!preferences.Shortcuts.TryGetValue(action, out var shortcut))
                {
                    preferences.Shortcuts[action] = DefaultBindings[action];
                }
                else if (shortcut is { } binding &&
                         (!KeyOptions.Any(option => option.Value == binding.Key) ||
                          !ModifierOptions.Any(option => option.Value == binding.Modifiers)))
                {
                    preferences.Shortcuts[action] = binding.Key == Key.None ? null : DefaultBindings[action];
                }
            }
            return new(preferences.ShowTrackChanges,
                Actions.ToDictionary(action => action, action => preferences.Shortcuts[action]),
                Enum.IsDefined(preferences.Language) ? preferences.Language : AppLanguage.System,
                preferences.CheckUpdates,
                Enum.IsDefined(preferences.ToastCorner) ? preferences.ToastCorner : ToastCorner.BottomRight);
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
