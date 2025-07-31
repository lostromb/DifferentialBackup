using Durandal.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffBackup.Schemas
{
    internal class TreeDirectory
    {
        public TreeDirectory(string directoryName, StringComparer filesystemCaseComparison)
        {
            Subdirectories = new Dictionary<string, TreeDirectory>(filesystemCaseComparison);
            Files = new Dictionary<string, TreeFile>(filesystemCaseComparison);
            DirectoryName = directoryName.AssertNonNullOrEmpty(nameof(directoryName));
        }

        public string DirectoryName { get; private set; }

        public Dictionary<string, TreeDirectory> Subdirectories { get; private set; }

        public Dictionary<string, TreeFile> Files { get; private set; }

        public int ChildFileCount
        {
            get
            {
                return Files.Count + Subdirectories.Values.Sum(s => s.ChildFileCount);
            }
        }

        public long ChildFileSize
        {
            get
            {
                return Files.Values.Sum(s => (long)s.FileSize.GetValueOrDefault(0)) + Subdirectories.Values.Sum(s => s.ChildFileSize);
            }
        }
    }
}
