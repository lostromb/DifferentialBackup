
namespace DiffBackup.Math
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Numerics;
    using System.Reflection;

    /// <summary>
    /// Represents a dynamically-sized set of numbers for which we may want to calculate
    /// useful statistics such as arithmetic mean, standard deviation, etc.
    /// Calculations are vectorized and cached for best performance where possible.
    /// Operations on this class are thread safe.
    /// </summary>
    public class StatisticalSet
    {
        private static readonly bool _canUseListVectorizationHack = false;
        private static readonly FieldInfo? _listInnerArrayFieldAccessor = null;

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

        static StatisticalSet()
        {
            try
            {
                // Probe to see if we can pry into the underlying double[] array beneath the List<double>,
                // and if so, cache the reflection accessor that makes that possible
                _listInnerArrayFieldAccessor = typeof(List<double>).GetRuntimeFields()
                    .Where((s) => string.Equals("_items", s.Name, StringComparison.Ordinal)).FirstOrDefault();
                _canUseListVectorizationHack = 
                    Vector.IsHardwareAccelerated &&
                    _listInnerArrayFieldAccessor != null &&
                    _listInnerArrayFieldAccessor.FieldType == typeof(double[]);
            }
            catch (Exception) { }
        }

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
        /// Adds an enumerable set of samples to this sample set.
        /// If the input is an array of values, use the array method signature for better performance.
        /// </summary>
        /// <param name="samples">The <see cref="IEnumerable{double}"/> of samples to add.</param>
        public void Add(IEnumerable<double> samples)
        {
            lock (_samples)
            {
                try
                {
                    double sumOfAllNewSamples = 0;
                    int originalSampleSize = _samples.Count;
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

                        _samples.Add(sample);
                        sumOfAllNewSamples += sample;
                        _cachedVariance = null;
                    }

                    // Update the mean all at once
                    _currentMean = (sumOfAllNewSamples + (_currentMean * originalSampleSize)) / _samples.Count;
                }
                catch (Exception)
                {
                    // Assume there was a NaN in the input data set.
                    // Recalculate the mean manually so we're not in an invalid state
                    if (_samples.Count == 0)
                    {
                        _currentMean = 0;
                    }
                    else
                    {
                        double sum = 0;
                        foreach (double sample in _samples)
                        {
                            sum += sample;
                        }

                        _currentMean = sum / (double)_samples.Count;
                    }

                    throw;
                }
            }
        }

        /// <summary>
        /// Adds an array segment of samples to this sample set.
        /// </summary>
        /// <param name="samples">The <see cref="IEnumerable{Double}"/> of samples to add.</param>
        public void Add(double[] array, int offset, int count)
        {
            if (array is null)
            {
                throw new ArgumentNullException("Sample array is null");
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

            lock (_samples)
            {
                try
                {
                    double sumOfAllNewSamples = 0;
                    int originalSampleSize = _samples.Count;
                    int index = offset;
                    int endIndex = offset + count;
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

#if NET8_0_OR_GREATER
                    _samples.AddRange(array.AsSpan(offset, count));
#else
                    _samples.AddRange(array.Skip(offset).Take(count));
#endif

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
                }
                catch (Exception)
                {
                    // Assume there was a NaN in the input data set.
                    // Recalculate the mean manually so we're not in an invalid state
                    if (_samples.Count == 0)
                    {
                        _currentMean = 0;
                    }
                    else
                    {
                        double sum = 0;
                        foreach (double sample in _samples)
                        {
                            sum += sample;
                        }

                        _currentMean = sum / (double)_samples.Count;
                    }

                    throw;
                }
                finally
                {
                    _cachedVariance = null;
                }
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
                        if (_samples.Count >= 128 && _canUseListVectorizationHack && _listInnerArrayFieldAccessor != null)
                        {
                            // Hack to access the underlying array behind the List<double>
                            object? rawRef = _listInnerArrayFieldAccessor.GetValue(_samples);
                            if (rawRef == null || !(rawRef is double[]))
                            {
                                throw new NullReferenceException("List<double> had null or incorrect inner array; this should never happen");
                            }

                            double[] rawArray = (double[])rawRef;
                            int index = 0;
                            int endIndex = _samples.Count;
                            int vectorEndIndex = endIndex - (endIndex % Vector<double>.Count);
                            Vector<double> meanVec = new Vector<double>(_currentMean);
                            while (index < vectorEndIndex)
                            {
                                Vector<double> sampleVec = new Vector<double>(rawArray, index);
                                sampleVec = Vector.Subtract<double>(sampleVec, meanVec);
                                // Use dot product as a clever trick to square and sum the entire vector as one operation
                                sumVariance += Vector.Dot(sampleVec, sampleVec);

                                //sampleVec = Vector.Multiply(sampleVec, sampleVec);
                                //sumVariance += Vector.Dot(Vector<double>.One, sampleVec);

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
