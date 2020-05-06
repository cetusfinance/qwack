namespace Qwack.Transport.BasicTypes
{
    public enum Interpolator1DType
    {
        CubicSpline,
        MonotoneCubicSpline,
        Linear,
        LinearFlatExtrap,
        LinearFlatExtrapNoBinSearch,
        LinearInVariance,
        FloaterHormannRational,
        LogLinear,
        GaussianKernel,
        DummyPoint,
        NextValue,
        PreviousValue,
        ConstantHazzard,
        Other
    }
}
