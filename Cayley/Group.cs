using System;
using System.Collections.Generic;
using System.Linq;

namespace Cayley
{
    // This has the static methods for identifying a group.
    public class Group
    {
        private Graph graph;
        private Tuple<int[], int[]> tree;
        private int groupID;
        private string groupName;

        private int[] factors;
        private int[] divisors;
        private int[] orders;
        private Stack<int>[] elements;
        private bool isAbelian;

        public Group(Graph G) : this(G, G.BFS(0)) { }

        public Group(Graph G, Tuple<int[], int[]> T)
        {
            graph = G;
            tree = T;

            elements = new Stack<int>[graph.Order];

            getProperties();
            SetGroupID();

            // DEBUG
            //int[] z = GetCenter();
            //int[] d = GetDerivedSubgroup();
            // DEBUG
        }

        public int Order
        {
            get { return graph.Order; }
        }

        public int GroupID
        {
            get { return groupID; }
        }

        public string GroupName
        {
            get { return groupName; }
        }

        private void getProperties()
        {
            int i, j, k;

            factors = PrimeFactors(graph.Order);
            divisors = Divisors(graph.Order);

            // Elements
            for (i = 0; i < graph.Order; i++)
            {
                elements[i] = new Stack<int>();
                j = i;
                while (tree.Item1[j] != -1)
                {
                    k = tree.Item1[j];
                    j = graph.InEdges[j, k];
                    elements[i].Push(k);
                }
            }

            // Orders
            orders = new int[graph.Order];
            orders[0] = 1;
            for (i = 0; i < graph.Order; i++)
            {
                j = op(i, 0);
                k = 1;
                while (j != 0)
                {
                    j = op(i, j);
                    k++;
                }
                orders[i] = k;
            }

            // isAbelian
            isAbelian = true;
            for (i = 0; i < graph.Degree; i++)
            {
                j = graph.OutEdges[0, i];
                if (!IsCentral(j))
                {
                    isAbelian = false;
                }
            }
        }

        private int inv(int a)
        {
            int i = a, j;
            while (true)
            {
                j = op(a, i);
                if (j == 0) return i;
                i = j;
            }
        }

        private int op(int a, int b)
        {
            Stack<int> path = new Stack<int>(elements[a].Reverse());
            while (path.Count != 0)
            {
                b = graph.OutEdges[b, path.Pop()];
            }
            return b;
        }

        // It's fast exponentiation because multiplications are nontrivial
        private int pow(int a, int n)
        {
            if (n < 0) { a = inv(a); n = -n; }
            else if (n == 0) return 0;
            int t = 0;
            while (n > 1)
            {
                if (n % 2 == 0) { a = op(a, a); n /= 2; }
                else { t = op(a, t); a = op(a, a); n = (n - 1) / 2; }
            }
            return op(a, t);
        }

        private int CountPowers(int n)
        {
            HashSet<int> powers = new HashSet<int>();
            for (int i = 0; i < graph.Order; i++)
            {
                powers.Add(pow(i, n));
            }
            return powers.Count();
        }

        private bool IsCentral(int a)
        {
            int x;
            for (int i = 0; i < graph.Degree; i++)
            {
                x = graph.OutEdges[0, i];
                if (op(a, x) != op(x, a))
                {
                    return false;
                }
            }
            return true;
        }

        private int[] GetCenter()
        {
            if (isAbelian)
            {
                // I will probably change how this gets handled, but we really don't want to compute a subgroup unless we have to.
                throw new Exception("Group is abelian");
            }

            List<int> center = new List<int>();
            for(int i = 0; i < graph.Order; i++)
            {
                if (IsCentral(i))
                {
                    center.Add(i);
                }
            }
            return center.ToArray();
        }

        private int[] GetDerivedSubgroup()
        {
            HashSet<int> elms = new HashSet<int>();
            elms.Add(0);
            for (int i = 1; i < graph.Order; i++)
            {
                for (int j = 1; j < i; j++)
                {
                    int ij = op(i, j);
                    int ji = op(j, i);
                    elms.Add(op(ij, inv(ji)));
                    elms.Add(op(ji, inv(ij)));
                }
            }
            return elms.ToArray();
        }

        // Potentially change this to return a pair (order, id) rather than a group. Could end up computing a (potentially infinite) subgroup series
        private Group GetSubgroup(int[] elements)
        {
            // TODO this is tricky
            /* 
             * Strat is this:
             * 1. Put the identity in a set T
             * 2. Put the first element not in T and all of its powers in T.
             * 3. Maintain an inEdges and outEdges
            */
            return null;
        }

        private void SetGroupID()
        {
            int[] factors = PrimeFactors(graph.Order);

            // The order sequence divides the groups into small classes.
            int[] seq = new int[divisors.Length];
            for (int i = 0; i < divisors.Length; i++) seq[i] = orders.Count(x => x == divisors[i]);

            #region Decision tree from your nightmares. Abandon all hope ye who enter here.

            // The decision tree (mostly) looks at order sequence followed by power statistics.
            // But the first four cases look for general classes of groups.

            if (seq[divisors.Length - 1] > 0) { groupID = 0; groupName = "Z" + graph.Order; return; }
            else if (factors.Length == 2 && factors[0] == factors[1]) { groupID = 1; groupName = "Z" + factors[0] + "^2"; return; }
            else if (factors.Length == 2 && factors[0] == 2) { groupID = 1; groupName = (graph.Order == 6 ? "S" : "D") + factors[1]; return; }
            else if (factors.Length == 2 && factors[1] % factors[0] == 1) { groupID = 1; groupName = "<x,y | x^" + factors[1] + " = y^" + factors[0] + " = 1, yxy^-1 = x^" + (factors[0] * factors[0]) % factors[1] + ">"; return; }
            else if (graph.Order == 8)
            {
                if (seq[1] == 7) { groupID = 1; groupName = "Z2^3"; return; }
                else if (seq[1] == 5) { groupID = 2; groupName = "D4"; return; }
                else if (seq[1] == 3) { groupID = 3; groupName = "Z4 x Z2"; return; }
                else { groupID = 4; groupName = "Q8"; return; }
            }
            else if (graph.Order == 12)
            {
                if (isAbelian) { groupID = 1; groupName = "Z6 x Z2"; return; }
                else if (seq[1] == 7) { groupID = 2; groupName = "D6"; return; }
                else
                {
                    if (seq[2] == 8) { groupID = 3; groupName = "A4"; return; }
                    else { groupID = 4; groupName = "Dic3"; return; }
                }
            }
            else if (graph.Order == 16)
            {
                if (seq[1] == 15) { groupID = 1; groupName = "Z2^4"; return; }
                else if (seq[1] == 11) { groupID = 2; groupName = "D4 x Z2"; return; }
                else if (seq[1] == 9) { groupID = 3; groupName = "D8"; return; }
                else if (seq[1] == 7)
                {
                    if (isAbelian) { groupID = 4; groupName = "Z4 x Z2^2"; return; }
                    else if (CountPowers(2) == 2) { groupID = 5; groupName = "<x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; return; } // 13
                    else { groupID = 6; groupName = "<x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zxz^-1 = xy>"; return; } // 3
                }
                else if (seq[1] == 5) { groupID = 7; groupName = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^3>"; return; }
                else if (seq[1] == 3)
                {
                    if (seq[2] == 4)
                    {
                        if (isAbelian) { groupID = 8; groupName = "Z8 x Z2"; return; }
                        else { groupID = 9; groupName = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^5>"; return; }
                    }
                    else
                    {
                        if (isAbelian) { groupID = 10; groupName = "Z4^2"; return; }
                        if (CountPowers(2) == 3) { groupID = 11; groupName = "<x,y | x^4 = y^4 = 1, yxy^-1 = x^3>"; return; }
                        else { groupID = 12; groupName = "Q8 x Z2"; return; }
                    }
                }
                else { groupID = 13; groupName = "Q16"; return; }
            }
            else if (graph.Order == 18)
            {
                if (isAbelian) { groupID = 1; groupName = "Z3^2 x Z2"; return; }
                else if (seq[1] == 3) { groupID = 2; groupName = "S3 x Z3"; return; }
                else if (seq[2] == 2) { groupID = 3; groupName = "D9"; return; }
                else { groupID = 4; groupName = "D(Z3^2)"; return; }
            }
            else if (graph.Order == 20)
            {
                if (isAbelian) { groupID = 1; groupName = "Z5 x Z2^2"; return; }
                else if (seq[1] == 1) { groupID = 2; groupName = "Dic5"; return; }
                else if (seq[1] == 5) { groupID = 3; groupName = "<a,b | a^5 = b^4 = 1, bab^-1 = a^2>"; return; } // 3
                else { groupID = 4; groupName = "D10"; return; }
            }
            else if (graph.Order == 24)
            {
                if (seq[1] == 15) { groupID = 1; groupName = "D6 x Z2"; return; }
                else if (seq[1] == 13) { groupID = 2; groupName = "D12"; return; }
                else if (seq[1] == 9)
                {
                    if (seq[2] == 8) { groupID = 3; groupName = "S4"; return; }
                    else { groupID = 4; groupName = "<x,y,z | x^2 = y^2 = z^3 = (yx)^4 = 1, zxz = x, yzy = z>"; return; } // 8
                }
                else if (seq[1] == 7)
                {
                    if (isAbelian) { groupID = 5; groupName = "Z3 x Z2^3"; return; }
                    else if (seq[2] == 8) { groupID = 6; groupName = "A4 x Z2"; return; }
                    else { groupID = 7; groupName = "S3 x Z4"; return; }
                }
                else if (seq[1] == 5) { groupID = 8; groupName = "D4 x Z3"; return; }
                else if (seq[1] == 3)
                {
                    if (isAbelian) { groupID = 9; groupName = "Z4 x Z3 x Z2"; return; }
                    else { groupID = 10; groupName = "Dic3 x Z2"; return; }
                }
                else
                {
                    if (seq[2] == 8) { groupID = 11; groupName = "SL(2,3)"; return; } // 3 (maybe use <x,y,z | x^3 = y^3 = z^3 = xyz>?)
                    else if (seq[3] == 14) { groupID = 12; groupName = "Dic6"; return; }
                    else if (seq[3] == 6) { groupID = 13; groupName = "Q8 x Z3"; return; }
                    else { groupID = 14; groupName = "<a,x | a^3 = x^8 = 1, axa = x> "; return; } // 1
                }
            }
            else if (graph.Order == 27)
            {
                if (seq[1] == 26)
                {
                    if (isAbelian) { groupID = 1; groupName = "Z3^3"; return; }
                    else { groupID = 2; groupName = "<x,y,z | x^3 = y^3 = z^3 = 1, zyx = xy, xz = zx, yz = zy>"; return; }
                }
                else
                {
                    if (isAbelian) { groupID = 3; groupName = "Z9 x Z3"; return; }
                    else { groupID = 4; groupName = "<a,b | a^9 = b^3 = 1, bab^-1 = a^4>"; return; }
                }
            }
            else if (graph.Order == 28)
            {
                if (seq[1] == 15) { groupID = 1; groupName = "D14"; return; }
                if (seq[1] == 3) { groupID = 2; groupName = "Z7 x Z2^2"; return; }
                else { groupID = 3; groupName = "Dic7"; return; }
            }
            else if (graph.Order == 30)
            {
                if (seq[1] == 15) { groupID = 1; groupName = "D15"; return; }
                else if (seq[1] == 5) { groupID = 2; groupName = "D5 x Z3"; return; }
                else { groupID = 3; groupName = "S3 x Z5"; return; }
            }
            else if (graph.Order == 32)
            {
                // Stretch goal
                if (seq[1] == 31) { groupID = 1; groupName = "Z2^5"; return; }
                else if (seq[1] == 23) { groupID = 2; groupName = "D4 x Z2^2"; return; }
                else if (seq[1] == 19)
                {
                    if (seq[2] == 4) { groupID = 3; groupName = "D8 x Z2"; return; }
                    else
                    {
                        if (CountPowers(2) == 2) { groupID = 4; groupName = "<w,x,y,z | w^2 = x^2 = y^2 = z^2 = (wx)^4 = (yxw)^2 = 1, wy = yw, wz = zw, xz = zx, (xw)^2 = (yz)^2>"; return; } // 49
                        // These two have the same order sequence (1, 19, 12, 0, 0, 0), power statistics (32, 4, 1, 1, 1, 1), center (Z2^2), and derived subgroup (Z2^2)
                        // 34 -- D(Z4^2)
                        // 27 -- <x,y,z | x^2 = y^2 = z^2 = (zy)^2 = (yxzx)^2 = (yx)^4 = (zx)^4 = (zxy)^4 = 1>
                    }
                }
                else if (seq[1] == 17) { groupID = 7; groupName = "D16"; return; }
                else if (seq[1] == 15)
                {
                    if (seq[2] == 8) { groupID = 8; groupName = "<x,y,z | x^8 = y^2 = z^2 = 1, yxy^-1 = x^7, zxz^-1 = x^5>"; return; } // 43
                    else
                    {
                        if (isAbelian) { groupID = 9; groupName = "Z4 x Z2^3"; return; }
                        int[] Z = GetCenter(); // The centers of the next groups are Z2^2, Z2 x Z4, and Z2^3
                        if (Z.Length == 4) { groupID = 10; groupName = "<x,y,z | x^2 = y^4 = z^2 = (xy)^2 = (zx)^4 = 1, yz = zy, xy^2 = y^2x>"; return; } // 28
                        for (int i = 1; i < 5; i++)
                        {
                            if (orders[Z[i]] == 4) { groupID = 11; groupName = "Z2 x <x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; return; } // 48
                        }
                        groupID = 12; groupName = "Z2 x <x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zx = xyz>"; return; // 22
                    }
                }
                else if (seq[1] == 11)
                {
                    if (seq[2] == 20)
                    {
                        //  -- D8 x Z4
                        //  -- <x,y | x^4 = y^2 = (yxy^-1x^-1)^2 = (yx)^4 = (x^2y)^4 = 1>
                        //  -- <x,y,z | x^2 = y^2 = z^4 = (xz)^4 = (z^2x)^2 = 1, yz = zy, (yx)^2 = z^2>
                        //  -- <x,y,z | x^2 = y^4 = z^4 = 1, yz = zy, xz^2 = z^2x, xy^2 = y^2x, yzy = xzx, xy = z^2yx>
                        //  -- <w,x,y,z | w^2 = x^2 = y^4 = (xy)^2 = y^2z^2 = 1, wy = yw, wz = zw, xzx = z, zyz = y, ywy = xwx>
                    }
                    else if (seq[2] == 12)
                    {
                        // 40 -- Z2 x <x,y | x^8 = y^2 = 1, yxy^-1 = x^3>
                        // 9 -- <x,y | x^4 = y^2 = (xyx)^2 = (yx^-1yxyxyx)^2 = (xyx^-1y^-1)^4 = 1>
                        // 42 -- <x,y,z | x^2 = y^2 = z^4 = 1, xz = zx, yz = zy, (xy)^4 = z^2>
                    }
                    else { groupID = 21; groupName = "<x,y | y^2 = (xyx)^2 = (x^-1yxy)^2 = x^8 = 1>"; return; } // 7
                }
                else if (seq[1] == 9) { groupID = 22; groupName = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^7>"; return; } // 19
                else if (seq[1] == 7)
                {
                    if (seq[2] == 24)
                    {
                        if (isAbelian) { groupID = 23; groupName = "Z4^2 x Z2"; return; }
                        // 2 -- <x,y,z | x^4 = y^4 = z^2 = 1, xz = zx, yz = zy, xy = zyx>
                        // 23 -- Z2 x <x,y | x^4 = y^4 = 1, yxy^-1 = x^3>
                        // 24 -- <x,y,z | x^4 = y^4 = z^2 = 1, xy = yx, xz = zx, zyz^1 = x^2y>
                        // 29 -- <x,y,z | x^4 = y^4 = z^4 = 1, x^2 = y^2, xy = yx, zxz = x, zyz = y>
                        // 33 -- <x,y,z | x^4 = y^2 = z^4 = 1, xz = zx, yxy^-1x^-1 = z^2, yzy^-1z^-1 = x^2z^2>
                        // 47 -- Q8 x Z2^2
                    }
                    else if (seq[2] == 16)
                    {
                        // Maybe find another representation of Z4 Wr Z2. Wreath products are too awesome
                        if (graph.Degree == 2 || GetCenter().Length == 4) { groupID = 30; groupName = "Z4 Wr Z2"; return; }
                        else { groupID = 31; groupName = "<x,y,z | x^2 = z^2 = y^2(zx)^2 = 1, yz = zy, xy^2x = y^2, (xy^-1)^3 = y^-1x>"; return; } // 44
                    }
                    else
                    {
                        if (isAbelian) { groupID = 32; groupName = "Z8 x Z2^ 2"; return; }
                        //  -- <x,y | x^8 = y^2 = 1, x^2y=yx^2, (xy)^2 = (yx)^2>
                        //  -- <x,y | x^8 = y^2 = 1, yxy = x^3> x Z2
                        //  -- <x,y,z | x^8 = y^2 = z^2 = 1, xy = yx, xz = zx, (yz)^2 = x^4>
                    }
                }
                else if (seq[1] == 3)
                {
                    if (seq[2] == 28)
                    {
                        // 26 -- Q8 x Z4
                        // 32 -- <x,y,z | x^4 = y^4 = x^2z^2 = 1, xyx = y, zy = yz, xy^2 = y^2x>
                        // 35 -- <x,y,z | x^2y^2 = y^4 = z^4 = 1, yxy = zyz = x, yz = zy>
                    }
                    else if (seq[2] == 20)
                    {
                        //  -- Q16 x Z2
                        //  -- <x,y | x^4 = y^4 = (x^-1y^2)^2 = (yx^2)^2 = (x^2y^2)^2 = 1, xy = yxyx^3yx>
                        //  -- <x,y | x^8 = y^4 = 1, yxy^-1 = x^3>
                        //  -- <x,y | x^8 = y^4 = 1, xyx = y>
                    }
                    else if (seq[2] == 12)
                    {
                        if (isAbelian) { groupID = 43; groupName = "Z8 x Z4"; return; }
                        if (GetCenter().Length == 2) { groupID = 44; groupName = "<x,y,z | x^8 = y^2 = 1, z^2 = x^4, yxy^-1 = x^5, yz = zy, ac = cab>"; return; } // 8
                        // 4 -- <x,y | x^8 = y^4 = 1, yxy^-1 = x^5> (8 squares?)
                        // 12 -- <x,y | x^4 = y^8 = 1, yxy^-1 = x^3> (6 squares?)
                    }
                    else if (seq[3] == 24) { groupID = 47; groupName = "<x,y | x^2yxyx = 1, x = y^2xy^2, x^3y^-1x = y, y^3x = xy> "; return; } // 15
                    else
                    {
                        if (isAbelian) { groupID = 48; groupName = "Z16 x Z2"; return; } // 16
                        else { groupID = 49; groupName = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^9>"; return; } // 17
                    }
                }
                else { groupID = 50; groupName = "Q32"; return; }
            }

            groupID = -1;
            groupName = "Can't Identify";
            #endregion
        }

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
        /// This is the inefficient way to do it. Don't use for large input, please
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

        // Unused
        public static int GCD(int a, int b)
        {
            if (b == 0)
            {
                return a;
            }
            return GCD(b, a % b);
        }

        // Unused
        public static bool IsCyclicNumber(int n)
        {
            int c = 0;
            for (int i = 1; i <= n; i++)
            {
                if (GCD(n, i) == 1)
                {
                    c++;
                }
            }

            return GCD(n, c) == 1;
        }
    }
}
