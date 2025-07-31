using DiffBackup.Schemas;
using Durandal.API;
using Durandal.Common.File;
using Durandal.Common.Instrumentation;
using Durandal.Common.Logger;
using Durandal.Common.Tasks;
using Durandal.Common.Time;
using Durandal.Common.Utils.NativePlatform;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiffBackup.TaskEngines
{
    public static class BackupEngine
    {
        private static readonly VirtualPath LOCK_FILE_PATH = new VirtualPath("/BACKUP_IN_PROGRESS");

        // Files smaller than this will use async reads and potentially different buffer sizes to tune performance
        private const int ASYNC_FILE_SIZE_THRESHOLD = 1024 * 1024; // 1 MB

        // The max number of threads to spawn when reading data from disk
        private const int FILE_IO_PARALLELISM = 8;

        public static async Task Run(
            BackupJobConfiguration jobConfig,
            IFileSystem sourceFileSystem,
            IFileSystem targetFileSystem,
            ILogger logger,
            IRealTimeProvider realTime,
            CancellationToken cancelToken)
        {
            ////////// Validate config //////////

            Guid backupGuid = new Guid();
            logger.Log("Assigning GUID " + backupGuid.ToString() + " to this job", LogLevel.Vrb);

            ////////// Lock file system //////////
            // Also create a named system mutex to try and detect if multiple backups are happening at once on this machine.
            // We don't actually lock the mutex, we just use the creation signal to tell if another process already made one.
            // So it could be something else like a pipe if we wanted.
            bool mutexCreated;
            using (Mutex globalMutex = new Mutex(false, "DiffBackupIpcMtx", out mutexCreated))
            {
                if (!mutexCreated)
                {
                    logger.Log("Another process appears to be running a backup at the same time on this machine. Execution WILL NOT continue, even with override. Please check your running processes.", LogLevel.Err);
                    return;
                }

                bool lockOK = TryObtainLockFile(backupGuid, LOCK_FILE_PATH, targetFileSystem, logger);

                if (!lockOK)
                {
                    if (jobConfig.OverrideExistingLock)
                    {
                        logger.Log("Overriding lock from previous backup and continuing. This is intended only to resume a previously failed backup.", LogLevel.Err);
                    }
                    else
                    {
                        // handle if override mode is set in job config
                        // TODO Get the GUID of the in-progress job?
                        throw new Exception("Could not get file system lock. Throwing exception for now.");
                    }
                }

                try
                {
                    ////////// Check for existing backup data //////////

                    ////////// Create index of backup target //////////

                    ////////// Create index of backup source //////////

                    ////////// Calculate deltas between source and target //////////





                }
                finally
                {
                    ////////// Unlock file system, backup finished! //////////
                    logger.Log("Deleting lock file", LogLevel.Vrb);
                    try
                    {
                        await targetFileSystem.DeleteAsync(LOCK_FILE_PATH).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        logger.Log(e, LogLevel.Wrn);
                    }

                    logger.LogFormat(LogLevel.Std,
                        DataPrivacyClassification.SystemMetadata,
                        "Backup completed at {0:G}!",
                        realTime.Time.ToLocalTime());
                }
            }
        }

        private static bool TryObtainLockFile(
            Guid backupGuid,
            VirtualPath lockFile,
            IFileSystem targetFileSystem,
            ILogger logger)
        {
            try
            {
                using (Stream lockFileStream = targetFileSystem.OpenStream(
                    new FileStreamParams(
                        lockFile,
                        FileOpenMode.CreateNew,
                        FileAccessMode.ReadWrite,
                        FileShareMode.None)))
                {
                    byte[] guidBytes = backupGuid.ToByteArray();
                    lockFileStream.Write(guidBytes, 0, guidBytes.Length);
                }

                return true;
            }
            catch (Exception ex)
            {
                logger.Log("Could not obtain lock file on backup target directory. An existing backup is assumed to be in progress.", LogLevel.Wrn);
                logger.Log(ex, LogLevel.Wrn);
                return false;
            }
        }

        /// <summary>
        /// Given a directory name, create a fully detailed tree representation of all children of
        /// that directory, including files and subdirectories recursively.
        /// This method also calculates file checksums using a fixed-capacity async thread pool,
        /// so it is costly in the sense that it will read the full contents of every file to be indexed.
        /// </summary>
        /// <param name="rootDirectory"></param>
        /// <param name="logger"></param>
        /// <param name="cancelToken"></param>
        /// <param name="threadPool"></param>
        /// <returns></returns>
        internal static async Task<TreeDirectory> BuildFullMetadataTree(IFileSystem fileSystem, VirtualPath rootDirectory, ILogger logger, CancellationToken cancelToken, IThreadPool threadPool)
        {
            OSAndArchitecture arch = NativePlatformUtils.GetCurrentPlatform(logger);
            StringComparer fsComparer = arch.OS == PlatformOperatingSystem.Windows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;
            TreeDirectory returnVal = new TreeDirectory(".", fsComparer);
            AtomicCounter queuedThreads = new AtomicCounter();
            AtomicCounter finishedThreads = new AtomicCounter();
            ConcurrentQueue<VirtualPath> erroredFiles = new ConcurrentQueue<VirtualPath>();
            using (IThreadPool fixedThreadPool = new FixedCapacityThreadPool(
                threadPool,
                NullLogger.Singleton,
                NullMetricCollector.Singleton,
                DimensionSet.Empty,
                maxCapacity: FILE_IO_PARALLELISM,
                overschedulingBehavior: ThreadPoolOverschedulingBehavior.BlockUntilThreadsAvailable))
            {
                await BuildFullMetadataTree_Internal(
                    returnVal,
                    fileSystem,
                    rootDirectory,
                    logger,
                    fixedThreadPool,
                    fsComparer,
                    cancelToken,
                    queuedThreads,
                    finishedThreads,
                    erroredFiles).ConfigureAwait(false);
            }

            // Wait for all async tasks to resolve
            SpinWait.SpinUntil(() => queuedThreads.Count == finishedThreads.Count);

            // TODO do something with the errored files?

            return returnVal;
        }

        private static async ValueTask BuildFullMetadataTree_Internal(
            TreeDirectory currentNode,
            IFileSystem fileSystem,
            VirtualPath currentDirectory,
            ILogger logger,
            IThreadPool threadPool,
            StringComparer fsNameComparer,
            CancellationToken cancelToken,
            AtomicCounter queuedThreads,
            AtomicCounter finishedThreads,
            ConcurrentQueue<VirtualPath> erroredFiles)
        {
            logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.EndUserIdentifiableInformation, "Starting indexing directory {0}", currentDirectory.FullName);

            // Enumerate files in this entire directory on the thread pool.
            // Each thread will iteratively mutate the Files list of the current node, so we have to be careful
            // that ownership of that reference is carefully managed to avoid collisions
            queuedThreads.Increment();
            threadPool.EnqueueUserAsyncWorkItem(async () =>
            {
                ValueStopwatch timer = ValueStopwatch.StartNew();
                try
                {
                    foreach (VirtualPath file in await fileSystem.ListFilesAsync(currentDirectory).ConfigureAwait(false))
                    {
                        try
                        {
                            timer.Restart();
                            logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.EndUserIdentifiableInformation, "Starting indexing file {0}", file.FullName);
                            TreeFile fileNode = new TreeFile(file.Name);

                            FileStat stat = await fileSystem.StatAsync(file).ConfigureAwait(false);
                            fileNode.FileSize = (ulong)stat.Size;
                            fileNode.ModTime = (ulong)stat.LastWriteTime.ToUnixTimeMilliseconds();

                            if (stat.Size < ASYNC_FILE_SIZE_THRESHOLD)
                            {
                                // Do small files in overlapped I/O
                                // Rely on NTFS file systems having 64K blocks by default to hopefully get the best perf here, even for small files
                                using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536))
                                {
                                    fileNode.FileCRC32 = await Hasher.GetCRC32CHashAsync(fileStream, cancelToken, bufferSize: 65536);
                                }
                            }
                            else
                            {
                                // Do large files with synchronous linear I/O which performs better for large files (at least on windows)
                                using (FileStream fileStream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 65536))
                                {
                                    fileNode.FileCRC32 = Hasher.GetCRC32CHash(fileStream, cancelToken, bufferSize: 65536);
                                }
                            }

                            timer.Stop();
                            logger.LogFormat(LogLevel.Vrb, DataPrivacyClassification.EndUserIdentifiableInformation, "Finished indexing file after {0:F3} ms: {1}", timer.ElapsedMillisecondsPrecise(), file.FullName);
                            currentNode.Files.Add(fileNode.FileName, fileNode);
                        }
                        catch (Exception e)
                        {
                            // Individual file errors could be because the file is locked, a system file, FS error, network error, etc.
                            // Add errored files to an error queue for reliability
                            logger.Log(e);
                            erroredFiles.Enqueue(file);
                        }
                    }
                }
                catch (Exception e)
                {
                    logger.Log(e);
                }
                finally
                {
                    finishedThreads.Increment();
                }
            });

            foreach (VirtualPath subdir in await fileSystem.ListDirectoriesAsync(currentDirectory).ConfigureAwait(false))
            {
                // Then iterate through all subdirectories
                TreeDirectory subdirNode = new TreeDirectory(subdir.Name, fsNameComparer);
                await BuildFullMetadataTree_Internal(subdirNode, fileSystem, subdir, logger, threadPool, fsNameComparer, cancelToken, queuedThreads, finishedThreads, erroredFiles).ConfigureAwait(false);
                currentNode.Subdirectories.Add(subdirNode.DirectoryName, subdirNode);
            }
        }
    }
}
