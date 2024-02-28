namespace DiffBackup.Schemas
{
    using DiffBackup.Math;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class FileTypeStatistics
    {
        // used to calculate something like a p-value of how certain we are
        // that a particular file type is compressible below the specified threshold
        private const double COMPRESSION_RATIO_THRESHOLD = 0.85;
        private const double STD_DEVIATIONS_OF_CERTAINTY = 1.0;

        public static FileTypeCompressibility GetCompressionSuitability(this StatisticalSet stats)
        {
            if (stats.SampleCount < 50)
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
