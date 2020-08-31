using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Drawing.Imaging;
using System.Globalization;
using System.Drawing;
using System.Net.Http;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

using Renci.SshNet;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Cryptography;
using Unknown6656.Controls.Console;
using Unknown6656.Common;
using Unknown6656.Imaging;

namespace Unknown6656.IO
{
    public unsafe sealed class From
        : IEnumerable<byte>
    {
        public static From Empty { get; } = new byte[0];


        public From this[Range range] => Slice(range);

        public From this[Index start, Index end] => Slice(start, end);

        public byte this[Index index] => Data[index];

        public byte[] Data { get; }

        public int ByteCount => Data.Length;

        public To To => new To(Data);


        private From(byte[] data) => Data = data;

        public From Compress(CompressionFunction algorithm) => Data.Compress(algorithm);

        public From Uncompress(CompressionFunction algorithm) => Data.Uncompress(algorithm);

        public From Encrypt(BinaryCipher algorithm, byte[] key) => Data.Encrypt(algorithm, key);

        public From Decrypt(BinaryCipher algorithm, byte[] key) => Data.Decrypt(algorithm, key);

        public From Hash<T>(T hash_function) where T : HashFunction<T> => hash_function.Hash(Data);

        public From HexDump()
        {
            ConsoleExtensions.HexDump(Data);

            return this;
        }

        public From Slice(Index start, Index end) => Slice(start..end);

        public From Slice(Range range) => Bytes(Data[range]);

        public From Concat(params From[] others) => Multiple(others.Prepend(this));

        public From Append(params From[] others) => Multiple(others.Prepend(this));

        public From Prepend(From first) => Multiple(first, this);

        public From Where(Func<byte, bool> predicate) => Data.Where(predicate).ToArray();

        public From Select(Func<byte, byte> function) => Data.ToArray(function);

        public From Reverse() => Data.Reverse().ToArray();

        public override string ToString() => To.String();

        public string ToString(Encoding encoding) => To.String(encoding);

        public IEnumerator<byte> GetEnumerator() => ((IEnumerable<byte>)Data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Multiple(params From?[]? sources) => Multiple(sources as IEnumerable<From?>);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Multiple(IEnumerable<From?>? sources)
        {
            MemoryStream s = new MemoryStream();

            foreach (From? source in sources ?? System.Array.Empty<From>())
                if (source is { })
                    s.Write(source.Data, 0, source.ByteCount);

            return Stream(s);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Unmanaged<T>(T data) where T : unmanaged => Pointer(&data);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Pointer<T>(T* data) where T : unmanaged => Pointer(data, sizeof(T));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Pointer(nint pointer, int byte_count) => Pointer((void*)pointer, byte_count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Pointer(void* data, int byte_count) => Pointer((byte*)data, byte_count);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Pointer<T>(T* data, int byte_count)
            where T : unmanaged
        {
            byte[] arr = new byte[byte_count];
            byte* ptr = (byte*)data;

            for (int i = 0; i < byte_count; ++i)
                arr[i] = ptr[i];

            return Bytes(arr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Array<T>(T[] array)
            where T : unmanaged
        {
            byte[] arr = new byte[array.Length * sizeof(T) + 4];

            fixed (byte* ptr = arr)
            {
                *(int*)ptr = array.Length;
                T* dst = (T*)(ptr + 4);

                for (int i = 0; i < array.Length; ++i)
                    dst[i] = array[i];
            }

            return Bytes(arr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From RGBAEncodedBitmap(Bitmap bitmap) => Array(bitmap.ToPixelArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Bitmap(Bitmap bitmap)
        {
            using MemoryStream ms = new MemoryStream();

            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            return Stream(ms);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Stream(Stream stream)
        {
            using MemoryStream ms = new MemoryStream();

            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return Bytes(ms.ToArray());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From String(object? obj) => String(obj, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From String(object? obj, Encoding enc) => String(obj?.ToString() ?? "", enc);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From String(string str) => String(str, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From String(string str, Encoding enc) => Bytes(enc.GetBytes(str));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From INI(IDictionary<string, IDictionary<string, string>> ini) => INI(ini, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From INI(IDictionary<string, IDictionary<string, string>> ini, Encoding enc)
        {
            StringBuilder sb = new StringBuilder();

            foreach (string section in ini.Keys)
            {
                sb.AppendLine($"[{section}]");

                foreach (string property in ini[section].Keys)
                    sb.AppendLine($"{property}={ini[section][property]}");
            }

            return StringBuilder(sb, enc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From StringBuilder(StringBuilder sb) => StringBuilder(sb, BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From StringBuilder(StringBuilder sb, Encoding enc) => String(sb.ToString(), enc);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Lines(IEnumerable<string> lines, string separator = "\n") => Lines(lines, BytewiseEncoding.Instance, separator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Lines(IEnumerable<string> lines, Encoding enc, string separator = "\n") => String(string.Join(separator, lines), enc);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Base64(string str) => Bytes(Convert.FromBase64String(str));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From File(string path) => File(new FileInfo(path));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From File(FileInfo file) => Stream(file.OpenRead());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From File(Uri path) => File(Path.GetFileName(path.LocalPath));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From CompressedMatrix<Field>(Field[,] matrix)
            where Field : unmanaged, IField<Field> => CompressedStorageFormat(new CompressedStorageFormat<Field>(matrix));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From CompressedMatrix<Field>(Algebra<Field>.IComposite2D matrix)
            where Field : unmanaged, IField<Field> => CompressedStorageFormat(new CompressedStorageFormat<Field>(matrix));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From CompressedStorageFormat<Field>(CompressedStorageFormat<Field> compressed) where Field : unmanaged, IField<Field> => Bytes(compressed.ToBytes());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From WebResource(Uri uri)
        {
            using WebClient wc = new WebClient();

            return Bytes(wc.DownloadData(uri));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From WebResource(string uri)
        {
            using WebClient wc = new WebClient();

            return Bytes(wc.DownloadData(uri));
        }

        public static From HTTP(string uri)
        {
            using HttpClient hc = new HttpClient
            {
                Timeout = new TimeSpan(0, 0, 15)
            };

            return Bytes(hc.GetByteArrayAsync(uri)
                           .ConfigureAwait(false)
                           .GetAwaiter()
                           .GetResult());
        }

        public static From FTP(string uri)
        {
            FtpWebRequest req;
            byte[] content;

            if (uri.Match(@"^(?<protocol>ftps?):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<url>.+)$", out ReadOnlyIndexer<string, string>? g))
            {
                req = (FtpWebRequest)WebRequest.Create($"{g["protocol"]}://{g["url"]}");
                req.Method = WebRequestMethods.Ftp.DownloadFile;
                req.Credentials = new NetworkCredential(g["uname"], g["passw"]);
            }
            else
            {
                req = (FtpWebRequest)WebRequest.Create(uri);
                req.Method = WebRequestMethods.Ftp.DownloadFile;
            }

            using (FtpWebResponse resp = (FtpWebResponse)req.GetResponse())
            using (Stream s = resp.GetResponseStream())
                return Stream(s);
        }

        public static From SSH(string uri)
        {
            if (uri.Match(@"^(sftp|ssh|scp):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<host>[^:\/]+|\[[0-9a-f\:]+\])(:(?<port>[0-9]{1,6}))?(\/|\\)(?<path>.+)$", out ReadOnlyIndexer<string, string>? g))
            {
                string host = g["host"];
                string uname = g["uname"];
                string passw = g["passw"];
                string rpath = '/' + g["path"];

                if (!int.TryParse(g["port"], out int port))
                    port = 22;

                using (SftpClient sftp = new SftpClient(host, port, uname, passw))
                using (MemoryStream ms = new MemoryStream())
                {
                    sftp.Connect();
                    sftp.DownloadFile(rpath, ms);
                    sftp.Disconnect();
                    ms.Seek(0, SeekOrigin.Begin);

                    return Stream(ms);
                }
            }
            else
                throw new ArgumentException($"Invalid SSH URI: The URI should have the format '<protocol>://<user>:<password>@<host>:<port>/<path>'.", nameof(uri));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From DataURI(string uri)
        {
            if (uri.Match(/*lang=regex*/@"^.\s*data:\s*[^\w\/\-\+]+\s*;(\s*base64\s*,)?(?<data>(?:[A-Za-z0-9+/]{4})*(?:[A-Za-z0-9+/]{2}==|[A-Za-z0-9+/]{3}=)?)$", out ReadOnlyIndexer<string, string>? groups))
                return Base64(groups!["data"]);

            throw new ArgumentException("Invalid data URI.");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Hex(string str)
        {
            str = new string(str.ToLower().Where(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')).ToArray());

            if ((str.Length % 2) == 1)
                str = '0' + str;

            byte[] data = new byte[str.Length / 2];

            for (int i = 0; i < data.Length; ++i)
                data[i] = byte.Parse(str[(i * 2)..((i + 1) * 2)], NumberStyles.HexNumber);

            return Bytes(data);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Bytes(IEnumerable<byte>? bytes) => Bytes(bytes?.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Bytes(params byte[]? bytes) => new From(bytes ?? new byte[0]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Bytes(byte[] bytes, int offset) => Bytes(bytes[offset..]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Bytes(byte[] bytes, int offset, int count) => Bytes(bytes[offset..(offset + count)]);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Span<T>(Span<T> bytes) where T : unmanaged => Array(bytes.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Span<T>(ReadOnlySpan<T> bytes) where T : unmanaged => Array(bytes.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Memory<T>(Memory<T> bytes) where T : unmanaged => Array(bytes.ToArray());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static From Memory<T>(ReadOnlyMemory<T> bytes) where T : unmanaged => Array(bytes.ToArray());


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte[](From from) => from.Data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator From(byte[] bytes) => new From(bytes);
    }

    public unsafe sealed class To
    {
        private static readonly Regex INI_REGEX_SECTION = new Regex(@"^\s*\[\s*(?<sec>[\w\-]+)\s*\]", RegexOptions.Compiled);
        private static readonly Regex INI_REGEX_PROPERTY = new Regex(@"^\s*(?<prop>[\w\-]+)\s*\=\s*(?<val>.*)\s*$", RegexOptions.Compiled);


        public byte[] Bytes { get; }


        internal To(byte[] data) => Bytes = data;

        public override string ToString() => String();

        public string DrunkBishop() => DrunkBishop(17, 9);

        public string DrunkBishop(int width, int height, string chars = " .o+=*BOX@%&#/^", bool asci_border = true)
        {
            byte[,] matrix = new byte[height, width];
            int y = height / 2;
            int x = width / 2;
            int s, i = 0;

            foreach (byte b in Bytes)
                for (i = 4; i-- > 0; matrix[
                    y -= s < 2 ? y > 0 ? 1 : 0 : y / (height - 1) - 1,
                    x -= s % 2 > 0 ? x / (width - 1) - 1 : x > 0 ? 1 : 0
                ]++)
                    s = (b >> (i * 2)) & 3;

            matrix[y, x] = 0xff;
            matrix[height / 2, width / 2] = 0xfe;
            i = 0;

            string r = asci_border ? $"+{new string('-', width)}+\n|"
                                   : $"┌{new string('─', width)}┐\n│";

            do
            {
                byte value = matrix[y = i / width, x = i % width];

                r += value switch { 0xff => 'E', 0xfe => 'S' , byte v when v >= chars.Length => chars[value % (chars.Length - 1) + 1], _ => chars[value] };

                if (x > width - 2)
                {
                    r += asci_border ? "|\n" : "│\n";

                    if (y < height - 1)
                        r += asci_border ? '|' : '│';
                    else
                        r += asci_border ? $"+{new string('-', width)}+" : $"└{new string('─', width)}┘";
                }
            }
            while (++i < width * height);

            return r;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Hex(bool uppercase = true, bool spacing = false) => string.Join(spacing ? " " : "", Bytes.Select(b => b.ToString(uppercase ? "X2" : "x2")));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string String() => String(BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string String(Encoding enc) => enc.GetString(Bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringBuilder StringBuilder() => StringBuilder(BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public StringBuilder StringBuilder(Encoding enc) => new StringBuilder(String(enc));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDictionary<string, IDictionary<string, string>> INI() => INI(BytewiseEncoding.Instance);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public IDictionary<string, IDictionary<string, string>> INI(Encoding enc)
        {
            Dictionary<string, IDictionary<string, string>> ini = new();
            string section = "";

            foreach (string line in Lines(enc))
            {
                string ln = (line.Contains('#') ? line[..line.LastIndexOf('#')] : line).Trim();

                if (ln.Match(INI_REGEX_SECTION, out Match m))
                    section = m.Groups["sec"].ToString();
                else if (ln.Match(INI_REGEX_PROPERTY, out m))
                {
                    if (!ini.ContainsKey(section))
                        ini[section] = new Dictionary<string, string>();

                    ini[section][m.Groups["prop"].ToString()] = m.Groups["val"].ToString();
                }
            }

            return ini;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string[] Lines(string separator = "\n") => Lines(BytewiseEncoding.Instance, separator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string[] Lines(Encoding enc, string separator = "\n") => String(enc).SplitIntoLines(separator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string Base64() => Convert.ToBase64String(Bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string DataURI(string mime = "application/octet-stream") => $"data:{mime};base64,{Base64()}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Stream(Stream stream) => Stream().CopyTo(stream);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public MemoryStream Stream() => new MemoryStream(Bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public BinaryReader BinaryReader() => new BinaryReader(Stream());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void BinaryWriter(BinaryWriter writer) => writer.Write(Bytes);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void File(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            fs.Write(Bytes);
            fs.Flush();
            fs.Close();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void File(Uri path) => File(Path.GetFileName(path.LocalPath));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void File(FileInfo file) => File(file.FullName);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap Bitmap()
        {
            using MemoryStream ms = Stream();

            return (Bitmap)Image.FromStream(ms);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Bitmap RGBAEncodedBitmap()
        {
            RGBAColor[] pixels = Array<RGBAColor>();
            int len = pixels.Length;
            int i = (int)Math.Sqrt(len);
            int fac = 1;

            while (i-- > 1)
                if (len % i == 0)
                {
                    fac = i;

                    break;
                }

            Bitmap bitmap = new Bitmap(fac, len / fac, PixelFormat.Format32bppArgb);

            bitmap.LockRGBAPixels((ptr, _, _) => pixels.CopyTo(ptr));

            return bitmap;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Pointer<T>(T* pointer)
            where T : unmanaged
        {
            byte* dst = (byte*)pointer;

            for (int i = 0, l = Bytes.Length; i < l; ++i)
                dst[i] = Bytes[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Unmanaged<T>()
            where T : unmanaged
        {
            T t = default;

            Pointer(&t);

            return t;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T[] Array<T>()
            where T : unmanaged
        {
            byte[] arr = Bytes;

            fixed (byte* ptr = arr)
            {
                int len = *(int*)ptr;
                T[] res = new T[len];
                T* src = (T*)(ptr + 4);

                for (int i = 0; i < len; ++i)
                    res[i] = src[i];

                return res;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<T> Span<T>() where T : unmanaged => Array<T>().AsSpan();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<T> ReadOnlySpan<T>() where T : unmanaged => new ReadOnlySpan<T>(Array<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<T> Memory<T>() where T : unmanaged => new Memory<T>(Array<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlyMemory<T> ReadOnlyMemory<T>() where T : unmanaged => new ReadOnlyMemory<T>(Array<T>());

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Field[,] CompressedMatrix<Field>() where Field : unmanaged, IField<Field> => CompressedStorageFormat<Field>().ToMatrix();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public CompressedStorageFormat<Field> CompressedStorageFormat<Field>() where Field : unmanaged, IField<Field> =>
            Mathematics.LinearAlgebra.CompressedStorageFormat<Field>.FromBytes(Bytes);

        public UnsafeFunctionPointer FunctionPointer()
        {
            byte[] bytes = Bytes;
            void* buffer;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                buffer = NativeInterop.VirtualAlloc(null, bytes.Length, 0x1000, 4);

                int dummy;

                Marshal.Copy(bytes, 0, (nint)buffer, bytes.Length);
                NativeInterop.VirtualProtect(buffer, bytes.Length, 0x20, &dummy);
            }
            else
            {
                NativeInterop.posix_memalign(&buffer, 4096, bytes.Length);
                Marshal.Copy(bytes, 0, (nint)buffer, bytes.Length);
                NativeInterop.mprotect(buffer, bytes.Length, 0b_0000_0111); // rwx
            }

            return new UnsafeFunctionPointer(buffer, bytes.Length);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte[](To to) => to.Bytes;
    }

    public unsafe sealed partial class UnsafeFunctionPointer
        : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;

        public void* BufferAddress { get; }

        public int BufferSize { get; }

        public ReadOnlySpan<byte> InstructionBytes => new ReadOnlySpan<byte>(BufferAddress, BufferSize);


        internal UnsafeFunctionPointer(void* buffer, int size)
        {
            BufferAddress = buffer;
            BufferSize = size;
        }

        ~UnsafeFunctionPointer() => Dispose(false);

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    NativeInterop.VirtualFree(BufferAddress, 0, 0x8000);
                else
                    NativeInterop.free(BufferAddress);

                IsDisposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);

            GC.SuppressFinalize(this);
        }
    }


    // DATA:
    //   - array
    //   - bitmap
    //   - qr code
    //   - dictionary
    //   - anonymous obj
    // REPR:
    //   - json
    //   - xml
}
