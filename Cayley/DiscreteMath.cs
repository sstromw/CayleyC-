using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cayley
{
    /// <summary>
    /// Just a class with a handful of helper functions
    /// </summary>
    class DiscreteMath
    {
        /// <summary>
        /// Classic power modulus function
        /// </summary>
        public static int ModPow(int a, int exp, int mod)
        {
            if (exp == 0) return 1;

            int t = 1;
            while (exp > 1)
            {
                if (exp % 2 == 0)
                {
                    a = (a * a) % mod;
                    exp /= 2;
                }
                else
                {
                    t = (a * t) % mod;
                    a = (a * a) % mod;
                    exp = (exp - 1) / 2;
                }
            }

            return (a * t) % mod;
        }

        /// <summary>
        /// Return the Euler totient of a number
        /// </summary>
        public static int Phi(int n)
        {
            int[] facs = PrimeFactors(n);
            int p = 1;
            for (int i = 0, j = 0; i < facs.Length; i++)
            {
                if (facs[i] != j)
                {
                    j = facs[i];
                    p *= facs[i] - 1;
                }
                else
                {
                    p *= facs[i];
                }
            }
            return p;
        }

        /// <summary>
        /// Return the prime factors of the input
        /// </summary>
        public static int[] PrimeFactors(int n)
        {
            List<int> factors = new List<int>();
            while (n % 2 == 0)
            {
                n /= 2;
                factors.Add(2);
            }

            int k = 3;
            while (n > 1)
            {
                while (n % k == 0)
                {
                    n /= k;
                    factors.Add(k);
                }
                k += 2;
            }

            return factors.ToArray();
        }

        /// <summary>
        /// This is (technically) the inefficient way to do it. Don't use for huge input, please
        /// </summary>
        public static int[] Divisors(int n)
        {
            List<int> divs = new List<int>();
            for (int i = 1; i <= n; i++)
            {
                if (n % i == 0)
                {
                    divs.Add(i);
                }
            }
            return divs.ToArray();
        }

        /// <summary>
        /// Compute the greatest common divisor of two numbers
        /// </summary>
        public static int GCD(int m, int n)
        {
            return n == 0 ? m : GCD(n, m % n);
        }
    }
}
