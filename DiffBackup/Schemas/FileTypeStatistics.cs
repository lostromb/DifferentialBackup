﻿namespace DiffBackup.Schemas
{
    using DiffBackup.Math;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class FileTypeStatistics
    {
        public int SampleCount;
        public StatisticalSet CompressionRatioStats = new StatisticalSet();
    }
}
