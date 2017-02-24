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
        private string groupString;

        // Yeah, I store the Cayley table. It's O(n^2) memory. The old strategy was complicated and "slow".
        private int[,] op;
        private int[] inv;
        private int[] factors;
        private int[] divisors;
        private int[] orders;
        private bool isAbelian;

        public Group(Graph G) : this(G, G.BFS(0)) { }

        public Group(Graph G, Tuple<int[], int[]> T)
        {
            graph = G;
            tree = T;

            getProperties();
            SetGroupID();
            
            groupString = groupID == -1 ? "?" : string.Format("({0}, {1})", graph.Order, groupID);
        }

        public int Order { get { return graph.Order; } }
        public int GroupID { get { return groupID; } }
        public string GroupString { get { return groupString; } }

        // Sets the main properties of the group
        private void getProperties()
        {
            int i, j, k;

            factors = DiscreteMath.PrimeFactors(graph.Order);
            divisors = DiscreteMath.Divisors(graph.Order);

            // Elements
            Stack<int> element = new Stack<int>();
            op = new int[graph.Order, graph.Order];
            inv = new int[graph.Order];
            for (i = 0; i < graph.Order; i++)
            {
                element = new Stack<int>();
                j = i;
                while (tree.Item1[j] != -1)
                {
                    k = tree.Item1[j];
                    j = graph.InEdges[j, k];
                    element.Push(k);
                }

                for (j = 0; j < graph.Order; j++)
                {
                    k = j;
                    foreach (int g in element)
                    {
                        k = graph.OutEdges[k, g];
                    }
                    op[j, i] = k;
                    if (k == 0) inv[j] = i;
                }
            }

            // Orders
            orders = new int[graph.Order];
            orders[0] = 1;
            for (i = 0; i < graph.Order; i++)
            {
                j = op[i, 0];
                k = 1;
                while (j != 0)
                {
                    j = op[i, j];
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

        /// <summary>
        /// Returns a power of an element. It's fast exponentiation because multiplications used to be nontrivial
        /// </summary>
        private int pow(int element, int exponent)
        {
            if (exponent < 0) { element = inv[element]; exponent = -exponent; }
            else if (exponent == 0) return 0;

            exponent = exponent % orders[element];

            int t = 0;
            while (exponent > 1)
            {
                if (exponent % 2 == 0) { element = op[element, element]; exponent /= 2; }
                else { t = op[element, t]; element = op[element, element]; exponent = (exponent - 1) / 2; }
            }
            return op[element, t];
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

        /// <summary>
        /// Determine if the group is dihedral.
        /// </summary>
        /// <returns></returns>
        private bool IsDihedral()
        {
            if (factors[0] != 2) return false;
            else if (factors.Length == 2) return true;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns true if the passed element commutes with all other elements in the group.
        /// </summary>
        private bool IsCentral(int a)
        {
            int x;
            for (int i = 0; i < graph.Degree; i++)
            {
                x = graph.OutEdges[0, i];
                if (op[a, x] != op[x, a])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Get int array of elements in the derived subgroup (set of all central elements)
        /// </summary>
        private int[] GetCenter()
        {
            if (isAbelian)
            {
                // I will probably change how this gets handled, but I really don't want to compute a subgroup unless we have to.
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

        /// <summary>
        /// Get int array of elements in the derived subgroup (set of all commutators)
        /// </summary>
        private int[] GetDerivedSubgroup()
        {
            HashSet<int> elms = new HashSet<int>();
            elms.Add(0);
            for (int i = 1; i < graph.Order; i++)
            {
                for (int j = 1; j < i; j++)
                {
                    elms.Add(op[op[i, j], inv[op[j, i]]]);
                    elms.Add(op[op[j, i], inv[op[i, j]]]);
                }
            }
            return elms.ToArray();
        }

        /// <summary>
        /// UNFINISHED (and frankly I haven't found a use yet)
        /// </summary>
        /// <param name="elements">The elements in the subgroup</param>
        /// <returns>Returns a pair (order, id) of the group</returns>
        private string GetSubgroup(int[] elements)
        {
            // TODO this is tricky
            /* 
             * Strat is this:
             * 1. Put the identity in a set H
             * 2. Put the first element not in H and all of its powers in H.
             * 3. Repeat.
             * 4. Maintain a graph all the while
            */

            int n = elements.Length;
            if (n == graph.Order) return groupString;
            else if (n == 1) return "(1, 0)";

            Graph G = new Graph(n);

            return G.ToCayleyGraph().GroupString;
        }

        /// <summary>
        /// Identifies the group and gives it the id (groupID) and description (groupName)
        /// </summary>
        private void SetGroupID()
        {
            // The order sequence divides the groups into small classes.
            int[] seq = new int[divisors.Length];
            for (int i = 0; i < divisors.Length; i++) seq[i] = orders.Count(x => x == divisors[i]);

            #region Decision tree from your nightmares. Abandon all hope ye who enter here.
            #region Seriously, I'll have good style from now on but don't make me go back in there.

            // The decision tree (mostly) looks at order sequence followed by power statistics.
            // But the first four cases look for general classes of groups. It's a little bit of Hölder's classification.
            // This is "Layer 1" http://www.icm.tu-bs.de/ag_algebra/software/small/README
            
            int p, q, r, n;
            if (seq[divisors.Length - 1] > 0) { groupID = 0; return; } // Zn
            else if (IsDihedral()) { groupID = 1; return; } // Dn (or S3)
            else if (factors.Length == 2) // A001358
            {
                // These all have factorization p^2 or pq.
                // The letters make a little bit more readable
                p = factors[0];
                q = factors[1];

                if (p == q) { groupID = 2; } // Zp^2
                else if (factors[1] % factors[0] == 1)
                {
                    for (n = 2; DiscreteMath.ModPow(n, p, q) != 1; n++) ;
                    groupID = 3;
                } // q:p -- [1, 1] [p, (p-1)q] [q, q-1] [pq, 0] -- <x,y | x^q = y^p = 1, yxy^-1 = x^n>

                return;
            }
            else if (factors.Length == 3) // A014612
            {
                // These all have factorization p^3, p^2r, pr^2, or pqr with p < q < r.
                // The letters make a little bit more readable
                p = factors[0];
                q = factors[1];
                r = factors[2];

                if (p == q && q == r) // p^3
                {
                    if (isAbelian)
                    {
                        if (seq[1] == graph.Order - 1) { groupID = 2; } // Zp^3
                        else { groupID = 3; } // Z(p^2) x Zp
                    }
                    else
                    {
                        if (seq[1] == graph.Order - 1) { groupID = 4; } // <x,y,z | x^p = y^p = z^p = 1, yx = xyz, xz = zx, yz = zy>
                        else { groupID = 5; } // Q8 or <x,y,z | x^p = y^p = z, z^p = 1, yx = xyz, xz = zx, yz = zy>
                    }

                    return;
                }
                else if (p == q) // p^2r
                {
                    // Distinguish between two cases: 1 < p < p^2 < q < pq < p^2q and 1 < p < q < p^2 < pq < p^2q
                    int ip2 = p * p < r ? 2 : 3;
                    int ir = 5 - ip2;

                    if (isAbelian) { groupID = 2; } // Zp x Zr^2
                    else
                    {
                        if (r % p == 1)
                        {
                            if (seq[1] == (p - 1) * (p * r + 1) && seq[ir] == r - 1 && seq[4] == (p - 1) * (r - 1))
                            {
                                for (n = 2; DiscreteMath.ModPow(n, p, r) != 1; n++) ;
                                groupID = 3;
                            } // (r:p) x p -- [1, 1] [p, (p-1)(pq+1)] [q, q-1] [pq, (p-1)(q-1)] -- Zp x <x,y | x^r = y^p = 1, yxy^-1 = x^n>
                            else if (false)
                            {
                                /*
                                 * (p x r).p -- [???] -- ???
                                 * This is the group G such that 1 -> Zp -> G -> Zp x Zr -> 1 is a short exact sequence
                                 * Zp is a normal subroup of G and G/Zp <-> Zp x Zr
                                */
                                groupID = 4;
                            }
                        }
                        else if (r % (p*p) == 1 && false)
                        {
                            for (n = 2; DiscreteMath.ModPow(n, p*p, r) != 1; n++) ;
                            groupID = 5;
                        } // r:Z(p^2) -- [???] -- <x,y | x^r = y^(p^2) = 1, yxy^-1 = x^n>
                    }

                    return;
                }
                else if (q == r) // pr^2
                {
                    if (isAbelian) { groupID = 2; groupString = "Z" + factors[2] + "^2 x Z" + factors[0]; return; }
                    else
                    {
                        /* Nonabelian groups
                        1 group of type r:p x r if r = 1 mod p
                        1 group of type C(r^2):p if r = 1 mod p
                        1 group of type (rxr):p if p > 2 and r+1 = 0 mod p 
                        groups of type r:p + r:p
                            (1 group for p = 2 and (p+1)/2 groups otherwise)
                        */
                    }
                }
            }

            // On to "Layer 2", going order by order and looking at the order sequence.
            // Layer 2 is a huge mess because I keep making changes to Layer 1
            if (graph.Order == 12)
            {
                //if (seq[1] == 7) { groupID = 6; groupString = "D6"; return; }
                if (seq[2] == 8) { groupID = 6; } // A4
                else { groupID = 7; } // Dic3
                return;
            }
            else if (graph.Order == 16)
            {
                if (seq[1] == 15) { groupID = 6; } // Z2^4
                else if (seq[1] == 11) { groupID = 7; } // D4 x Z2
                //else if (seq[1] == 9) { groupID = 8; groupString = "D8"; return; }
                else if (seq[1] == 7)
                {
                    if (isAbelian) { groupID = 8; } // Z4 x Z2^2
                    else if (CountPowers(2) == 2) { groupID = 9; } // 13 <x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>
                    else { groupID = 10; } // 3 <x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zxz^-1 = xy>
                }
                else if (seq[1] == 5) { groupID = 11; } // <x,y | x^8 = y^2 = 1, yxy^-1 = x^3>
                else if (seq[1] == 3)
                {
                    if (seq[2] == 4)
                    {
                        if (isAbelian) { groupID = 12; } // Z8 x Z2
                        else { groupID = 13; } // <x,y | x^8 = y^2 = 1, yxy^-1 = x^5>
                    }
                    else
                    {
                        if (isAbelian) { groupID = 14; } // Z4^2
                        if (CountPowers(2) == 3) { groupID = 15; } // <x,y | x^4 = y^4 = 1, yxy^-1 = x^3>
                        else { groupID = 16; } // Q8 x Z2
                    }
                }
                else { groupID = 17; } // Q16

                return;
            }
            else if (graph.Order == 18)
            {
                if (seq[1] == 3) { groupID = 6; } // S3 x Z3
                //else if (seq[2] == 2) { groupID = 3; groupString = "D9"; return; }
                else { groupID = 7; } // D(Z3^2)

                return;
            }
            else if (graph.Order == 20)
            {
                if (seq[1] == 1) { groupID = 6; } // Dic5
                else if (seq[1] == 5) { groupID = 7; } // 3 <a,b | a^5 = b^4 = 1, bab^-1 = a^2>
                //else { groupID = 4; groupString = "D10"; return; }

                return;
            }
            else if (graph.Order == 24)
            {
                if (seq[1] == 15) { groupID = 6; } // D6 x Z2
                //else if (seq[1] == 13) { groupID = 2; groupString = "D12"; return; }
                else if (seq[1] == 9)
                {
                    if (seq[2] == 8) { groupID = 7; } // S4
                    else { groupID = 8; } // 8 <x,y,z | x^2 = y^2 = z^3 = (yx)^4 = 1, zxz = x, yzy = z>
                }
                else if (seq[1] == 7)
                {
                    if (isAbelian) { groupID = 9; } // Z3 x Z2^3
                    else if (seq[2] == 8) { groupID = 10; } // A4 x Z2
                    else { groupID = 11; } // S3 x Z4
                }
                else if (seq[1] == 5) { groupID = 12; } // D4 x Z3
                else if (seq[1] == 3)
                {
                    if (isAbelian) { groupID = 13; } // Z4 x Z3 x Z2
                    else { groupID = 14; } // Dic3 x Z2
                }
                else
                {
                    if (seq[2] == 8) { groupID = 15; } // 3 SL(2,3) (maybe use <x,y,z | x^3 = y^3 = z^3 = xyz>?)
                    else if (seq[3] == 14) { groupID = 16; } // Dic6
                    else if (seq[3] == 6) { groupID = 17; } // Q8 x Z3
                    else { groupID = 18; } // 1 <a,x | a^3 = x^8 = 1, axa = x>
                }

                return;
            }
            else if (graph.Order == 28)
            {
                // Dic7 ?

                return;
            }
            else if (graph.Order == 30)
            {
                //if (seq[1] == 15) { groupID = 1; groupString = "D15"; return; }
                if (seq[1] == 5) { groupID = 2; } // D5 x Z3
                else { groupID = 3; } // S3 x Z5

                return;
            }
            else if (graph.Order == 32)
            {
                // Stretch goal
                if (seq[1] == 31) { groupID = 1; return; } // Z2^5
                else if (seq[1] == 23) { groupID = 2; return; } // D4 x Z2^2
                else if (seq[1] == 19)
                {
                    if (seq[2] == 4) { groupID = 3; return; } // D8 x Z2
                    else
                    {
                        if (CountPowers(2) == 2) { groupID = 4; return; } // 49 <w,x,y,z | w^2 = x^2 = y^2 = z^2 = (wx)^4 = (yxw)^2 = 1, wy = yw, wz = zw, xz = zx, (xw)^2 = (yz)^2>
                        // These two have the same order sequence (1, 19, 12, 0, 0, 0), power statistics (32, 4, 1, 1, 1, 1), center (Z2^2), and derived subgroup (Z2^2)
                        // 34 -- D(Z4^2)
                        // 27 -- <x,y,z | x^2 = y^2 = z^2 = (zy)^2 = (yxzx)^2 = (yx)^4 = (zx)^4 = (zxy)^4 = 1>
                    }
                }
                //else if (seq[1] == 17) { groupID = 7; groupString = "D16"; return; }
                else if (seq[1] == 15)
                {
                    if (seq[2] == 8) { groupID = 8; } // 43 <x,y,z | x^8 = y^2 = z^2 = 1, yxy^-1 = x^7, zxz^-1 = x^5>
                    else
                    {
                        if (isAbelian) { groupID = 9; } // Z4 x Z2^3
                        int[] Z = GetCenter(); // The centers of the next groups are Z2^2, Z2 x Z4, and Z2^3
                        if (Z.Length == 4) { groupID = 10; } // 28 <x,y,z | x^2 = y^4 = z^2 = (xy)^2 = (zx)^4 = 1, yz = zy, xy^2 = y^2x>
                        for (int i = 1; i < 5; i++)
                        {
                            if (orders[Z[i]] == 4) { groupID = 11; } // 48 Z2 x <x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>
                        }
                        groupID = 12; // 22 Z2 x <x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zx = xyz>
                    }

                    return;
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
                    else { groupID = 21; return; } // 7 <x,y | y^2 = (xyx)^2 = (x^-1yxy)^2 = x^8 = 1>
                }
                else if (seq[1] == 9) { groupID = 22; return; } // 19 <x,y | x^16 = y^2 = 1, yxy^-1 = x^7>
                else if (seq[1] == 7)
                {
                    if (seq[2] == 24)
                    {
                        if (isAbelian) { groupID = 23; return; } // Z4^2 x Z2
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
                        if (graph.Degree == 2 || GetCenter().Length == 4) { groupID = 30; } // Z4 Wr Z2
                        else { groupID = 31; } // 44 <x,y,z | x^2 = z^2 = y^2(zx)^2 = 1, yz = zy, xy^2x = y^2, (xy^-1)^3 = y^-1x>

                        return;
                    }
                    else
                    {
                        if (isAbelian) { groupID = 32; return; } // Z8 x Z2^2
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
                        if (isAbelian) { groupID = 43; return; } // Z8 x Z4
                        if (GetCenter().Length == 2) { groupID = 44; return; } // 8 <x,y,z | x^8 = y^2 = 1, z^2 = x^4, yxy^-1 = x^5, yz = zy, ac = cab>
                        // 4 -- <x,y | x^8 = y^4 = 1, yxy^-1 = x^5> (8 squares?)
                        // 12 -- <x,y | x^4 = y^8 = 1, yxy^-1 = x^3> (6 squares?)
                    }
                    else if (seq[3] == 24) { groupID = 47; } // 15 <x,y | x^2yxyx = 1, x = y^2xy^2, x^3y^-1x = y, y^3x = xy>
                    else
                    {
                        if (isAbelian) { groupID = 48; } // 16 Z16 x Z2
                        else { groupID = 49; } // 17 <x,y | x^16 = y^2 = 1, yxy^-1 = x^9>

                        return;
                    }
                }
                else { groupID = 50; return; } // Q32
            }

            groupID = -1;
            
            #endregion
            #endregion
        }
    }
}
