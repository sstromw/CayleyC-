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
        private string groupDescription;

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
        }

        public int Order { get { return graph.Order; } }
        public int GroupID { get { return groupID; } }
        public string GroupDescription { get { return groupDescription; } }

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
            exponent = exponent % orders[element];
            if (exponent < 0) { element = inv[element]; exponent = -exponent; }
            else if (exponent == 0) return 0;

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
        /// Determine if the group is dihedral. I haven't determined whether or not this works.
        /// </summary>
        private bool IsDihedral()
        {
            if (factors[0] != 2) return false;
            else if (factors.Length == 2) return true;
            
            for (int i = 0; i < graph.Order; i++)
            {
                if (orders[i] == graph.Order / 2)
                {
                    for (int j = 0; j < graph.Order; j++)
                    {
                        if (orders[j] == 2 && op[op[j,i],inv[j]] == inv[i])
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
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

        /* Broken */
#if false
        /// <summary>
        /// Get a generated subgroup
        /// </summary>
        private int[] GetGeneratedSubgroup(int[] generators)
        {
            List<int> subgroup = new List<int>(generators);
            bool[] inSubgroup = new bool[graph.Order];
            foreach (int i in generators) inSubgroup[i] = true;
            foreach (int h in subgroup)
            {
                foreach (int k in subgroup)
                {
                    if (!inSubgroup[op[h,k]])
                    {
                        subgroup.Add(op[h, k]);
                        inSubgroup[op[h, k]] = true;
                    }
                    if (h == k) break;
                }
            }

            return subgroup.ToArray();
        }

        /// <summary>
        /// I think it works.
        /// </summary>
        /// <param name="elements">The elements in the subgroup</param>
        /// <returns>Returns a pair (order, id) of the group</returns>
        private string GetSubgroup(int[] elements)
        {
            int n = elements.Length;
            if (n == graph.Order) return groupDescription;
            else if (n == 1 && elements[0] == 0) return "(1, 0)";
            else if (n == 1)
            {
                throw new Exception("Not a subgroup");
            }

            Graph G = new Graph(n);
            bool[] connected = new bool[n];
            for (int i = 1, color = 0; i < n; i++)
            {
                if (!connected[i])
                {
                    for (int j = 0; j < n; j++)
                    {
                        int idx = Array.FindIndex(elements, x => x == op[elements[i], elements[j]]);
                        if (idx == -1)
                        {
                            throw new Exception("Not a subgroup");
                        }
                        G.AddEdge(j, idx, color);
                    }

                    Tuple<int[], int[]> generated = G.BFS(0);
                    for (int j = 0; j < n; j++) connected[j] = generated.Item1[j] != -1;
                    color++;
                }
            }

            return G.ToCayleyGraph().GroupDescription;
        }
#endif

        /// <summary>
        /// Identifies the group and gives it the id (groupID) and description (groupDescription)
        /// </summary>
        private void SetGroupID()
        {
            // The order sequence divides the groups into small classes.
            int[] seq = new int[divisors.Length];
            for (int i = 0; i < divisors.Length; i++) seq[i] = orders.Count(x => x == divisors[i]);
            
            // "Layer 0"
            if (seq[divisors.Length - 1] > 0) { groupID = 1; groupDescription = "Z" + graph.Order; return; }
            else if (graph.Order > 6 && IsDihedral()) { groupID = 0; groupDescription = "D" + graph.Order / 2; return; } // Dn (or S3)

            // The decision tree (mostly) looks at order sequence followed by power statistics.
            // But the first four cases look for general classes of groups. It's a little bit of Hölder's classification.
            // This is "Layer 1" http://www.icm.tu-bs.de/ag_algebra/software/small/README
            // But we only check cases p^2, pq, p^3, and abelian p^4. The other cases don't agree with the rest of the design.

            if (factors.Length == 2) // A001358
            {
                int p = factors[0], q = factors[1], n;

                if (p == q) { groupID = 2; groupDescription = string.Format("Z{0}^2", p); } // Zp^2
                else if (factors[1] % factors[0] == 1) { groupID = 3; for (n = 2; DiscreteMath.ModPow(n, p, q) != 1; n++) ; groupDescription = string.Format("<x,y | x^{0} = y^{1} = 1, yxy^-1 = x^{2}>", q, p, n); } // <x,y | x^q = y^p = 1, yxy^-1 = x^n> "q:p"
                return;
            }
            else if (factors.Length == 3 && factors[0] == factors[1] && factors[0] == factors[2])
            {
                int p = factors[0];

                if (isAbelian)
                {
                    if (seq[1] != graph.Order - 1) { groupID = 2; groupDescription = string.Format("Z{0} x Z{1}", p * p, p); } // Z(p^2) x Zp
                    else { groupID = 3; groupDescription = string.Format("Z{0}^3", p); } // Zp^3
                }
                else // Extra special
                {
                    if (seq[1] != graph.Order - 1) { groupID = 4; groupDescription = p == 2 ? "Q8" : string.Format("<x,y,z | x^{0} = y^{0} = z, z^{0} = 1, yx = xyz, xz = zx, yz = zy>", p); }
                    else { groupID = 5; groupDescription = string.Format("<x,y,z | x^{0} = y^{0} = z^{0} = 1, yx = xyz, xz = zx, yz = zy>", p); }
                }

                return;
            }

            // On to "Layer 2", going order by order and looking at the order sequence.
            // Layer 2 is a huge mess because I keep making changes to Layer 1
            else if (graph.Order == 12)
            {
                if (isAbelian) { groupID = 2; groupDescription = "Z6 x Z2"; return; }
                else if (seq[2] == 8) { groupID = 3; groupDescription = "A4"; return; }
                else { groupID = 4; groupDescription = "Dic3"; return; }
            }
            else if (graph.Order == 16)
            {
                if (seq[1] == 15) { groupID = 2; groupDescription = "Z2^4"; return; }
                else if (seq[1] == 11) { groupID = 3; groupDescription = "D4 x Z2"; return; }
                else if (seq[1] == 7)
                {
                    if (isAbelian) { groupID = 4; groupDescription = "Z4 x Z2^2"; return; }
                    else if (CountPowers(2) == 2) { groupID = 5; groupDescription = "<x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; return; } // 13
                    else { groupID = 6; groupDescription = "<x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zxz^-1 = xy>"; return; } // 3
                }
                else if (seq[1] == 5) { groupID = 7; groupDescription = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^3>"; return; }
                else if (seq[1] == 3)
                {
                    if (seq[2] == 4)
                    {
                        if (isAbelian) { groupID = 8; groupDescription = "Z8 x Z2"; return; }
                        else { groupID = 9; groupDescription = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^5>"; return; }
                    }
                    else
                    {
                        if (isAbelian) { groupID = 10; groupDescription = "Z4^2"; return; }
                        if (CountPowers(2) == 3) { groupID = 11; groupDescription = "<x,y | x^4 = y^4 = 1, yxy^-1 = x^3>"; return; }
                        else { groupID = 12; groupDescription = "Q8 x Z2"; return; }
                    }
                }
                else { groupID = 13; groupDescription = "Q16"; return; }
            }
            else if (graph.Order == 18)
            {
                if (isAbelian) { groupID = 2; groupDescription = "Z3^2 x Z2"; return; }
                else if (seq[1] == 3) { groupID = 3; groupDescription = "S3 x Z3"; return; }
                else { groupID = 4; groupDescription = "D(Z3^2)"; return; }
            }
            else if (graph.Order == 20)
            {
                if (isAbelian) { groupID = 2; groupDescription = "Z5 x Z2^2"; return; }
                else if (seq[1] == 1) { groupID = 3; groupDescription = "Dic5"; return; }
                else { groupID = 4; groupDescription = "<a,b | a^5 = b^4 = 1, bab^-1 = a^2>"; return; } // 3
            }
            else if (graph.Order == 24)
            {
                if (seq[1] == 15) { groupID = 2; groupDescription = "D6 x Z2"; return; }
                else if (seq[1] == 9)
                {
                    if (seq[2] == 8) { groupID = 3; groupDescription = "S4"; return; }
                    else { groupID = 4; groupDescription = "<x,y,z | x^2 = y^2 = z^3 = (yx)^4 = 1, zxz = x, yzy = z>"; return; } // 8
                }
                else if (seq[1] == 7)
                {
                    if (isAbelian) { groupID = 5; groupDescription = "Z3 x Z2^3"; return; }
                    else if (seq[2] == 8) { groupID = 6; groupDescription = "A4 x Z2"; return; }
                    else { groupID = 7; groupDescription = "S3 x Z4"; return; }
                }
                else if (seq[1] == 5) { groupID = 8; groupDescription = "D4 x Z3"; return; }
                else if (seq[1] == 3)
                {
                    if (isAbelian) { groupID = 9; groupDescription = "Z4 x Z3 x Z2"; return; }
                    else { groupID = 10; groupDescription = "Dic3 x Z2"; return; }
                }
                else
                {
                    if (seq[2] == 8) { groupID = 11; groupDescription = "SL(2,3)"; return; } // 3 (maybe use <x,y,z | x^3 = y^3 = z^3 = xyz>?)
                    else if (seq[3] == 14) { groupID = 12; groupDescription = "Dic6"; return; }
                    else if (seq[3] == 6) { groupID = 13; groupDescription = "Q8 x Z3"; return; }
                    else { groupID = 14; groupDescription = "<a,x | a^3 = x^8 = 1, axa = x> "; return; } // 1
                }
            }
            else if (graph.Order == 28)
            {
                if (seq[1] == 3) { groupID = 2; groupDescription = "Z7 x Z2^2"; return; }
                else { groupID = 3; groupDescription = "Dic7"; return; }
            }
            else if (graph.Order == 30)
            {
                if (seq[1] == 5) { groupID = 2; groupDescription = "D5 x Z3"; return; }
                else { groupID = 3; groupDescription = "S3 x Z5"; return; }
            }
            else if (graph.Order == 32) // This has "problem groups" that have identical order sequences, power sequences, centers, and derived subgroups
            {
                // Stretch goal
                if (seq[1] == 31) { groupID = 2; groupDescription = "Z2^5"; return; }
                else if (seq[1] == 23) { groupID = 3; groupDescription = "D4 x Z2^2"; return; }
                else if (seq[1] == 19)
                {
                    if (seq[2] == 4) { groupID = 4; groupDescription = "D8 x Z2"; return; }
                    else
                    {
                        if (CountPowers(2) == 2) { groupID = 5; groupDescription = "<w,x,y,z | w^2 = x^2 = y^2 = z^2 = (wx)^4 = (yxw)^2 = 1, wy = yw, wz = zw, xz = zx, (xw)^2 = (yz)^2>"; return; } // 49
#if false
                        // Gotta distinguish between 32,27 (Z2^4 : Z2) and 32,34 (Z4^2 : Z2) and this thing here is no good
                        for (int i = 0; i < graph.Order; i++) // Ugh O(n^3)
                        {
                            if (orders[i] != 4) continue;
                            for (int j = 0; j < i; j++)
                            {
                                if (orders[j] != 2) continue;
                                for (int k = 0; k < j; k++)
                                {
                                    if (op[i,j] == op[j,i] && op[i,k] == op[k,i] && op[j,k] == op[k,j]) { groupID = 6; groupDescription = "<x,y,z | x^2 = y^2 = z^2 = (zy)^2 = (yxzx)^2 = (yx)^4 = (zx)^4 = (zxy)^4 = 1>"; return; }
                                }
                            }
                        }
                        groupID = 7; groupDescription = "D(Z4^2)"; return;
#endif
                    }
                }
                else if (seq[1] == 15)
                {
                    if (seq[2] == 8) { groupID = 8; groupDescription = "<x,y,z | x^8 = y^2 = z^2 = 1, yxy^-1 = x^7, zxz^-1 = x^5>"; } // 43
                    else
                    {
                        int[] Z = GetCenter(); // The centers of the next groups are Z2^2, Z2 x Z4, and Z2^3
                        if (isAbelian) { groupID = 9; groupDescription = "Z4 x Z2^3"; }
                        if (Z.Length == 4) { groupID = 10; groupDescription = "<x,y,z | x^2 = y^4 = z^2 = (xy)^2 = (zx)^4 = 1, yz = zy, xy^2 = y^2x>"; } // 28
                        else if (Array.Exists(Z, x => orders[x] == 4)) { groupID = 11; groupDescription = "Z2 x <x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; } // 48
                        else { groupID = 12; groupDescription = "Z2 x <x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zx = xyz>"; } // 22
                    }
                    return;
                }
                else if (seq[1] == 11)
                {
                    if (seq[2] == 20)
                    {
                        int squares = CountPowers(2);
                        if (squares == 6) { groupID = 13; groupDescription = "<x,y | x^4 = y^2 = (yxy^-1x^-1)^2 = (yx)^4 = (x^2y)^4 = 1>"; return; } // 6
                        else if (squares == 2) { groupID = 14; groupDescription = "<w,x,y,z | w^2 = x^2 = y^4 = (xy)^2 = y^2z^2 = 1, wy = yw, wz = zw, xzx = z, zyz = y, ywy = xwx>"; return; } // 50
                        else if (GetCenter().Length == 8) { groupID = 15; groupDescription = "D8 x Z4"; return; }
                        // Problem groups
                        // 30 -- <x,y,z | x^2 = y^2 = z^4 = (xz)^4 = (z^2x)^2 = 1, yz = zy, (yx)^2 = z^2>
                        // 31 -- <x,y,z | x^2 = y^4 = z^4 = 1, yz = zy, xyx^-1y^1 = z^2, xzx^-1z^-1 = y^2>
                    }
                    else if (seq[2] == 12)
                    {
                        if (CountPowers(2) == 5) { groupID = 18; groupDescription = "<x,y | x^4 = y^2 = (xyx)^2 = (yx^-1yxyxyx)^2 = (xyx^-1y^-1)^4 = 1>"; } // 9
                        else if (Array.Exists(GetCenter(), x => orders[x] == 4)) { groupID = 19; groupDescription = "<x,y,z | x^2 = y^2 = z^4 = 1, xz = zx, yz = zy, (xy)^4 = z^2>"; } // 42
                        else { groupID = 20; groupDescription = "Z2 x <x,y | x^8 = y^2 = 1, yxy^-1 = x^3>"; }
                        return;
                    }
                    else { groupID = 21; groupDescription = "<x,y | y^2 = (xyx)^2 = (x^-1yxy)^2 = x^8 = 1>"; return; } // 7
                }
                else if (seq[1] == 9) { groupID = 22; groupDescription = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^7>"; return; } // 19
                else if (seq[1] == 7)
                {
                    if (seq[2] == 24)
                    {
                        int squares = CountPowers(2);
                        int[] Z = GetCenter();
                        if (isAbelian) { groupID = 23; groupDescription = "Z4^2 x Z2"; }
                        else if (squares == 2) { groupID = 24; groupDescription = "Q8 x Z2^2"; }
                        else if (Z.Length == 8 && squares == 3) { groupID = 25; groupDescription = "Z2 x <x,y | x^4 = y^4 = 1, yxy^-1 = x^3>"; } // 23
                        else if (Z.Length == 4 && squares == 3) { groupID = 26; groupDescription = "<x,y,z | x^4 = y^4 = z^4 = 1, x^2 = y^2, xy = yx, zxz = x, zyz = y>"; } // 29
                        else if (Z.Length == 4) { groupID = 27; groupDescription = "<x,y,z | x^4 = y^2 = z^4 = 1, xz = zx, xy = z^2yx, zx^2yz = y>"; } // 33
                        else if (Array.Exists(Z, x => orders[x] == 4)) { groupID = 28; groupDescription = "<x,y,z | x^4 = y^4 = z^2 = 1, xy = yx, xz = zx, zyz^1 = x^2y>"; } // 24
                        else { groupID = 29; groupDescription = "<x,y,z | x^4 = y^4 = z^2 = 1, xz = zx, yz = zy, xy = zyx>"; }
                    }
                    else if (seq[2] == 16)
                    {
                        // Maybe find another representation of Z4 Wr Z2. Wreath products are too awesome
                        if (graph.Degree == 2 || GetCenter().Length == 4) { groupID = 30; groupDescription = "Z4 Wr Z2"; }
                        else { groupID = 31; groupDescription = "<x,y,z | x^2 = z^2 = y^2(zx)^2 = 1, yz = zy, xy^2x = y^2, (xy^-1)^3 = y^-1x>"; } // 44
                    }
                    else
                    {
                        if (isAbelian) { groupID = 32; groupDescription = "Z8 x Z2^2"; return; }
                        if (CountPowers(2) == 6) { groupID = 33; groupDescription = "<x,y | x^8 = y^2 = 1, x^2y=yx^2, (xy)^2 = (yx)^2>"; } // 5
                        if (Array.Exists(GetCenter(), x => orders[x] == 8)) { groupID = 34; groupDescription = "<x,y,z | x^8 = y^2 = z^2 = 1, xy = yx, xz = zx, (yz)^2 = x^4>"; } // 38
                        else { groupID = 35; groupDescription = "Z2 x <x,y | x^8 = y^2 = 1, yxy = x^3>"; }
                    }
                    return;
                }
                else if (seq[1] == 3)
                {
                    if (seq[2] == 28)
                    {
                        if (GetCenter().Length == 8) { groupID = 36; groupDescription = "Q8 x Z4"; return; }
                        // Problem groups
                        // 32 -- <x,y,z | x^4 = y^4 = x^2z^2 = 1, xyx = y, zy = yz, xy^2 = y^2x>
                        // 35 -- <x,y,z | x^2y^2 = y^4 = z^4 = 1, yxy = zyz = x, yz = zy>
                    }
                    else if (seq[2] == 20)
                    {
                        int squares = CountPowers(2);
                        if (squares == 4) { groupID = 39; groupDescription = "Q16 x Z2"; return; }
                        else if (squares == 6) { groupID = 40; groupDescription = "<x,y | x^8 = y^4 = 1, yxy^-1 = x^3>"; return; } // 13
                        // Problem groups
                        // 10 -- <x,y | x^4 = y^4 = (x^-1y^2)^2 = (yx^2)^2 = (x^2y^2)^2 = 1, xy = yxyx^3yx>
                        // 14 -- <x,y | x^8 = y^4 = 1, xyx^-1 = x^-1>
                    }
                    else if (seq[2] == 12)
                    {
                        if (isAbelian) { groupID = 43; groupDescription = "Z8 x Z4"; }
                        if (GetCenter().Length == 2) { groupID = 44; groupDescription = "<x,y,z | x^8 = y^2 = 1, z^2 = x^4, yxy^-1 = x^5, yz = zy, ac = cab>"; } // 8
                        if (CountPowers(2) == 8) { groupID = 45; groupDescription = "<x,y | x^8 = y^4 = 1, yxy^-1 = x^5>"; } // 4
                        else { groupID = 46; groupDescription = "<x,y | x^4 = y^8 = 1, yxy^-1 = x^3>"; } // 12
                        return;
                    }
                    else if (seq[3] == 24) { groupID = 47; groupDescription = "<x,y | x^2yxyx = 1, x = y^2xy^2, x^3y^-1x = y, y^3x = xy> "; return; } // 15
                    else
                    {
                        if (isAbelian) { groupID = 48; groupDescription = "Z16 x Z2"; } // 16
                        else { groupID = 49; groupDescription = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^9>"; } // 17
                        return;
                    }
                }
                else { groupID = 50; groupDescription = "Q32"; return; }
            }
            else if (graph.Order == 36)
            {
                if (seq[1] == 19) { groupID = 2; groupDescription = "Z2 x D(Z3^2)"; } // 13
                else if (seq[1] == 15) { groupID = 3; groupDescription = "S3^2"; }
                else if (seq[1] == 9) { groupID = 4; groupDescription = ""; } // 9
                else if (seq[1] == 7) { groupID = 5; groupDescription = ""; } // 12
                else if (seq[1] == 3)
                {
                    if (seq[2] == 26) { groupID = 6; groupDescription = "Z3 x A4"; }
                    else if (seq[2] == 8) { groupID = 7; groupDescription = "Z6 x Z6"; }
                    else if (isAbelian) { groupID = 8; groupDescription = "Z18 x Z2"; }
                    else { groupID = 9; groupDescription = ""; } // 3
                }
                else
                {
                    if (seq[2] == 2) { groupID = 10; groupDescription = "Dic9"; }
                    else if (seq[3] == 18) { groupID = 11; groupDescription = ""; } // 7
                    else if (seq[3] == 6) { groupID = 12; groupDescription = ""; } // 6
                    else { groupID = 13; groupDescription = ""; } // 8
                }
                return;
            }
            else if (graph.Order == 40)
            {
                if (seq[1] == 23) { groupID = 2; groupDescription = "Z2^2 x D5"; }
                else if (seq[1] == 13) { groupID = 3; groupDescription = ""; } // 8
                else if (seq[1] == 11 && seq[2] == 20) { groupID = 4; groupDescription = ""; } // 12
                else if (seq[1] == 11) { groupID = 5; groupDescription = "Z4 x D5"; }
                else if (seq[1] == 7) { groupID = 6; groupDescription = "Z10 x Z2^2"; }
                else if (seq[1] == 5) { groupID = 7; groupDescription = "Z5 x D4"; }
                else if (seq[1] == 3 && seq[2] == 20) { groupID = 8; groupDescription = ""; } // 7
                else if (seq[1] == 3) { groupID = 9; groupDescription = "Z20 x Z2"; }
                else if (seq[2] == 22) { groupID = 10; groupDescription = ""; } // 4
                else if (seq[2] == 10) { groupID = 11; groupDescription = ""; } // 3
                else if (seq[2] == 6) { groupID = 12; groupDescription = "Z5 x Q8"; }
                else { groupID = 13; groupDescription = ""; } // 1
                return;
            }
            else if (graph.Order == 42)
            {
                if (seq[1] == 7 && seq[2] == 14) { groupID = 2; groupDescription = ""; } // 1
                else if (seq[1] == 7) { groupID = 3; groupDescription = "Z3 x D7"; }
                else if (seq[1] == 3) { groupID = 4; groupDescription = "Z7 x S3"; }
                else { groupID = 5; groupDescription = ""; } // 2
                return;
            }
            else if (graph.Order == 44)
            {
                if (isAbelian) { groupID = 2; groupDescription = "Z22 x Z2"; }
                else { groupID = 3; groupDescription = "Dic11"; }
                return;
            }
            else if (graph.Order == 45) { groupID = 1; groupDescription = "Z15 x Z3"; return; }
            else if (graph.Order == 48)
            {
                // Really stretch goal (!)
            }
            else if (graph.Order == 50)
            {
                if (isAbelian) { groupID = 2; groupDescription = "Z10 x Z5"; }
                else if (seq[1] == 25) { groupID = 3; groupDescription = "D(Z5^2)"; } // 4
                else { groupID = 4; groupDescription = ""; } // 3
                return;
            }
            else if (graph.Order == 52)
            {
                if (isAbelian) { groupID = 2; groupDescription = "Z26 x Z2"; }
                else if (seq[1] == 13) { groupID = 3; groupDescription = "<x,y | x^13 = y^4 = 1, yxy^-1 = x^5>"; } // 13
                else { groupID = 4; groupDescription = "Dic13"; }
                return;
            }
            // ...

            groupID = -1;
            groupDescription = "Can't Identify";
        }
    }
}
