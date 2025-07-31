
namespace Sandbox
{
    using DiffBackup;
    using DiffBackup.File;
    using DiffBackup.Schemas;
    using DiffBackup.TaskEngines;
    using Durandal.Common.Config;
    using Durandal.Common.File;
    using Durandal.Common.IO;
    using Durandal.Common.Logger;
    using Durandal.Common.MathExt;
    using Durandal.Common.Tasks;
    using Durandal.Common.Utils;
    using Durandal.Common.Utils.NativePlatform;
    using Durandal.Extensions.Compression.Brotli;
    using Durandal.Extensions.Compression.Crc;
    using Durandal.Extensions.Compression.ZStandard;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    internal class Program
    {
        public static readonly Stopwatch HashTimer = new Stopwatch();
        public static long HashedBytes;

        public static void Main(string[] args)
        {
            AsyncMain(args).Await();
        }

        public static async Task AsyncMain(string[] args)
        {
            //BenchmarkDotNet.Running.BenchmarkRunner.Run(typeof(Benchmarks));

            ILogger logger = new ConsoleLogger();
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            AssemblyReflector.ApplyAccelerators(typeof(CRC32CAccelerator).Assembly, logger.Clone("Accelerators"));

            //CompressionRatioStatistics compressionRatioStats = new CompressionRatioStatistics(logger.Clone("FileCompressionStats"));
            //compressionRatioStats.AddCommonFileFormats();
            //RecurseDirectoryAndProbeCompression(new DirectoryInfo(@"S:\Unsorted\Leela and Salinger"), logger, compressionRatioStats);
            //compressionRatioStats.PrintInternalStats();

            //using (SemaphoreSlim diskIoSemaphore = new SemaphoreSlim(FILE_IO_PARALLELISM))
            //{
            //    HashedBytes = 0;
            //    int filesCounted = 0;
            //    for (int pass = 0; pass < 10; pass++)
            //    {
            //        HashTimer.Start();
            //        filesCounted += RecurseDirectoryAndHash(new DirectoryInfo(@"D:\Backup Test"), logger, diskIoSemaphore).Await();
            //        HashTimer.Stop();
            //        logger.Log("Counted " + filesCounted + " files in " + HashTimer.Elapsed.TotalSeconds + " seconds");
            //        logger.Log(((double)filesCounted / HashTimer.Elapsed.TotalSeconds) + " files per second");
            //        //logger.Log(((double)HashedBytes / 1024 / 1024 / HashTimer.Elapsed.TotalSeconds) + " MB/s");
            //    }
            //}

            InMemoryConfiguration rawConfig = new InMemoryConfiguration(NullLogger.Singleton);
            BackupConfiguration config = new BackupConfiguration(rawConfig);
            IThreadPool totalThreadPool = new TaskThreadPool();
            RealFileSystem fileSystem = new RealFileSystem(logger.Clone("FileSystem"), @"D:\", isReadOnly: true);
            Stopwatch timer = Stopwatch.StartNew();
            TreeDirectory rootDir = await BackupEngine.BuildFullMetadataTree(
                fileSystem,
                new VirtualPath("Backup Test"),
                logger.Clone("Scanner"),
                CancellationToken.None,
                totalThreadPool,
                config).ConfigureAwait(false);

            timer.Stop();
            logger.Log("Indexed " + ((double)rootDir.ChildFileCount / timer.Elapsed.TotalSeconds) + " files per second");
            logger.Log("Indexed " + ((double)rootDir.ChildFileSize / (1024.0 * 1024.0) / timer.Elapsed.TotalSeconds) + " MB per second");

            //FileInformation thisFileInfo = new FileInformation();
            //thisFileInfo.Path = file.FullName;
            //thisFileInfo.NullableSize = thisFileInfo.NullableSize ?? new FileInformation.nullableSize();
            //thisFileInfo.NullableSize.which = FileInformation.nullableSize.WHICH.Value;
            //thisFileInfo.NullableSize.Value = (ulong)file.Length;
            //thisFileInfo.NullableModTime = thisFileInfo.NullableModTime ?? new FileInformation.nullableModTime();
            //thisFileInfo.NullableModTime.which = FileInformation.nullableModTime.WHICH.Value;
            //thisFileInfo.NullableModTime.Value = (ulong)file.LastWriteTimeUtc.ToFileTimeUtc();

            //using (Stream fileOutStream = new FileStream(@"D:\Backup Test\test.cap", FileMode.Create, FileAccess.Write))
            //{
            //    MessageBuilder messageBuilder = MessageBuilder.Create();
            //    manifest.serialize(messageBuilder.BuildRoot<FileManifest.WRITER>());
            //    new FramePump(fileOutStream).Send(messageBuilder.Frame);
            //}

            //// And parse the file back
            //using (Stream fileInStream = new FileStream(@"D:\Backup Test\test.cap", FileMode.Open, FileAccess.Read))
            //{
            //    FileManifest.READER reader = new FileManifest.READER(
            //        DeserializerState.CreateRoot(Framing.ReadSegments(fileInStream)));
            //    reader.GetHashCode();
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
                            ulong hash = await Hasher.HashFile_XXH64_Async(file).ConfigureAwait(false);
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
                        ulong hash = Hasher.HashFile_XXH64(file);
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

        

        private static void RecurseDirectoryAndProbeCompression(
            DirectoryInfo root,
            ILogger logger,
            CompressionRatioStatistics compressionStats)
        {
            if (root == null)
            {
                return;
            }

            foreach (FileInfo file in root.GetFiles())
            {
                try
                {
                    string fileExt = file.Extension;
                    if (string.Equals(file.Name, file.Extension, StringComparison.Ordinal))
                    {
                        // This is mainly to catch hidden dot files on linux will appear to be nothing but an extension
                        // In this case, assume the file has no extension. It still might be compressible
                        fileExt = string.Empty;
                    }

                    // Do we need to update the ratio?
                    if (compressionStats.GetCompressibility(fileExt) == FileTypeCompressibility.Unknown)
                    {
                        compressionStats.AddDynamicEntry(fileExt, GetZstCompressionRatio(file));
                    }
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
                    outputScratch, NullLogger.Singleton, compressionLevel: 6, isolateInnerStream: true))
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
            logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
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
            logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
                "ZStd {0:F2} MB/s, ratio {1:F3}",
                (double)file.Length / 1024 / 1024 / timer.Elapsed.TotalSeconds, ratio);
        }
    }
}
