using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using System;

using Unknown6656.Imaging;
using Unknown6656.Common;

namespace Unknown6656.Controls.Console
{
    using Console = System.Console;


    [Flags]
    public enum ConsoleMode
        : uint
    {
        ENABLE_PROCESSED_INPUT = 0x0001,
        ENABLE_LINE_INPUT = 0x0002,
        ENABLE_ECHO_INPUT = 0x0004,
        ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004,
        ENABLE_WINDOW_INPUT = 0x0008,
        ENABLE_MOUSE_INPUT = 0x0010,
        ENABLE_INSERT_MODE = 0x0020,
        ENABLE_QUICK_EDIT_MODE = 0x0040,
        ENABLE_EXTENDED_FLAGS = 0x0080,
        ENABLE_VIRTUAL_TERMINAL_INPUT = 0x0200,
    }

    public static unsafe class ConsoleExtensions
    {
        private static RGBAColor? _fg;
        private static RGBAColor? _bg;


        public static RGBAColor RGBForegroundColor
        {
            get => _fg ?? RGBAColor.FromConsoleColor(Console.ForegroundColor, ConsoleColorScheme.Legacy);
            set
            {
                _fg = value;

                Console.Write(value.ToVT100ForegroundString());
            }
        }

        public static RGBAColor RGBBackgroundColor
        {
            get => _bg ?? RGBAColor.FromConsoleColor(Console.BackgroundColor, ConsoleColorScheme.Legacy);
            set
            {
                _bg = value;

                Console.Write(value.ToVT100BackgroundString());
            }
        }

        public static bool IsWindowsConsole { get; } = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static void* STDINHandle
        {
            get
            {
                if (!IsWindowsConsole)
                    throw new InvalidOperationException("This operation is not supported on non-Windows operating systems.");

                return NativeInterop.GetStdHandle(-10);
            }
        }

        public static void* STDOUTHandle
        {
            get
            {
                if (!IsWindowsConsole)
                    throw new InvalidOperationException("This operation is not supported on non-Windows operating systems.");

                return NativeInterop.GetStdHandle(-11);
            }
        }

        public static ConsoleMode STDINConsoleMode
        {
            set
            {
                if (!IsWindowsConsole)
                    throw new InvalidOperationException("Writing the STDIN console mode is not supported on non-Windows operating systems.");

                if (!NativeInterop.SetConsoleMode(STDINHandle, value))
                    throw new InvalidOperationException("An error occurred when writing the STDIN console mode.");
            }
            get
            {
                if (!IsWindowsConsole)
                    throw new InvalidOperationException("Reading the STDIN console mode is not supported on non-Windows operating systems.");

                ConsoleMode mode = default;

                return NativeInterop.GetConsoleMode(STDINHandle, &mode) ? mode : throw new InvalidOperationException("An error occurred when reading the STDIN console mode.");
            }
        }

        public static ConsoleMode STDOUTConsoleMode
        {
            set
            {
                if (!IsWindowsConsole)
                    throw new InvalidOperationException("Writing the STDOUT console mode is not supported on non-Windows operating systems.");

                if (!NativeInterop.SetConsoleMode(STDOUTHandle, value))
                    throw new InvalidOperationException("An error occurred when writing the STDOUT console mode.");
            }
            get
            {
                if (!IsWindowsConsole)
                    throw new InvalidOperationException("Reading the STDOUT console mode is not supported on non-Windows operating systems.");

                ConsoleMode mode = default;

                return NativeInterop.GetConsoleMode(STDOUTHandle, &mode) ? mode : throw new InvalidOperationException("An error occurred when reading the STDOUT console mode.");
            }
        }


        static ConsoleExtensions()
        {
            if (IsWindowsConsole)
            {
                // STDINConsoleMode |= ConsoleMode.ENABLE_VIRTUAL_TERMINAL_INPUT;
                STDINConsoleMode |= ConsoleMode.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                STDOUTConsoleMode |= ConsoleMode.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            }
        }

        public static void Write(object? value, (int left, int top) starting_pos)
        {
            Console.SetCursorPosition(starting_pos.left, starting_pos.top);
            Console.Write(value);
        }

        public static void WriteVertical(object? value) => WriteVertical(value, (Console.CursorLeft, Console.CursorTop));

        public static void WriteVertical(object? value, (int left, int top) starting_pos)
        {
            string s = value?.ToString() ?? "";

            Console.CursorTop = starting_pos.top;
            Console.CursorLeft = starting_pos.left;

            for (int i = 0; i < s.Length; i++)
            {
                Console.Write(s[i]);
                Console.CursorTop = starting_pos.top + i;
                Console.CursorLeft = starting_pos.left;
            }
        }

        public static void WriteUnderlined(object? value) => Console.Write($"\x1b[4m{value}\x1b[24m");

        public static void WriteInverted(object? value) => Console.Write($"\x1b[7m{value}\x1b[27m");

        /// <summary>
        /// Dumps the given byte array as hexadecimal text viewer
        /// </summary>
        /// <param name="value">Byte array to be dumped</param>
        public static unsafe void HexDump([NotNull] this Span<byte> data)
        {
            if (data.Length == 0)
                return;

            ConsoleColor fc = Console.ForegroundColor;
            ConsoleColor bc = Console.BackgroundColor;
            bool cv = Console.CursorVisible;
            int w = Console.WindowWidth - 16;
            int l = (w - 3) / 4;
            byte b;

            l -= l % 16;

            int h = (int)Math.Ceiling((float)data.Length / l);

            Console.CursorVisible = false;
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;
            Console.WriteLine($" {data.Length} bytes:\n\n");
            Console.CursorLeft += 8;

            for (int j = 0; j <= l; j++)
            {
                Console.CursorTop--;
                Console.Write($"  {j / 16:x}");
                Console.CursorLeft -= 3;
                Console.CursorTop++;
                Console.Write($"  {j % 16:x}");
            }

            Console.WriteLine();

            fixed (byte* ptr = data)
                for (int i = 0; i < h; i++)
                {
                    Console.Write($"{i * l:x8}:  ");

                    bool cflag;

                    for (int j = 0; (j < l) && (i * l + j < data.Length); j++)
                    {
                        b = ptr[i * l + j];
                        cflag = *((int*)(ptr + i * l + (j / 4) * 4)) != 0;

                        Console.ForegroundColor = b == 0 ? cflag ? ConsoleColor.White : ConsoleColor.DarkGray : ConsoleColor.Yellow;
                        Console.Write($"{b:x2} ");
                    }

                    Console.ForegroundColor = ConsoleColor.White;
                    Console.CursorLeft = 3 * l + 11;
                    Console.Write("| ");

                    for (int j = 0; (j < l) && (i * l + j < data.Length); j++)
                    {
                        byte _ = ptr[i * l + j];
                        bool ctrl = (_ < 0x20) || ((_ >= 0x7f) && (_ <= 0xa0));

                        if (ctrl)
                            Console.ForegroundColor = ConsoleColor.Red;

                        Console.Write(ctrl ? '.' : (char)_);
                        Console.ForegroundColor = ConsoleColor.White;
                    }

                    Console.Write("\n");
                }

            Console.WriteLine();
            Console.CursorVisible = cv;
            Console.ForegroundColor = fc;
            Console.BackgroundColor = bc;
        }

        public static unsafe void HexDump(this byte[] data) => HexDump(new Span<byte>(data));

        public static unsafe void HexDump(void* ptr, int length) => HexDump(new Span<byte>(ptr, length));

        public static ConsoleState SaveConsoleState() => new ConsoleState
        {
            Background = Console.BackgroundColor,
            Foreground = Console.ForegroundColor,
            InputEncoding = Console.InputEncoding,
            OutputEncoding = Console.OutputEncoding,
            CursorVisible = FunctionExtensions.TryDo<bool?>(() => Console.CursorVisible, null),
            CursorSize = FunctionExtensions.TryDo<int?>(() => Console.CursorSize, null),
            Mode = IsWindowsConsole ? STDINConsoleMode : default,
        };

        public static void RestoreConsoleState(ConsoleState? state)
        { 
            if (state is { })
            {
                Console.BackgroundColor = state.Background;
                Console.ForegroundColor = state.Foreground;
                Console.InputEncoding = state.InputEncoding ?? Encoding.Default;
                Console.OutputEncoding = state.OutputEncoding ?? Encoding.Default;

                if (IsWindowsConsole)
                    STDINConsoleMode = state.Mode;

                if (state.CursorSize is int sz)
                    FunctionExtensions.TryDo(() => Console.CursorSize = sz);

                if (state.CursorVisible is bool vis)
                    FunctionExtensions.TryDo(() => Console.CursorVisible = vis);
            }
        }
    }

    public sealed class ConsoleState
    {
        public ConsoleMode Mode { get; set; }
        public ConsoleColor Background { set; get; }
        public ConsoleColor Foreground { set; get; }
        public Encoding? OutputEncoding { set; get; }
        public Encoding? InputEncoding { set; get; }
        public bool? CursorVisible { set; get; }
        public int? CursorSize { set; get; }
    }
}
