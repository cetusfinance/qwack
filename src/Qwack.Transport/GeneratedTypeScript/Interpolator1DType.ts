

export enum Interpolator1DType { 
    cubicSpline = 0,
    monotoneCubicSpline = 1,
    linear = 2,
    linearFlatExtrap = 3,
    linearFlatExtrapNoBinSearch = 4,
    linearInVariance = 5,
    floaterHormannRational = 6,
    logLinear = 7,
    gaussianKernel = 8,
    dummyPoint = 9,
    nextValue = 10,
    previousValue = 11,
    constantHazzard = 12,
    linearHazzard = 13,
    other = 14
}

