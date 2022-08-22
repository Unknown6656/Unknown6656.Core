using System;

using Unknown6656.Imaging;
using Unknown6656.Imaging.Rendering;

namespace Unknown6656.Controls.Console;

using Console = System.Console;


public unsafe class BitmapConsoleRenderer
    : Renderer
    , IDisposable
{
    private readonly ConsoleState _state;


    public BitmapConsoleRenderer(BitmapRenderingOptions options)
        : base(options) => _state = ConsoleExtensions.SaveConsoleState();

    protected override (int width, int height) GetOutputDimensions() => (Console.WindowWidth, Console.WindowHeight);

    protected override void RenderBitmap(RGBAColor[,] colors)
    {

        throw new NotImplementedException();

    }

    public void Dispose() => ConsoleExtensions.RestoreConsoleState(_state);
}
