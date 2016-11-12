using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Qwack.Dates
{
    public enum RollType
    {
        Following = 0,
        F = Following,
        Previous = 1,
        P = Previous,
        ModFollowing = 2,
        MF = ModFollowing,
        ModFol = ModFollowing,
        ModPrevious = 3,
        MP = ModPrevious,
        ModPrev = ModPrevious,
        NearestFolow = 4,
        NF = NearestFolow,
        NearestPrev = 5,
        NP = NearestPrev,
        LME = 6,
        LME_Nearest = LME,
        NearestModFol = LME,
        NearestMF = LME,
        NMF = LME,
        MF_LIBOR = 7,
        EndToEnd = MF_LIBOR,
        IMM = 8,
        EOM = 9,
        ShortFLongMF = 10
    }
}
