using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Generics;
using Unknown6656.IO;

namespace Unknown6656.Imaging.Video;


public record class GIFFrame(Image Image, int? Duration = null);

public class GIF
{
    public static void WriteGIF(string filepath, IEnumerable<GIFFrame> frames, int default_frame_delay = 500, int loop = GIFWriter.LOOP_INDEFINITELY)
    {
        using GIFWriter writer = new(filepath, default_frame_delay, loop);

        writer.WriteFrames(frames);
    }

    public static void WriteGIF(FileInfo filepath, IEnumerable<GIFFrame> frames, int default_frame_delay = 500, int loop = GIFWriter.LOOP_INDEFINITELY) =>
        WriteGIF(filepath.FullName, frames, default_frame_delay, loop);

    public static void WriteGIF(Stream stream, IEnumerable<GIFFrame> frames, int default_frame_delay = 500, int loop = GIFWriter.LOOP_INDEFINITELY)
    {
        using GIFWriter writer = new(stream, default_frame_delay, loop);

        writer.WriteFrames(frames);
    }

    public static void WriteGIF(string filepath, IEnumerable<Image> frames, int default_frame_delay = 500, int loop = GIFWriter.LOOP_INDEFINITELY) =>
        WriteGIF(filepath, frames.Select(f => new GIFFrame(f)), default_frame_delay, loop);

    public static void WriteGIF(FileInfo filepath, IEnumerable<Image> frames, int default_frame_delay = 500, int loop = GIFWriter.LOOP_INDEFINITELY) =>
        WriteGIF(filepath, frames.Select(f => new GIFFrame(f)), default_frame_delay, loop);

    public static void WriteGIF(Stream stream, IEnumerable<Image> frames, int default_frame_delay = 500, int loop = GIFWriter.LOOP_INDEFINITELY) =>
        WriteGIF(stream, frames.Select(f => new GIFFrame(f)), default_frame_delay, loop);

    public static GIFFrame[] ReadGIF(string filename) => ReadGIF(DataStream.FromFile(filename));

    public static GIFFrame[] ReadGIF(FileInfo filename) => ReadGIF(filename.FullName);

    public static GIFFrame[] ReadGIF(Stream stream)
    {
        Image gif = Image.FromStream(stream);
        int count = gif.GetFrameCount(FrameDimension.Time);

        if (count <= 1)
            return [new GIFFrame(gif)];

        byte[] times = gif.GetPropertyItem(0x5100).Value;
        List<GIFFrame> frames = [];

        for (int frame = 0; frame < count; ++frame)
        {
            int duration = BitConverter.ToInt32(times, 4 * frame);

            gif.SelectActiveFrame(FrameDimension.Time, frame);
            frames.Add(new(new Bitmap(gif), duration));
        }

        gif.Dispose();

        return [.. frames];
    }
}

/// <summary>
/// Creates a GIF using .NET GIF encoding and additional animation headers.
/// </summary>
public class GIFWriter
    : IDisposable
{
    public const int DONT_LOOP = -1;
    public const int LOOP_INDEFINITELY = 0;
    private const long SourceGlobalColorInfoPosition = 10;
    private const long SourceImageBlockPosition = 789;

    private readonly BinaryWriter _writer;
    private readonly object _mutex = new();
    private bool _first_frame = true;


    /// <summary>
    /// Gets or gets the default width of a frame. Used when unspecified.
    /// </summary>
    public int DefaultWidth { get; set; }

    /// <summary>
    /// Gets or sets the default height of a frame. Used when unspecified.
    /// </summary>
    public int DefaultHeight { get; set; }

    /// <summary>
    /// Gets or sets the default frame delay in milliseconds.
    /// </summary>
    public int DefaultFrameDelay { get; set; }

    /// <summary>
    /// Number of times the GIF should repeat. The special values <see cref="DONT_LOOP"/> (-1) and <see cref="LOOP_INDEFINITELY"/> (0) may also be used.
    /// </summary>
    public int Loop { get; }


    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="out_stream">The <see cref="Stream"/> to output the GIF to.</param>
    /// <param name="default_frame_delay">Default delay between consecutive frames in ms. The frame rate is determined by <c>1000 / </c><paramref name="default_frame_delay"/>.</param>
    /// <param name="loop">Number of times the GIF should repeat. The special values <see cref="DONT_LOOP"/> (-1) and <see cref="LOOP_INDEFINITELY"/> (0) may also be used.</param>
    public GIFWriter(Stream out_stream, int default_frame_delay = 500, int loop = LOOP_INDEFINITELY)
    {
        if (out_stream is null)
            throw new ArgumentNullException(nameof(out_stream));
        else if (default_frame_delay <= 0)
            throw new ArgumentOutOfRangeException(nameof(default_frame_delay));
        else if (loop is < -1 or > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(loop));

        _writer = new BinaryWriter(out_stream);
        DefaultFrameDelay = default_frame_delay;
        Loop = loop;
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="filepath">The target file.</param>
    /// <param name="default_frame_delay">Default delay between consecutive frames in ms. The frame rate is determined by <c>1000 / </c><paramref name="default_frame_delay"/>.</param>
    /// <param name="loop">Number of times the GIF should repeat. The special values <see cref="DONT_LOOP"/> (-1) and <see cref="LOOP_INDEFINITELY"/> (0) may also be used.</param>
    public GIFWriter(string filepath, int default_frame_delay = 500, int loop = LOOP_INDEFINITELY)
        : this(new FileStream(filepath, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read), default_frame_delay, loop)
    {
    }

    /// <summary>
    /// Creates a new instance.
    /// </summary>
    /// <param name="filepath">The target file.</param>
    /// <param name="default_frame_delay">Default delay between consecutive frames in ms. The frame rate is determined by <c>1000 / </c><paramref name="default_frame_delay"/>.</param>
    /// <param name="loop">Number of times the GIF should repeat. The special values <see cref="DONT_LOOP"/> (-1) and <see cref="LOOP_INDEFINITELY"/> (0) may also be used.</param>
    public GIFWriter(FileInfo filepath, int default_frame_delay = 500, int loop = LOOP_INDEFINITELY)
        : this(filepath.FullName, default_frame_delay, loop)
    {
    }

    /// <summary>
    /// Frees all resources used by this object.
    /// </summary>
    public void Dispose()
    {
        _writer.Write((byte)0x3b);
        _writer.Flush();
        _writer.BaseStream.Flush();

        //if (_writer.BaseStream is not DataStream)
        //{
        //    _writer.BaseStream.Dispose()
        //    _writer.Dispose();
        //}
    }

    public void WriteFrames(IEnumerable<Image> frames) => frames.Do(WriteFrame);

    public void WriteFrames(IEnumerable<GIFFrame> frames) => frames.Do(WriteFrame);

    public void WriteFrames(params Image[] frames) => WriteFrames(frames as IEnumerable<Image>);

    public void WriteFrames(params GIFFrame[] frames) => WriteFrames(frames as IEnumerable<GIFFrame>);

    /// <summary>
    /// Adds a frame to this animation.
    /// </summary>
    /// <param name="frame">The frame to add</param>
    public void WriteFrame(Image frame) => WriteFrame(frame, DefaultFrameDelay);

    /// <summary>
    /// Adds a frame to this animation.
    /// </summary>
    /// <param name="frame">The frame to add</param>
    public void WriteFrame(GIFFrame frame) => WriteFrame(frame.Image, frame.Duration ?? DefaultFrameDelay);

    /// <summary>
    /// Adds a frame to this animation.
    /// </summary>
    /// <param name="frame">The frame to add</param>
    /// <param name="Delay">Delay in milliseconds between the given and previous frame.</param>
    public void WriteFrame(Image frame, int delay)
    {
        lock (_mutex)
            using (MemoryStream stream = new())
            {
                frame.Save(stream, ImageFormat.Gif);

                if (_first_frame)
                    InitHeader(stream, _writer, frame.Width, frame.Height);

                WriteGraphicControlBlock(stream, _writer, delay);
                WriteImageBlock(stream, _writer, !_first_frame, 0, 0, frame.Width, frame.Height);

                _first_frame = false;
            }
    }

    private void InitHeader(Stream source, BinaryWriter writer, int w, int h)
    {
        writer.Write("GIF".ToCharArray()); // File type
        writer.Write("89a".ToCharArray()); // File Version
        writer.Write((short)(DefaultWidth <= 0 ? w : DefaultWidth)); // Initial Logical Width
        writer.Write((short)(DefaultHeight <= 0 ? h : DefaultHeight)); // Initial Logical Height

        source.Position = SourceGlobalColorInfoPosition;
        writer.Write((byte)source.ReadByte()); // Global Color Table Info
        writer.Write((byte)0); // Background Color Index
        writer.Write((byte)0); // Pixel aspect ratio
        AllocColorTable(source, writer);

        if (Loop > -1)
        {
            writer.Write(unchecked((short)0xff21)); // Application Extension Block Identifier
            writer.Write((byte)0x0b); // Application Block Size
            writer.Write("NETSCAPE2.0".ToCharArray()); // Application Identifier
            writer.Write((byte)3); // Application block length
            writer.Write((byte)1);
            writer.Write((short)Loop); // Repeat count for images.
            writer.Write((byte)0); // terminator
        }
    }

    static void AllocColorTable(Stream source, BinaryWriter writer)
    {
        source.Position = 13; // Locating the image color table

        byte[] table = new byte[768];

        source.Read(table, 0, table.Length);
        writer.Write(table, 0, table.Length);
    }

    static void WriteGraphicControlBlock(Stream source, BinaryWriter writer, int delay)
    {
        source.Position = 781; // Locating the source GCE

        byte[] blockhead = new byte[8];

        source.Read(blockhead, 0, blockhead.Length); // Reading source GCE
        writer.Write(unchecked((short)0xf921)); // Identifier
        writer.Write((byte)0x04); // Block Size
        writer.Write((byte)(blockhead[3] & 0xf7 | 0x08)); // Setting disposal flag
        writer.Write((short)(delay / 10)); // Setting frame delay
        writer.Write(blockhead[6]); // Transparent color index
        writer.Write((byte)0); // Terminator
    }

    static void WriteImageBlock(Stream source, BinaryWriter writer, bool include_color_table, int x, int y, int width, int height)
    {
        source.Position = SourceImageBlockPosition; // Locating the image block

        byte[] header = new byte[11];

        source.Read(header, 0, header.Length);
        writer.Write(header[0]); // Separator
        writer.Write((short)x); // Position X
        writer.Write((short)y); // Position Y
        writer.Write((short)width); // Width
        writer.Write((short)height); // Height

        if (include_color_table) // If first frame, use global color table - else use local
        {
            source.Position = SourceGlobalColorInfoPosition;
            writer.Write((byte)(source.ReadByte() & 0x3f | 0x80)); // Enabling local color table

            AllocColorTable(source, writer);
        }
        else
            writer.Write((byte)(header[9] & 0x07 | 0x07)); // Disabling local color table

        writer.Write(header[10]); // LZW Min Code Size

        // Read/Write image data
        source.Position = SourceImageBlockPosition + header.Length;

        int length;

        while ((length = source.ReadByte()) > 0)
        {
            byte[] dat = new byte[length];

            source.Read(dat, 0, length);
            writer.Write((byte)length);
            writer.Write(dat, 0, length);
            length = source.ReadByte();
        }

        writer.Write((byte)0); // Terminator
    }
}
