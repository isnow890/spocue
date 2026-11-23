using System;
using System.IO;
using System.Security;
using Microsoft.Win32;

namespace TrackRelay;

internal static class StartupRegistration
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string EntryName = "SpoCue";
    public const string LaunchArgument = "--startup";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return key?.GetValue(EntryName) is string;
    }

    public static void Save(bool enabled, Action savePreferences)
    {
        using var key = enabled
            ? Registry.CurrentUser.CreateSubKey(RunKey)
            : Registry.CurrentUser.OpenSubKey(RunKey, writable: true);
        if (enabled && key is null)
        {
            throw new IOException("Windows startup settings are unavailable.");
        }

        var previous = key?.GetValue(EntryName, null, RegistryValueOptions.DoNotExpandEnvironmentNames);
        var previousKind = previous is null ? RegistryValueKind.Unknown : key!.GetValueKind(EntryName);
        var command = enabled ? CreateCommand() : null;
        if (Equals(previous, command))
        {
            savePreferences();
            return;
        }

        Write(key!, command, RegistryValueKind.String);
        try
        {
            savePreferences();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or SecurityException)
        {
            Write(key!, previous, previousKind);
            throw;
        }
    }

    private static string CreateCommand()
    {
        var executable = Environment.ProcessPath;
        if (string.IsNullOrWhiteSpace(executable) || !Path.IsPathFullyQualified(executable) ||
            string.Equals(Path.GetFileName(executable), "dotnet.exe", StringComparison.OrdinalIgnoreCase))
        {
            throw new IOException("Windows startup requires the SpoCue executable.");
        }

        return $"\"{executable}\" {LaunchArgument}";
    }

    private static void Write(RegistryKey key, object? value, RegistryValueKind kind)
    {
        if (value is null)
        {
            key.DeleteValue(EntryName, throwOnMissingValue: false);
        }
        else
        {
            key.SetValue(EntryName, value, kind);
        }
    }
}
