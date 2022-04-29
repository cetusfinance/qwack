using System.Collections.Generic;

namespace Qwack.Futures
{
    public class BasicFutureCodeParser
    {
        private readonly int _yearBeforeWhich2DigitDatesAreUsed;
        private readonly IDictionary<string, FutureSettings> _dateSettings;

        public BasicFutureCodeParser(int yearBeforeWhich2DigitDatesAreUsed, IDictionary<string, FutureSettings> dateSettings)
        {
            _dateSettings = dateSettings;
            _yearBeforeWhich2DigitDatesAreUsed = yearBeforeWhich2DigitDatesAreUsed;
        }


    }
}
