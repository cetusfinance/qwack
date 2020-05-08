namespace Qwack.Transport.BasicTypes
{
    public enum OptionType
    {
        Call = 0,
        C = 0,
        Cap = 0,
        Put = 1,
        P = 1,
        Floor = 1,
        Straddle = 2,
        Swap = 3
    }

    public enum OptionExerciseType
    {
        European,
        American,
        Bermudan,
        Asian
    }

    public enum OptionMarginingType
    {
        Regular,
        FuturesStyle
    }
}
