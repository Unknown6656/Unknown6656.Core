using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Globalization;
using System.Net.Http;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System;

using Renci.SshNet;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics.Cryptography;
using Unknown6656.Controls.Console;
using Unknown6656.Imaging;
using Unknown6656.Common;


// TODO : obj file format


namespace Unknown6656.IO
{
    /// <summary>
    /// A class containing serialization/deserialization functions.
    /// </summary>
    public unsafe sealed class From
        : IEnumerable<byte>
    {
        private static readonly Regex FTP_PROTOCOL_REGEX = new(@"^(?<protocol>ftps?):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<url>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SSH_PROTOCOL_REGEX = new(@"^(sftp|ssh|s?scp):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<host>[^:\/]+|\[[0-9a-f\:]+\])(:(?<port>[0-9]{1,6}))?(\/|\\)(?<path>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BASE64_REGEX = new(@"^.\s*data:\s*[^\w\/\-\+]+\s*;(\s*base64\s*,)?(?<data>(?:[a-z0-9+/]{4})*(?:[a-z0-9+/]{2}==|[a-z0-9+/]{3}=)?)$", RegexOptions.Compiled | RegexOptions.Compiled);


        public static From Empty { get; } = new(System.Array.Empty<byte>());


        public From this[Range range] => Slice(range);

        public From this[Index start, Index end] => Slice(start, end);

        public ref byte this[Index index] => ref Data[index];

        public byte[] Data { get; }

        public int ByteCount => Data.Length;


        private From(byte[] data) => Data = data;

        public From Compress(CompressionFunction algorithm) => Data.Compress(algorithm);

        public From Uncompress(CompressionFunction algorithm) => Data.Uncompress(algorithm);

        public From Encrypt(BinaryCipher algorithm, byte[] key) => Data.Encrypt(algorithm, key);

        public From Decrypt(BinaryCipher algorithm, byte[] key) => Data.Decrypt(algorithm, key);

        public From Hash<T>(T hash_function) where T : HashFunction<T> => hash_function.Hash(Data);

        public From Hash<T>() where T : HashFunction<T>, new() => Hash(new T());

        public From HexDump()
        {
            ConsoleExtensions.HexDump(Data);

            return this;
        }

        public From HexDump(TextWriter writer)
        {
            ConsoleExtensions.HexDump(Data, writer);

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

        public IEnumerator<byte> GetEnumerator() => ((IEnumerable<byte>)Data).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


        public override string ToString() => ToString(BytewiseEncoding.Instance);

        public string ToString(Encoding encoding) => encoding.GetString(Data);

        public StringBuilder ToStringBuilder() => ToStringBuilder(BytewiseEncoding.Instance);

        public StringBuilder ToStringBuilder(Encoding encoding) => new(ToString(encoding));

        public void SendAsEMailBody(string smtp_server, string email_address, string password, string recipient_email, string subject, ushort smtp_port = 587, bool ssl = true, bool body_as_html = false)
        {
            using SmtpClient client = new(smtp_server)
            {
                Credentials = new NetworkCredential(email_address, password),
                Port = smtp_port,
                EnableSsl = ssl,
            };

            client.Send(new MailMessage(email_address, recipient_email, subject, ToString())
            {
                IsBodyHtml = body_as_html,
            });
        }

        public void SendAsEMailAttachment(string smtp_server, string email_address, string password, string recipient_email, string subject, string body, ContentType attachment_type, ushort smtp_port = 587, bool ssl = true, bool body_as_html = false)
        {
            using SmtpClient client = new(smtp_server)
            {
                Credentials = new NetworkCredential(email_address, password),
                Port = smtp_port,
                EnableSsl = ssl,
            };
            MailMessage email = new(email_address, recipient_email, subject, body)
            {
                IsBodyHtml = body_as_html,
            };
            email.Attachments.Add(new Attachment(ToStream(), attachment_type));

            client.Send(email);
        }

        public string[] ToLines(string separator = "\n") => ToLines(BytewiseEncoding.Instance, separator);

        public string[] ToLines(Encoding enc, string separator = "\n") => ToString(enc).SplitIntoLines(separator);

        public string ToDrunkBishop() => ToDrunkBishop(17, 9);

        public string ToDrunkBishop(int width, int height, string chars = " .o+=*BOX@%&#/^", bool asci_border = true)
        {
            byte[,] matrix = new byte[height, width];
            int y = height / 2;
            int x = width / 2;
            int s, i = 0;

            foreach (byte b in Data)
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

                r += value switch
                {
                    0xff => 'E',
                    0xfe => 'S',
                    byte v when v >= chars.Length => chars[value % (chars.Length - 1) + 1],
                    _ => chars[value],
                };

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

        public string ToHexString(bool uppercase = true, bool spacing = false) => string.Join(spacing ? " " : "", Data.Select(b => b.ToString(uppercase ? "X2" : "x2")));

        public string HexDumpString(int width) => ConsoleExtensions.HexDumpToString(Data, width);

        public string ToBaseString(int @base)
        {
            if (@base == 16)
                return ToHexString(false, false);
            else if (@base == 64)
                return ToBase64();

            // TODO

            throw new NotImplementedException();
        }

        public string ToBase64() => Convert.ToBase64String(Data);

        public string ToDataURI(string mime = "application/octet-stream") => $"data:{mime};base64,{ToBase64()}";

        public void ToStream(Stream stream) => ToStream().CopyTo(stream);

        public MemoryStream ToStream() => new(Data);

        public BinaryReader ToBinaryReader() => new(ToStream());

        public void ToBinaryWriter(BinaryWriter writer) => writer.Write(Data);

        public void ToFile(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read);

            fs.Write(Data);
            fs.Flush();
            fs.Close();
        }

        public void ToFile(Uri path) => ToFile(Path.GetFileName(path.LocalPath));

        public void ToFile(FileInfo file) => ToFile(file.FullName);

        public Bitmap ToBitmap()
        {
            using MemoryStream ms = ToStream();

            return (Bitmap)Image.FromStream(ms);
        }

        public Bitmap ToRGBAEncodedBitmap()
        {
            RGBAColor[] pixels = ToArray<RGBAColor>();
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

        public void ToPointer<T>(T* pointer)
            where T : unmanaged
        {
            byte* dst = (byte*)pointer;

            for (int i = 0, l = Data.Length; i < l; ++i)
                dst[i] = Data[i];
        }

        public byte[] ToBytes() => Data;

        public T ToUnmanaged<T>()
            where T : unmanaged
        {
            T t = default;

            ToPointer(&t);

            return t;
        }

        public T[] ToArray<T>()
            where T : unmanaged
        {
            byte[] arr = Data;

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

        public T[][] ToJaggedArray2D<T>() where T : unmanaged => ToBinaryReader().ReadJaggedCollection2D<T>();

        public T[][][] ToJaggedArray3D<T>() where T : unmanaged => ToBinaryReader().ReadJaggedCollection3D<T>();

        public T[][][][] ToJaggedArray4D<T>()
            where T : unmanaged
        {
            BinaryReader reader = ToBinaryReader();
            int size = reader.ReadInt32();
            T[][][][] arrays = new T[size][][][];

            for (int i = 0; i < size; ++i)
                arrays[i] = reader.ReadJaggedCollection3D<T>();

            return arrays;
        }

        public T[,] ToMultiDimensionalArray2D<T>()
            where T : unmanaged
        {
            From[] sources = ToArrayOfSources();
            int dim0 = sources[0].ToUnmanaged<int>();
            int dim1 = sources[1].ToUnmanaged<int>();
            T[] flat = sources[2].ToArray<T>();
            T[,] array = new T[dim0, dim1];

            Parallel.For(0, flat.Length, i => array[i / dim0, i % dim0] = flat[i]);

            return array;
        }

        public T[,,] ToMultiDimensionalArray3D<T>()
            where T : unmanaged
        {
            From[] sources = ToArrayOfSources();
            int dim0 = sources[0].ToUnmanaged<int>();
            int dim1 = sources[1].ToUnmanaged<int>();
            int dim2 = sources[2].ToUnmanaged<int>();
            T[] flat = sources[3].ToArray<T>();
            T[,,] array = new T[dim0, dim1, dim2];

            Parallel.For(0, flat.Length, i => array[i / (dim2 * dim1), i / dim2 % dim1, i % dim2] = flat[i]);

            return array;
        }

        public T[,,,] ToMultiDimensionalArray4D<T>()
            where T : unmanaged
        {
            From[] sources = ToArrayOfSources();
            int dim0 = sources[0].ToUnmanaged<int>();
            int dim1 = sources[1].ToUnmanaged<int>();
            int dim2 = sources[2].ToUnmanaged<int>();
            int dim3 = sources[3].ToUnmanaged<int>();
            T[] flat = sources[4].ToArray<T>();
            T[,,,] array = new T[dim0, dim1, dim2, dim3];

            Parallel.For(0, flat.Length, i => array[i / (dim3 * dim2 * dim1), i / (dim3 * dim2) % dim1, i / dim3 % dim2, i % dim3] = flat[i]);

            return array;
        }

        public From[] ToArrayOfSources() => ToJaggedArray2D<byte>().ToArray(bytes => new From(bytes));

        public Span<T> ToSpan<T>() where T : unmanaged => ToArray<T>().AsSpan();

        public ReadOnlySpan<T> ToReadOnlySpan<T>() where T : unmanaged => new ReadOnlySpan<T>(ToArray<T>());

        public Memory<T> ToMemory<T>() where T : unmanaged => new Memory<T>(ToArray<T>());

        public ReadOnlyMemory<T> ToReadOnlyMemory<T>() where T : unmanaged => new ReadOnlyMemory<T>(ToArray<T>());

        public Field[,] ToCompressedMatrix<Field>() where Field : unmanaged, IField<Field> => ToCompressedStorageFormat<Field>().ToMatrix();

        public CompressedStorageFormat<Field> ToCompressedStorageFormat<Field>()
            where Field : unmanaged, IField<Field> => Mathematics.LinearAlgebra.CompressedStorageFormat<Field>.FromBytes(Data);

        public UnsafeFunctionPointer ToFunctionPointer()
        {
            byte[] bytes = Data;
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

        public INIFile ToINI() => ToINI(BytewiseEncoding.Instance);

        public INIFile ToINI(Encoding encoding) => INIFile.FromINIString(ToString(encoding));


        public static From Multiple(params From?[]? sources) => Multiple(sources as IEnumerable<From?>);

        public static From Multiple(IEnumerable<From?>? sources)
        {
            MemoryStream s = new MemoryStream();

            foreach (From? source in sources ?? System.Array.Empty<From>())
                if (source is { })
                    s.Write(source.Data, 0, source.ByteCount);

            s.Seek(0, SeekOrigin.Begin);

            return Stream(s);
        }

        public static From Unmanaged<T>(T data) where T : unmanaged => Pointer(&data);

        public static From Pointer<T>(T* data) where T : unmanaged => Pointer(data, sizeof(T));

        public static From Pointer(nint pointer, int byte_count) => Pointer((void*)pointer, byte_count);

        public static From Pointer(void* data, int byte_count) => Pointer((byte*)data, byte_count);

        public static From Pointer<T>(T* data, int byte_count)
            where T : unmanaged
        {
            byte[] arr = new byte[byte_count];
            byte* ptr = (byte*)data;

            for (int i = 0; i < byte_count; ++i)
                arr[i] = ptr[i];

            return Bytes(arr);
        }

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

        public static From ArrayOfSources(IEnumerable<From> sources) => ArrayOfSources(sources.ToArray());

        public static From ArrayOfSources(params From[] sources) => JaggedArray(sources.ToArray(s => s.Data));

        public static From JaggedArray<T>(T[][] array)
            where T : unmanaged
        {
            From[] arrays = array.ToArray(Array);

            return Unmanaged(arrays.Length).Append(arrays);
        }

        public static From JaggedArray<T>(T[][][] array)
            where T : unmanaged
        {
            From[] arrays = array.ToArray(JaggedArray);

            return Unmanaged(arrays.Length).Append(arrays);
        }

        public static From JaggedArray<T>(T[][][][] array)
            where T : unmanaged
        {
            From[] arrays = array.ToArray(JaggedArray);

            return Unmanaged(arrays.Length).Append(arrays);
        }

        public static From MultiDimensionalArray<T>(T[,] array)
            where T : unmanaged
        {
            int dim0 = array.GetLength(0);
            int dim1 = array.GetLength(1);
            T[] flat = new T[dim0 * dim1];

            Parallel.For(0, flat.Length, i => flat[i] = array[i / dim0, i % dim0]);

            return ArrayOfSources(
                Unmanaged(dim0),
                Unmanaged(dim1),
                Array(flat)
            );
        }

        public static From MultiDimensionalArray<T>(T[,,] array)
            where T : unmanaged
        {
            int dim0 = array.GetLength(0);
            int dim1 = array.GetLength(1);
            int dim2 = array.GetLength(2);
            T[] flat = new T[dim0 * dim1 * dim2];

            Parallel.For(0, flat.Length, i => flat[i] = array[i / (dim2 * dim1), i / dim2 % dim1, i % dim2]);

            return ArrayOfSources(
                Unmanaged(dim0),
                Unmanaged(dim1),
                Unmanaged(dim2),
                Array(flat)
            );
        }

        public static From MultiDimensionalArray<T>(T[,,,] array)
            where T : unmanaged
        {
            int dim0 = array.GetLength(0);
            int dim1 = array.GetLength(1);
            int dim2 = array.GetLength(2);
            int dim3 = array.GetLength(3);
            T[] flat = new T[dim0 * dim1 * dim2 * dim3];

            Parallel.For(0, flat.Length, i => flat[i] = array[i / (dim3 * dim2 * dim1), i / (dim3 * dim2) % dim1, i / dim3 % dim2, i % dim3]);

            return ArrayOfSources(
                Unmanaged(dim0),
                Unmanaged(dim1),
                Unmanaged(dim2),
                Unmanaged(dim3),
                Array(flat)
            );
        }

        public static From MultiDimensionalArray<T>(Array array, int dimensions)
        {
            int[] dims = Enumerable.Range(0, dimensions).ToArray(array.GetLength);

            // Todo

            throw new NotImplementedException();
        }

        public static From RGBAEncodedBitmap(Bitmap bitmap) => Array(bitmap.ToPixelArray());

        public static From Bitmap(Bitmap bitmap)
        {
            using MemoryStream ms = new MemoryStream();

            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            return Stream(ms);
        }

        public static From Stream(Stream stream, bool seek_beginning = true)
        {
            using MemoryStream ms = new MemoryStream();

            if (seek_beginning && stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return Bytes(ms.ToArray());
        }

        public static From String(object? obj) => String(obj, BytewiseEncoding.Instance);

        public static From String(object? obj, Encoding enc) => String(obj?.ToString() ?? "", enc);

        public static From String(string str) => String(str, BytewiseEncoding.Instance);

        public static From String(string str, Encoding enc) => Bytes(enc.GetBytes(str));

        public static From INI(INISection ini_section) => INI(ini_section, BytewiseEncoding.Instance);

        public static From INI(INISection ini_section, Encoding enc) => INI(new INIFile() { [string.Empty] = ini_section }, enc);

        public static From INI(INIFile ini) => INI(ini, BytewiseEncoding.Instance);

        public static From INI(INIFile ini, Encoding enc) => String(ini.Serialize(), enc);

        public static From StringBuilder(StringBuilder sb) => StringBuilder(sb, BytewiseEncoding.Instance);

        public static From StringBuilder(StringBuilder sb, Encoding enc) => String(sb.ToString(), enc);

        public static From Lines(IEnumerable<string> lines, string separator = "\n") => Lines(lines, BytewiseEncoding.Instance, separator);

        public static From Lines(IEnumerable<string> lines, Encoding enc, string separator = "\n") => String(string.Join(separator, lines), enc);

        public static From Base64(string str) => Bytes(Convert.FromBase64String(str));

        public static From File(string path) => File(new FileInfo(path));

        public static From File(FileInfo file) => Stream(file.OpenRead());

        public static From File(Uri path) => File(Path.GetFileName(path.LocalPath));

        public static From CompressedMatrix<Field>(Field[,] matrix)
            where Field : unmanaged, IField<Field> => CompressedStorageFormat(new CompressedStorageFormat<Field>(matrix));

        public static From CompressedMatrix<Field>(Algebra<Field>.IComposite2D matrix)
            where Field : unmanaged, IField<Field> => CompressedStorageFormat(new CompressedStorageFormat<Field>(matrix));

        public static From CompressedStorageFormat<Field>(CompressedStorageFormat<Field> compressed) where Field : unmanaged, IField<Field> => Bytes(compressed.ToBytes());

        public static From WebResource(Uri uri)
        {
            using WebClient wc = new WebClient();

            return Bytes(wc.DownloadData(uri));
        }

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

            if (uri.Match(FTP_PROTOCOL_REGEX, out ReadOnlyIndexer<string, string>? g))
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
            if (uri.Match(SSH_PROTOCOL_REGEX, out ReadOnlyIndexer<string, string>? g))
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

        public static From DataURI(string uri)
        {
            if (uri.Match(BASE64_REGEX, out ReadOnlyIndexer<string, string>? groups))
                return Base64(groups!["data"]);

            throw new ArgumentException("Invalid data URI.");
        }

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

        public static From Bytes(IEnumerable<byte>? bytes) => Bytes(bytes?.ToArray());

        public static From Bytes(params byte[]? bytes) => new From(bytes ?? new byte[0]);

        public static From Bytes(byte[] bytes, int offset) => Bytes(bytes[offset..]);

        public static From Bytes(byte[] bytes, int offset, int count) => Bytes(bytes[offset..(offset + count)]);

        public static From Span<T>(Span<T> bytes) where T : unmanaged => Array(bytes.ToArray());

        public static From Span<T>(ReadOnlySpan<T> bytes) where T : unmanaged => Array(bytes.ToArray());

        public static From Memory<T>(Memory<T> bytes) where T : unmanaged => Array(bytes.ToArray());

        public static From Memory<T>(ReadOnlyMemory<T> bytes) where T : unmanaged => Array(bytes.ToArray());


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator byte[](From from) => from.Data;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator From(byte[] bytes) => new From(bytes);
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

        public static UnsafeFunctionPointer FromBuffer(Span<byte> buffer) => From.Span(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(ReadOnlySpan<byte> buffer) => From.Span(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(Memory<byte> buffer) => From.Memory(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(ReadOnlyMemory<byte> buffer) => From.Memory(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(IEnumerable<byte> bytes) => FromBuffer(new Span<byte>(bytes.ToArray()));
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
