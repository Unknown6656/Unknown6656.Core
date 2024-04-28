using System.Collections.Immutable;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Mathematics;
using Unknown6656.Generics;
using Unknown6656.Imaging;

namespace Unknown6656.Physics.Optics;


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

    public static Wavelength LowestVisibleWavelength { get; } = 380;

    public static Wavelength HighestVisibleWavelength { get; } = 720;

    [Obsolete("Use '" + nameof(HighestVisibleWavelength) + "' instead.")]
    public static Wavelength LowestVisibleFrequency => HighestVisibleWavelength;

    [Obsolete("Use '" + nameof(LowestVisibleWavelength) + "' instead.")]
    public static Wavelength HighestVisibleFrequency => LowestVisibleWavelength;


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
    public readonly bool IsVisible => InNanometers >= LowestVisibleWavelength && InNanometers <= HighestVisibleWavelength;


    static Wavelength()
    {
        List<Wavelength> wavelengths = [];

        for (Wavelength w = LowestVisibleWavelength; w < HighestVisibleWavelength; w = w.InNanometers + 1)
            wavelengths.Add(w);

        VisibleWavelengths = [.. wavelengths];
    }

    public Wavelength(double wavelength_in_nm) => InNanometers = wavelength_in_nm;

    /// <summary>
    /// Computes the emittance of a black body at the given temperature (in Kelvin) for the current wavelength.
    /// </summary>
    /// <param name="temperature">Black body temperature (in Kelvin).</param>
    /// <returns>Black body emittance (in Watt per square meters).</returns>
    public readonly double GetBlackBodyEmittance(double temperature) => 3.74183e-16 * Math.Pow(InMeters, -5.0) / Math.Exp(1.4388e-2 / (InMeters * temperature) - 1.0);

    public readonly HDRColor ToColor() => HDRColor.FromWavelength(in this);

    public readonly HDRColor ToColor(double α) => HDRColor.FromWavelength(in this, α);

    public readonly RGBAColor ToRGBAColor() => ToColor();

    public readonly RGBAColor ToRGBAColor(double α) => ToColor(α);

    public override string ToString() => $"{InNanometers} nm / {Frequency} Hz ({(IsVisible ? "" : "in")}visible)";

    public override bool Equals(object? obj) => obj is Wavelength other && Equals(other);

    public bool Equals(Wavelength other) => InNanometers.Is(other.InNanometers);

    public override int GetHashCode() => InNanometers.GetHashCode();

    public int CompareTo(Wavelength other) => InNanometers.CompareTo(other.InNanometers);

    /// <summary>
    /// Converts the given frequency (in Hz) to the corresponding wavelength.
    /// </summary>
    /// <param name="frequency">Frequency (in Hz).</param>
    /// <returns>Wavelength</returns>
    public static Wavelength FromFrequency(double frequency) => new((C / 1e9) / frequency);

    public static Wavelength FromAngstrom(double ångström) => new(ångström * .1);


    public static Wavelength operator +(Wavelength wavelength) => wavelength;

    public static Wavelength operator -(Wavelength wavelength) => -wavelength.InNanometers;

    public static Wavelength operator +(Wavelength first, Wavelength second) => first.InNanometers + second.InNanometers;

    public static Wavelength operator -(Wavelength first, Wavelength second) => first.InNanometers - second.InNanometers;

    public static Wavelength operator *(Wavelength wavelength, double factor) => wavelength.InNanometers * factor;

    public static Wavelength operator /(Wavelength wavelength, double factor) => wavelength.InNanometers / factor;

    public static bool operator <(Wavelength left, Wavelength right) => left.CompareTo(right) < 0;

    public static bool operator <=(Wavelength left, Wavelength right) => left.CompareTo(right) <= 0;

    public static bool operator >(Wavelength left, Wavelength right) => left.CompareTo(right) > 0;

    public static bool operator >=(Wavelength left, Wavelength right) => left.CompareTo(right) >= 0;

    public static bool operator ==(Wavelength first, Wavelength second) => first.Equals(second);

    public static bool operator !=(Wavelength first, Wavelength second) => !(first == second);

    public static implicit operator Wavelength(double nm) => new(nm);

    public static implicit operator HDRColor(Wavelength wavelength) => wavelength.ToColor();

    public static implicit operator RGBAColor(Wavelength wavelength) => wavelength.ToRGBAColor();
}

public abstract partial class Spectrum
{
    public static ContinuousSpectrum EmptySpectrum { get; } = new(_ => 0d);

    public static ContinuousSpectrum ConstantOneSpectrum { get; } = new(_ => 1d);

    public static ContinuousSpectrum ConstantOneVisibleSpectrum { get; } = new(λ => λ.IsVisible ? 1d : 0d);


    public abstract double GetIntensity(Wavelength wavelength);

    public virtual Spectrum InvertSpectrum() => NegateSpectrum().ApplyIntensityOffset(1);

    public virtual Spectrum NegateSpectrum() => new ContinuousSpectrum(λ => -GetIntensity(λ));

    public virtual Spectrum ApplyIntensityOffset(double intensity_offset) => new ContinuousSpectrum(λ => GetIntensity(λ) + intensity_offset);

    public virtual Spectrum ScaleSpectrumIntensities(double factor) => new ContinuousSpectrum(λ => GetIntensity(λ) * factor);

    public virtual ContinuousSpectrum ToContinuous() => new(GetIntensity);

    //public virtual ContinuousColorMap ToColorMap(Wavelength lowest, Wavelength highest)
    //{
    //    if (lowest > highest)
    //        (lowest, highest) = (highest, lowest);
    //
    //    double dist = (highest - lowest).InNanometers;
    //
    //    return new(s => GetIntensity(s * dist + lowest.InNanometers));
    //
    //}
    //
    //public ContinuousColorMap ToVisibleColorMap() => ToColorMap(Wavelength.LowestVisibleWavelength, Wavelength.HighestVisibleWavelength);

    public HDRColor ToVisibleColor(Wavelength lowest, Wavelength highest, double wavelength_resolution_in_nm) =>
        ToVisibleColor(lowest, highest, wavelength_resolution_in_nm, 1);

    public virtual HDRColor ToVisibleColor(Wavelength lowest, Wavelength highest, double wavelength_resolution_in_nm, double α)
    {
        if (lowest > highest)
            (lowest, highest) = (highest, lowest);

        wavelength_resolution_in_nm = Math.Max(wavelength_resolution_in_nm, Scalar.ComputationalEpsilon);

        HDRColor color = new();

        for (Wavelength nm = lowest; nm <= highest; nm += wavelength_resolution_in_nm)
            if (nm.IsVisible)
                color += GetIntensity(nm) * nm.ToColor();

        return color;
    }


    public static implicit operator Func<Wavelength, double>(Spectrum spectrum) => spectrum.GetIntensity;

    public static Spectrum operator +(Spectrum spectrum) => spectrum.ToContinuous();

    public static Spectrum operator -(Spectrum spectrum) => spectrum.NegateSpectrum();

    public static Spectrum operator +(Spectrum spectrum, double intensity_offset) => spectrum.ApplyIntensityOffset(intensity_offset);

    public static Spectrum operator +(double intensity_offset, Spectrum spectrum) => spectrum.ApplyIntensityOffset(intensity_offset);

    public static Spectrum operator -(Spectrum spectrum, double intensity_offset) => spectrum.ApplyIntensityOffset(-intensity_offset);

    public static Spectrum operator -(double intensity_offset, Spectrum spectrum) => spectrum.NegateSpectrum().ApplyIntensityOffset(intensity_offset);

    public static Spectrum operator *(Spectrum spectrum, double factor) => spectrum.ScaleSpectrumIntensities(factor);

    public static Spectrum operator *(double factor, Spectrum spectrum) => spectrum.ScaleSpectrumIntensities(factor);

    public static Spectrum operator /(Spectrum spectrum, double factor) => spectrum.ScaleSpectrumIntensities(1d / factor);
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

    public DiscreteSpectrum Normalize()
    {
        double max = Intensities.Values.Max();

        return new(Intensities.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / max));
    }

    public DiscreteSpectrum NormalizeVisible()
    {
        List<(Wavelength, double)> intensities = new(Intensities.Count);
        double max = 0;

        foreach (KeyValuePair<Wavelength, double> kvp in Intensities)
        {
            intensities.Add((kvp.Key, kvp.Value));

            if (kvp.Key.IsVisible)
                max = Math.Max(max, kvp.Value);
        }

        if (max is 0)
            max = 1;
        
        return new(intensities.ToDictionary(t => t.Item1, t => t.Item2 / max));
    }

    public DiscreteSpectrum ToVisibleSpectrum() => new(Intensities.Where(kvp => kvp.Key.IsVisible).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));

    public override DiscreteSpectrum NegateSpectrum() => new(Intensities.ToDictionary(kvp => kvp.Key, kvp => -GetIntensity(kvp.Value)));

    public override DiscreteSpectrum ApplyIntensityOffset(double intensity_offset) => new(Intensities.ToDictionary(kvp => kvp.Key, kvp => GetIntensity(kvp.Value) + intensity_offset));

    public override DiscreteSpectrum ScaleSpectrumIntensities(double factor) => new(Intensities.ToDictionary(kvp => kvp.Key, kvp => GetIntensity(kvp.Value) * factor));

    public override DiscreteSpectrum InvertSpectrum() => new(Intensities.ToDictionary(kvp => kvp.Key, kvp => 1 - GetIntensity(kvp.Value)));

    public override double GetIntensity(Wavelength wavelength) => Intensities.TryGetValue(wavelength, out double intensity) ? intensity : 0;

    public HDRColor ToVisibleColor() => ToVisibleColor(1);

    public HDRColor ToVisibleColor(double α) => ToVisibleColor(Wavelength.LowestVisibleWavelength, Wavelength.HighestVisibleWavelength, 0, α);

    public override HDRColor ToVisibleColor(Wavelength lowest, Wavelength highest, double _ignored_, double α)
    {
        if (lowest > highest)
            (lowest, highest) = (highest, lowest);

        HDRColor color = new();

        foreach (KeyValuePair<Wavelength, double> kvp in Intensities)
            if (kvp.Key.IsVisible && kvp.Key >= lowest && kvp.Key <= highest)
                color += kvp.Value * kvp.Key.ToColor();

        return color;
    }

    public ColorPalette ToColorPalette() => new(Intensities.Keys.Select(λ => (RGBAColor)λ.ToColor()));

    public DiscreteColorMap ToColorMap()
    {
        if (Intensities.Keys.OrderBy(λ => λ.InNanometers).ToArray() is { Length: > 0 } wavelengths)
        {
            Scalar min = wavelengths[^1].InNanometers;
            Scalar max = wavelengths[0].InNanometers;

            return new(wavelengths.Select(λ => ((λ.InNanometers - min) / (max - min), (RGBAColor)λ.ToColor())));
        }
        else
            throw new InvalidOperationException("The spectrum must not be empty.");
    }

    public override string ToString() => $"{Intensities.Count} Wavelengths: [{string.Join(", ", Intensities.Select(kvp => $"{kvp.Key.InNanometers}nm:{kvp.Value}"))}]";

    public IEnumerator<(Wavelength Wavelength, double Intensity)> GetEnumerator() => Intensities.Select(kvp => (kvp.Key, kvp.Value)).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public static implicit operator ColorPalette(DiscreteSpectrum spectrum) => spectrum.ToColorPalette();

    public static implicit operator DiscreteColorMap(DiscreteSpectrum spectrum) => spectrum.ToColorMap();

    public static implicit operator ContinuousSpectrum(DiscreteSpectrum spectrum) => spectrum.ToContinuous();

    public static implicit operator HDRColor(DiscreteSpectrum spectrum) => spectrum.ToVisibleColor();
}

public class ContinuousSpectrum(Func<Wavelength, double> intensity_function)
    : Spectrum
{
    public Func<Wavelength, double> IntensityFunction { get; } = intensity_function;

    public override double GetIntensity(Wavelength wavelength) => IntensityFunction(wavelength);


    public static implicit operator ContinuousSpectrum(Func<Wavelength, double> intensity_function) => new(intensity_function);
}
