using Durandal.Common.File;
using Durandal.Common.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffBackup.File
{
    /// <summary>
    /// A wrapper for a preexisting <see cref="IFileSystem"/> which forbids any write calls
    /// to the underlying system, including creating files or directories or opening
    /// files with write access.
    /// </summary>
    public class ReadOnlyFileSystem : IFileSystem
    {
        private readonly IFileSystem _innerFileSystem;

        public ReadOnlyFileSystem(IFileSystem innerFileSystem)
        {
            _innerFileSystem = innerFileSystem.AssertNonNull(nameof(innerFileSystem));
        }

        public void CreateDirectory(VirtualPath directory)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public Task CreateDirectoryAsync(VirtualPath directory)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public Task<IFileSystemWatcher> CreateDirectoryWatcher(VirtualPath watchPath, string filter, bool recurseSubdirectories)
        {
            return _innerFileSystem.CreateDirectoryWatcher(watchPath, filter, recurseSubdirectories);
        }

        public void Delete(VirtualPath path)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public Task DeleteAsync(VirtualPath path)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public bool Exists(VirtualPath path)
        {
            return _innerFileSystem.Exists(path);
        }

        public Task<bool> ExistsAsync(VirtualPath path)
        {
            return _innerFileSystem.ExistsAsync(path);
        }

        public IEnumerable<VirtualPath> ListDirectories(VirtualPath directoryName)
        {
            return _innerFileSystem.ListDirectories(directoryName);
        }

        public Task<IEnumerable<VirtualPath>> ListDirectoriesAsync(VirtualPath directoryName)
        {
            return _innerFileSystem.ListDirectoriesAsync(directoryName);
        }

        public IEnumerable<VirtualPath> ListFiles(VirtualPath directoryName)
        {
            return _innerFileSystem.ListFiles(directoryName);
        }

        public Task<IEnumerable<VirtualPath>> ListFilesAsync(VirtualPath directoryName)
        {
            return _innerFileSystem.ListFilesAsync(directoryName);
        }

        public void Move(VirtualPath source, VirtualPath target)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public Task MoveAsync(VirtualPath source, VirtualPath target)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public Stream OpenStream(VirtualPath file, FileOpenMode openMode, FileAccessMode accessMode, int? bufferSizeHint = null)
        {
            if (openMode == FileOpenMode.Create ||
                openMode == FileOpenMode.CreateNew ||
                (accessMode & FileAccessMode.Write) != 0)
            {
                throw new InvalidOperationException("Read-only file system");
            }

            return _innerFileSystem.OpenStream(file, openMode, accessMode, bufferSizeHint);
        }

        public Task<Stream> OpenStreamAsync(VirtualPath file, FileOpenMode openMode, FileAccessMode accessMode, int? bufferSizeHint = null)
        {
            if (openMode == FileOpenMode.Create ||
                openMode == FileOpenMode.CreateNew ||
                (accessMode & FileAccessMode.Write) != 0)
            {
                throw new InvalidOperationException("Read-only file system");
            }

            return _innerFileSystem.OpenStreamAsync(file, openMode, accessMode, bufferSizeHint);
        }

        public IEnumerable<string> ReadLines(VirtualPath sourceFile)
        {
            return _innerFileSystem.ReadLines(sourceFile);
        }

        public Task<IEnumerable<string>> ReadLinesAsync(VirtualPath sourceFile)
        {
            return _innerFileSystem.ReadLinesAsync(sourceFile);
        }

        public FileStat Stat(VirtualPath fileName)
        {
            return _innerFileSystem.Stat(fileName);
        }

        public Task<FileStat> StatAsync(VirtualPath fileName)
        {
            return _innerFileSystem.StatAsync(fileName);
        }

        public ResourceType WhatIs(VirtualPath resourceName)
        {
            return _innerFileSystem.WhatIs(resourceName);
        }

        public Task<ResourceType> WhatIsAsync(VirtualPath resourceName)
        {
            return _innerFileSystem.WhatIsAsync(resourceName);
        }

        public void WriteLines(VirtualPath targetFile, IEnumerable<string> data)
        {
            throw new InvalidOperationException("Read-only file system");
        }

        public Task WriteLinesAsync(VirtualPath targetFile, IEnumerable<string> data)
        {
            throw new InvalidOperationException("Read-only file system");
        }
    }
}
