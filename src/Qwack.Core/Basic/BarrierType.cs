namespace Qwack.Core.Basic
{
    public enum BarrierType
    {
        KO = 0,
        KnockOut = 0,
        Out = 0,
        KI = 1,
        KnockIn = 1,
        In = 1
    }

    public enum BarrierObservationType
    {
        Continuous,
        Daily,
        European
    }

    public enum BarrierSide
    {
        Up,
        Down
    }
}
