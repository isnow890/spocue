using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace TrackRelay;

internal static class SpotifyVolume
{
    private const float Step = 0.05f;
    private static readonly Guid DeviceEnumeratorClsid = new("BCDE0395-E52F-467C-8E3D-C4579291692E");
    private static readonly Guid AudioSessionManager2Iid = new("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F");

    public static bool TryAdjust(int direction, out int percent)
    {
        percent = 0;
        var enumeratorType = Type.GetTypeFromCLSID(DeviceEnumeratorClsid);
        if (enumeratorType is null)
        {
            return false;
        }

        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        object? managerObject = null;
        IAudioSessionEnumerator? sessionEnumerator = null;

        try
        {
            enumerator = (IMMDeviceEnumerator)Activator.CreateInstance(enumeratorType)!;
            // eRender = 0, eMultimedia = 1
            if (enumerator.GetDefaultAudioEndpoint(0, 1, out device) != 0 || device is null)
            {
                return false;
            }

            var iid = AudioSessionManager2Iid;
            if (device.Activate(ref iid, 23, IntPtr.Zero, out managerObject) != 0 ||
                managerObject is not IAudioSessionManager2 manager)
            {
                return false;
            }

            if (manager.GetSessionEnumerator(out sessionEnumerator) != 0 || sessionEnumerator is null ||
                sessionEnumerator.GetCount(out var count) != 0)
            {
                return false;
            }

            var adjusted = false;
            for (var index = 0; index < count; index++)
            {
                if (sessionEnumerator.GetSession(index, out var control) != 0 || control is null)
                {
                    continue;
                }

                try
                {
                    if (control is not IAudioSessionControl2 control2 ||
                        control is not ISimpleAudioVolume simpleVolume ||
                        !IsSpotifySession(control2))
                    {
                        continue;
                    }

                    if (simpleVolume.GetMasterVolume(out var current) != 0)
                    {
                        continue;
                    }

                    var target = Math.Clamp((float)Math.Round((current + (direction * Step)) * 20f) / 20f, 0f, 1f);
                    if (target > 0f && simpleVolume.GetMute(out var muted) == 0 && muted)
                    {
                        simpleVolume.SetMute(false, Guid.Empty);
                    }

                    if (simpleVolume.SetMasterVolume(target, Guid.Empty) == 0)
                    {
                        percent = (int)Math.Round(target * 100f);
                        adjusted = true;
                    }
                }
                finally
                {
                    Marshal.ReleaseComObject(control);
                }
            }

            return adjusted;
        }
        catch (Exception exception) when (exception is COMException or InvalidCastException or UnauthorizedAccessException)
        {
            return false;
        }
        finally
        {
            if (sessionEnumerator is not null) Marshal.ReleaseComObject(sessionEnumerator);
            if (managerObject is not null) Marshal.ReleaseComObject(managerObject);
            if (device is not null) Marshal.ReleaseComObject(device);
            if (enumerator is not null) Marshal.ReleaseComObject(enumerator);
        }
    }

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
        int EnumAudioEndpoints(int dataFlow, int stateMask, out IntPtr devices);

        [PreserveSig]
        int GetDefaultAudioEndpoint(int dataFlow, int role, out IMMDevice? endpoint);
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
