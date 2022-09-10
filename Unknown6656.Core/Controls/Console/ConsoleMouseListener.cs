using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System;

using Unknown6656.Generics;
using Unknown6656.Runtime;

namespace Unknown6656.Controls.Console;


public delegate void ConsoleMouseEventHandler(int x, int y, MouseButtons buttons, ModifierKeysState modifiers);


public static class ConsoleMouseListener
{
    private static volatile bool _running = false;


    public static bool IsRunning => _running;

    public static event ConsoleMouseEventHandler? MouseMove;
    public static event ConsoleMouseEventHandler? MouseDoubleClick;
    public static event ConsoleMouseEventHandler? MouseHorizontalWheel;
    public static event ConsoleMouseEventHandler? MouseVerticalWheel;
    // TODO : key events

    public static void Start()
    {
        if (!_running)
        {
            _running = true;

            Task.Factory.StartNew(async delegate
            {
                nint handle;
                unsafe
                {
                    handle = (nint)ConsoleExtensions.STDINHandle;
                }
                ConsoleMode mode = ConsoleExtensions.STDINConsoleMode;
                ConsoleExtensions.STDINConsoleMode = (mode | ConsoleMode.ENABLE_MOUSE_INPUT
                                                           | ConsoleMode.ENABLE_WINDOW_INPUT
                                                           | ConsoleMode.ENABLE_EXTENDED_FLAGS)
                                                          & ~ConsoleMode.ENABLE_QUICK_EDIT_MODE;

                while (_running)
                    if (NativeInterop.GetNumberOfConsoleInputEvents(handle, out int count))
                        try
                        {
                            List<INPUT_RECORD> records = Enumerable.Repeat(new INPUT_RECORD(), count).ToList();
                            NativeInterop.ReadConsoleInput(handle, records.GetInternalArray(), count, out int read);

                            if (read < records.Count)
                                records.RemoveRange(read, records.Count - read);

                            for (int i = 0; i < records.Count; ++i)
                                if (records[i] is { EventType: EventType.MouseEvent, MouseEvent: { } @event })
                                {
                                    (@event.dwEventFlags switch
                                    {
                                        MouseActions.Movement => MouseMove,
                                        MouseActions.DoubleClick => MouseDoubleClick,
                                        MouseActions.Wheel => MouseVerticalWheel,
                                        MouseActions.HorizontalWheel => MouseHorizontalWheel,
                                        _ => null
                                    })?.Invoke(@event.wMousePositionX, @event.wMousePositionY, @event.dwButtonState, @event.dwControlKeyState);
                                    records.RemoveAt(i--);
                                }
                                // TODO : key event

                            if (records.Count > 0)
                                NativeInterop.WriteConsoleInput(handle, records.ToArray(), records.Count, out _);
                        }
                        catch (Exception e)
                        {
                            System.Console.WriteLine(e);
                        }
                    else
                        await Task.Delay(10);

                ConsoleExtensions.STDINConsoleMode = mode;
            });
        }
    }

    public static void Stop() => _running = false;
}

[StructLayout(LayoutKind.Explicit)]
internal record struct INPUT_RECORD
{
    [FieldOffset(0)]
    public EventType EventType;
    [FieldOffset(4)]
    public KeyEvent KeyEvent;
    [FieldOffset(4)]
    public MouseEvent MouseEvent;
    [FieldOffset(4)]
    public short WindowBufferSizeEventX;
    [FieldOffset(6)]
    public short WindowBufferSizeEventY;
    // [FieldOffset(4)]
    // public MENU_EVENT_RECORD MenuEvent;
    [FieldOffset(4)]
    public int FocusEvent;
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

public record struct MouseEvent
{
    public short wMousePositionX;
    public short wMousePositionY;
    public MouseButtons dwButtonState;
    public ModifierKeysState dwControlKeyState;
    public MouseActions dwEventFlags;
}

[StructLayout(LayoutKind.Explicit, CharSet = CharSet.Unicode)]
public record struct KeyEvent
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
