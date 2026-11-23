using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using System.Windows.Interop;

namespace TrackRelay;

internal sealed class GlobalHotkeys : IDisposable
{
    private const int HotkeyMessage = 0x0312;
    private const uint NoRepeat = 0x4000;
    private readonly HwndSource _source = new(new HwndSourceParameters("TrackRelay shortcuts")
    {
        ParentWindow = new IntPtr(-3)
    });
    private Dictionary<Shortcut, (int Id, ShortcutAction Action)> _active = [];

    public GlobalHotkeys() => _source.AddHook(OnMessage);

    public event Action<ShortcutAction>? Pressed;

    public string? Apply(Preferences preferences, Action? save = null)
    {
        // Keep existing registrations until all new bindings and the settings write succeed.
        // Bindings are keyed by the chord so two actions can swap keys in a single save.
        var next = new Dictionary<Shortcut, (int Id, ShortcutAction Action)>();
        var newlyRegistered = new List<int>();
        var warnings = new List<string>();
        var nextId = 1;

        try
        {
            foreach (var action in Preferences.Actions)
            {
                if (preferences.Shortcuts[action] is not { } shortcut)
                {
                    continue;
                }

                string? error = null;
                if (next.ContainsKey(shortcut))
                {
                    error = Strings.Format("DuplicateShortcut", shortcut, Preferences.Label(action));
                }
                else if (_active.TryGetValue(shortcut, out var existing))
                {
                    next.Add(shortcut, (existing.Id, action));
                }
                else
                {
                    while (_active.Values.Any(binding => binding.Id == nextId) || newlyRegistered.Contains(nextId))
                    {
                        nextId++;
                    }

                    if (RegisterHotKey(_source.Handle, nextId, (uint)shortcut.Modifiers | NoRepeat,
                        (uint)KeyInterop.VirtualKeyFromKey(shortcut.Key)))
                    {
                        next.Add(shortcut, (nextId, action));
                        newlyRegistered.Add(nextId);
                    }
                    else
                    {
                        error = Strings.Format("UnavailableShortcut", shortcut, Preferences.Label(action));
                    }
                }

                if (error is null)
                {
                    continue;
                }
                if (save is not null)
                {
                    return error;
                }
                warnings.Add(error);
            }

            save?.Invoke();
            foreach (var (shortcut, binding) in _active)
            {
                if (!next.ContainsKey(shortcut))
                {
                    UnregisterHotKey(_source.Handle, binding.Id);
                }
            }
            _active = next;
            newlyRegistered.Clear();
            return warnings.Count == 0 ? null : string.Join(Environment.NewLine, warnings);
        }
        finally
        {
            foreach (var id in newlyRegistered)
            {
                UnregisterHotKey(_source.Handle, id);
            }
        }
    }

    private IntPtr OnMessage(IntPtr hwnd, int message, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (message == HotkeyMessage)
        {
            foreach (var binding in _active.Values)
            {
                if (binding.Id == wParam.ToInt32())
                {
                    handled = true;
                    Pressed?.Invoke(binding.Action);
                    break;
                }
            }
        }
        return IntPtr.Zero;
    }

    public void Dispose()
    {
        foreach (var binding in _active.Values)
        {
            UnregisterHotKey(_source.Handle, binding.Id);
        }
        _source.Dispose();
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RegisterHotKey(IntPtr hwnd, int id, uint modifiers, uint key);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnregisterHotKey(IntPtr hwnd, int id);
}
