using System;

namespace Cayley
{
    public class MathUtils
    {
        public static int IntPow(int a, int b)
        {
            int y = 1;
            while (b != 0)
            {
                if (b % 2 == 1) y *= a;
                a *= a;
                b >>= 1;
            }
            return y;
        }

        public static int Multiplicity(int n, int p)
        {
            int i = 0;
            while (n % p == 0)
            {
                i++;
                n /= p;
            }
            return i;
        }
    }
}
