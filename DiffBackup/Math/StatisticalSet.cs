
namespace DiffBackup.Math
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    /// <summary>
    /// Represents a dynamically-sized set of numbers for which we may want to calculate
    /// useful statistics such as arithmetic mean, standard deviation, etc.
    /// Calculations are vectorized and cached for best performance where possible.
    /// Operations on this class are thread safe.
    /// </summary>
    public class StatisticalSet
    {
        /// <summary>
        /// The current set of samples. This implementation only allows the set to be added to or cleared.
        /// </summary>
        private readonly List<double> _samples;

        /// <summary>
        /// The currently calculated arithmetic mean, updated every time we add a new value to the set.
        /// </summary>
        private double _currentMean = 0;

        /// <summary>
        /// The cached value for the variance of the set, lazily updated.
        /// </summary>
        private double? _cachedVariance = null;

        /// <summary>
        /// Creates a new <see cref="StatisticalSet"/> with default initial capacity.
        /// </summary>
        public StatisticalSet() : this(16)
        {
        }

        /// <summary>
        /// Creates a new <see cref="StatisticalSet"/> with a specific initial capacity (to avoid future allocations).
        /// </summary>
        public StatisticalSet(int suggestedCapacity)
        {
            if (suggestedCapacity <= 0)
            {
                throw new ArgumentOutOfRangeException("Capacity must be greater than zero", nameof(suggestedCapacity));
            }

            _samples = new List<double>(suggestedCapacity);
        }

        /// <summary>
        /// Clears all samples from the value set.
        /// </summary>
        public void Clear()
        {
            lock (_samples)
            {
                _samples.Clear();
                _cachedVariance = null;
                _currentMean = 0;
            }
        }

        /// <summary>
        /// Adds a single sample to the value set.
        /// </summary>
        /// <param name="sample">The sample to add. Must be a real number.</param>
        public void Add(double sample)
        {
            if (double.IsNaN(sample))
            {
                throw new ArgumentException("Numeric sample cannot be NaN", nameof(sample));
            }

            if (double.IsInfinity(sample))
            {
                throw new ArgumentException("Numeric sample must be a finite number", nameof(sample));
            }

            lock (_samples)
            {
                _samples.Add(sample);

                // Update the current average
                _currentMean = (sample + (_currentMean * (_samples.Count - 1))) / _samples.Count;

                // And invalidate the cached variance
                _cachedVariance = null;
            }
        }

        /// <summary>
        /// Adds a collection of samples to this sample set.
        /// If the input is an array of values, use the array method signature for better performance.
        /// </summary>
        /// <param name="samples">The <see cref="IReadOnlyCollection{double}"/> of samples to add.</param>
        public void Add(IReadOnlyCollection<double>? samples)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (samples.Count == 0)
            {
                return;
            }

            // Validate all input values in advance so we're not left in an inconsistent state
            foreach (double sample in samples)
            {
                if (double.IsNaN(sample))
                {
                    throw new ArgumentException("Numeric sample cannot be NaN", nameof(sample));
                }

                if (double.IsInfinity(sample))
                {
                    throw new ArgumentException("Numeric sample must be a finite number", nameof(sample));
                }
            }

            lock (_samples)
            {
                double sumOfAllNewSamples = 0;
                int originalSampleSize = _samples.Count;
                _samples.AddRange(samples);

                // Vectorize calculation of the mean if possible
                ArraySegment<double> samplesAsSegment = default;
                if (Vector.IsHardwareAccelerated &&
                    samples.Count >= 128 &&
                    ListHacks.TryGetUnderlyingArraySegment(samples, out samplesAsSegment))
                {
                    int index = samplesAsSegment.Offset;
                    int endIndex = samplesAsSegment.Count;
                    int vectorEndIndex = endIndex - (samplesAsSegment.Count % Vector<double>.Count);
                    double[] rawArray = samplesAsSegment.Array ?? Array.Empty<double>();
                    while (index < vectorEndIndex)
                    {
                        sumOfAllNewSamples += Vector.Dot(Vector<double>.One, new Vector<double>(rawArray, index));
                        index += Vector<double>.Count;
                    }

                    while (index < endIndex)
                    {
                        sumOfAllNewSamples += rawArray[index++];
                    }
                }
                else
                {
                    foreach (double sample in samples)
                    {
                        sumOfAllNewSamples += sample;
                    }
                }

                // Update the mean all at once
                _currentMean = (sumOfAllNewSamples + (_currentMean * originalSampleSize)) / _samples.Count;
                _cachedVariance = null;
            }
        }

        /// <summary>
        /// Adds an array segment of samples to this sample set.
        /// </summary>
        /// <param name="samples">The <see cref="IEnumerable{Double}"/> of samples to add.</param>
        public void Add(double[]? array, int offset, int count)
        {
            if (array is null)
            {
                throw new ArgumentNullException(nameof(array));
            }

            if (offset < 0 || offset >= array.Length)
            {
                throw new IndexOutOfRangeException("Offset must be within the bounds of the array");
            }

            if (count == 0)
            {
                return;
            }

            if (count < 0)
            {
                throw new ArgumentOutOfRangeException("Count must be non-negative");
            }

            if (offset + count > array.Length)
            {
                throw new IndexOutOfRangeException("Offset + count exceeds upper bound of the array");
            }

            // Validate all input values in advance so we're not left in an inconsistent state
            int index = offset;
            int endIndex = offset + count;
            int originalSampleSize = _samples.Count;
            while (index < endIndex)
            {
                double sample = array[index++];
                if (double.IsNaN(sample))
                {
                    throw new ArgumentException("Numeric sample cannot be NaN", nameof(sample));
                }

                if (double.IsInfinity(sample))
                {
                    throw new ArgumentException("Numeric sample must be a finite number", nameof(sample));
                }
            }

            // Now commit to the change
            lock (_samples)
            {
#if NET8_0_OR_GREATER
                _samples.AddRange(array.AsSpan(offset, count));
#else
                _samples.AddRange(array.Skip(offset).Take(count));
#endif

                double sumOfAllNewSamples = 0;
                index = offset;
                endIndex = offset + count;

                // Use SIMD if possible to sum all elements of the input array (can be faster than one-by-one enumeration)
                // 128 elements is approximate size threshold based on benchmarking
                if (Vector.IsHardwareAccelerated && count >= 128)
                {
                    int vectorEndIndex = endIndex - (count % Vector<double>.Count);
                    while (index < vectorEndIndex)
                    {
                        sumOfAllNewSamples += Vector.Dot(Vector<double>.One, new Vector<double>(array, index));
                        index += Vector<double>.Count;
                    }
                }

                // Residual loop
                while (index < endIndex)
                {
                    sumOfAllNewSamples += array[index++];
                }

                _currentMean = (sumOfAllNewSamples + (_currentMean * originalSampleSize)) / _samples.Count;
                _cachedVariance = null;
            }
        }

        /// <summary>
        /// Gets the list of sample values currently in this set.
        /// </summary>
        public IReadOnlyCollection<double> Samples => _samples;

        /// <summary>
        /// Gets the total number of samples in the set.
        /// </summary>
        public int SampleCount
        {
            get
            {
                lock (_samples)
                {
                    return _samples.Count;
                }
            }
        }

        /// <summary>
        /// Gets the current arithmetic mean of the set.
        /// </summary>
        public double Mean
        {
            get
            {
                lock (_samples)
                {
                    return _currentMean;
                }
            }
        }

        /// <summary>
        /// Gets the variance of the set.
        /// </summary>
        public double Variance
        {
            get
            {
                lock (_samples)
                {
                    if (_samples.Count == 0)
                    {
                        return 0;
                    }

                    if (!_cachedVariance.HasValue)
                    {
                        double sumVariance = 0;

                        // Calculate the variance now, using SIMD if possible
                        // 128 elements is approximate size threshold based on benchmarking
                        ArraySegment<double> rawArraySegment = default;
                        if (Vector.IsHardwareAccelerated &&
                            _samples.Count >= 128 &&
                            ListHacks.TryGetUnderlyingArraySegment(_samples, out rawArraySegment))
                        {
                            double[] rawArray = rawArraySegment.Array ?? Array.Empty<double>();
                            int index = rawArraySegment.Offset;
                            int endIndex = rawArraySegment.Count;
                            int vectorEndIndex = endIndex - (rawArraySegment.Count % Vector<double>.Count);
                            Vector<double> meanVec = new Vector<double>(_currentMean);
                            while (index < vectorEndIndex)
                            {
                                Vector<double> sampleVec = new Vector<double>(rawArray, index);
                                sampleVec = Vector.Subtract(sampleVec, meanVec);
                                // Use dot product as a clever trick to square and sum the entire vector as one operation
                                sumVariance += Vector.Dot(sampleVec, sampleVec);
                                index += Vector<double>.Count;
                            }

                            // Residual loop
                            while (index < endIndex)
                            {
                                double sample = rawArray[index++];
                                double delta = sample - _currentMean;
                                sumVariance += (delta * delta);
                            }
                        }
                        else
                        {
                            foreach (double sample in _samples)
                            {
                                double delta = sample - _currentMean;
                                sumVariance += (delta * delta);
                            }
                        }

                        _cachedVariance = sumVariance / _samples.Count;
                    }

                    return _cachedVariance.Value;
                }
            }
        }

        /// <summary>
        /// Gets the standard deviation of the set.
        /// </summary>
        public double StandardDeviation => Math.Sqrt(Variance);
    }
}
