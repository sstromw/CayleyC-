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
        private int[] orders;
        private Stack<int>[] elements;
        private int groupID;
        private string groupName;

        public Group(Graph G) : this(G, G.BFS(0)) { }

        public Group(Graph G, Tuple<int[], int[]> T)
        {
            graph = G;
            tree = T;

            elements = new Stack<int>[graph.Order];
            orders = new int[graph.Order];

            getElements();
            getOrders();
            SetGroupID();
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

        private void getElements()
        {
            int i, j, k;
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
        }

        private void getOrders()
        {
            orders[0] = 1;
            int i, j, k;
            for (i = 1; i < graph.Order; i++)
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
            List<int> center = new List<int>();
            int i, j, gen;
            bool commutes;
            for(i = 0; i < graph.Order; i++)
            {
                commutes = true;
                for (j = 0; j < graph.Degree; j++)
                {
                    gen = graph.OutEdges[0, j];
                    if (i != gen && op(i, gen) != op(gen, i))
                    {
                        commutes = false;
                    }
                }

                if (commutes)
                {
                    center.Add(i);
                }
            }
            return center.ToArray();
        }

        private void SetGroupID()
        {
            int[] factors = PrimeFactors(graph.Order);
            int[] center = GetCenter();
            int centerSize = center.Length;
            int[] seq = (int[])orders.Clone();
            Array.Sort(seq);

            // Decision tree from your nightmares. Abandon all hope ye who enter here.
            if (seq[graph.Order - 1] == graph.Order) { groupID = 0; groupName = "Z" + graph.Order; return; }
            else if (factors.Length == 2 && factors[0] == factors[1]) { groupID = 1; groupName = "Z" + factors[0] + "^2"; return; }
            else if (factors.Length == 2 && factors[0] == 2) { groupID = 1; groupName = "D" + factors[1]; return; }
            else if (factors.Length == 2 && factors[1] % factors[0] == 1) { groupID = 1; groupName = "<x,y | x^" + factors[1] + " = y^" + factors[0] + " = 1, yxy^-1 = x^" + (factors[0] * factors[0]) % factors[1] + ">"; return; }
            else if (graph.Order == 8)
            {
                if (seq[7] == 2) { groupID = 1; groupName = "Z2^3"; return; }
                else if (seq[5] == 2) { groupID = 2; groupName = "D4"; return; }
                else if (seq[2] == 4) { groupID = 3; groupName = "Q8"; return; }
                else { groupID = 4; groupName = "Z4 x Z2"; return; }
            }
            else if (graph.Order == 12)
            {
                if (seq[11] == 3) { groupID = 1; groupName = "A4"; return; }
                else if (seq[2] == 3) { groupID = 2; groupName = "Dic3"; return; }
                else if (seq[9] == 3) { groupID = 3; groupName = "D6"; return; }
                else { groupID = 4; groupName = "Z6 x Z2"; return; }
            }
            else if (graph.Order == 14) { groupID = 1; groupName = "D7"; return; }
            else if (graph.Order == 16)
            {
                if (seq[15] == 2) { groupID = 1; groupName = "Z2^4"; return; }
                else if (seq[2] == 4) { groupID = 2; groupName = "Q16"; return; }
                else if (seq[11] == 2) { groupID = 3; groupName = "D4 x Z2"; return; }
                else if (seq[9] == 2) { groupID = 4; groupName = "D8"; return; }
                else if (seq[11] == 4 && seq[12] == 8) { groupID = 5; groupName = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^3>"; return; }
                else if (seq[15] == 8)
                {
                    if (centerSize == graph.Order) { groupID = 6; groupName = "Z8 x Z2"; return; }
                    else { groupID = 7; groupName = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^5>"; return; }
                }
                else if (seq[4] == 4)
                {
                    if (centerSize == graph.Order) { groupID = 8; groupName = "Z4 x Z4"; return; }
                    for (int i = 0; i < graph.Order - 1; i++) // Ugh O(n^2)
                    {
                        if (orders[i] == 4)
                        {
                            for (int j = i + 1; j < graph.Order; j++)
                            {
                                if (orders[j] == 4 && op(op(i, i), op(j, j)) != 0) { groupID = 9; groupName = "<x,y | x^4 = y^4 = 1, yxy^-1 = x^3>"; return; }
                            }
                        }
                    }
                    groupID = 10; groupName = "Q8 x Z2"; return;
                }
                else
                {
                    if (centerSize == graph.Order) { groupID = 11; groupName = "Z4 x Z2^2"; return; }
                    for (int i = 0; i < graph.Order; i++)
                    {
                        if (orders[i] == 2 && IsCentral(i)) { groupID = 12; groupName = "<x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; return; }
                    }
                    groupID = 13; groupName = "<x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zxz^-1 = xy>"; return;
                }
            }
            else if (graph.Order == 18)
            {
                if (seq[17] == 9) { groupID = 1; groupName = "D9"; return; }
                else if (seq[17] == 3) { groupID = 2; groupName = "D(Z3^2)"; return; }
                else if (seq[2] == 3) { groupID = 3; groupName = "Z3 ^ 2 x Z2"; return; }
                else { groupID = 4; groupName = "S3 x Z3"; return; }
            }
            else if (graph.Order == 20)
            {
                if (seq[19] == 5) { groupID = 1; groupName = "<a,b | a^5 = b^4 = 1, bab^-1 = a^2>"; return; }
                else if (seq[6] == 2) { groupID = 2; groupName = "D10"; return; }
                else if (seq[2] == 4) { groupID = 3; groupName = "Dic5"; return; }
                else { groupID = 4; groupName = "Z5 x Z2^ 2"; return; }
            }
            else if (graph.Order == 21) { groupID = 1; groupName = "<x,y | x^3 = y^7 = 1, yxy^-1 = xy>"; return; }
            else if (graph.Order == 24)
            {
                if (seq[23] == 4) { groupID = 1; groupName = "S4"; return; }
                else if (seq[17] == 3) { groupID = 2; groupName = "D6 x Z2"; return; }
                else if (seq[12] == 12) { groupID = 3; groupName = "Q8 x Z3"; return; }
                else if (seq[6] == 6) { groupID = 4; groupName = "<a,x | a^3 = x^8 = 1, axa = x> "; return; }
                else if (seq[4] == 4) { groupID = 5; groupName = "Dic6"; return; }
                else if (seq[2] == 3) { groupID = 6; groupName = "SL(2,3)"; return; }
                else if (seq[6] == 3) { groupID = 7; groupName = "D4 x Z3"; return; }
                else if (seq[16] == 12) { groupID = 8; groupName = "Z4 x Z3 x Z2"; return; }
                else if (seq[13] == 2) { groupID = 9; groupName = "D12"; return; }
                else if (seq[23] == 12) { groupID = 10; groupName = "S3 x Z4"; return; }
                else if (seq[6] == 4) { groupID = 11; groupName = "Dic3 x Z2"; return; }
                else if (seq[9] == 2) { groupID = 12; groupName = "<x,y,z | x^2 = y^2 = z^3 = (yx)^4 = 1, zxz = x, yzy = z>"; return; }
                else if (seq[10] == 6) { groupID = 13; groupName = "Z3 x Z2^3"; return; }
                else { groupID = 14; groupName = "A4 x Z2"; return; }
            }
            else if (graph.Order == 27)
            {
                if (seq[26] == 3)
                {
                    if (centerSize == graph.Order) { groupID = 1; groupName = "Z3^3"; return; }
                    else { groupID = 2; groupName = "<x,y,z | x ^ 3 = y ^ 3 = z ^ 3 = 1, zyx = xy, xz = zx, yz = zy>"; return; }
                }
                else
                {
                    if (centerSize == graph.Order) { groupID = 3; groupName = "Z9 x Z3"; return; }
                    else { groupID = 4; groupName = "<a,b | a^9 = b^3 = 1, bab^-1 = a^4>"; return; }
                }
            }
            else if (graph.Order == 28)
            {
                if (seq[4] == 7) { groupID = 1; groupName = "Z7 x Z2^2"; return; }
                else if (seq[15] == 2) { groupID = 2; groupName = "D14"; return; }
                else { groupID = 3; groupName = "Dic7"; return; }
            }
            else if (graph.Order == 30)
            {
                if (seq[4] == 3) { groupID = 1; groupName = "S3 x Z5"; return; }
                else if (seq[6] == 3) { groupID = 2; groupName = "D5 x Z3"; return; }
                else { groupID = 3; groupName = "D15"; return; }
            }
            else if (graph.Order == 32)
            {
                // Stretch goal
                if (seq[31] == 2) { groupID = 1; groupName = "Z2^5"; return; }
                else if (seq[23] == 2) { groupID = 2; groupName = "D4 x Z2^2"; return; }
                else if (seq[2] == 4) { groupID = 3; groupName = "<x,y,z | x^2 = z^2 = y^2(zx)^2 = 1, zyz = y, xy^2x = y^2, (xy)^3 = yx>"; return; }
                else if (seq[8] == 8 && seq[31] == 8) { groupID = 4; groupName = "<x,y | x^2yxyx = 1, x = y^2xy^2, x^3y^-1x = y, y^3x = xy> "; return; }
                else if (seq[11] == 2 && seq[16] == 8) { groupID = 5; groupName = "<x,y | y^2 = (xyx)^2 = (x^-1yxy)^2 = x^8 = 1>"; return; }
                else if (seq[17] == 2 && seq[31] == 16) { groupID = 6; groupName = "D16"; return; }
                else if (seq[19] == 2 && seq[24] == 8) { groupID = 7; groupName = "D8 x Z2"; return; }
                else if (seq[23] == 8 && seq[24] == 16) { groupID = 8; groupName = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^7>"; return; }
                else if (seq[15] == 2 && seq[24] == 8) { groupID = 9; groupName = "<x,y,z | x^8 = y^2 = z^2 = 1, xyx = y, zxz^-1 = x^5>"; return; }
                else if (seq[16] == 16)
                {
                    if (centerSize == graph.Order) { groupID = 10; groupName = "Z16 x Z2"; return; }
                    else { groupID = 11; groupName = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^9>"; return; }
                }
                else if (seq[19] == 2)
                {
                    // D(Z4^2)
                    // <x,y,z | (zy)^2 = (yxzx)^2 = (yx)^4 = (zx)^4 = (zxy)^4 = 1>
                    // <w,x,y,z | w^2 = x^2 = y^2 = z^2 = (wx)^4 = (zx)^2 = (wy)^2 = (wz)^2 = (yxw)^2  = xyw = 1>
                }
                else if (seq[15] == 2)
                {
                    if (centerSize == graph.Order) { groupID = 15; groupName = "Z4 x Z2^3"; return; }
                    // <x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zx = xyz> x Z2
                    // <x,y,z | x^2 = y^4 = z^2 = (xy)^2 = (zx)^4 = 1, yz = zy, xy^2 = y^2x>
                    // <a,x,y | a^4 = x^2 = (xa)^2 = 1, a^2 = y^2, ay = ya, xy = yx> x Z2
                }
                else if (seq[4] == 4 && seq[31] == 4)
                {
                    // Q8 x Z4
                    // <x,y,z | x^4 = y^4 = x^2z^2 = 1, xyx = y, zy = yz, xy^2 = y^2x>
                    // <x,y,z | x^2y^2 = y^4 = z^4 = 1, yxy = zyz = x, yz = zy>
                }
                else if (seq[8] == 4 && seq[31] == 4)
                {
                    if (centerSize == graph.Order) { groupID = 23; groupName = "Z4 ^ 2 x Z2"; return; }
                    // <x,y | x^4 = y^4 = (yxy^-1x^-1)^2 = (xyx^-1y)^2 = (xy^-1)^4 = 1, yx^2 = x^2y, xy^2 = y^2x>
                    // <x,y | x^4 = y^4 = 1, yxy^-1 = x^3> x Z2
                    // <x,y,z | x^4 = y^2 = z^4 = 1, xz = zx, yz = zx, x^2y = yx^2, xyx^-1 = yz^2>
                    // <x,y,z | x^4 = z^2 = x^2y^2 = (xzx)^2 = (xz)^4 = (zxzx^-1)^2 = 1, xyx = y, yxy = x, yz = zy>
                    // <x,y,z | x^2 = y^4 = z^4 = (y^2x)^2 = (z^2x)^2 = 1, yz = zy, xyx^-1 = z^2y, yxzyx = z>
                    // Q8 x Z2^2
                }
                else if (seq[31] == 4)
                {
                    // D8 x Z4
                    // <x,y | x^4 = y^2 = (yxy^-1x^-1)^2 = (yx)^4 = (x^2y)^4 = 1>
                    // <x,y,z | x^2 = y^2 = z^4 = (xz)^4 = (z^2x)^2 = 1, yz = zy, (yx)^2 = z^2>
                    // <x,y,z | x^2 = y^4 = z^4 = 1, yz = zy, xz^2 = z^2x, xy^2 = y^2x, yzy = xzx, xy = z^2yx>
                    // <w,x,y,z | w^2 = x^2 = y^4 = (xy)^2 = y^2z^2 = 1, wy = yw, wz = zw, xzx = z, zyz = y, ywy = xwx>
                }
                else if (seq[11] == 2)
                {
                    // <x,y | x^8 = y^2 = 1, yxy^-1 = x^3> x Z2
                    // <x,y | x^4 = y^2 = (xyx)^2 = (yx^-1yxyxyx)^2 = (xyx^-1y^-1)^4 = 1>
                    // <x,y,z | x^2 = y^2 = z^4 = 1, xz = zx, yz = zy, (xy)^4 = z^2>
                }
                else if (seq[4] == 4 && seq[23] == 4)
                {
                    // Q16 x Z2
                    // <x,y | x^4 = y^4 = (x^-1y^2)^2 = (yx^2)^2 = (x^2y^2)^2 = 1, xy = yxyx^3yx>
                    // <a,b | a^8 = b^4 = 1, bab^-1 = a^3>
                    // <a,b | a^8 = b^4 = 1, aba = b>
                }
                else if (seq[4] == 4)
                {
                    if (centerSize == graph.Order) { groupID = 42; groupName = "Z8 x Z4"; return; }
                    // <a,b | a^8 = b^4 = 1, bab^-1 = a^5>
                    // <x,y | y^4 = (x^1yxy)^2 = (yx^-1y^-1x)^2 = 1, x^4 = y^2, yx^2y = x^2, (x^-1y)^4 = y^2>
                    // <x,y | x^8 = y^4 = 1, yxy = x, xy^2 = y^2x>
                }
                else if (seq[16] == 8)
                {
                    if (centerSize == graph.Order) { groupID = 46; groupName = "Z8 x Z2^ 2"; return; }
                    // <x,y | x^8 = y^2 = 1, x^2y=yx^2, (xy)^2 = (yx)^2>
                    // <x,y | x^8 = y^2 = 1, yxy = x^3> x Z2
                    // <x,y,z | x^8 = y^2 = z^2 = 1, xy = yx, xz = zx, (yz)^2 = x^4>
                }
                else
                {
                    if (graph.Degree == 2) { groupID = 50; groupName = "Z4 Wr Z2"; return; } // This is sufficient and efficient but not necessary. Need another check.
                    // Z4 Wr Z2 (maybe find a representation for this one wreath products are too awesome)
                    // <x,y,z | x^2 = z^2 = y^2(zx)^2 = 1, yz = zy, xy^2x = y^2, (xy^-1)^3 = y^-1x>
                }
            }

            groupID = -1;
            groupName = "Can't Identify";
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
