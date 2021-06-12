using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using System.Drawing.Imaging;
using System.Globalization;
using System.Text.Json;
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
    public unsafe record DataStream(byte[] Data)
        : IEnumerable<byte>
    {
        private static readonly Regex FTP_PROTOCOL_REGEX = new(@"^(?<protocol>ftps?):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<url>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SSH_PROTOCOL_REGEX = new(@"^(sftp|ssh|s?scp):\/\/(?<uname>[^:]+)(:(?<passw>[^@]+))?@(?<host>[^:\/]+|\[[0-9a-f\:]+\])(:(?<port>[0-9]{1,6}))?(\/|\\)(?<path>.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex BASE64_REGEX = new(@"^.\s*data:\s*[^\w\/\-\+]+\s*;(\s*base64\s*,)?(?<data>(?:[a-z0-9+/]{4})*(?:[a-z0-9+/]{2}==|[a-z0-9+/]{3}=)?)$", RegexOptions.Compiled | RegexOptions.Compiled);


        public static DataStream Empty { get; } = new(Array.Empty<byte>());

        public static JsonSerializerOptions DefaultJSONOptions { get; } = new()
        {
            AllowTrailingCommas = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
            PropertyNameCaseInsensitive = true,
            WriteIndented = true,
            IncludeFields = true,
        };


        public DataStream this[Range range] => Slice(range);

        public DataStream this[Index start, Index end] => Slice(start, end);

        public ref byte this[Index index] => ref Data[index];

        public int ByteCount => Data.Length;


        public DataStream Compress(CompressionFunction algorithm) => Data.Compress(algorithm);

        public DataStream Uncompress(CompressionFunction algorithm) => Data.Uncompress(algorithm);

        public DataStream Encrypt(BinaryCipher algorithm, byte[] key) => Data.Encrypt(algorithm, key);

        public DataStream Decrypt(BinaryCipher algorithm, byte[] key) => Data.Decrypt(algorithm, key);

        public DataStream Hex() => FromString(ToHexString());

        public DataStream UnHex() => FromHex(ToString());

        public DataStream Hash<T>(T hash_function) where T : HashFunction<T> => hash_function.Hash(Data);

        public DataStream Hash<T>() where T : HashFunction<T>, new() => Hash(new T());

        public DataStream HexDump()
        {
            ConsoleExtensions.HexDump(Data);

            return this;
        }

        public DataStream HexDump(TextWriter writer)
        {
            ConsoleExtensions.HexDump(Data, writer);

            return this;
        }

        public DataStream Slice(Index start, Index end) => Slice(start..end);

        public DataStream Slice(Range range) => FromBytes(Data[range]);

        public DataStream Concat(params DataStream[] others) => Concat(others.Prepend(this));

        public DataStream Append(params DataStream[] others) => Concat(others.Prepend(this));

        public DataStream Prepend(DataStream first) => DataStream.Concat(new[] { first, this });

        public DataStream Where(Func<byte, bool> predicate) => Data.Where(predicate).ToArray();

        public DataStream Select(Func<byte, byte> function) => Data.ToArray(function);

        public DataStream Reverse() => Data.Reverse().ToArray();

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

        public void ToFile(string path) => ToFile(path, FileMode.Create);

        public void ToFile(string path, FileMode mode, FileAccess access = FileAccess.Write, FileShare share = FileShare.Read)
        {
            using FileStream fs = new(path, mode, access, share);

            fs.Write(Data);
            fs.Flush();
            fs.Close();
            fs.Dispose();
        }

        public void ToFile(Uri path) => ToFile(Path.GetFileName(path.LocalPath));

        public void ToFile(Uri path, FileMode mode, FileAccess access = FileAccess.Write, FileShare share = FileShare.Read) =>
            ToFile(Path.GetFileName(path.LocalPath), mode, access, share);

        public void ToFile(FileInfo file) => ToFile(file.FullName);

        public void ToFile(FileInfo file, FileMode mode, FileAccess access = FileAccess.Write, FileShare share = FileShare.Read) =>
            ToFile(file.FullName, mode, access, share);

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

            Bitmap bitmap = new(fac, len / fac, PixelFormat.Format32bppArgb);

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
            DataStream[] sources = ToArrayOfSources();
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
            DataStream[] sources = ToArrayOfSources();
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
            DataStream[] sources = ToArrayOfSources();
            int dim0 = sources[0].ToUnmanaged<int>();
            int dim1 = sources[1].ToUnmanaged<int>();
            int dim2 = sources[2].ToUnmanaged<int>();
            int dim3 = sources[3].ToUnmanaged<int>();
            T[] flat = sources[4].ToArray<T>();
            T[,,,] array = new T[dim0, dim1, dim2, dim3];

            Parallel.For(0, flat.Length, i => array[i / (dim3 * dim2 * dim1), i / (dim3 * dim2) % dim1, i / dim3 % dim2, i % dim3] = flat[i]);

            return array;
        }

        public DataStream[] ToArrayOfSources() => ToJaggedArray2D<byte>().ToArray(bytes => new DataStream(bytes));

        public Span<T> ToSpan<T>() where T : unmanaged => ToArray<T>().AsSpan();

        public ReadOnlySpan<T> ToReadOnlySpan<T>() where T : unmanaged => new(ToArray<T>());

        public Memory<T> ToMemory<T>() where T : unmanaged => new(ToArray<T>());

        public ReadOnlyMemory<T> ToReadOnlyMemory<T>() where T : unmanaged => new(ToArray<T>());

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

        public T ToJSON<T>(JsonSerializerOptions? options = null) => ToJSON<T>(BytewiseEncoding.Instance, options);

        public T ToJSON<T>(Encoding enc, JsonSerializerOptions? options = null) => (T)ToJSON(typeof(T), enc, options)!;

        public object? ToJSON(Type type, JsonSerializerOptions? options = null) => ToJSON(type, BytewiseEncoding.Instance, options);

        public object? ToJSON(Type type, Encoding enc, JsonSerializerOptions? options = null) => JsonSerializer.Deserialize(ToString(enc), type, options ?? DefaultJSONOptions);

        public object? ToObject() => FunctionExtensions.TryDo(() =>
        {
            DataStream[] sources = ToArrayOfSources();

            return sources[1].ToJSON(sources[0].ToType());
        }, null);

        public Type? ToType()
        {
            DataStream[] sources = ToArrayOfSources();
            Guid clsid = sources[0].ToUnmanaged<Guid>();
            string name = sources[1].ToString();

            return System.Type.GetTypeFromCLSID(clsid) ?? System.Type.GetType(name);
        }


        public static DataStream Concat(IEnumerable<DataStream?>? sources)
        {
            MemoryStream s = new();

            foreach (DataStream? source in sources ?? Array.Empty<DataStream>())
                if (source is { })
                    s.Write(source.Data, 0, source.ByteCount);

            s.Seek(0, SeekOrigin.Begin);

            return FromStream(s);
        }

        public static DataStream FromUnmanaged<T>(T data) where T : unmanaged => FromPointer(&data);

        public static DataStream FromPointer<T>(T* data) where T : unmanaged => FromPointer(data, sizeof(T));

        public static DataStream FromPointer(nint pointer, int byte_count) => FromPointer((void*)pointer, byte_count);

        public static DataStream FromPointer(void* data, int byte_count) => FromPointer((byte*)data, byte_count);

        public static DataStream FromPointer<T>(T* data, int byte_count)
            where T : unmanaged
        {
            byte[] arr = new byte[byte_count];
            byte* ptr = (byte*)data;

            for (int i = 0; i < byte_count; ++i)
                arr[i] = ptr[i];

            return FromBytes(arr);
        }

        public static DataStream FromArray<T>(IEnumerable<T> collection)
            where T : unmanaged
        {
            T[] array = collection as T[] ?? collection.ToArray();
            byte[] bytes = new byte[array.Length * sizeof(T) + 4];

            fixed (byte* ptr = bytes)
            {
                *(int*)ptr = array.Length;
                T* dst = (T*)(ptr + 4);

                for (int i = 0; i < array.Length; ++i)
                    dst[i] = array[i];
            }

            return FromBytes(bytes);
        }

        public static DataStream FromArrayOfSources(IEnumerable<DataStream> sources) => FromArrayOfSources(sources.ToArray());

        public static DataStream FromArrayOfSources(params DataStream[] sources) => FromJaggedArray(sources.ToArray(s => s.Data));

        public static DataStream FromJaggedArray<T>(T[][] array)
            where T : unmanaged
        {
            DataStream[] arrays = array.ToArray(FromArray);

            return FromUnmanaged(arrays.Length).Append(arrays);
        }

        public static DataStream FromJaggedArray<T>(T[][][] array)
            where T : unmanaged
        {
            DataStream[] arrays = array.ToArray(FromJaggedArray);

            return FromUnmanaged(arrays.Length).Append(arrays);
        }

        public static DataStream FromJaggedArray<T>(T[][][][] array)
            where T : unmanaged
        {
            DataStream[] arrays = array.ToArray(FromJaggedArray);

            return FromUnmanaged(arrays.Length).Append(arrays);
        }

        public static DataStream FromMultiDimensionalArray<T>(T[,] array)
            where T : unmanaged
        {
            int dim0 = array.GetLength(0);
            int dim1 = array.GetLength(1);
            T[] flat = new T[dim0 * dim1];

            Parallel.For(0, flat.Length, i => flat[i] = array[i / dim0, i % dim0]);

            return FromArrayOfSources(
                FromUnmanaged(dim0),
                FromUnmanaged(dim1),
                FromArray(flat)
            );
        }

        public static DataStream FromMultiDimensionalArray<T>(T[,,] array)
            where T : unmanaged
        {
            int dim0 = array.GetLength(0);
            int dim1 = array.GetLength(1);
            int dim2 = array.GetLength(2);
            T[] flat = new T[dim0 * dim1 * dim2];

            Parallel.For(0, flat.Length, i => flat[i] = array[i / (dim2 * dim1), i / dim2 % dim1, i % dim2]);

            return FromArrayOfSources(
                FromUnmanaged(dim0),
                FromUnmanaged(dim1),
                FromUnmanaged(dim2),
                FromArray(flat)
            );
        }

        public static DataStream FromMultiDimensionalArray<T>(T[,,,] array)
            where T : unmanaged
        {
            int dim0 = array.GetLength(0);
            int dim1 = array.GetLength(1);
            int dim2 = array.GetLength(2);
            int dim3 = array.GetLength(3);
            T[] flat = new T[dim0 * dim1 * dim2 * dim3];

            Parallel.For(0, flat.Length, i => flat[i] = array[i / (dim3 * dim2 * dim1), i / (dim3 * dim2) % dim1, i / dim3 % dim2, i % dim3]);

            return FromArrayOfSources(
                FromUnmanaged(dim0),
                FromUnmanaged(dim1),
                FromUnmanaged(dim2),
                FromUnmanaged(dim3),
                FromArray(flat)
            );
        }

        public static DataStream FromMultiDimensionalArray<T>(Array array, int dimensions)
        {
            int[] dims = Enumerable.Range(0, dimensions).ToArray(array.GetLength);

            // Todo

            throw new NotImplementedException();
        }

        public static DataStream FromBitmapAsRGBAEncoded(Bitmap bitmap) => FromArray(bitmap.ToPixelArray());

        public static DataStream FromBitmap(Bitmap bitmap)
        {
            using MemoryStream ms = new();

            bitmap.Save(ms, ImageFormat.Png);
            ms.Seek(0, SeekOrigin.Begin);

            return FromStream(ms);
        }

        public static DataStream FromStream(Stream stream, bool seek_beginning = true)
        {
            using MemoryStream ms = new();

            if (seek_beginning && stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);

            stream.CopyTo(ms);
            ms.Seek(0, SeekOrigin.Begin);

            return FromBytes(ms.ToArray());
        }

        public static DataStream FromString(object? obj) => FromString(obj, BytewiseEncoding.Instance);

        public static DataStream FromString(object? obj, Encoding enc) => FromString(obj?.ToString() ?? "", enc);

        public static DataStream FromString(string str) => FromString(str, BytewiseEncoding.Instance);

        public static DataStream FromString(string str, Encoding enc) => FromBytes(enc.GetBytes(str));

        public static DataStream FromINI(INISection ini_section) => FromINI(ini_section, BytewiseEncoding.Instance);

        public static DataStream FromINI(INISection ini_section, Encoding enc) => FromINI(new INIFile() { [string.Empty] = ini_section }, enc);

        public static DataStream FromINI(INIFile ini) => FromINI(ini, BytewiseEncoding.Instance);

        public static DataStream FromINI(INIFile ini, Encoding enc) => FromString(ini.Serialize(), enc);

        public static DataStream FromObjectAsJSON(object? obj, JsonSerializerOptions? options = null) => FromObjectAsJSON(obj, BytewiseEncoding.Instance, options);

        public static DataStream FromObjectAsJSON(object? obj, Encoding enc, JsonSerializerOptions? options = null) => FromString(JsonSerializer.Serialize(obj, options ?? DefaultJSONOptions), enc);

        public static DataStream FromObject(object? obj)
        {
            if (obj is null)
                return Empty;
            else
                return FromArrayOfSources(
                    FromType(obj.GetType()),
                    FromObjectAsJSON(obj)
                );
        }

        public static DataStream FromStringBuilder(StringBuilder sb) => FromStringBuilder(sb, BytewiseEncoding.Instance);

        public static DataStream FromStringBuilder(StringBuilder sb, Encoding enc) => FromString(sb.ToString(), enc);

        public static DataStream FromTextLines(IEnumerable<string> lines, string separator = "\n") => FromTextLines(lines, BytewiseEncoding.Instance, separator);

        public static DataStream FromTextLines(IEnumerable<string> lines, Encoding enc, string separator = "\n") => FromString(string.Join(separator, lines), enc);

        public static DataStream FromBase64(string str) => FromBytes(Convert.FromBase64String(str));

        public static DataStream FromFile(string path) => FromFile(new FileInfo(path));

        public static DataStream FromFile(string path, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read) =>
            FromFile(new FileInfo(path), mode, access, share);

        public static DataStream FromFile(FileInfo file) => FromFile(file, FileMode.Open);

        public static DataStream FromFile(FileInfo file, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read)
        {
            using FileStream fs = new(file.FullName, mode, access, share);
            DataStream data = FromStream(fs);

            fs.Close();
            fs.Dispose();

            return data;
        }

        public static DataStream FromFile(Uri path) => FromFile(Path.GetFileName(path.LocalPath));

        public static DataStream FromFile(Uri path, FileMode mode, FileAccess access = FileAccess.Read, FileShare share = FileShare.Read) =>
            FromFile(Path.GetFileName(path.LocalPath), mode, access, share);

        public static DataStream FromCompressedMatrix<Field>(Field[,] matrix)
            where Field : unmanaged, IField<Field> => FromCompressedStorageFormat(new CompressedStorageFormat<Field>(matrix));

        public static DataStream FromCompressedMatrix<Field>(Algebra<Field>.IComposite2D matrix)
            where Field : unmanaged, IField<Field> => FromCompressedStorageFormat(new CompressedStorageFormat<Field>(matrix));

        public static DataStream FromCompressedStorageFormat<Field>(CompressedStorageFormat<Field> compressed) where Field : unmanaged, IField<Field> => FromBytes(compressed.ToBytes());

        public static DataStream FromWebResource(Uri uri)
        {
            using WebClient wc = new();

            return FromBytes(wc.DownloadData(uri));
        }

        public static DataStream FromWebResource(string uri)
        {
            using WebClient wc = new();

            return FromBytes(wc.DownloadData(uri));
        }

        public static DataStream FromHTTP(string uri)
        {
            using HttpClient hc = new()
            {
                Timeout = new TimeSpan(0, 0, 15)
            };

            return FromBytes(hc.GetByteArrayAsync(uri)
                           .ConfigureAwait(false)
                           .GetAwaiter()
                           .GetResult());
        }

        public static DataStream FromFTP(string uri)
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
                return FromStream(s);
        }

        public static DataStream FromSSH(string uri)
        {
            if (uri.Match(SSH_PROTOCOL_REGEX, out ReadOnlyIndexer<string, string>? g))
            {
                string host = g["host"];
                string uname = g["uname"];
                string passw = g["passw"];
                string rpath = '/' + g["path"];

                if (!int.TryParse(g["port"], out int port))
                    port = 22;

                using (SftpClient sftp = new(host, port, uname, passw))
                using (MemoryStream ms = new())
                {
                    sftp.Connect();
                    sftp.DownloadFile(rpath, ms);
                    sftp.Disconnect();
                    ms.Seek(0, SeekOrigin.Begin);

                    return FromStream(ms);
                }
            }
            else
                throw new ArgumentException($"Invalid SSH URI: The URI should have the format '<protocol>://<user>:<password>@<host>:<port>/<path>'.", nameof(uri));
        }

        public static DataStream FromDataURI(string uri)
        {
            if (uri.Match(BASE64_REGEX, out ReadOnlyIndexer<string, string>? groups))
                return FromBase64(groups!["data"]);

            throw new ArgumentException("Invalid data URI.");
        }

        public static DataStream FromHex(string str)
        {
            str = new string(str.ToLower().Where(c => (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f')).ToArray());

            if ((str.Length % 2) == 1)
                str = '0' + str;

            byte[] data = new byte[str.Length / 2];

            for (int i = 0; i < data.Length; ++i)
                data[i] = byte.Parse(str[(i * 2)..((i + 1) * 2)], NumberStyles.HexNumber);

            return FromBytes(data);
        }

        public static DataStream FromBytes(IEnumerable<byte>? bytes) => FromBytes(bytes?.ToArray());

        public static DataStream FromBytes(params byte[]? bytes) => new(bytes ?? Array.Empty<byte>());

        public static DataStream FromBytes(byte[] bytes, int offset) => FromBytes(bytes[offset..]);

        public static DataStream FromBytes(byte[] bytes, int offset, int count) => FromBytes(bytes[offset..(offset + count)]);

        public static DataStream FromSpan<T>(Span<T> bytes) where T : unmanaged => FromArray(bytes.ToArray());

        public static DataStream FromSpan<T>(ReadOnlySpan<T> bytes) where T : unmanaged => FromArray(bytes.ToArray());

        public static DataStream FromMemory<T>(Memory<T> bytes) where T : unmanaged => FromArray(bytes.ToArray());

        public static DataStream FromMemory<T>(ReadOnlyMemory<T> bytes) where T : unmanaged => FromArray(bytes.ToArray());

        public static DataStream FromType(Type type) => FromArrayOfSources(FromUnmanaged(type.GUID), FromString(type.AssemblyQualifiedName ?? type.FullName ?? type.ToString()));

        public static DataStream FromType<T>() => FromType(typeof(T));

        [SkipLocalsInit]
        public static DataStream FromGhostStackFrame(int offset, int size)
        {
            byte __start;

            return FromPointer(&__start + offset, size);
        }


        public static implicit operator byte[](DataStream data) => data.Data;

        public static implicit operator DataStream(byte[] bytes) => new(bytes);

        public static implicit operator MemoryStream(DataStream data) => data.ToStream();

        public static implicit operator DataStream(Stream stream) => FromStream(stream);
    }

    public unsafe sealed partial class UnsafeFunctionPointer
        : IDisposable
    {
        public bool IsDisposed { get; private set; } = false;

        public void* BufferAddress { get; }

        public int BufferSize { get; }

        public ReadOnlySpan<byte> InstructionBytes => new(BufferAddress, BufferSize);


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

        public static UnsafeFunctionPointer FromBuffer(Span<byte> buffer) => DataStream.FromSpan(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(ReadOnlySpan<byte> buffer) => DataStream.FromSpan(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(Memory<byte> buffer) => DataStream.FromMemory(buffer).ToFunctionPointer();

        public static UnsafeFunctionPointer FromBuffer(ReadOnlyMemory<byte> buffer) => DataStream.FromMemory(buffer).ToFunctionPointer();

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
