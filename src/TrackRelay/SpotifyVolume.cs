using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

namespace TrackRelay;

internal enum VolumeResult
{
    Adjusted,
    /// <summary>No Spotify audio session on this PC (nothing played yet, or Spotify closed).</summary>
    Unavailable,
    /// <summary>Spotify is playing, but not through this PC's audio (e.g. Spotify Connect to a phone).</summary>
    PlayingElsewhere
}

internal static class SpotifyVolume
{
    private const float Step = 0.05f;
    private const int RenderFlow = 0;              // eRender
    private const int DeviceStateActive = 0x1;     // DEVICE_STATE_ACTIVE
    private const int ClsctxAll = 23;              // CLSCTX_ALL
    private const int AudioSessionStateActive = 1; // AudioSessionStateActive
    private static readonly Guid DeviceEnumeratorClsid = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid AudioSessionManager2Iid = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");

    /// <param name="allowIdleSessions">
    /// When false, only sessions that are producing sound on this PC are adjusted. Pass false while Spotify reports
    /// playback: an idle local session then means the audio is on another device (Spotify Connect), so changing it
    /// would be silent and the toast would claim a change nobody can hear.
    /// </param>
    public static VolumeResult TryAdjust(int direction, bool allowIdleSessions, out int percent)
    {
        percent = 0;
        var enumeratorType = Type.GetTypeFromCLSID(DeviceEnumeratorClsid);
        if (enumeratorType is null)
        {
            return VolumeResult.Unavailable;
        }

        IMMDeviceEnumerator? enumerator = null;
        IMMDeviceCollection? devices = null;
        var sessions = new List<SpotifyAudioSession>();

        try
        {
            enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType)!;
            // Spotify can be routed to any output (Windows per-app routing or Spotify's own device picker),
            // so look through every active render endpoint instead of only the default one.
            if (enumerator.EnumAudioEndpoints(RenderFlow, DeviceStateActive, out devices) != 0 || devices is null ||
                devices.GetCount(out var deviceCount) != 0)
            {
                return VolumeResult.Unavailable;
            }

            for (uint index = 0; index < deviceCount; index++)
            {
                if (devices.Item(index, out var device) != 0 || device is null)
                {
                    continue;
                }

                try
                {
                    CollectSpotifySessions(device, sessions);
                }
                catch (Exception exception) when (exception is COMException or InvalidCastException or UnauthorizedAccessException)
                {
                    // One misbehaving endpoint (e.g. a disconnecting Bluetooth headset) must not block the others.
                }
                finally
                {
                    Marshal.ReleaseComObject(device);
                }
            }

            if (sessions.Count == 0)
            {
                return VolumeResult.Unavailable;
            }

            // Prefer sessions that are producing sound: switching outputs can leave an idle Spotify session behind.
            var targets = sessions.Where(session => session.IsActive).ToList();
            if (targets.Count == 0)
            {
                if (!allowIdleSessions)
                {
                    return VolumeResult.PlayingElsewhere;
                }
                targets = sessions;
            }
            if (targets[0].Volume.GetMasterVolume(out var current) != 0)
            {
                return VolumeResult.Unavailable;
            }

            var target = Math.Clamp((float)Math.Round((current + (direction * Step)) * 20f) / 20f, 0f, 1f);
            var adjusted = false;
            foreach (var session in targets)
            {
                if (target > 0f && session.Volume.GetMute(out var muted) == 0 && muted)
                {
                    session.Volume.SetMute(false, Guid.Empty);
                }

                if (session.Volume.SetMasterVolume(target, Guid.Empty) == 0)
                {
                    adjusted = true;
                }
            }

            if (!adjusted)
            {
                return VolumeResult.Unavailable;
            }
            percent = (int)Math.Round(target * 100f);
            return VolumeResult.Adjusted;
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or UnauthorizedAccessException)
        {
            return VolumeResult.Unavailable;
        }
        finally
        {
            foreach (var session in sessions)
            {
                Marshal.ReleaseComObject(session.Control);
            }
            if (devices is not null) Marshal.ReleaseComObject(devices);
            if (enumerator is not null) Marshal.ReleaseComObject(enumerator);
        }
    }

    private static void CollectSpotifySessions(IMMDevice device, List<SpotifyAudioSession> sessions)
    {
        object? managerObject = null;
        IAudioSessionEnumerator? sessionEnumerator = null;
        try
        {
            var iid = AudioSessionManager2Iid;
            if (device.Activate(ref iid, ClsctxAll, IntPtr.Zero, out managerObject) != 0 ||
                managerObject is not IAudioSessionManager2 manager)
            {
                return;
            }

            if (manager.GetSessionEnumerator(out sessionEnumerator) != 0 || sessionEnumerator is null ||
                sessionEnumerator.GetCount(out var count) != 0)
            {
                return;
            }

            for (var index = 0; index < count; index++)
            {
                if (sessionEnumerator.GetSession(index, out var control) != 0 || control is null)
                {
                    continue;
                }

                var keep = false;
                try
                {
                    if (control is IAudioSessionControl2 control2 &&
                        control is ISimpleAudioVolume simpleVolume &&
                        IsSpotifySession(control2))
                    {
                        var isActive = control2.GetState(out var state) == 0 && state == AudioSessionStateActive;
                        sessions.Add(new SpotifyAudioSession(control, simpleVolume, isActive));
                        keep = true;
                    }
                }
                finally
                {
                    if (!keep)
                    {
                        Marshal.ReleaseComObject(control);
                    }
                }
            }
        }
        finally
        {
            if (sessionEnumerator is not null) Marshal.ReleaseComObject(sessionEnumerator);
            if (managerObject is not null) Marshal.ReleaseComObject(managerObject);
        }
    }

    private sealed record SpotifyAudioSession(object Control, ISimpleAudioVolume Volume, bool IsActive);

    private static bool IsSpotifySession(IAudioSessionControl2 control)
    {
        if (control.GetProcessId(out var pid) == 0 && pid != 0)
        {
            try
            {
                using var process = Process.GetProcessById((int)pid);
                if (string.Equals(process.ProcessName, "Spotify", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
            {
            }
        }

        if (control.GetSessionIdentifier(out var identifier) == 0 && !string.IsNullOrEmpty(identifier))
        {
            return identifier.Contains("Spotify.exe", StringComparison.OrdinalIgnoreCase) ||
                   identifier.Contains("SpotifyAB.SpotifyMusic", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    [ComImport]
    [Guid("A95664D2-9614-4F35-A746-DE8DB63617E6")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig]
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IMMDeviceCollection? devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? endpoint);
    }

    [ComImport]
    [Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig]
        int GetCount(out uint count);

        [PreserveSig]
        int Item(uint index, out IMMDevice? device);
    }

    [ComImport]
    [Guid("D666063F-1587-4E43-81F1-B948E807363F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig]
        int Activate(ref Guid iid, int clsCtx, IntPtr activationParams, [MarshalAs(UnmanagedType.IUnknown)] out object? interfacePointer);
    }

    [ComImport]
    [Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionManager2
    {
        [PreserveSig]
        int GetAudioSessionControl(IntPtr audioSessionGuid, uint streamFlags, out IntPtr sessionControl);

        [PreserveSig]
        int GetSimpleAudioVolume(IntPtr audioSessionGuid, uint streamFlags, out IntPtr audioVolume);

        [PreserveSig]
        int GetSessionEnumerator(out IAudioSessionEnumerator? sessionEnum);
    }

    [ComImport]
    [Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionEnumerator
    {
        [PreserveSig]
        int GetCount(out int sessionCount);

        [PreserveSig]
        int GetSession(int sessionCount, [MarshalAs(UnmanagedType.Interface)] out object? session);
    }

    [ComImport]
    [Guid("bfb7ff88-7239-4fc9-8fa2-07c950be9c6d")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioSessionControl2
    {
        [PreserveSig]
        int GetState(out int state);

        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string? displayName);

        [PreserveSig]
        int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string value, [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string? path);

        [PreserveSig]
        int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string value, [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetGroupingParam(out Guid groupingParam);

        [PreserveSig]
        int SetGroupingParam([MarshalAs(UnmanagedType.LPStruct)] Guid @override, [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int RegisterAudioSessionNotification(IntPtr newNotifications);

        [PreserveSig]
        int UnregisterAudioSessionNotification(IntPtr newNotifications);

        [PreserveSig]
        int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string? retVal);

        [PreserveSig]
        int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string? retVal);

        [PreserveSig]
        int GetProcessId(out uint retVal);

        [PreserveSig]
        int IsSystemSoundsSession();

        [PreserveSig]
        int SetDuckingPreference([MarshalAs(UnmanagedType.Bool)] bool optOut);
    }

    [ComImport]
    [Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface ISimpleAudioVolume
    {
        [PreserveSig]
        int SetMasterVolume(float level, [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetMasterVolume(out float level);

        [PreserveSig]
        int SetMute([MarshalAs(UnmanagedType.Bool)] bool muted, [MarshalAs(UnmanagedType.LPStruct)] Guid eventContext);

        [PreserveSig]
        int GetMute([MarshalAs(UnmanagedType.Bool)] out bool muted);
    }
}
