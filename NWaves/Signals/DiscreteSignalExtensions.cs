﻿using System;
using System.Linq;
using NWaves.Utils;

namespace NWaves.Signals
{
    /// <summary>
    /// In general, any finite DT signal is simply an array of data sampled at certain sampling rate.
    /// 
    /// This array of samples can be:
    ///     - delayed (shifted) by positive or negative number of samples
    ///     - superimposed with another array of samples (another signal)
    ///     - concatenated with another array of samples (another signal)
    ///     - repeated N times
    ///     - amplified
    ///
    /// Note.
    /// Method implementations are LINQ-less and do Buffer.BlockCopy() for better performance.
    /// </summary>
    public static class DiscreteSignalExtensions
    {
        /// <summary>
        /// Method delays the signal
        ///     either by shifting it to the right (positive, e.g. Delay(1000))
        ///         or by shifting it to the left (negative, e.g. Delay(-1000))
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="delay"></param>
        /// <returns></returns>
        public static DiscreteSignal Delay(this DiscreteSignal signal, int delay)
        {
            var length = signal.Length;

            if (delay <= 0)
            {
                delay = -delay;

                if (delay >= length)
                {
                    throw new ArgumentException("Delay can not exceed the length of the signal!");
                }

                return new DiscreteSignal(
                                signal.SamplingRate,
                                signal.Samples.FastCopyFragment(length - delay, delay));
            }
            
            return new DiscreteSignal(
                            signal.SamplingRate,
                            signal.Samples.FastCopyFragment(length, destinationOffset: delay));
        }

        /// <summary>
        /// Method superimposes two signals.
        /// If sizes are different then the smaller signal is broadcasted 
        /// to fit the size of the larger signal.
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal Superimpose(this DiscreteSignal signal1, DiscreteSignal signal2)
        {
            if (signal1.SamplingRate != signal2.SamplingRate)
            {
                throw new ArgumentException("Sampling rates must be the same!");
            }

            DiscreteSignal superimposed;

            if (signal1.Length >= signal2.Length)
            {
                superimposed = signal1.Copy();

                for (var i = 0; i < signal2.Length; i++)
                {
                    superimposed[i] += signal2.Samples[i];
                }
            }
            else
            {
                superimposed = signal2.Copy();

                for (var i = 0; i < signal1.Length; i++)
                {
                    superimposed[i] += signal1.Samples[i];
                }
            }

            return superimposed;
        }

        /// <summary>
        /// Method superimposes two signals.
        /// If sizes are different then the smaller signal is broadcasted 
        /// to fit the size of the larger signal.
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal SuperimposeMany(this DiscreteSignal signal1, DiscreteSignal signal2, int[] positions)
        {
            if (signal1.SamplingRate != signal2.SamplingRate)
            {
                throw new ArgumentException("Sampling rates must be the same!");
            }

            var totalLength = Math.Max(signal1.Length, signal2.Length + positions.Max());

            DiscreteSignal superimposed = new DiscreteSignal(signal1.SamplingRate, totalLength);
            signal1.Samples.FastCopyTo(superimposed.Samples, signal1.Length);

            for (var p = 0; p < positions.Length; p++)
            {
                var offset = positions[p];

                for (var i = 0; i < signal2.Length; i++)
                {
                    superimposed[offset + i] += signal2.Samples[i];
                }
            }

            return superimposed;
        }

        /// <summary>
        /// Method subtracts one signal from another.
        /// If sizes are different then the smaller signal is broadcasted 
        /// to fit the size of the larger signal.
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal Subtract(this DiscreteSignal signal1, DiscreteSignal signal2)
        {
            if (signal1.SamplingRate != signal2.SamplingRate)
            {
                throw new ArgumentException("Sampling rates must be the same!");
            }

            DiscreteSignal subtracted;

            if (signal1.Length >= signal2.Length)
            {
                subtracted = signal1.Copy();

                for (var i = 0; i < signal2.Length; i++)
                {
                    subtracted[i] -= signal2.Samples[i];
                }
            }
            else
            {
                subtracted = new DiscreteSignal(signal2.SamplingRate, signal2.Length);

                for (var i = 0; i < signal1.Length; i++)
                {
                    subtracted[i] = signal1.Samples[i] - signal2.Samples[i];
                }
                for (var i = signal1.Length; i < signal2.Length; i++)
                {
                    subtracted[i] = -signal2.Samples[i];
                }
            }

            return subtracted;
        }

        /// <summary>
        /// Method concatenates two signals.
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal Concatenate(this DiscreteSignal signal1, DiscreteSignal signal2)
        {
            if (signal1.SamplingRate != signal2.SamplingRate)
            {
                throw new ArgumentException("Sampling rates must be the same!");
            }

            return new DiscreteSignal(
                            signal1.SamplingRate,
                            signal1.Samples.MergeWithArray(signal2.Samples));
        }

        /// <summary>
        /// Method returns repeated n times copy of the signal
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="times"></param>
        /// <returns></returns>
        public static DiscreteSignal Repeat(this DiscreteSignal signal, int times)
        {
            if (times <= 0)
            {
                throw new ArgumentException("Number of repeat times must be at least once");
            }

            return new DiscreteSignal(
                            signal.SamplingRate,
                            signal.Samples.RepeatArray(times));
        }

        /// <summary>
        /// In-place signal amplification by coeff
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="coeff"></param>
        public static void Amplify(this DiscreteSignal signal, float coeff)
        {
            for (var i = 0; i < signal.Length; i++)
            {
                signal[i] *= coeff;
            }
        }

        /// <summary>
        /// In-place signal attenuation by coeff
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="coeff"></param>
        public static void Attenuate(this DiscreteSignal signal, float coeff)
        {
            if (Math.Abs(coeff) < 1e-10)
            {
                throw new ArgumentException("Attenuation coefficient can't be zero");
            }

            signal.Amplify(1 / coeff);
        }

        /// <summary>
        /// Return copy of first N samples
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="sampleCount">Number of samples</param>
        /// <returns>Copy of the first samples of signal</returns>
        public static DiscreteSignal First(this DiscreteSignal signal, int sampleCount)
        {
            if (sampleCount <= 0 || sampleCount > signal.Length)
            {
                throw new ArgumentException("Number of samples must be positive and must not exceed the signal length!");
            }
            
            return new DiscreteSignal(
                            signal.SamplingRate,
                            signal.Samples.FastCopyFragment(sampleCount));
        }

        /// <summary>
        /// More or less efficient LINQ-less version.
        /// Skip() would require unnecessary enumeration.
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="sampleCount"></param>
        /// <returns></returns>
        public static DiscreteSignal Last(this DiscreteSignal signal, int sampleCount)
        {
            if (sampleCount <= 0 || sampleCount > signal.Length)
            {
                throw new ArgumentException("Number of samples must be positive and must not exceed the signal length!");
            }

            return new DiscreteSignal(
                            signal.SamplingRate,
                            signal.Samples.FastCopyFragment(sampleCount, signal.Length - sampleCount));
        }

        /// <summary>
        /// Full rectification (in-place)
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <returns>Fully rectified signal</returns>
        public static void FullRectify(this DiscreteSignal signal)
        {
            var s = signal.Samples;

            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] < 0)
                {
                    s[i] = -s[i];
                }
            }
        }

        /// <summary>
        /// Half rectification (in-place)
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <returns>Half rectified signal</returns>
        public static void HalfRectify(this DiscreteSignal signal)
        {
            var s = signal.Samples;

            for (var i = 0; i < s.Length; i++)
            {
                if (s[i] < 0)
                {
                    s[i] = 0;
                }
            }
        }

        /// <summary>
        /// Method copies discrete signal samples into complex signal
        /// </summary>
        /// <param name="signal">Real-valued signal</param>
        /// <returns>Corresponding complex-valued signal</returns>
        public static ComplexDiscreteSignal ToComplex(this DiscreteSignal signal)
        {
            return new ComplexDiscreteSignal(signal.SamplingRate, signal.Samples.ToDoubles());
        }
    }
}
