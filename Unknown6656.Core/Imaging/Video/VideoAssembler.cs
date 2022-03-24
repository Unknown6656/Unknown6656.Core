using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System;

using Unknown6656.Runtime;
using Unknown6656.IO;

namespace Unknown6656.Imaging.Video;


public record VideoDisassemblerOptions
{
    public static VideoDisassemblerOptions Default { get; } = new();

    public string FFMEGPath { get; init; } = "ffmpeg";
    public TimeOnly Skip { get; init; } = new(0);
    public TimeSpan? Duration { get; init; } = null;
    public int? FrameRate { get; init; } = null;
}

public record VideoAssemblerOptions
{
    public static VideoAssemblerOptions Default { get; } = new();

    public string OutputCodec { get; init; } = "libx264";
    public string PixelFormat { get; init; } = "yuv420p";
    public string FFMEGPath { get; init; } = "ffmpeg";
    public int FrameRate { get; init; } = 30;
    public bool Parallelized { get; init; } = true;
}


// TODO : FFMPEG downloader etc.


//[SupportedOSPlatform(OS.WIN)]
public static class VideoAssembler
{
    public static bool JoinVideoFrames(this Image[] frames, FileInfo output_file, VideoAssemblerOptions options)
    {
        DirectoryInfo temp = FileSystemExtensions.GetTemporaryDirectory();
        Process? proc = null;
        bool result = false;

        try
        {
            Parallel.For(0, frames.Length, i => frames[i].Save($"{temp.FullName}/{i:D6}.png", ImageFormat.Png));

            string ext = output_file.Extension;
            proc = Process.Start(new ProcessStartInfo
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

        proc?.Kill();
        proc?.Dispose();
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

        return frames.JoinVideoFrames(output_file, options);
    }

    public static bool CreateVideo(FileInfo output_file, int frame_count, Size frame_size, Action<int, Bitmap> frame_manipulator, VideoAssemblerOptions? options = null) =>
        CreateVideo(output_file, frame_count, i =>
        {
            Bitmap frame = new(frame_size.Width, frame_size.Height, PixelFormat.Format32bppArgb);

            frame_manipulator(i, frame);

            return frame;
        }, options);
}

public static class VideoDisassembler
{
    public static bool GetVideoFrames(FileInfo video_file, [NotNullWhen(true), MaybeNullWhen(false)] out Bitmap[]? video_frames, VideoDisassemblerOptions? options = null)
    {
        DirectoryInfo temp = FileSystemExtensions.GetTemporaryDirectory();
        Process? proc = null;

        video_frames = null;
        options ??= VideoDisassemblerOptions.Default;

        try
        {
            string fps = options.FrameRate is int f ? $"-vf fps={f}" : "";
            string skip = options.Skip.Ticks > TimeSpan.TicksPerMillisecond ? $"-ss {options.Skip.Ticks / (float)TimeSpan.TicksPerSecond}" : "";
            string duration = options.Duration?.TotalSeconds is double secs and > .01 ? $"-t {secs}" : "";

            proc = Process.Start(new ProcessStartInfo
            {
                FileName = options.FFMEGPath,
                Arguments = $" -i \"{video_file.FullName}\" {skip} {duration} {fps} \"{temp.FullName}/%06d.png\"",
                UseShellExecute = true,
                CreateNoWindow = true,
            });

            proc?.WaitForExit();

            if (proc?.ExitCode is 0)
            {
                string[] images = (from file in temp.EnumerateFiles("*.png")
                                   orderby file.Name ascending
                                   select file.FullName).ToArray();
                Bitmap[] frames = new Bitmap[images.Length];

                Parallel.For(0, frames.Length, i => frames[i] = (Bitmap)Image.FromFile(images[i]));

                video_frames = frames;
            }
        }
        catch
        {
        }

        proc?.Kill();
        proc?.Dispose();
        temp.Delete(true);

        return video_frames is { };
    }
}
