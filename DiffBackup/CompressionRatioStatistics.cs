using DiffBackup.Schemas;
using Durandal.Common.Logger;
using Durandal.Common.MathExt;
using Durandal.Common.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiffBackup
{
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
