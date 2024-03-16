using DiffBackup.Schemas;
using Durandal.API;
using Durandal.Common.File;
using Durandal.Common.Logger;
using Durandal.Common.Time;
using System;
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

                    logger.Log(LogLevel.Std,
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
    }
}
