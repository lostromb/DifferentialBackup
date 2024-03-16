using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffBackup.Schemas
{
    public class BackupJobConfiguration
    {
        public string SourcePath { get; set; }
        public string DestinationPath { get; set; }
        public bool DryRun { get; set; }
        public bool OverrideExistingLock { get; set; }

        public BackupJobConfiguration(
            string sourcePath,
            string destinationPath)
        {
            SourcePath = sourcePath;
            DestinationPath = destinationPath;
        }
    }
}
