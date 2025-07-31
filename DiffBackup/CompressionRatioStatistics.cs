
namespace DiffBackup
{
    using DiffBackup.Schemas;
    using Durandal.Common.Logger;
    using Durandal.Common.MathExt;
    using Durandal.Common.Utils;
    using System;
    using System.Collections.Generic;

    internal class CompressionRatioStatistics
    {
        // used to calculate something like a p-value of how certain we are
        // that a particular file type is compressible below the specified threshold
        private const double COMPRESSION_RATIO_THRESHOLD = 0.85;
        private const double STD_DEVIATIONS_OF_CERTAINTY = 1.0;
        private const int MIN_SAMPLE_COUNT_REQUIRED = 50;

        private readonly Dictionary<string, FileTypeCompressibility> _fixedFileTypeStats;
        private readonly Dictionary<string, StatisticalSet?> _dynamicFileTypeStats;
        private readonly ILogger _logger;

        public CompressionRatioStatistics(ILogger logger)
        {
            _fixedFileTypeStats = new Dictionary<string, FileTypeCompressibility>(StringComparer.OrdinalIgnoreCase);
            _dynamicFileTypeStats = new Dictionary<string, StatisticalSet?>(StringComparer.OrdinalIgnoreCase);
            _logger = logger.AssertNonNull(nameof(logger));
        }

        public FileTypeCompressibility GetCompressibility(string extension)
        {
            FileTypeCompressibility returnVal;
            if (_fixedFileTypeStats.TryGetValue(extension, out returnVal))
            {
                return returnVal;
            }

            StatisticalSet? stats;
            if (_dynamicFileTypeStats.TryGetValue(extension, out stats))
            {
                stats.AssertNonNull(nameof(stats));
                return GetCompressionSuitability(stats);
            }

            return FileTypeCompressibility.Unknown;
        }

        public void AddDynamicEntry(string extension, double ratio)
        {
            StatisticalSet? statsForThisFileType;
            if (!_dynamicFileTypeStats.TryGetValue(extension, out statsForThisFileType))
            {
                statsForThisFileType = new StatisticalSet();
                _dynamicFileTypeStats.Add(extension, statsForThisFileType);
            }

            statsForThisFileType.AssertNonNull(nameof(statsForThisFileType));
            statsForThisFileType.Add(ratio);
            _logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata,
                "{0:F4} {1:F4} \"{2}\"",
                statsForThisFileType.Mean, statsForThisFileType.StandardDeviation, extension);
        }

        public void AddFixedEntry(string extension, FileTypeCompressibility compressibility)
        {
            // TODO validate extension
            _fixedFileTypeStats[extension] = compressibility;
        }

        public void AddCommonFileFormats()
        {
            // Common text formats
            AddFixedEntry(".txt", FileTypeCompressibility.Suitable);
            AddFixedEntry(".ini", FileTypeCompressibility.Suitable);
            AddFixedEntry(".xml", FileTypeCompressibility.Suitable);
            AddFixedEntry(".json", FileTypeCompressibility.Suitable);
            AddFixedEntry(".css", FileTypeCompressibility.Suitable);
            AddFixedEntry(".html", FileTypeCompressibility.Suitable);
            AddFixedEntry(".htm", FileTypeCompressibility.Suitable);
            AddFixedEntry(".js", FileTypeCompressibility.Suitable);
            AddFixedEntry(".yml", FileTypeCompressibility.Suitable);
            AddFixedEntry(".log", FileTypeCompressibility.Suitable);
            AddFixedEntry(".tsv", FileTypeCompressibility.Suitable);
            AddFixedEntry(".csv", FileTypeCompressibility.Suitable);

            // Common image formats
            AddFixedEntry(".jpg", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".jpeg", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".jpe", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".gif", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".png", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".webp", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".heic", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".dng", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".jfif", FileTypeCompressibility.Unsuitable);

            // Common archive formats
            AddFixedEntry(".zip", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".rar", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".7z", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".gz", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".bzip", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".bz2", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".mobi", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".epub", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".azw3", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".cbz", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".cbr", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".jar", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".zst", FileTypeCompressibility.Unsuitable);

            // Common media formats
            AddFixedEntry(".mpg", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".mpeg", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".mp3", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".mp4", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".m4a", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".mkv", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".ogg", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".opus", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".webm", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".flac", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".aac", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".avi", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".mov", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".wav", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".wmv", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".wma", FileTypeCompressibility.Unsuitable);
            AddFixedEntry(".avif", FileTypeCompressibility.Unsuitable);
        }

        internal void PrintInternalStats()
        {
            _logger.Log("Final statistics:");
            foreach (var fileType in _fixedFileTypeStats)
            {
                fileType.Value.AssertNonNull(nameof(fileType));
                _logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F4} {1:F4} {2} \"{3}\" {4}",
                    1.0,
                    0.0,
                    MIN_SAMPLE_COUNT_REQUIRED,
                    fileType.Key,
                    fileType.Value);
            }

            foreach (var fileType in _dynamicFileTypeStats)
            {
                fileType.Value.AssertNonNull(nameof(fileType));
                _logger.LogFormat(LogLevel.Std, DataPrivacyClassification.SystemMetadata, "{0:F4} {1:F4} {2} \"{3}\" {4}",
                    fileType.Value.Mean,
                    fileType.Value.StandardDeviation,
                    fileType.Value.SampleCount,
                    fileType.Key,
                    GetCompressionSuitability(fileType.Value));
            }
        }

        private static FileTypeCompressibility GetCompressionSuitability(StatisticalSet stats)
        {
            if (stats.SampleCount < MIN_SAMPLE_COUNT_REQUIRED)
            {
                return FileTypeCompressibility.Unknown;
            }
            else
            {
                if (stats.Mean +
                    (stats.StandardDeviation * STD_DEVIATIONS_OF_CERTAINTY) < COMPRESSION_RATIO_THRESHOLD)
                {
                    return FileTypeCompressibility.Suitable;
                }
                else if (stats.Mean -
                    (stats.StandardDeviation * STD_DEVIATIONS_OF_CERTAINTY) > COMPRESSION_RATIO_THRESHOLD)
                {
                    return FileTypeCompressibility.Unsuitable;
                }
                else
                {
                    return FileTypeCompressibility.Unknown;
                }
            }
        }
    }
}
