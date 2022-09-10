using System.IO;
using System;

using Unknown6656.Controls.Console;

namespace Unknown6656.Imaging.Rendering;


public class ASCIIRenderer
    : Renderer
{
    public override ASCIIRenderingOptions Options { get; }
    public TextWriter Output { get; }


    public ASCIIRenderer(TextWriter output, ASCIIRenderingOptions options)
        : base(options)
    {
        Options = options;
        Output = output;
    }

    protected override (int width, int height) GetOutputDimensions() => (Options.Width, Options.Height);

    protected override void RenderBitmap(RGBAColor[,] colors, RenderingOptions options_override)
    {
        string charset = Options.Charset;

        for (int y = 0, w = colors.GetLength(0), h = colors.GetLength(1); y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
                Output.Write(charset[(int)(colors[x, y].CIEGray * charset.Length)]);

            Output.WriteLine();
        }
    }
}

public class ASCIIConsoleRenderer
    : ASCIIRenderer
{
    private readonly bool _grayscale;


    public ASCIIConsoleRenderer(ASCIIRenderingOptions options, bool grayscale = true)
        : base(Console.Out, options) => _grayscale = grayscale;

    protected override (int width, int height) GetOutputDimensions() => (
        Math.Min(Math.Min(Console.BufferWidth, Console.WindowWidth), Options.Width < 0 ? short.MaxValue : Options.Width),
        Math.Min(Math.Min(Console.BufferHeight, Console.WindowHeight), Options.Height < 0 ? short.MaxValue : Options.Height)
    );

    protected override void RenderBitmap(RGBAColor[,] colors, RenderingOptions options_override)
    {
        string charset = Options.Charset;

        for (int y = 0, w = colors.GetLength(0), h = colors.GetLength(1); y < h; ++y)
        {
            for (int x = 0; x < w; ++x)
            {
                if (!_grayscale)
                    ConsoleExtensions.RGBForegroundColor = colors[x, y];

                Output.Write(charset[(int)(colors[x, y].CIEGray * charset.Length)]);
            }

            Output.WriteLine();
        }
    }
}

public record ASCIIRenderingOptions
    : RenderingOptions
{
    public required int Width { get; init; } = 30;
    public required int Height { get; init; } = 15;
    public double CharacterAspectRatio { get; init; } = .5;

    /// <summary>
    /// Ordered by darkness descending
    /// </summary>
    public string Charset { get; init; } = "@$%B8&WM#*oahkbdpqwmZO0QLCJUYXzcvunxrjft/\\|()1{}[]?-_+~<>i!lI;:,\"^'`. ";
}
