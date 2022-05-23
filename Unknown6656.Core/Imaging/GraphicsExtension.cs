using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Imaging;

public static class GraphicsExtension
{
    public static void DrawGradientLine(this Graphics g, Vector2 start_pos, Vector2 end_pos, RGBAColor start_color, RGBAColor end_color) =>
        g.DrawGradientLine(start_pos, end_pos, start_color, end_color, Scalar.One);

    public static void DrawGradientLine(this Graphics g, Vector2 start_pos, Vector2 end_pos, RGBAColor start_color, RGBAColor end_color, Scalar thickness)
    {
        using LinearGradientBrush brush = new(start_pos, end_pos, start_color, end_color);
        using Pen pen = new(brush, thickness);

        g.DrawLine(pen, start_pos, end_pos);
    }

    public static void DrawGradientLine(this Graphics g, float start_x, float start_y, float end_x, float end_y, RGBAColor start_color, RGBAColor end_color) =>
        g.DrawGradientLine(start_x, end_x, start_y, end_y, start_color, end_color, Scalar.One);

    public static void DrawGradientLine(this Graphics g, float start_x, float start_y, float end_x, float end_y, RGBAColor start_color, RGBAColor end_color, Scalar thickness) =>
        g.DrawGradientLine(new(start_x, start_y), new(end_x, end_y), start_color, end_color);
}
