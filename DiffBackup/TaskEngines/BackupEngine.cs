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
        public static async Task Run(
            BackupJobConfiguration jobConfig,
            IFileSystem sourceFileSystem,
            IFileSystem targetFileSystem,
            ILogger logger,
            IRealTimeProvider realTime,
            CancellationToken cancelToken)
        {
            ////////// Validate config //////////
            
            ////////// Lock file system //////////
            Guid backupGuid = new Guid();
            logger.Log("Assigning GUID " + backupGuid.ToString() + " to this job", LogLevel.Vrb);
            VirtualPath lockFile = new VirtualPath("/BACKUP_IN_PROGRESS");
            bool lockOK = TryObtainLockFile(backupGuid, lockFile, targetFileSystem, logger);

            if (!lockOK)
            {
                // handle if override mode is set in job config
                // TODO Get the GUID of the in-progress job?
                throw new Exception("Could not get file system lock. Throwing exception for now.");
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
                await targetFileSystem.DeleteAsync(lockFile).ConfigureAwait(false);
                logger.Log(LogLevel.Std,
                    DataPrivacyClassification.SystemMetadata,
                    "Backup completed at {0:G}!",
                    realTime.Time.ToLocalTime());
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
