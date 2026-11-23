using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TrackRelay;

internal static class SpotifyWindow
{
    public static bool Open()
    {
        try
        {
            var processes = Process.GetProcessesByName("Spotify");
            try
            {
                foreach (var process in processes)
                {
                    IntPtr window;
                    try
                    {
                        window = process.MainWindowHandle;
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }

                    if (window == IntPtr.Zero)
                    {
                        continue;
                    }
                    if (IsIconic(window))
                    {
                        ShowWindowAsync(window, 9); // SW_RESTORE
                    }
                    if (SetForegroundWindow(window))
                    {
                        return true;
                    }
                }
            }
            finally
            {
                foreach (var process in processes)
                {
                    process.Dispose();
                }
            }

            using var launched = Process.Start(new ProcessStartInfo("spotify:") { UseShellExecute = true });
            return true;
        }
        catch (Exception exception) when (exception is Win32Exception or InvalidOperationException)
        {
            return false;
        }
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindowAsync(IntPtr window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(IntPtr window);
}
