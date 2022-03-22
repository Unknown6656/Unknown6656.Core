using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System;

using Unknown6656.Runtime;
using Unknown6656.IO;

namespace Unknown6656.Imaging;


public record VideoAssemblerOptions
{
    public static VideoAssemblerOptions Default { get; } = new();

    public string FFMEGPath { get; init; } = "ffmpeg";
    public string OutputCodec { get; init; } = "libx264";
    public string PixelFormat { get; init; } = "yuv420p";
    public int FrameRate { get; init; } = 30;
    public bool Parallelized { get; init; } = false;
}

[SupportedOSPlatform(OS.WIN)]
public static class VideoAssembler
{
    public static bool JoinVideoFrames(this Image[] frames, FileInfo output_file, VideoAssemblerOptions options)
    {
        DirectoryInfo temp = FileSystemExtensions.GetTemporaryDirectory();
        bool result = false;

        try
        {
            Parallel.For(0, frames.Length, i => frames[i].Save($"{temp.FullName}/{i:D6}.png", ImageFormat.Png));

            string ext = output_file.Extension;
            using Process? proc = Process.Start(new ProcessStartInfo
            {
                FileName = options.FFMEGPath,
                Arguments = $"-start_number 0 -i \"{temp.FullName}/%06d.png\" -c:v {options.OutputCodec} -vf \"fps={options.FrameRate},format={options.PixelFormat}\" \"{temp.FullName}/output{ext}\"",
                UseShellExecute = true,
                CreateNoWindow = true,
            });

            proc?.WaitForExit();

            if (proc?.ExitCode is 0)
            {
                File.Move($"{temp.FullName}/output{ext}", output_file.FullName, true);

                result = true;
            }
        }
        catch
        {
        }

        temp.Delete(true);

        return result;
    }

    public static bool CreateVideo(FileInfo output_file, int frame_count, Func<int, Bitmap> frame_provider, VideoAssemblerOptions? options = null)
    {
        Bitmap[] frames = new Bitmap[frame_count];
        options ??= VideoAssemblerOptions.Default;

        if (options.Parallelized)
            Parallel.For(0, frame_count, i => frames[i] = frame_provider(i));
        else
            for (int i = 0; i < frame_count; ++i)
                frames[i] = frame_provider(i);

        return JoinVideoFrames(frames, output_file, options);
    }

    public static bool CreateVideo(FileInfo output_file, int frame_count, Size frame_size, Action<int, Bitmap> frame_manipulator, VideoAssemblerOptions? options = null)
    {
        Bitmap[] frames = CreateFrames(frame_count, frame_size);
        options ??= VideoAssemblerOptions.Default;

        if (options.Parallelized)
            Parallel.For(0, frame_count, i => frame_manipulator(i, frames[i]));
        else
            for (int i = 0; i < frame_count; ++i)
                frame_manipulator(i, frames[i]);

        return JoinVideoFrames(frames, output_file, options);
    }

    private static Bitmap[] CreateFrames(int count, Size frame_size)
    {
        Bitmap[] frames = new Bitmap[count];

        Parallel.For(0, count, i => frames[i] = new Bitmap(frame_size.Width, frame_size.Height, PixelFormat.Format32bppArgb));

        return frames;
    }
}
