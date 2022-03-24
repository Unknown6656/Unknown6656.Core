using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Generics;
using Unknown6656.Imaging;
using Unknown6656.Runtime;
using Unknown6656.Common;

namespace Unknown6656.Controls.Console;

using Console = System.Console;


[Flags]
[SupportedOSPlatform(OS.WIN)]
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

    [SupportedOSPlatform(OS.WIN)]
    public static void* STDINHandle => OS.IsWindows ? NativeInterop.GetStdHandle(-10)
                                                    : throw new InvalidOperationException("This operation is not supported on non-Windows operating systems.");

    [SupportedOSPlatform(OS.WIN)]
    public static void* STDOUTHandle => OS.IsWindows ? NativeInterop.GetStdHandle(-11)
                                                     : throw new InvalidOperationException("This operation is not supported on non-Windows operating systems.");

    [SupportedOSPlatform(OS.WIN)]
    public static ConsoleMode STDINConsoleMode
    {
        set
        {
            if (!OS.IsWindows)
                throw new InvalidOperationException("Writing the STDIN console mode is not supported on non-Windows operating systems.");
            else if (!NativeInterop.SetConsoleMode(STDINHandle, value))
                throw new InvalidOperationException("An error occurred when writing the STDIN console mode.");
        }
        get
        {
            if (!OS.IsWindows)
                throw new InvalidOperationException("Reading the STDIN console mode is not supported on non-Windows operating systems.");

            ConsoleMode mode = default;

            return NativeInterop.GetConsoleMode(STDINHandle, &mode) ? mode : throw new InvalidOperationException("An error occurred when reading the STDIN console mode.");
        }
    }

    [SupportedOSPlatform(OS.WIN)]
    public static ConsoleMode STDOUTConsoleMode
    {
        set
        {
            if (!OS.IsWindows)
                throw new InvalidOperationException("Writing the STDOUT console mode is not supported on non-Windows operating systems.");
            else if (!NativeInterop.SetConsoleMode(STDOUTHandle, value))
                throw new InvalidOperationException("An error occurred when writing the STDOUT console mode.");
        }
        get
        {
            if (!OS.IsWindows)
                throw new InvalidOperationException("Reading the STDOUT console mode is not supported on non-Windows operating systems.");

            ConsoleMode mode = default;

            return NativeInterop.GetConsoleMode(STDOUTHandle, &mode) ? mode : throw new InvalidOperationException("An error occurred when reading the STDOUT console mode.");
        }
    }


    static ConsoleExtensions()
    {
        if (OS.IsWindows)
        {
            // STDINConsoleMode |= ConsoleMode.ENABLE_VIRTUAL_TERMINAL_INPUT;
            STDINConsoleMode |= ConsoleMode.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
            STDOUTConsoleMode |= ConsoleMode.ENABLE_VIRTUAL_TERMINAL_PROCESSING;
        }
    }

    public static void Write(object? value, int left, int top) => Write(value, (left, top));

    public static void Write(object? value, (int left, int top) starting_pos)
    {
        Console.SetCursorPosition(starting_pos.left, starting_pos.top);
        Console.Write(value);
    }

    public static (int max_line_length, int line_count) WriteBlock(string value, int left, int top) =>
        WriteBlock(value, (left, top));

    public static (int max_line_length, int line_count) WriteBlock(string value, (int left, int top) starting_pos) =>
        WriteBlock(value.SplitIntoLines(), starting_pos);

    public static (int max_line_length, int line_count) WriteBlock(IEnumerable<string> lines, int left, int top) => WriteBlock(lines, (left, top));

    public static (int max_line_length, int line_count) WriteBlock(IEnumerable<string> lines, (int left, int top) starting_pos) =>
        WriteBlock(lines, starting_pos, (0x0fffffff, 0x0fffffff), true);

    public static (int max_line_length, int line_count) WriteBlock(string value, int left, int top, int max_width, int max_height, bool wrap_overflow = true) =>
        WriteBlock(value, (left, top), (max_width, max_height), wrap_overflow);

    public static (int max_line_length, int line_count) WriteBlock(string value, (int left, int top) starting_pos, (int width, int height) max_size, bool wrap_overflow = true) =>
        WriteBlock(value.SplitIntoLines(), starting_pos, max_size, wrap_overflow);

    public static (int max_line_length, int line_count) WriteBlock(IEnumerable<string> lines, int left, int top, int max_width, int max_height, bool wrap_overflow = true) =>
        WriteBlock(lines, (left, top), (max_width, max_height), wrap_overflow);

    public static (int max_line_length, int line_count) WriteBlock(IEnumerable<string> lines, (int left, int top) starting_pos, (int width, int height) max_size, bool wrap_overflow = true)
    {
        List<string> cropped_lines = new();

        foreach (string line in lines)
        {
            string[] sub_lines = line.PartitionByArraySize(max_size.width).ToArray(c => new string(c));

            if (!wrap_overflow && sub_lines.Length > 0)
                cropped_lines.Add(sub_lines[0]);
            else
                cropped_lines.AddRange(sub_lines);
        }

        int line_no = 0;

        while (cropped_lines.Count > max_size.height)
            cropped_lines.RemoveAt(cropped_lines.Count - 1);

        foreach (string line in cropped_lines.Take(max_size.height))
        {
            Console.SetCursorPosition(starting_pos.left, starting_pos.top + line_no);
            Console.Write(line);

            ++line_no;
        }

        return (cropped_lines.Max(line => line.Length), cropped_lines.Count);
    }

    public static void WriteVertical(object? value) => WriteVertical(value, Console.CursorLeft, Console.CursorTop);

    public static void WriteVertical(object? value, int left, int top) => WriteVertical(value, (left, top));

    public static void WriteVertical(object? value, (int left, int top) starting_pos)
    {
        string s = value?.ToString() ?? "";

        for (int i = 0; i < s.Length; i++)
        {
            Console.CursorTop = starting_pos.top + i;
            Console.CursorLeft = starting_pos.left;
            Console.Write(s[i]);
        }
    }

    public static void WriteUnderlined(object? value) => Console.Write($"\x1b[4m{value}\x1b[24m");

    public static void WriteInverted(object? value) => Console.Write($"\x1b[7m{value}\x1b[27m");


    public static unsafe string HexDumpToString(this byte[] data, int width) => HexDumpToString(new Span<byte>(data), width);

    public static unsafe string HexDumpToString(void* ptr, int length, int width) => HexDumpToString(new Span<byte>(ptr, length), width);

    public static unsafe string HexDumpToString([NotNull] this Span<byte> data, int width, bool colored = true)
    {
        if (data.Length == 0)
            return "";

        width -= 16;

        StringBuilder builder = new();
        int horizontal_count = (width - 3) / 4;
        byte b;

        horizontal_count -= horizontal_count % 16;

        int h_digits = (int)Math.Log(horizontal_count, 16);
        int vertical_count = (int)Math.Ceiling((float)data.Length / horizontal_count);

        if (colored)
            builder.Append(RGBAColor.White.ToVT100ForegroundString());

        builder.Append(data.Length)
               .Append(" bytes:");

        for (int i = h_digits; i >= 0; --i)
        {
            builder.Append('\n')
                   .Append(new string(' ', 8));

            for (int j = 0; j < horizontal_count; ++j)
                builder.Append($"  {(int)(j / Math.Pow(16, i)) % 16:x}");
        }

        builder.Append('\n');

        fixed (byte* ptr = data)
            for (int i = 0; i < vertical_count; i++)
            {
                builder.Append($"{i * horizontal_count:x8}:  ");

                bool cflag;

                for (int j = 0; (j < horizontal_count) && (i * horizontal_count + j < data.Length); ++j)
                {
                    b = ptr[i * horizontal_count + j];
                    cflag = *(int*)(ptr + (i * horizontal_count) + (j / 4) * 4) != 0;

                    if (colored)
                        builder.Append((b is 0 ? cflag ? RGBAColor.White : RGBAColor.DarkGray : RGBAColor.Orange).ToVT100ForegroundString());

                    builder.Append($"{b:x2} ");
                }

                if (colored)
                    builder.Append(RGBAColor.White.ToVT100ForegroundString());

                if (i == vertical_count - 1)
                    builder.Append(new string(' ', 3 * (horizontal_count * vertical_count - data.Length)));

                builder.Append("| ");

                for (int j = 0; (j < horizontal_count) && (i * horizontal_count + j < data.Length); j++)
                {
                    byte @byte = ptr[i * horizontal_count + j];
                    bool ctrl = (@byte < 0x20) || ((@byte >= 0x7f) && (@byte <= 0xa0));

                    if (ctrl && colored)
                        builder.Append(RGBAColor.Red.ToVT100ForegroundString());

                    builder.Append(ctrl ? '.' : (char)@byte);

                    if (colored)
                        builder.Append(RGBAColor.White.ToVT100ForegroundString());
                }

                builder.AppendLine();
            }

        return builder.ToString();
    }

    public static unsafe void HexDump(this byte[] data) => HexDump(new Span<byte>(data));

    public static unsafe void HexDump(void* ptr, int length) => HexDump(new Span<byte>(ptr, length));

    public static unsafe void HexDump([NotNull] this Span<byte> data) => HexDump(data, Console.Out);

    public static unsafe void HexDump(this byte[] data, TextWriter writer) => HexDump(new Span<byte>(data), writer);

    public static unsafe void HexDump(void* ptr, int length, TextWriter writer) => HexDump(new Span<byte>(ptr, length), writer);

    /// <summary>
    /// Dumps the given byte array as hexadecimal text viewer
    /// </summary>
    /// <param name="value">Byte array to be dumped</param>
    public static unsafe void HexDump([NotNull] this Span<byte> data, TextWriter writer)
    {
        if (data.Length == 0)
            return;

        ConsoleColor fc = Console.ForegroundColor;
        ConsoleColor bc = Console.BackgroundColor;
        ConsoleState state = SaveConsoleState();

        if (Console.CursorLeft > 0)
            Console.WriteLine();

        string str = HexDumpToString(data, Console.WindowWidth - 3, true);

        Console.WriteLine(str);
        Console.ForegroundColor = fc;
        Console.BackgroundColor = bc;

        RestoreConsoleState(state);
    }

    public static ConsoleState SaveConsoleState() => new()
    {
        Background = Console.BackgroundColor,
        Foreground = Console.ForegroundColor,
        InputEncoding = Console.InputEncoding,
        OutputEncoding = Console.OutputEncoding,
        CursorVisible = OS.IsPosix || Console.CursorVisible,
        CursorSize = OS.IsWindows ? Console.CursorSize : 100,
        Mode = OS.IsWindows ? STDINConsoleMode : default,
    };

    public static void RestoreConsoleState(ConsoleState? state)
    { 
        if (state is { })
        {
            Console.BackgroundColor = state.Background;
            Console.ForegroundColor = state.Foreground;
            Console.InputEncoding = state.InputEncoding ?? Encoding.Default;
            Console.OutputEncoding = state.OutputEncoding ?? Encoding.Default;

            if (OS.IsWindows)
            {
#pragma warning disable CA1416 // Validate platform compatibility
                STDINConsoleMode = state.Mode;

                if (state.CursorSize is int sz)
                    LINQ.TryDo(() => Console.CursorSize = sz);
#pragma warning restore CA1416
            }

            if (state.CursorVisible is bool vis)
                LINQ.TryDo(() => Console.CursorVisible = vis);
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
