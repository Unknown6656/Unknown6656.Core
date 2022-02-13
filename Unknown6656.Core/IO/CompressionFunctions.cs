﻿///////////////////////////////////////////////////////////////////////
//             AUTOGENERATED 2020-08-31 11:29:02.588715              //
//   All your changes to this file will be lost upon re-generation.  //
///////////////////////////////////////////////////////////////////////

#nullable enable

using System.IO.Compression;
using System.IO;

namespace Unknown6656.IO
{
    public abstract partial class CompressionFunction
    {

        public static GZipCompression GZip { get; } = new GZipCompression();

        public static DeflateCompression Deflate { get; } = new DeflateCompression();

        public static BrotliCompression Brotli { get; } = new BrotliCompression();
    }

    public sealed class GZipCompression
        : CompressionFunction
    {
        public override byte[] CompressData(byte[] data)
        {
            using MemoryStream mso = new();
            using GZipStream compr = new(mso, CompressionLevel.Optimal);
            using BinaryWriter wr = new(compr);

            wr.Write(data.Length);
            wr.Write(data, 0, data.Length);
            wr.Flush();
            wr.Close();
            compr.Close();

            return mso.ToArray();
        }

        public override byte[] UncompressData(byte[] data)
        {
            using MemoryStream msi = new(data);
            using GZipStream decompr = new(msi, CompressionMode.Decompress);
            using MemoryStream mso = new();
            using BinaryReader rd = new(mso);

            decompr.CopyTo(mso);
            decompr.Close();
            mso.Seek(0, SeekOrigin.Begin);

            int count = rd.ReadInt32();

            return rd.ReadBytes(count);
        }
    }

    public sealed class DeflateCompression
        : CompressionFunction
    {
        public override byte[] CompressData(byte[] data)
        {
            using MemoryStream mso = new();
            using DeflateStream compr = new(mso, CompressionLevel.Optimal);
            using BinaryWriter wr = new(compr);

            wr.Write(data.Length);
            wr.Write(data, 0, data.Length);
            wr.Flush();
            wr.Close();
            compr.Close();

            return mso.ToArray();
        }

        public override byte[] UncompressData(byte[] data)
        {
            using MemoryStream msi = new(data);
            using DeflateStream decompr = new(msi, CompressionMode.Decompress);
            using MemoryStream mso = new();
            using BinaryReader rd = new(mso);

            decompr.CopyTo(mso);
            decompr.Close();
            mso.Seek(0, SeekOrigin.Begin);

            int count = rd.ReadInt32();

            return rd.ReadBytes(count);
        }
    }

    public sealed class BrotliCompression
        : CompressionFunction
    {
        public override byte[] CompressData(byte[] data)
        {
            using MemoryStream mso = new();
            using BrotliStream compr = new(mso, CompressionLevel.Optimal);
            using BinaryWriter wr = new(compr);

            wr.Write(data.Length);
            wr.Write(data, 0, data.Length);
            wr.Flush();
            wr.Close();
            compr.Close();

            return mso.ToArray();
        }

        public override byte[] UncompressData(byte[] data)
        {
            using MemoryStream msi = new(data);
            using BrotliStream decompr = new(msi, CompressionMode.Decompress);
            using MemoryStream mso = new();
            using BinaryReader rd = new(mso);

            decompr.CopyTo(mso);
            decompr.Close();
            mso.Seek(0, SeekOrigin.Begin);

            int count = rd.ReadInt32();

            return rd.ReadBytes(count);
        }
    }
}