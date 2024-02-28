
namespace Sandbox
{
    using DiffBackup.File;
    using DiffBackup.Math;
    using Durandal.API;
    using Durandal.Common.Instrumentation;
    using Durandal.Common.IO;
    using Durandal.Common.IO.Crc;
    using Durandal.Common.Logger;
    using Durandal.Common.MathExt;
    using Durandal.Common.Tasks;
    using Durandal.Common.Time;
    using Durandal.Common.Utils;
    using Durandal.Common.Utils.NativePlatform;
    using Durandal.Extensions.Compression.Brotli;
    using Durandal.Extensions.Compression.Crc;
    using Durandal.Extensions.Compression.ZStandard;
    using K4os.Hash.xxHash;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        public static readonly Stopwatch HashTimer = new Stopwatch();
        public static long HashedBytes;
        private const int FILE_READ_BUFFER_SIZE = 65536;
        private const int FILE_IO_PARALLELISM = 4;

        public static void Main(string[] args)
        {
            //BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(Benchmarks));

            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            CRC32CAccelerator.Apply(NullLogger.Singleton);
            ILogger logger = new ConsoleLogger();
            
            Dictionary<string, StatisticalSet> compressionRatioStats = new Dictionary<string, StatisticalSet>();
            RecurseDirectoryAndProbeCompression(new DirectoryInfo(@"E:\Data"), logger, compressionRatioStats);

            //using (SemaphoreSlim diskIoSemaphore = new SemaphoreSlim(FILE_IO_PARALLELISM))
            //{
            //    HashedBytes = 0;
            //    int filesCounted = 0;
            //    for (int pass = 0; pass < 10; pass++)
            //    {
            //        HashTimer.Start();
            //        filesCounted += RecurseDirectoryAndHash(new DirectoryInfo(@"F:\Data"), logger, diskIoSemaphore).Await();
            //        HashTimer.Stop();
            //        logger.Log("Counted " + filesCounted + " files in " + HashTimer.Elapsed.TotalSeconds + " seconds");
            //        logger.Log(((double)filesCounted / HashTimer.Elapsed.TotalSeconds) + " files per second");
            //        //logger.Log(((double)HashedBytes / 1024 / 1024 / HashTimer.Elapsed.TotalSeconds) + " MB/s");
            //    }
            //}
        }

        private static async ValueTask<int> RecurseDirectoryAndHash(
            DirectoryInfo root,
            ILogger logger,
            SemaphoreSlim diskIoSemaphore)
        {
            if (root == null)
            {
                return 0;
            }

            int filesCounted = 0;

            // Do 2 passes for I/O efficiency.
            // Pass 1: do small files in overlapped I/O
            const int asyncFileSizeThreshold = 1024 * 1024; // 1 MB
            List<Task> backgroundTasks = new List<Task>(root.GetFiles().Length);
            foreach (FileInfo file in root.GetFiles())
            {
                if (file.Length < asyncFileSizeThreshold)
                {
                    backgroundTasks.Add(Task.Run(async () =>
                    {
                        await diskIoSemaphore.WaitAsync();
                        try
                        {
                            ulong hash = await HashFile_XXH64_Async(file).ConfigureAwait(false);
                            Interlocked.Add(ref HashedBytes, file.Length);
                            //logger.Log(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:X16} {1}", hash, file.FullName);
                            logger.Log(((double)HashedBytes / 1024 / 1024 / HashTimer.Elapsed.TotalSeconds) + " MB/s");
                        }
                        finally
                        {
                            diskIoSemaphore.Release();
                        }
                    }));

                    filesCounted++;
                }
            }

            foreach (var task in backgroundTasks)
            {
                try
                {
                    await task.ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    // Handle errors on main thread instead of on async
                    logger.Log(e);
                }
            }

            // Pass 2: do large files with synchronous linear I/O which performs better for large files
            foreach (FileInfo file in root.GetFiles())
            {
                if (file.Length >= asyncFileSizeThreshold)
                {
                    try
                    {
                        ulong hash = HashFile_XXH64(file);
                        Interlocked.Add(ref HashedBytes, file.Length);
                        //logger.Log(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:X16} {1}", hash, file.FullName);
                        logger.Log(((double)HashedBytes / 1024 / 1024 / HashTimer.Elapsed.TotalSeconds) + " MB/s");
                    }
                    catch (Exception e)
                    {
                        logger.Log(e);
                    }

                    filesCounted++;
                }
            }

            foreach (DirectoryInfo subDir in root.GetDirectories())
            {
                filesCounted += await RecurseDirectoryAndHash(subDir, logger, diskIoSemaphore).ConfigureAwait(false);
            }

            return filesCounted;
        }

        public static async ValueTask<uint> HashFile_Crc32c_Async(FileInfo file)
        {
            using (FileStream stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: FILE_READ_BUFFER_SIZE, useAsync: true))
            {
                ICRC32C crc = CRC32CFactory.Create();
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

                        crc.Slurp(scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                return crc.Checksum;
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

        private static void RecurseDirectoryAndProbeCompression(
            DirectoryInfo root,
            ILogger logger,
            Dictionary<string, StatisticalSet> compressionStats)
        {
            if (root == null)
            {
                return;
            }

            foreach (FileInfo file in root.GetFiles())
            {
                try
                {
                    string fileExt = file.Extension.ToLowerInvariant();
                    if (file.Name.StartsWith('.'))
                    {
                        fileExt = string.Empty;
                    }

                    StatisticalSet? statsForThisFileType;
                    if (!compressionStats.TryGetValue(fileExt, out statsForThisFileType))
                    {
                        statsForThisFileType = new StatisticalSet();
                        compressionStats.Add(fileExt, statsForThisFileType);
                    }

                    double ratio = GetZstCompressionRatio(file);
                    statsForThisFileType.Add(ratio);
                    logger.Log(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
                        "{0:F4} {1:F4} \"{2}\"", 
                        statsForThisFileType.Mean, statsForThisFileType.StandardDeviation, fileExt);
                }
                catch (Exception e)
                {
                    logger.Log(e);
                }
            }

            foreach (DirectoryInfo subDir in root.GetDirectories())
            {
                RecurseDirectoryAndProbeCompression(subDir, logger, compressionStats);
            }
        }

        public static double GetBrotliCompressionRatio(FileInfo file)
        {
            const long maxFileProbeSize = 10 * 1024 * 1024; // 10MB
            using (FileStream fileInput = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FiniteReadStreamWrapper inputStreamWrapper = new FiniteReadStreamWrapper(fileInput, maxFileProbeSize))
            using (RecyclableMemoryStream outputScratch = new RecyclableMemoryStream(RecyclableMemoryStreamManager.Default))
            {
                if (inputStreamWrapper.Length == 0)
                {
                    return 1.0;
                }

                using (BrotliCompressorStream compressor = new BrotliCompressorStream(outputScratch, leaveOpen: true, quality: 6, window: 16))
                {
                    inputStreamWrapper.CopyToPooled(compressor);
                }

                return (double)outputScratch.Length / (double)inputStreamWrapper.Length;
            }
        }

        public static double GetZstCompressionRatio(FileInfo file)
        {
            const long maxFileProbeSize = 10 * 1024 * 1024; // 10MB
            using (FileStream fileInput = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (FiniteReadStreamWrapper inputStreamWrapper = new FiniteReadStreamWrapper(fileInput, maxFileProbeSize))
            using (RecyclableMemoryStream outputScratch = new RecyclableMemoryStream(RecyclableMemoryStreamManager.Default))
            {
                if (inputStreamWrapper.Length == 0)
                {
                    return 1.0;
                }

                using (Stream compressor = ZStandardCodec.CreateCompressorStream(
                    outputScratch, NullLogger.Singleton, compressionLevel: 4, isolateInnerStream: true))
                {
                    inputStreamWrapper.CopyToPooled(compressor);
                }

                return (double)outputScratch.Length / (double)inputStreamWrapper.Length;
            }
        }

        public static void TestBrotliCompressionSpeed(FileInfo file, ILogger logger)
        {
            Stopwatch timer = Stopwatch.StartNew();
            double ratio = 1.0;
            using (FileStream fileInput = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (NullOutputStream outputScratch = new NullOutputStream())
            {
                using (BrotliCompressorStream brotliCompressor = new BrotliCompressorStream(
                    outputScratch, leaveOpen: true, quality: 6, window: 16))
                {
                    fileInput.CopyToPooled(brotliCompressor);
                }

                ratio = (double)outputScratch.Position / file.Length;
            }

            timer.Stop();
            logger.Log(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
                "Brotli {0:F2} MB/s, ratio {1:F3}",
                (double)file.Length / 1024 / 1024 / timer.Elapsed.TotalSeconds, ratio);
        }

        public static void TestZstCompressionSpeed(FileInfo file, ILogger logger)
        {
            Stopwatch timer = Stopwatch.StartNew();
            double ratio = 1.0;
            using (FileStream fileInput = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (NullOutputStream outputScratch = new NullOutputStream())
            {
                using (Stream compressor = ZStandardCodec.CreateCompressorStream(
                    outputScratch, NullLogger.Singleton, compressionLevel: 4, isolateInnerStream: true))
                {
                    fileInput.CopyToPooled(compressor);
                }

                ratio = (double)outputScratch.Position / file.Length;
            }

            timer.Stop();
            logger.Log(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
                "ZStd {0:F2} MB/s, ratio {1:F3}",
                (double)file.Length / 1024 / 1024 / timer.Elapsed.TotalSeconds, ratio);
        }
    }
}
