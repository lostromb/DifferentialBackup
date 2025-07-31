
namespace Sandbox
{
    using BenchmarkDotNet.Attributes;
    using Durandal.Common.Compression.Crc;
    using Durandal.Common.IO.Crc;
    using Durandal.Common.Logger;
    using Durandal.Common.MathExt;
    using Durandal.Common.Utils;
    using Durandal.Common.Utils.NativePlatform;
    using Durandal.Extensions.Compression.Crc;
    using K4os.Hash.xxHash;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [MemoryDiagnoser]
    public class Benchmarks
    {
        //public static readonly FastRandom _random = new FastRandom();
        //public StatisticalSet _incrementalSet = new StatisticalSet();
        //public double[] _valueSet = Array.Empty<double>();

        //[Params(10, 500, 10000, 200000)]
        //public int SetLength { get; set; }

        [Params(1024)]
        public int ScratchSize { get; set; }

        public static byte[] TheFile = Array.Empty<byte>();

        [GlobalSetup]
        public void GlobalSetup()
        {
            NativePlatformUtils.SetGlobalResolver(new NativeLibraryResolverImpl());
            AssemblyReflector.ApplyAccelerators(typeof(CRC32CAccelerator).Assembly, DebugLogger.Default);
            TheFile = File.ReadAllBytes(@"D:\Backup Test\Complete\thedetroitstarwarsspectacular_tc.mp3");
            //_incrementalSet = new StatisticalSet(SetLength);
            //_valueSet = new double[SetLength];
            //for (int c = 0; c < SetLength; c++)
            //{
            //    _valueSet[c] = _random.NextDouble(-10000, 10000);
            //}
        }

        //[IterationSetup]
        //public void IterationSetup()
        //{
        //}

        [Benchmark]
        public void HashFile_Crc32c()
        {
            using (MemoryStream stream = new MemoryStream(TheFile, false))
            {
                ICRC32C crc = CRC32CFactory.Create();
                CRC32CState crcState = default(CRC32CState);
                byte[] scratch = ArrayPool<byte>.Shared.Rent(ScratchSize);
                try
                {
                    while (stream.Position < stream.Length)
                    {
                        int readSize = stream.Read(scratch, 0, ScratchSize);
                        if (readSize <= 0)
                        {
                            return;
                        }

                        crc.Slurp(ref crcState, scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                crcState.Checksum.GetHashCode();
            }
        }

        [Benchmark]
        public void HashFile_XxHash32()
        {
            using (MemoryStream stream = new MemoryStream(TheFile, false))
            {
                XXH32.State state = new XXH32.State();
                byte[] scratch = ArrayPool<byte>.Shared.Rent(ScratchSize);
                try
                {
                    while (stream.Position < stream.Length)
                    {
                        int readSize = stream.Read(scratch, 0, ScratchSize);
                        if (readSize <= 0)
                        {
                            return;
                        }

                        XXH32.Update(ref state, scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                XXH32.Digest(state).GetHashCode();
            }
        }

        [Benchmark]
        public void HashFile_XxHash64()
        {
            using (MemoryStream stream = new MemoryStream(TheFile, false))
            {
                XXH64.State state = new XXH64.State();
                byte[] scratch = ArrayPool<byte>.Shared.Rent(ScratchSize);
                try
                {
                    while (stream.Position < stream.Length)
                    {
                        int readSize = stream.Read(scratch, 0, ScratchSize);
                        if (readSize <= 0)
                        {
                            return;
                        }

                        XXH64.Update(ref state, scratch.AsSpan(0, readSize));
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(scratch);
                }

                XXH64.Digest(state).GetHashCode();
            }
        }

        //[Benchmark]
        //public void CreateSetAndCalcMean_Single()
        //{
        //    _incrementalSet.Clear();
        //    for (int c = 0; c < SetLength; c++)
        //    {
        //        _incrementalSet.Add(_valueSet[c]);
        //    }

        //    _incrementalSet.Mean.GetHashCode();
        //}

        //[Benchmark]
        //public void CreateSetAndCalcMean_Enumerated()
        //{
        //    _incrementalSet.Clear();
        //    IEnumerable<double> enumerable = _valueSet;
        //    _incrementalSet.Add(enumerable);
        //    _incrementalSet.Mean.GetHashCode();
        //}

        //[Benchmark]
        //public void CreateSetAndCalcMean_Array()
        //{
        //    _incrementalSet.Clear();
        //    _incrementalSet.Add(_valueSet, 0, SetLength);
        //    _incrementalSet.Mean.GetHashCode();
        //}
    }
}
