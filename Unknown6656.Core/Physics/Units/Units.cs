using Unknown6656.Mathematics.LinearAlgebra;

namespace Unknown6656.Physics.Units;


public interface IUnit<Unit>
    where Unit : IUnit<Unit>
{
    static abstract string Name { get; }

    // TODO
}

public interface IMeasurable<Scalar, Unit>
    where Scalar : IScalar<Scalar>
    where Unit : IUnit<Unit>
{
    // TODO
}


// TODO
