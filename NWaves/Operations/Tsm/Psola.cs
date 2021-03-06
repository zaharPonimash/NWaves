﻿using System;
using NWaves.Filters.Base;
using NWaves.Signals;

namespace NWaves.Operations.Tsm
{
    /// <summary>
    /// Pitch-Synchronized Overlap-Add
    /// </summary>
    public class Psola : IFilter
    {
        /// <summary>
        /// Hop size at analysis stage (STFT decomposition)
        /// </summary>
        private readonly int _hopAnalysis;

        /// <summary>
        /// Hop size at synthesis stage (STFT merging)
        /// </summary>
        private readonly int _hopSynthesis;

        /// <summary>
        /// Size of FFT for analysis
        /// </summary>
        private readonly int _fftAnalysis;

        /// <summary>
        /// Size of FFT for synthesis
        /// </summary>
        private readonly int _fftSynthesis;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="hopAnalysis"></param>
        /// <param name="hopSynthesis"></param>
        /// <param name="fftAnalysis"></param>
        /// <param name="fftSynthesis"></param>
        public Psola(int hopAnalysis, int hopSynthesis, int fftAnalysis = 0, int fftSynthesis = 0)
        {
            _hopAnalysis = hopAnalysis;
            _hopSynthesis = hopSynthesis;
            _fftAnalysis = (fftAnalysis > 0) ? fftAnalysis : 4 * hopAnalysis;
            _fftSynthesis = (fftSynthesis > 0) ? fftSynthesis : 4 * hopSynthesis;
        }

        /// <summary>
        /// PSOLA algorithm
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public DiscreteSignal ApplyTo(DiscreteSignal signal,
                                      FilteringMethod method = FilteringMethod.Auto)
        {
            throw new NotImplementedException();
        }
    }
}
