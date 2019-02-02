﻿using System;
using NWaves.Filters.Base;
using NWaves.Operations;
using NWaves.Signals;

namespace NWaves.Effects
{
    /// <summary>
    /// Class for tremolo effect
    /// </summary>
    public class TremoloEffect : IFilter
    {
        /// <summary>
        /// Modulation frequency
        /// </summary>
        public float Frequency { get; }

        /// <summary>
        /// Tremolo index (modulation index)
        /// </summary>
        public float TremoloIndex { get; }
        
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="frequency"></param>
        /// <param name="tremoloIndex"></param>
        public TremoloEffect(float frequency = 10/*Hz*/, float tremoloIndex = 0.5f)
        {
            Frequency = frequency;
            TremoloIndex = tremoloIndex;
        }

        /// <summary>
        /// Method implements simple tremolo effect
        /// </summary>
        /// <param name="signal"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public DiscreteSignal ApplyTo(DiscreteSignal signal,
                                      FilteringMethod method = FilteringMethod.Auto)
        {
            return new Modulator().Amplitude(signal, Frequency, TremoloIndex);
        }
    }
}
