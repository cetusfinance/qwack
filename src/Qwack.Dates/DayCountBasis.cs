namespace Qwack.Dates
{
    public enum DayCountBasis
    {
        Act360 = 360,
        Act_360 = 360,
        Act365 = 365,
        Act_365 = 365,
        Act_Act = 365,

        Act_Act_ISDA,
        Act_Act_ICMA,

        Act_365F,
        Act_364,
        _30_360 = 30360,
        Thirty360 = 30360,

        _30E_360 = 303600,
        ThirtyE360 = 303600,

        Unity
    }
}
