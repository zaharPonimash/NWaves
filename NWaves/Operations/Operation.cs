﻿using System;
using System.Linq;
using NWaves.Filters.Base;
using NWaves.Filters.BiQuad;
using NWaves.Operations.Convolution;
using NWaves.Operations.Tsm;
using NWaves.Signals;
using NWaves.Utils;

namespace NWaves.Operations
{
    /// <summary>
    /// Main operations implemented:
    /// 
    ///     - convolution
    ///     - cross-correlation
    ///     - block convolution
    ///     - deconvolution
    ///     - resampling
    ///     - time-stretching
    ///     - rectification
    ///     - envelope extraction
    /// 
    /// </summary>
    public static partial class Operation
    {
        /// <summary>
        /// Fast convolution via FFT of real-valued signals.
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static DiscreteSignal Convolve(DiscreteSignal signal, DiscreteSignal kernel)
        {
            return new Convolver().Convolve(signal, kernel);
        }

        /// <summary>
        /// Fast convolution via FFT for general complex-valued case
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static ComplexDiscreteSignal Convolve(ComplexDiscreteSignal signal, ComplexDiscreteSignal kernel)
        {
            return new ComplexConvolver().Convolve(signal, kernel);
        }

        /// <summary>
        /// Fast convolution for double arrays (used mainly in filter design)
        /// </summary>
        /// <param name="input"></param>
        /// <param name="kernel"></param>
        /// <returns></returns>
        public static double[] Convolve(double[] input, double[] kernel)
        {
            return Convolve(new ComplexDiscreteSignal(1, input), 
                            new ComplexDiscreteSignal(1, kernel)).Real;
        }

        /// <summary>
        /// Fast cross-correlation via FFT
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal CrossCorrelate(DiscreteSignal signal1, DiscreteSignal signal2)
        {
            return new Convolver().CrossCorrelate(signal1, signal2);
        }

        /// <summary>
        /// Fast complex cross-correlation via FFT
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static ComplexDiscreteSignal CrossCorrelate(ComplexDiscreteSignal signal1, ComplexDiscreteSignal signal2)
        {
            return new ComplexConvolver().CrossCorrelate(signal1, signal2);
        }

        /// <summary>
        /// Method implements block convolution of signals (using either OLA or OLS algorithm)
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="kernel"></param>
        /// <param name="fftSize"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static DiscreteSignal BlockConvolve(DiscreteSignal signal,
                                                   DiscreteSignal kernel,
                                                   int fftSize,
                                                   FilteringMethod method = FilteringMethod.OverlapSave)
        {
            if (kernel.Length > fftSize)
            {
                throw new ArgumentException("Kernel length must not exceed the size of FFT!");
            }

            IFilter blockConvolver;

            if (method == FilteringMethod.OverlapAdd)
            {
                blockConvolver = new OlaBlockConvolver(kernel.Samples, fftSize);
            }
            else
            {
                blockConvolver = new OlsBlockConvolver(kernel.Samples, fftSize);
            }

            return blockConvolver.ApplyTo(signal);
        }
        
        /// <summary>
        /// Deconvolution via FFT for general complex-valued case.
        ///  
        /// NOTE!
        /// 
        /// Deconvolution is an experimental feature.
        /// It's problematic due to division by zero.
        /// 
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="kernel">Kernel</param>
        /// <returns>Deconvolved signal</returns>
        public static ComplexDiscreteSignal Deconvolve(ComplexDiscreteSignal signal, ComplexDiscreteSignal kernel)
        {
            return new ComplexConvolver().Deconvolve(signal, kernel);
        }

        /// <summary>
        /// Interpolation followed by low-pass filtering
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="factor"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static DiscreteSignal Interpolate(DiscreteSignal signal, int factor, FirFilter filter = null)
        {
            return new Resampler().Interpolate(signal, factor, filter);
        }

        /// <summary>
        /// Decimation preceded by low-pass filtering
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="factor"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static DiscreteSignal Decimate(DiscreteSignal signal, int factor, FirFilter filter = null)
        {
            return new Resampler().Decimate(signal, factor, filter);
        }

        /// <summary>
        /// Simple resampling (as the combination of interpolation and decimation).
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="newSamplingRate"></param>
        /// <param name="filter"></param>
        /// <returns></returns>
        public static DiscreteSignal Resample(DiscreteSignal signal, int newSamplingRate, FirFilter filter = null)
        {
            return new Resampler().Resample(signal, newSamplingRate, filter);
        }

        /// <summary>
        /// Resampling based on simple interpolation
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="up"></param>
        /// <param name="down"></param>
        /// <returns></returns>
        public static DiscreteSignal ResampleUpDown(DiscreteSignal signal, int up, int down)
        {
            return new Resampler().ResampleUpDown(signal, up, down);
        }

        /// <summary>
        /// Time stretching with parameters set by user
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="stretch">Stretch factor (ratio)</param>
        /// <param name="windowSize">Window size (for vocoders - FFT size)</param>
        /// <param name="hopSize">Hop size</param>
        /// <param name="algorithm">Algorithm for TSM (optional)</param>
        /// <returns>Time stretched signal</returns>
        public static DiscreteSignal TimeStretch(DiscreteSignal signal,
                                                 double stretch,
                                                 int windowSize,
                                                 int hopSize,
                                                 TsmAlgorithm algorithm = TsmAlgorithm.Wsola)
        {
            if (Math.Abs(stretch - 1.0) < 1e-10)
            {
                return signal.Copy();
            }

            IFilter stretchFilter;

            switch (algorithm)
            {
                case TsmAlgorithm.PhaseVocoder:
                    stretchFilter = new PhaseVocoder(stretch, hopSize, windowSize, false);
                    break;
                case TsmAlgorithm.PhaseVocoderPhaseLocking:
                    stretchFilter = new PhaseVocoder(stretch, hopSize, windowSize);
                    break;
                default:
                    stretchFilter = new Wsola(stretch, windowSize, hopSize);
                    break;
            }

            return stretchFilter.ApplyTo(signal, FilteringMethod.Auto);
        }

        /// <summary>
        /// Time stretching with auto-derived parameters
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="stretch">Stretch factor (ratio)</param>
        /// <param name="algorithm">Algorithm for TSM (optional)</param>
        /// <returns>Time stretched signal</returns>
        public static DiscreteSignal TimeStretch(DiscreteSignal signal,
                                                 double stretch,
                                                 TsmAlgorithm algorithm = TsmAlgorithm.Wsola)
        {
            if (Math.Abs(stretch - 1.0) < 1e-10)
            {
                return signal.Copy();
            }

            IFilter stretchFilter;
            int frameSize;

            switch (algorithm)
            {
                case TsmAlgorithm.PhaseVocoder:
                    frameSize = MathUtils.NextPowerOfTwo(1024 * signal.SamplingRate / 16000);
                    stretchFilter = new PhaseVocoder(stretch, frameSize / 10, frameSize, false);
                    break;
                case TsmAlgorithm.PhaseVocoderPhaseLocking:
                    frameSize = MathUtils.NextPowerOfTwo(1024 * signal.SamplingRate / 16000);
                    stretchFilter = new PhaseVocoder(stretch, frameSize / 4, frameSize);
                    break;
                default:
                    stretchFilter = new Wsola(stretch);
                    break;
            }

            return stretchFilter.ApplyTo(signal, FilteringMethod.Auto);
        }

        /// <summary>
        /// Method for extracting the envelope of a signal
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <param name="lowpassCutoff">LP filter cutoff frequency</param>
        /// <returns></returns>
        public static DiscreteSignal Envelope(DiscreteSignal signal, float lowpassCutoff = 0.05f)
        {
            var envelope = FullRectify(signal);

            var lowpassFilter = new LowPassFilter(lowpassCutoff);
            var smoothed = lowpassFilter.ApplyTo(envelope);

            return smoothed;
        }

        /// <summary>
        /// Full rectification
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <returns>Fully rectified signal</returns>
        public static DiscreteSignal FullRectify(DiscreteSignal signal)
        {
            return new DiscreteSignal(signal.SamplingRate,
                                      signal.Samples.Select(s => s < 0 ? -s : s));
        }

        /// <summary>
        /// Half rectification
        /// </summary>
        /// <param name="signal">Signal</param>
        /// <returns>Half rectified signal</returns>
        public static DiscreteSignal HalfRectify(DiscreteSignal signal)
        {
            return new DiscreteSignal(signal.SamplingRate,
                                      signal.Samples.Select(s => s < 0 ? 0 : s));
        }


        /****************************************************************************
         * 
         *    The following methods are included mainly for educational purposes
         * 
         ***************************************************************************/

        /// <summary>
        /// Direct convolution by formula in time domain
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal ConvolveDirect(DiscreteSignal signal1, DiscreteSignal signal2)
        {
            var a = signal1.Samples;
            var b = signal2.Samples;
            var length = a.Length + b.Length - 1;

            var conv = new float[length];

            for (var n = 0; n < length; n++)
            {
                for (var k = 0; k < b.Length; k++)
                {
                    if (n >= k && n - k < a.Length)
                    {
                        conv[n] += a[n - k] * b[k];
                    }
                }
            }

            return new DiscreteSignal(signal1.SamplingRate, conv);
        }

        /// <summary>
        /// Direct cross-correlation by formula in time domain
        /// </summary>
        /// <param name="signal1"></param>
        /// <param name="signal2"></param>
        /// <returns></returns>
        public static DiscreteSignal CrossCorrelateDirect(DiscreteSignal signal1, DiscreteSignal signal2)
        {
            var a = signal1.Samples;
            var b = signal2.Samples;
            var length = a.Length + b.Length - 1;

            var corr = new float[length];

            for (var n = 0; n < length; n++)
            {
                var pos = b.Length - 1;
                for (var k = 0; k < b.Length; k++)
                {
                    if (n >= k && n - k < a.Length)
                    {
                        corr[n] += a[n - k] * b[pos];
                    }
                    pos--;
                }
            }

            return new DiscreteSignal(signal1.SamplingRate, corr);
        }
    }
}
