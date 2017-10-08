using System;
using System.Collections.Generic;
using System.Text;

namespace Qwack.Math
{
    public static class BitShifting
    {
        public static int FindFirstSet(uint value)
        {
            uint count;

            if (value == 0)
            {
                count = 0;
            }
            else
            {
                count = 2;

                if ((value & 0xffff) == 0)
                {
                    value >>= 16;
                    count += 16;
                }

                if ((value & 0xff) == 0)
                {
                    value >>= 8;
                    count += 8;
                }

                if ((value & 0xf) == 0)
                {
                    value >>= 4;
                    count += 4;
                }

                if ((value & 0x3) == 0)
                {
                    value >>= 2;
                    count += 2;
                }

                count -= value & 0x1;
            }

            return (int)count;
        }

    }
}
