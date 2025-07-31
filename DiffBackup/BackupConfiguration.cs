
namespace DiffBackup
{
    using Durandal.Common.Config;
    using Durandal.Common.Utils;

    public class BackupConfiguration
    {
        private IConfiguration _internalConfig;

        public BackupConfiguration(IConfiguration internalConfig)
        {
            _internalConfig = internalConfig.AssertNonNull(nameof(internalConfig));
            ValidateConfig();
        }

        private void ValidateConfig()
        {
            FileIOThreads.AssertPositive(nameof(FileIOThreads));
            AsyncFileReadSizeThresholdBytes.AssertPositive(nameof(AsyncFileReadSizeThresholdBytes));
        }

        /// <summary>
        /// The number of threads to use when doing file operations during backup
        /// </summary>
        public int FileIOThreads => _internalConfig.GetInt32("FileIOThreads", 8);

        /// <summary>
        /// Files smaller than this will use async reads and potentially different buffer sizes to tune performance
        /// </summary>
        public int AsyncFileReadSizeThresholdBytes => _internalConfig.GetInt32("AsyncFileReadSizeThresholdBytes", 1024 * 1024);
    }
}
