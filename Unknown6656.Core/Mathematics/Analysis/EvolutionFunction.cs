using System.Collections.Generic;
using System.Linq;
using System;

using Unknown6656.Mathematics.LinearAlgebra;
using Unknown6656.Generics;

namespace Unknown6656.Mathematics.Analysis;


public interface IEvolutionFunction<T>
{
    public int CurrentIteration { get; }
    public T CurrentValue { get; }
    public T PreviousValue { get; }

    public void Reset();
    public void Iterate();
    public void Iterate(int count);
}

public abstract class EvolutionFunction<T>
    : IEvolutionFunction<T?>
    where T : IGroup<T>
{
    private protected readonly List<T?> _values = [];
    private T? _initial_val = default;

    public T? InitialValue
    {
        get => _initial_val;
        set => UpdateInitialValue(value);
    }
    public IEnumerable<T?> PastValues => _values;
    public int CurrentIteration => _values.Count - 1;
    public T? CurrentValue => _values[CurrentIteration];
    public T? PreviousValue => _values[CurrentIteration - 1];
    public T CurrentVelocity => CurrentValue! - PreviousValue!;
    public T PreviousVelocity => PreviousValue! - _values[CurrentIteration - 2]!;
    public T CurrentAcceleration => CurrentVelocity - PreviousVelocity;


    public virtual void Reset()
    {
        _values.Clear();
        _values.Add(InitialValue);
    }

    public void Iterate(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        while (count --> 0)
            Iterate();
    }

    public virtual void Iterate()
    {
        if (_values.Count is 0)
            _values.Add(InitialValue);

        _values.Add(ComputeNextValue());
    }

    protected abstract T? ComputeNextValue();

    public void UpdateInitialValue(T? new_initial_value)
    {
        if (_initial_val?.IsNot(new_initial_value) ?? new_initial_value?.IsNot(_initial_val) ?? true)
        {
            _initial_val = new_initial_value;

            int count = CurrentIteration + 1;

            Reset();

            while (count-- > 0)
                Iterate();
        }
    }

    public static EvolutionFunction<T> FromDelegate(Func<T?, T?> function, T? initial = default) => FromDelegate((v, _) => function(v), initial);

    public static EvolutionFunction<T> FromDelegate(Func<T?, EvolutionFunction<T>, T?> function, T? initial = default) => new _delegated(function, initial);


    private sealed class _delegated
        : EvolutionFunction<T>
    {
        private readonly Func<T?, EvolutionFunction<T>, T?> _func;


        public _delegated(Func<T?, EvolutionFunction<T>, T?> func, T? initial)
        {
            InitialValue = initial;
            _func = func;
        }

        protected override T? ComputeNextValue() => _func(CurrentValue, this);
    }
}

public class MultiPointEvolutionFunction<Function, T>
    : IEvolutionFunction<T?[]>
    where Function : EvolutionFunction<T>
    where T : IGroup<T>
{
    private readonly Func<Function> _constructor;


    public Function[] Evolutions { get; private set; }

    public int CurrentIteration => Evolutions.FirstOrDefault()?.CurrentIteration ?? 0;

    public T?[] CurrentValues => Evolutions.ToArray(f => f.CurrentValue);

    public T?[] PreviousValues => Evolutions.ToArray(f => f.PreviousValue);

    public T[] CurrentVelocities => Evolutions.ToArray(f => f.CurrentVelocity);

    public T[] PreviousVelocities => Evolutions.ToArray(f => f.PreviousVelocity);

    public T[] CurrentAccelerations => Evolutions.ToArray(f => f.CurrentAcceleration);

    T?[] IEvolutionFunction<T?[]>.CurrentValue => CurrentValues;

    T?[] IEvolutionFunction<T?[]>.PreviousValue => PreviousValues;


    public MultiPointEvolutionFunction(Func<Function> constructor, params T[] initial_values)
        : this(constructor, initial_values as IEnumerable<T>)
    {
    }

    public MultiPointEvolutionFunction(params T[] initial_values)
        : this(initial_values as IEnumerable<T>)
    {
    }

    public MultiPointEvolutionFunction(IEnumerable<T> initial_values)
        : this(Activator.CreateInstance<Function>, initial_values)
    {
    }

    public MultiPointEvolutionFunction(Func<Function> constructor, IEnumerable<T> initial_values)
    {
        _constructor = constructor;
        Evolutions = initial_values.ToArray(initial =>
        {
            Function func = constructor();

            func.InitialValue = initial;

            return func;
        });
    }

    public void UpdateInitialValues(IEnumerable<T> initial_values)
    {
        T[] arr = initial_values.ToArray();
        int old_sz = Evolutions.Length;

        if (arr.Length != Evolutions.Length)
        {
            Function[] evos = Evolutions;

            Array.Resize(ref evos, arr.Length);

            Evolutions = evos;
        }

        for (int i = 0; i < arr.Length; ++i)
            if (i < old_sz)
                Evolutions[i].UpdateInitialValue(arr[i]);
            else
            {
                Evolutions[i] = _constructor();
                Evolutions[i].InitialValue = arr[i];
                Evolutions[i].Iterate(CurrentIteration);
            }
    }

    public void Iterate(int count) => Evolutions.Do(f => f.Iterate(count));

    public void Iterate() => Iterate(1);

    public void Reset() => Evolutions.Do(f => f.Reset());
}

public class EvolutionFunction
    : EvolutionFunction<Scalar>
{
    private readonly Func<Scalar, EvolutionFunction, Scalar> _iterator;


    public EvolutionFunction(Func<Scalar, Scalar> iterator)
        : this((v, _) => iterator(v))
    {
    }

    public EvolutionFunction(Func<Scalar, EvolutionFunction, Scalar> iterator) => _iterator = iterator;

    public EvolutionFunction(Function<Scalar, Scalar> iterator)
        : this(iterator.Evaluate)
    {
    }

    protected override Scalar ComputeNextValue() => _iterator(CurrentValue, this);

    // TODO
}

public class EvolutionFunction2D
    : EvolutionFunction<Vector2>
{
    private readonly Func<Vector2, EvolutionFunction2D, Vector2> _iterator;


    public EvolutionFunction2D(Func<Vector2, Vector2> iterator)
        : this((v, _) => iterator(v))
    {
    }

    public EvolutionFunction2D(Func<Vector2, EvolutionFunction2D, Vector2> iterator) => _iterator = iterator;

    public EvolutionFunction2D(Function<Vector2, Vector2> iterator)
        : this(iterator.Evaluate)
    {
    }

    protected override Vector2 ComputeNextValue() => _iterator(CurrentValue, this);


    public static EvolutionFunction2D GingerBreadMap() => new(v => new(1 - v.Y + v.X.Abs(), v.X));

    public static EvolutionFunction2D ArnoldsCatMap() => new(v => new((2 * v.X + v.Y).DecimalPlaces, (v.X + v.Y).DecimalPlaces));

    public static EvolutionFunction2D ExponentialMap(Vector2 c) => new(v => v.ToComplex().Exp().Add(c).ToVector());

    public static EvolutionFunction2D MandelbrotMap(Vector2 c, int exponent = 2) => new(v => v.ToComplex().Power(exponent).Add(c).ToVector());

    public static EvolutionFunction2D GaussMap(Scalar a, Scalar b) => new(v => v.ToComplex().Power(2).Multiply(-a).Exp().Add(b));

    public static EvolutionFunction2D HenonMap(Scalar a, Scalar b) => new(v => new(1 - a * v.X * v.X + v.Y, b * v.X));

    public static EvolutionFunction2D DuffingMap(Scalar a, Scalar b) => new(v => new(v.X + v.Y, v.Y * (1 + a) - b * v.X - v.X.Power(3)));

    public static EvolutionFunction2D BogdanovMap(Scalar ε, Scalar k, Scalar µ) => new(v =>
    {
        Scalar y = (1 + ε) * v.Y + k * v.X * (v.X - 1) + µ * v.X * v.Y;

        return new(v.X + y, y);
    });

    public static EvolutionFunction2D StandardMap(Scalar k) => new(v =>
    {
        Scalar θ = v.Angle;
        Scalar r = v.Length;

        r = (r + k * θ.Sin()).Modulus(Scalar.τ);
        θ = (θ + r).Modulus(Scalar.τ);

        return Vector2.FromPolar(θ, r);
    });

    public static EvolutionFunction2D JuliaMap() => new(v => v.ToComplex().Power(3).Add(Complex.One).ToVector());

    public static LorenzAttractorMap LorenzAttractorMap(Scalar ρ, Scalar σ, Scalar β) => new(ρ, σ, β);



    // TODO
}

public class LorenzAttractorMap
    : EvolutionFunction2D
{
    private Vector3 _last;

    public Func<Vector3, Vector2> TransferFunction { get; }
    public Scalar ρ { get; }
    public Scalar σ { get; }
    public Scalar β { get; }


    public LorenzAttractorMap(Scalar ρ, Scalar σ, Scalar β)
        : this(ρ, σ, β, v => v.XY)
    {
    }

    internal LorenzAttractorMap(Scalar ρ, Scalar σ, Scalar β, Func<Vector3, Vector2> transfer)
        : base(LINQ.id)
    {
        TransferFunction = transfer;
        this.ρ = ρ;
        this.σ = σ;
        this.β = β;
    }

    protected override Vector2 ComputeNextValue()
    {
        Scalar dx = σ * (_last.Y - _last.X);
        Scalar dy = _last.X * (ρ - _last.Z) - _last.Y;
        Scalar dz = _last.X * _last.Y - β * _last.Z;

        _last += (dx, dy, dz);

        return TransferFunction(_last);
    }

    public override void Reset()
    {
        base.Reset();

        _last = new(CurrentValue);
    }
}
