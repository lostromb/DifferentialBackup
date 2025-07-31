using Durandal.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffBackup.Schemas
{
    internal class TreeFile
    {
        public TreeFile(string fileName)
        {
            FileName = fileName.AssertNonNullOrEmpty(nameof(fileName));
        }

        /// <summary>
        /// Contains this file's name within its directory.
        /// </summary>
        public string FileName { get; private set; }

        /// <summary>
        /// Contains the file's last modification time, if known
        /// </summary>
        public ulong? ModTime { get; set; }

        /// <summary>
        /// Contains the file's size in bytes, if known
        /// </summary>
        public ulong? FileSize { get; set; }

        /// <summary>
        /// Contains the file's CRC32C checksum, if known
        /// </summary>
        public uint? FileCRC32 { get; set; }
    }
}
