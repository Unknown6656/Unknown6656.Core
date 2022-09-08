using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Text;
using System;

using Unknown6656.Runtime;

namespace Unknown6656.Controls.Console;


public delegate void ConsoleMouseEvent(MouseEvent r);

public delegate void ConsoleKeyEvent(KeyEvent r);


public static class ConsoleMouseListener
{
    private static volatile bool _running = false;


    public static bool IsRunning => _running;

    public static event ConsoleMouseEvent? MouseEvent;

    public static event ConsoleKeyEvent? KeyEvent;


    public static void Start()
    {
        if (!_running)
        {
            _running = true;

            Task.Factory.StartNew(async delegate
            {
                nint handleIn;
                unsafe
                {
                    handleIn = (nint)ConsoleExtensions.STDINHandle;
                }

                while (_running)
                {
                    INPUT_RECORD[] record = { new() };
                    NativeInterop.ReadConsoleInput(handleIn, record, record.Length, out int read);

                    if (!_running)
                        NativeInterop.WriteConsoleInput(handleIn, record, record.Length, out _);
                    else if (read > 0)
                        switch (record[0].EventType)
                        {
                            case EventType.MouseEvent:
                                MouseEvent?.Invoke(record[0].MouseEvent);
                                break;
                            case EventType.KeyEvent:
                                KeyEvent?.Invoke(record[0].KeyEvent);
                                break;
                            case EventType.BufferSizeEvent:
                                break;
                        }
                    else
                        await Task.Delay(10);
                }
            });
        }
    }

    public static void Stop() => _running = false;
}

[StructLayout(LayoutKind.Explicit)]
internal struct INPUT_RECORD
{
    [FieldOffset(0)]
    public EventType EventType;
    [FieldOffset(4)]
    public KeyEvent KeyEvent;
    [FieldOffset(4)]
    public MouseEvent MouseEvent;
    // [FieldOffset(4)]
    // public (short X, short Y) WindowBufferSizeEvent;
    // [FieldOffset(4)]
    // public MENU_EVENT_RECORD MenuEvent;
    // [FieldOffset(4)]
    // public FOCUS_EVENT_RECORD FocusEvent;
}

public enum EventType
    : ushort
{
    KeyEvent = 1,
    MouseEvent = 2,
    BufferSizeEvent = 4,
    MenuEvent = 8,
    FocusEvent = 16,
}

[Flags]
public enum MouseButtons
    : uint
{
    LeftMost = 0x0001,
    RightMost = 0x0002,
    Button2 = 0x0004,
    Button3 = 0x0008,
    Button4 = 0x0010,
}

[Flags]
public enum MouseActions
    : uint
{
    Movement = 0x0001,
    DoubleClick = 0x0002,
    Wheel = 0x0004,
    HorizontalWheel = 0x0008,
}

[Flags]
public enum ModifierKeysState
    : uint
{
    RightAlt = 0x0001,
    LeftAlt = 0x0002,
    RightCrtl = 0x0004,
    LeftCrtl = 0x0008,
    Shift = 0x0010,
    NumLock = 0x0020,
    ScrollLock = 0x0040,
    CapsLock = 0x0080,
    Enhanced = 0x0100,
}

public struct MouseEvent
{
    public (short X, short Y) dwMousePosition;
    public MouseButtons dwButtonState;
    public ModifierKeysState dwControlKeyState;
    public MouseActions dwEventFlags;
}

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
public struct KeyEvent
{
    [FieldOffset(0)]
    public bool bKeyDown;
    [FieldOffset(4)]
    public ushort wRepeatCount;
    [FieldOffset(6)]
    public ushort wVirtualKeyCode;
    [FieldOffset(8)]
    public ushort wVirtualScanCode;
    [FieldOffset(10)]
    public char UnicodeChar;
    [FieldOffset(10)]
    public byte AsciiChar;
    [FieldOffset(12)]
    public ModifierKeysState dwControlKeyState;
}
