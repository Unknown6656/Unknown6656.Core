using System.Diagnostics;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System;

using Unknown6656.IO;

namespace Unknown6656.Imaging
{
    public delegate void FrameManipulator(int frameIndex, Bitmap frame);

    public delegate void ParallelFrameManipulator(int frameIndex, Bitmap frame);

    public static class VideoAssembler
    {
        public static bool CreateVideo(FileInfo output_file, int frame_count, Size frame_size, FrameManipulator manipulator, string ffmpeg_path = "ffmpeg", int frame_rate = 30) =>
            CreateVideo(output_file, frame_count, frame_size, manipulator, ffmpeg_path, frame_rate, false);

        public static bool CreateVideoParallel(FileInfo output_file, int frame_count, Size frame_size, ParallelFrameManipulator manipulator, string ffmpeg_path = "ffmpeg", int frame_rate = 30) =>
            CreateVideo(output_file, frame_count, frame_size, manipulator, ffmpeg_path, frame_rate, true);

        private static bool CreateVideo<T>(FileInfo output, int count, Size size, T cb, string ffmpeg, int rate, bool parallel)
            where T : Delegate
        {
            Bitmap[] frames = CreateFrames(count, size);

            if (parallel)
                Parallel.For(0, count, i => cb.DynamicInvoke(i, frames[i]));
            else
                for (int i = 0; i < count; ++i)
                    cb.DynamicInvoke(i, frames[i]);

            return JoinVideoFrames(frames, output, ffmpeg, rate);
        }

        private static Bitmap[] CreateFrames(int count, Size frame_size)
        {
            Bitmap[] frames = new Bitmap[count];

            Parallel.For(0, count, i => frames[i] = new Bitmap(frame_size.Width, frame_size.Height, PixelFormat.Format32bppArgb));

            return frames;
        }

        public static bool JoinVideoFrames(this Image[] frames, FileInfo output_file, string ffmpeg_path = "ffmpeg", int frame_rate = 30)
        {
            DirectoryInfo temp = FileSystemExtensions.GetTemporaryDirectory();
            bool result = false;

            try
            {
                Parallel.For(0, frames.Length, i => frames[i].Save($"{temp.FullName}/{i:D6}.png", ImageFormat.Png));

                using Process? proc = Process.Start(new ProcessStartInfo
                {
                    FileName = ffmpeg_path,
                    Arguments = $"-start_number 0 -i \"{temp.FullName}/%06d.png\" -c:v libx264 -vf \"fps={frame_rate},format=yuv420p\" \"{temp.FullName}/output.mp4\"",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                });

                proc?.WaitForExit();

                if (proc?.ExitCode is 0)
                {
                    File.Move(temp.FullName + "/output.mp4", output_file.FullName, true);

                    result = true;
                }
            }
            catch
            {
            }

            temp.Delete(true);

            return result;
        }
    }
}
