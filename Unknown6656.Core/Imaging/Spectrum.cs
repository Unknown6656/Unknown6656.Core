using System.Collections.Immutable;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics;
using Unknown6656.Common;

namespace Unknown6656.Imaging
{
    /// <summary>
    /// Represents a single (visible) wavelength.
    /// </summary>
    public readonly struct Wavelength
        : IEquatable<Wavelength>
        , IComparable<Wavelength>
    {
        /// <summary>
        /// The speed of light (in vacuum, m/s).
        /// </summary>
        public const double C = 299_792_458;
        /// <summary>
        /// The Planck constant (in Js).
        /// </summary>
        public const double h = 6.62607015e-34;


        public static ImmutableList<Wavelength> VisibleWavelengths { get; }

        public static Wavelength LowestVisibleFrequency { get; } = 720;

        public static Wavelength HighestVisibleFrequency { get; } = 380;


        static Wavelength()
        {
            List<Wavelength> wavelengths = new();

            for (Wavelength w = Wavelength.HighestVisibleFrequency; w < Wavelength.LowestVisibleFrequency; w = w.InNanometers + 1)
                wavelengths.Add(w);

            VisibleWavelengths = wavelengths.ToImmutableList();
        }


        /// <summary>
        /// Returns the wavelength in nanometers.
        /// </summary>
        public readonly double InNanometers { get; }

        /// <summary>
        /// Returns the wavelength in meters.
        /// </summary>
        public readonly double InMeters => InNanometers * 1e-9;

        /// <summary>
        /// Returns the wave's frequency (in Hz).
        /// </summary>
        public readonly double Frequency => C / InMeters;

        /// <summary>
        /// Returns the approximate energy of a photon with the current wavelength (in eV).
        /// </summary>
        public readonly double PhotonEnergy => 1239.8 / InNanometers;

        /// <summary>
        /// Indicates whether the light with the current wavelength is visible to the human eye.
        /// </summary>
        public readonly bool IsVisible => InNanometers >= HighestVisibleFrequency && InNanometers <= LowestVisibleFrequency;


        public Wavelength(double wavelength_in_nm) => InNanometers = wavelength_in_nm;

        /// <summary>
        /// Computes the emittance of a black body at the given temperature (in Kelvin) for the current wavelength.
        /// </summary>
        /// <param name="temperature">Black body temperature (in Kelvin).</param>
        /// <returns>Black body emittance (in Watt per square meters).</returns>
        public readonly double GetBlackBodyEmittance(double temperature) => 3.74183e-16 * Math.Pow(InMeters, -5.0) / Math.Exp(1.4388e-2 / (InMeters * temperature) - 1.0);

        public readonly RGBAColor ToRGBAColor() => RGBAColor.FromWavelength(in this);

        public readonly RGBAColor ToRGBAColor(double α) => RGBAColor.FromWavelength(in this, α);

        /// <inheritdoc/>
        public override string ToString() => $"{InNanometers} nm / {Frequency} Hz ({(IsVisible ? "" : "in")}visible)";

        /// <inheritdoc/>
        public override bool Equals(object? obj) => obj is Wavelength other && Equals(other);

        /// <inheritdoc/>
        public bool Equals(Wavelength other) => InNanometers.Is(other.InNanometers);

        /// <inheritdoc/>
        public override int GetHashCode() => InNanometers.GetHashCode();

        /// <inheritdoc/>
        public int CompareTo(Wavelength other) => InNanometers.CompareTo(other.InNanometers);

        /// <summary>
        /// Converts the given frequency (in Hz) to the corresponding wavelength.
        /// </summary>
        /// <param name="frequency">Frequency (in Hz).</param>
        /// <returns>Wavelength</returns>
        public static Wavelength FromFrequency(double frequency) => new Wavelength((C / 1e9) / frequency);

        public static bool operator <(Wavelength left, Wavelength right) => left.CompareTo(right) < 0;

        public static bool operator <=(Wavelength left, Wavelength right) => left.CompareTo(right) <= 0;

        public static bool operator >(Wavelength left, Wavelength right) => left.CompareTo(right) > 0;

        public static bool operator >=(Wavelength left, Wavelength right) => left.CompareTo(right) >= 0;

        public static implicit operator Wavelength(double nm) => new Wavelength(nm);
    }

    public abstract class Spectrum
    {
        public abstract double GetIntensity(Wavelength wavelength);

        public ContinousSpectrum InvertSpectrum() => new ContinousSpectrum(λ => 1 - GetIntensity(λ).Clamp());
    }

    public class DiscreteSpectrum
        : Spectrum
        , IEnumerable<(Wavelength Wavelength, double Intensity)>
    {
        public IReadOnlyDictionary<Wavelength, double> Intensities { get; }

        public bool HasInvisibleWavelengths => Intensities.All(kvp => kvp.Key.IsVisible || kvp.Value.IsZero());


        public DiscreteSpectrum(IEnumerable<(Wavelength Wavelength, double Intensity)> intensities)
            : this(intensities.ToDictionary())
        {
        }

        public DiscreteSpectrum(params (Wavelength Wavelength, double Intensity)[] intensities)
            : this(intensities as IEnumerable<(Wavelength, double)>)
        {
        }

        public DiscreteSpectrum(IDictionary<Wavelength, double> intensities) =>
            Intensities = intensities.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.Clamp());

        public override double GetIntensity(Wavelength wavelength) => Intensities.TryGetValue(wavelength, out double intensity) ? intensity : 0;

        public override string ToString() => $"{Intensities.Count} Wavelengths: [{string.Join(", ", Intensities.Select(kvp => $"{kvp.Key.InNanometers}nm:{kvp.Value}"))}]";

        public IEnumerator<(Wavelength Wavelength, double Intensity)> GetEnumerator() => Intensities.Select(kvp => (kvp.Key, kvp.Value)).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class ContinousSpectrum
        : Spectrum
    {
        public Func<Wavelength, double> IntensityFunction { get; }


        public ContinousSpectrum(Func<Wavelength, double> intensity_function) => IntensityFunction = intensity_function;

        public override double GetIntensity(Wavelength wavelength) => IntensityFunction(wavelength);
    }
}
