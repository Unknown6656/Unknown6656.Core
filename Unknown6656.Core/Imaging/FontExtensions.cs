using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Unknown6656.Common;
using Unknown6656.Generics;

namespace Unknown6656.Imaging;


public static class FontExtensions
{
    // TODO : list fonts
    // TODO : install font
    // TODO : check if monospaced
    // TODO : render font
    // TODO : font/text render effect for bitmaps?

    public static FontFamily FromFile(FileInfo file, string? name = null)
    {
        PrivateFontCollection collection = new();

        collection.AddFontFile(file.FullName);

        return new(name ?? file.Name.TrimEnd(file.Extension), collection);
    }

    public static unsafe FontFamily FromStream(Stream stream, string name)
    {
        PrivateFontCollection collection = new();
        using MemoryStream ms = new();

        stream.CopyTo(stream);
        ms.Seek(0, SeekOrigin.Begin);

        byte[] bytes = ms.ToArray();

        fixed (byte* ptr = bytes)
            collection.AddMemoryFont((nint)ptr, bytes.Length);

        return new(name, collection);
    }

    public static bool IsFontInstalled(string name) => IsFontInstalled(name, FontStyle.Regular)
                                                    || IsFontInstalled(name, FontStyle.Bold)
                                                    || IsFontInstalled(name, FontStyle.Italic);

    public static bool IsFontInstalled(string name, FontStyle style) => LINQ.TryDo(() =>
    {
        using Font font = new(name, 10, style);

        return name.Equals(font.Name, StringComparison.InvariantCultureIgnoreCase);
    }, false);
}
