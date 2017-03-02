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
        private string description;

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
            SetGroupDescription();
        }

        public int Order { get { return graph.Order; } }
        public string GroupDescription { get { return description; } }

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
        private void SetGroupDescription()
        {
            // The order sequence divides the groups into small classes.
            int[] seq = new int[divisors.Length];
            for (int i = 0; i < divisors.Length; i++) seq[i] = orders.Count(x => x == divisors[i]);
            
            // "Layer 0" abelian groups
            if (seq[divisors.Length - 1] > 0) { description = "Z" + graph.Order; }
            else if (isAbelian)
            {
                // Based on this proof https://groupprops.subwiki.org/wiki/Finite_abelian_groups_with_the_same_order_statistics_are_isomorphic
                int[] primes = factors.Distinct().ToArray();
                description = "";
                
                int[] t = new int[divisors.Length + 1];
                int i, j, k, partialSum, pPower, count;
                foreach (int p in primes)
                {
                    i = 0; partialSum = 0; pPower = 1; count = 0;
                    while (i < divisors.Length)
                    {
                        t[count++] = (partialSum += seq[i]);
                        pPower *= p;
                        while (i < divisors.Length && divisors[i] != pPower) i++;
                    }
                    t[count] = t[count - 1];

                    // Check for Z(p^n) and (Zp)^n
                    int ip = Array.FindIndex(divisors, x => x == p);
                    if (seq[ip] == p - 1) { description += string.Format("Z{0} x ", pPower / p); }
                    else if (seq[ip] == pPower - 1) { description += string.Format("Z{0}^{1} x ", p, count); }
                    else // Rest of the cases
                    {
                        for (i = 0; i < count; i++)
                        {
                            j = 0;
                            while (t[i] > 1) { t[i] /= p; j++; }
                            t[i] = j;
                        }
                        for (i = 1; i < count - 1; i++)
                        {
                            k = 2 * t[i] - t[i - 1] - t[i + 1];
                            if (k == 1) { description += string.Format("Z{0} x ", DiscreteMath.Pow(p, i)); }
                            else if (k > 1) { description += string.Format("Z{0}^{1} x ", DiscreteMath.Pow(p, i), k); }
                        }
                    }
                }

                description = description.Substring(0, description.Length - 3);
            }

            // The decision tree (mostly) looks at order sequence followed by power statistics.
            // But the first four cases look for general classes of groups. It's a little bit of Hölder's classification.
            // This is "Layer 1" http://www.icm.tu-bs.de/ag_algebra/software/small/README
            // But we only check cases p^2, pq, p^3. The other cases don't agree with the rest of the design.

            else if (factors.Length == 2) // A001358
            {
                int p = factors[0], q = factors[1], n;
                
                if (factors[0] == 2) { description = (graph.Order == 6 ? "S" : "D") + factors[1]; }
                if (factors[1] % factors[0] == 1) { for (n = 2; DiscreteMath.ModPow(n, p, q) != 1; n++) ; description = string.Format("<x,y | x^{0} = y^{1} = 1, yxy^-1 = x^{2}>", q, p, n); } // <x,y | x^q = y^p = 1, yxy^-1 = x^n> "q:p"
            }
            else if (factors.Length == 3 && factors[0] == factors[1] && factors[0] == factors[2] && factors[0] % 2 == 1)
            {
                int p = factors[0];

                // Extra special
                if (seq[1] != graph.Order - 1) { description = string.Format("<x,y,z | x^{0} = y^{0} = z, z^{0} = 1, yx = xyz, xz = zx, yz = zy>", p); }
                else { description = string.Format("<x,y,z | x^{0} = y^{0} = z^{0} = 1, yx = xyz, xz = zx, yz = zy>", p); }
            }

            // On to "Layer 2", going order by order and looking at the order sequence.
            // Layer 2 is a huge mess because I keep making changes to Layer 1
            else if (graph.Order == 8)
            {
                if (seq[1] == 5) { description = "D8"; }
                else { description = "Q8"; }
            }
            else if (graph.Order == 12)
            {
                if (seq[1] == 7) { description = "D6"; }
                if (seq[1] == 3) { description = "A4"; }
                else { description = "Dic3"; }
            }
            else if (graph.Order == 16)
            {
                if (seq[1] == 11) { description = "D4 x Z2"; }
                else if (seq[1] == 9) { description = "D8"; }
                else if (seq[1] == 7)
                {
                    if (CountPowers(2) == 2) { description = "<x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; } // 13
                    else { description = "<x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zxz^-1 = xy>"; } // 3
                }
                else if (seq[1] == 5) { description = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^3>"; }
                else if (seq[1] == 3)
                {
                    if (seq[2] == 4) { description = "<x,y | x^8 = y^2 = 1, yxy^-1 = x^5>"; }
                    else
                    {
                        if (CountPowers(2) == 3) { description = "<x,y | x^4 = y^4 = 1, yxy^-1 = x^3>"; }
                        else { description = "Q8 x Z2"; }
                    }
                }
                else { description = "Q16"; }
            }
            else if (graph.Order == 18)
            {
                if (seq[1] == 9) { description = "D9"; }
                if (seq[1] == 3) { description = "S3 x Z3"; }
                else { description = "D(Z3^2)"; }
            }
            else if (graph.Order == 20)
            {
                if (seq[1] == 11) { description = "D10"; }
                if (seq[1] == 1) { description = "Dic5"; }
                else { description = "<a,b | a^5 = b^4 = 1, bab^-1 = a^2>"; } // 3
            }
            else if (graph.Order == 24)
            {
                if (seq[1] == 15) { description = "D6 x Z2"; }
                if (seq[1] == 13) { description = "D12"; }
                else if (seq[1] == 9)
                {
                    if (seq[2] == 8) { description = "S4"; }
                    else { description = "<x,y,z | x^2 = y^2 = z^3 = (yx)^4 = 1, zxz = x, yzy = z>"; } // 8
                }
                else if (seq[1] == 7)
                {
                    if (seq[2] == 8) { description = "A4 x Z2"; }
                    else { description = "S3 x Z4"; }
                }
                else if (seq[1] == 5) { description = "D4 x Z3"; }
                else if (seq[1] == 3) { description = "Dic3 x Z2"; }
                else
                {
                    if (seq[2] == 8) { description = "SL(2,3)"; } // 3 (maybe use <x,y,z | x^3 = y^3 = z^3 = xyz>?)
                    else if (seq[3] == 14) { description = "Dic6"; }
                    else if (seq[3] == 6) { description = "Q8 x Z3"; }
                    else { description = "<a,x | a^3 = x^8 = 1, axa = x> "; } // 1
                }
            }
            else if (graph.Order == 28)
            {
                if (seq[1] == 15) { description = "D14"; }
                else { description = "Dic7"; }
            }
            else if (graph.Order == 30)
            {
                if (seq[1] == 15) { description = "D15"; }
                else if (seq[1] == 5) { description = "D5 x Z3"; }
                else { description = "S3 x Z5"; }
            }
            else if (graph.Order == 32) // This has "problem groups" that have identical order sequences, power sequences, centers, and derived subgroups
            {
                // Stretch goal
                if (seq[1] == 23) { description = "D4 x Z2^2"; }
                else if (seq[1] == 19)
                {
                    if (seq[2] == 4) { description = "D8 x Z2"; }
                    else
                    {
                        if (CountPowers(2) == 2) { description = "<w,x,y,z | w^2 = x^2 = y^2 = z^2 = (wx)^4 = (yxw)^2 = 1, wy = yw, wz = zw, xz = zx, (xw)^2 = (yz)^2>"; } // 49
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
                                    if (op[i,j] == op[j,i] && op[i,k] == op[k,i] && op[j,k] == op[k,j]) { groupDescription = "<x,y,z | x^2 = y^2 = z^2 = (zy)^2 = (yxzx)^2 = (yx)^4 = (zx)^4 = (zxy)^4 = 1>"; }
                                }
                            }
                        }
                        groupDescription = "D(Z4^2)"
#endif
                    }
                }
                if (seq[1] == 17) { description = "D16"; }
                else if (seq[1] == 15)
                {
                    if (seq[2] == 8) { description = "<x,y,z | x^8 = y^2 = z^2 = 1, yxy^-1 = x^7, zxz^-1 = x^5>"; } // 43
                    else
                    {
                        int[] Z = GetCenter(); // The centers of the next groups are Z2^2, Z2 x Z4, and Z2^3
                        if (Z.Length == 4) { description = "<x,y,z | x^2 = y^4 = z^2 = (xy)^2 = (zx)^4 = 1, yz = zy, xy^2 = y^2x>"; } // 28
                        else if (Array.Exists(Z, x => orders[x] == 4)) { description = "Z2 x <x,y,z | x^4 = y^2 = (yx)^2 = 1, x^2 = z^2, xz = zx, yz = zy>"; } // 48
                        else { description = "Z2 x <x,y,z | x^4 = y^2 = z^2 = 1, xy = yx, yz = zy, zx = xyz>"; } // 22
                    }
                }
                else if (seq[1] == 11)
                {
                    if (seq[2] == 20)
                    {
                        int squares = CountPowers(2);
                        if (squares == 6) { description = "<x,y | x^4 = y^2 = (yxy^-1x^-1)^2 = (yx)^4 = (x^2y)^4 = 1>"; } // 6
                        else if (squares == 2) { description = "<w,x,y,z | w^2 = x^2 = y^4 = (xy)^2 = y^2z^2 = 1, wy = yw, wz = zw, xzx = z, zyz = y, ywy = xwx>"; } // 50
                        else if (GetCenter().Length == 8) { description = "D8 x Z4"; }
                        // Problem groups
                        // 30 -- <x,y,z | x^2 = y^2 = z^4 = (xz)^4 = (z^2x)^2 = 1, yz = zy, (yx)^2 = z^2>
                        // 31 -- <x,y,z | x^2 = y^4 = z^4 = 1, yz = zy, xyx^-1y^1 = z^2, xzx^-1z^-1 = y^2>
                    }
                    else if (seq[2] == 12)
                    {
                        if (CountPowers(2) == 5) { description = "<x,y | x^4 = y^2 = (xyx)^2 = (yx^-1yxyxyx)^2 = (xyx^-1y^-1)^4 = 1>"; } // 9
                        else if (Array.Exists(GetCenter(), x => orders[x] == 4)) { description = "<x,y,z | x^2 = y^2 = z^4 = 1, xz = zx, yz = zy, (xy)^4 = z^2>"; } // 42
                        else { description = "Z2 x <x,y | x^8 = y^2 = 1, yxy^-1 = x^3>"; }
                    }
                    else { description = "<x,y | y^2 = (xyx)^2 = (x^-1yxy)^2 = x^8 = 1>"; } // 7
                }
                else if (seq[1] == 9) { description = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^7>"; } // 19
                else if (seq[1] == 7)
                {
                    if (seq[2] == 24)
                    {
                        int squares = CountPowers(2);
                        int[] Z = GetCenter();
                        //if (isAbelian) { groupDescription = "Z4^2 x Z2"; }
                        if (squares == 2) { description = "Q8 x Z2^2"; }
                        else if (Z.Length == 8 && squares == 3) { description = "Z2 x <x,y | x^4 = y^4 = 1, yxy^-1 = x^3>"; } // 23
                        else if (Z.Length == 4 && squares == 3) { description = "<x,y,z | x^4 = y^4 = z^4 = 1, x^2 = y^2, xy = yx, zxz = x, zyz = y>"; } // 29
                        else if (Z.Length == 4) { description = "<x,y,z | x^4 = y^2 = z^4 = 1, xz = zx, xy = z^2yx, zx^2yz = y>"; } // 33
                        else if (Array.Exists(Z, x => orders[x] == 4)) { description = "<x,y,z | x^4 = y^4 = z^2 = 1, xy = yx, xz = zx, zyz^1 = x^2y>"; } // 24
                        else { description = "<x,y,z | x^4 = y^4 = z^2 = 1, xz = zx, yz = zy, xy = zyx>"; }
                    }
                    else if (seq[2] == 16)
                    {
                        // Maybe find another representation of Z4 Wr Z2. Wreath products are too awesome
                        if (graph.Degree == 2 || GetCenter().Length == 4) { description = "Z4 Wr Z2"; }
                        else { description = "<x,y,z | x^2 = z^2 = y^2(zx)^2 = 1, yz = zy, xy^2x = y^2, (xy^-1)^3 = y^-1x>"; } // 44
                    }
                    else
                    {
                        //if (isAbelian) { groupDescription = "Z8 x Z2^2"; }
                        if (CountPowers(2) == 6) { description = "<x,y | x^8 = y^2 = 1, x^2y=yx^2, (xy)^2 = (yx)^2>"; } // 5
                        else if (Array.Exists(GetCenter(), x => orders[x] == 8)) { description = "<x,y,z | x^8 = y^2 = z^2 = 1, xy = yx, xz = zx, (yz)^2 = x^4>"; } // 38
                        else { description = "Z2 x <x,y | x^8 = y^2 = 1, yxy = x^3>"; }
                    }
                }
                else if (seq[1] == 3)
                {
                    if (seq[2] == 28)
                    {
                        if (GetCenter().Length == 8) { description = "Q8 x Z4"; }
                        // Problem groups
                        // 32 -- <x,y,z | x^4 = y^4 = x^2z^2 = 1, xyx = y, zy = yz, xy^2 = y^2x>
                        // 35 -- <x,y,z | x^2y^2 = y^4 = z^4 = 1, yxy = zyz = x, yz = zy>
                    }
                    else if (seq[2] == 20)
                    {
                        int squares = CountPowers(2);
                        if (squares == 4) { description = "Q16 x Z2"; }
                        else if (squares == 6) { description = "<x,y | x^8 = y^4 = 1, yxy^-1 = x^3>"; } // 13
                        // Problem groups
                        // 10 -- <x,y | x^4 = y^4 = (x^-1y^2)^2 = (yx^2)^2 = (x^2y^2)^2 = 1, xy = yxyx^3yx>
                        // 14 -- <x,y | x^8 = y^4 = 1, xyx^-1 = x^-1>
                    }
                    else if (seq[2] == 12)
                    {
                        //if (isAbelian) { groupDescription = "Z8 x Z4"; }
                        if (GetCenter().Length == 2) { description = "<x,y,z | x^8 = y^2 = 1, z^2 = x^4, yxy^-1 = x^5, yz = zy, ac = cab>"; } // 8
                        else if (CountPowers(2) == 8) { description = "<x,y | x^8 = y^4 = 1, yxy^-1 = x^5>"; } // 4
                        else { description = "<x,y | x^4 = y^8 = 1, yxy^-1 = x^3>"; } // 12
                    }
                    else if (seq[3] == 24) { description = "<x,y | x^2yxyx = 1, x = y^2xy^2, x^3y^-1x = y, y^3x = xy> "; } // 15
                    else if (isAbelian) { description = "Z16 x Z2"; }
                    else { description = "<x,y | x^16 = y^2 = 1, yxy^-1 = x^9>"; } // 17
                }
                else { description = "Q32"; }
            }
            else if (graph.Order == 36)
            {
                if (seq[1] == 19)
                {
                    if (seq[2] == 8) { description = "Z2 x D(Z3^2)"; } // 13
                    else { description = "D18"; }
                }
                else if (seq[1] == 15) { description = "S3^2"; }
                else if (seq[1] == 9) { description = "unnamed"; } // 9
                else if (seq[1] == 7) { description = "unnamed"; } // 12
                else if (seq[1] == 3)
                {
                    if (seq[2] == 26) { description = "Z3 x A4"; }
                    else { description = "unnamed"; } // 3
                }
                else
                {
                    if (seq[2] == 2) { description = "Dic9"; }
                    else if (seq[3] == 18) { description = "<x,y,z | x^3 = y^3 = z^4 = 1, xy = yx, zxz^-1 = x^-1, zyz^-1 = y^-1>"; } // 7
                    else if (seq[3] == 6) { description = "<x,y,z | x^3 = y^4 = z^3 = 1, yxy^-1 = x^-1, xz = zx, yz = zy>"; } // 6
                    else { description = "unnamed"; } // 8
                }
            }
            else if (graph.Order == 40)
            {
                if (seq[1] == 23) { description = "Z2^2 x D5"; }
                else if (seq[1] == 21) { description = "D20"; }
                else if (seq[1] == 13) { description = "unnamed"; } // 8
                else if (seq[1] == 11 && seq[2] == 20) { description = "unnamed"; } // 12
                else if (seq[1] == 11) { description = "Z4 x D5"; }
                else if (seq[1] == 5) { description = "Z5 x D4"; }
                else if (seq[1] == 3 && seq[2] == 20) { description = "unnamed"; } // 7
                else if (seq[2] == 22) { description = "unnamed"; } // 4
                else if (seq[2] == 10) { description = "unnamed"; } // 3
                else if (seq[2] == 6) { description = "Z5 x Q8"; }
                else { description = "unnamed"; } // 1
            }
            else if (graph.Order == 42)
            {
                if (seq[1] == 21) { description = "D21"; }
                else if (seq[1] == 7)
                {
                    if (seq[2] == 14) { description = "unnamed"; } // 1
                    else { description = "Z3 x D7"; }
                }
                else if (seq[1] == 3) { description = "Z7 x S3"; }
                else { description = "unnamed"; } // 2
            }
            else if (graph.Order == 44)
            {
                if (seq[1] == 23) { description = "D22"; }
                else { description = "Dic11"; }
            }
            else if (graph.Order == 48)
            {
                // Really stretch goal (!)
            }
            else if (graph.Order == 50)
            {
                if (seq[1] == 25)
                {
                    if (seq[2] == 24) { description = "D(Z5^2)"; } // 4
                    else { description = "D25"; }
                }
                else { description = "unnamed"; } // 3
            }
            else if (graph.Order == 52)
            {
                if (seq[1] == 27) { description = "D26"; }
                if (seq[1] == 13) { description = "<x,y | x^13 = y^4 = 1, yxy^-1 = x^5>"; } // 13
                else { description = "Dic13"; }
            }
            // ...

            if (description == null) { description = "Can't Identify"; }
        }
    }
}
