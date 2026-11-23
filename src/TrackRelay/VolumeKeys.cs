using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;

namespace TrackRelay;

internal sealed class VolumeKeys(Dispatcher dispatcher, Action onSendFailed) : IDisposable
{
    private const int KeyboardHook = 13;
    private const uint KeyDown = 0x0100;
    private const uint KeyUp = 0x0101;
    private const uint SystemKeyUp = 0x0105;
    private const int F10 = 0x79;
    private const int F12 = 0x7B;
    private const ushort VolumeMute = 0xAD;
    private const ushort VolumeDown = 0xAE;
    private const ushort VolumeUp = 0xAF;
    private const uint KeyboardInputType = 1;
    private const uint ExtendedKey = 1;
    private const uint ReleaseKey = 2;

    private readonly bool[] _captured = new bool[3];
    private HookProc? _callback;
    private IntPtr _hook;

    public bool Enabled => _hook != IntPtr.Zero;

    public void SetEnabled(bool enabled)
    {
        if (Enabled == enabled)
        {
            return;
        }

        if (enabled)
        {
            _callback = OnKeyboardInput;
            _hook = SetWindowsHookEx(KeyboardHook, _callback, GetModuleHandle(null), 0);
            if (_hook == IntPtr.Zero)
            {
                throw new Win32Exception();
            }
        }
        else
        {
            if (!UnhookWindowsHookEx(_hook))
            {
                throw new Win32Exception();
            }

            _hook = IntPtr.Zero;
            _callback = null;
            Array.Clear(_captured);
        }
    }

    private IntPtr OnKeyboardInput(int code, IntPtr message, IntPtr data)
    {
        if (code >= 0)
        {
            var virtualKey = Marshal.ReadInt32(data);
            if (virtualKey is >= F10 and <= F12)
            {
                var index = virtualKey - F10;
                var kind = (uint)message.ToInt64();
                if (kind is KeyUp or SystemKeyUp && _captured[index])
                {
                    _captured[index] = false;
                    return new IntPtr(1);
                }

                if (kind == KeyDown && (_captured[index] || !ModifierDown()))
                {
                    if (index != 0 || !_captured[index])
                    {
                        var mediaKey = index switch { 0 => VolumeMute, 1 => VolumeDown, _ => VolumeUp };
                        dispatcher.BeginInvoke(() => Send(mediaKey));
                    }

                    _captured[index] = true;
                    return new IntPtr(1);
                }
            }
        }
        return CallNextHookEx(IntPtr.Zero, code, message, data);
    }

    private void Send(ushort key)
    {
        if (!Enabled)
        {
            return;
        }

        Input[] input =
        [
            new() { Type = KeyboardInputType, Data = new() { Keyboard = new() { Key = key, Flags = ExtendedKey } } },
            new() { Type = KeyboardInputType, Data = new() { Keyboard = new() { Key = key, Flags = ExtendedKey | ReleaseKey } } }
        ];
        if (SendInput((uint)input.Length, input, Marshal.SizeOf<Input>()) != input.Length)
        {
            onSendFailed();
        }
    }

    private static bool ModifierDown() =>
        GetAsyncKeyState(0x10) < 0 || GetAsyncKeyState(0x11) < 0 || GetAsyncKeyState(0x12) < 0 ||
        GetAsyncKeyState(0x5B) < 0 || GetAsyncKeyState(0x5C) < 0;

    public void Dispose()
    {
        if (_hook != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hook);
            _hook = IntPtr.Zero;
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Input
    {
        public uint Type;
        public InputData Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputData
    {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        // INPUT uses a union. MOUSEINPUT supplies its full native size and alignment.
        [FieldOffset(0)] public MouseInput Mouse;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KeyboardInput
    {
        public ushort Key;
        public ushort ScanCode;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MouseInput
    {
        public int X;
        public int Y;
        public uint MouseData;
        public uint Flags;
        public uint Time;
        public UIntPtr ExtraInfo;
    }

    private delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);

    [DllImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int hookId, HookProc callback, IntPtr module, uint threadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hook);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);

    [DllImport("kernel32.dll", EntryPoint = "GetModuleHandleW", CharSet = CharSet.Unicode)]
    private static extern IntPtr GetModuleHandle(string? moduleName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(uint count, [In] Input[] input, int size);

    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int virtualKey);
}
