using Durandal.Common.IO;
using Durandal.Common.IO.Crc;
using K4os.Hash.xxHash;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiffBackup
{
    internal static class Hasher
    {
        public static async ValueTask<uint> GetCRC32CHashAsync(Stream inputStream, CancellationToken cancelToken, int bufferSize = 32768)
        {
            ICRC32C crc = CRC32CFactory.Create();
            CRC32CState state = new CRC32CState();
            using (PooledBuffer<byte> scratch = BufferPool<byte>.Rent(bufferSize))
            {
                while (true)
                {
                    int readSize = await inputStream.ReadAsync(scratch.Buffer, 0, scratch.Length, cancelToken).ConfigureAwait(false);
                    if (readSize <= 0)
                    {
                        break;
                    }

                    crc.Slurp(ref state, scratch.Buffer.AsSpan(0, readSize));
                }
            }

            return state.Checksum;
        }

        public static uint GetCRC32CHash(Stream inputStream, CancellationToken cancelToken, int bufferSize = 32768)
        {
            ICRC32C crc = CRC32CFactory.Create();
            CRC32CState state = new CRC32CState();
            using (PooledBuffer<byte> scratch = BufferPool<byte>.Rent(bufferSize))
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    int readSize = inputStream.Read(scratch.Buffer, 0, scratch.Length);
                    if (readSize <= 0)
                    {
                        break;
                    }

                    crc.Slurp(ref state, scratch.Buffer.AsSpan(0, readSize));
                }
            }

            return state.Checksum;
        }

        public static async ValueTask<ulong> GetXX64HashAsync(Stream inputStream, CancellationToken cancelToken, int bufferSize = 32768)
        {
            XXH64.State state = new XXH64.State();
            using (PooledBuffer<byte> scratch = BufferPool<byte>.Rent(bufferSize))
            {
                while (true)
                {
                    int readSize = await inputStream.ReadAsync(scratch.Buffer, 0, scratch.Length, cancelToken).ConfigureAwait(false);
                    if (readSize <= 0)
                    {
                        break;
                    }

                    XXH64.Update(ref state, scratch.Buffer.AsSpan(0, readSize));
                }
            }

            return XXH64.Digest(state);
        }

        private const int FILE_READ_BUFFER_SIZE = 65536;

        public static async ValueTask<uint> HashFile_Crc32c_Async(FileInfo file)
        {
            using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: FILE_READ_BUFFER_SIZE, useAsync: true))
            {
                ICRC32C crc = CRC32CFactory.Create();
                CRC32CState crcState = default(CRC32CState);
                byte[] scratch = ArrayPool<byte>.Shared.Rent(FILE_READ_BUFFER_SIZE);
                try
                {
                    while (stream.Position < stream.Length)
                    {
                        int readSize = await stream.ReadAsync(scratch, 0, FILE_READ_BUFFER_SIZE).ConfigureAwait(false);
                        if (readSize <= 0)
                        {
                            break;
                        }

                        crc.Slurp(ref crcState, scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                return crcState.Checksum;
            }
        }

        public static ulong HashFile_XXH64(FileInfo file)
        {
            using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: FILE_READ_BUFFER_SIZE))
            {
                XXH64.State state = new XXH64.State();
                byte[] scratch = ArrayPool<byte>.Shared.Rent(FILE_READ_BUFFER_SIZE);
                try
                {
                    while (stream.Position < stream.Length)
                    {
                        int readSize = stream.Read(scratch, 0, FILE_READ_BUFFER_SIZE);
                        if (readSize <= 0)
                        {
                            break;
                        }

                        XXH64.Update(ref state, scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                return XXH64.Digest(state);
            }
        }

        public static async ValueTask<ulong> HashFile_XXH64_Async(FileInfo file)
        {
            using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: FILE_READ_BUFFER_SIZE, useAsync: true))
            {
                XXH64.State state = new XXH64.State();
                byte[] scratch = ArrayPool<byte>.Shared.Rent(FILE_READ_BUFFER_SIZE);
                try
                {
                    while (stream.Position < stream.Length)
                    {
                        int readSize = await stream.ReadAsync(scratch, 0, FILE_READ_BUFFER_SIZE).ConfigureAwait(false);
                        if (readSize <= 0)
                        {
                            break;
                        }

                        XXH64.Update(ref state, scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                return XXH64.Digest(state);
            }
        }
    }
}
