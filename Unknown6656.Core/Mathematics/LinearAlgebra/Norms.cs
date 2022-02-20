﻿namespace Unknown6656.Mathematics.LinearAlgebra;


public enum VectorNorm
{
    EucledianNorm,
    TaxicabNorm,
    MaximumNorm,
    ManhattanNorm = TaxicabNorm,
    Linf_Norm = MaximumNorm,
}

public enum MatrixNorm
{
    EucledianNorm,
    FrobeniusNorm = EucledianNorm,
    L21_Norm,
    // MaxNorm,
    L1_Norm,
    Linf_Norm,
    L2_Norm,
    SpectralNorm = L2_Norm,
}
