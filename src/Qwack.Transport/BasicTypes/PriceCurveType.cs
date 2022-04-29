namespace Qwack.Transport.BasicTypes
{
    public enum PriceCurveType
    {
        Linear = 0,
        LME = 0,
        Next = 1,
        NYMEX = 1,
        NextButOnExpiry = 2,
        ICE = 2,
        Flat = 3,
        Constant = 3
    }

    public enum SparsePriceCurveType
    {
        Coal
    }
}
