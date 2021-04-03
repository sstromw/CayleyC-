using System;
using System.Collections.Generic;
using System.Linq;

using StatsMap = System.Collections.Generic.Dictionary<string, Cayley.AbstractGroupFactory>;

namespace Cayley
{
    public class GroupId
    {
        private Group group;
        private AbstractGroupFactory id;

        public GroupId(Group group) { this.group = group; }

        public AbstractGroupFactory Identify()
        {
            if (group.Order == 1)
                return (id = new GeneralGroup("1"));
            if (group.IsAbelian)
                return (id = new AbelianGroup(AbelianStructure()));
            if (group.Order == 6)
                return (id = new SymmetricGroup(3));
            if (group.IsDihedral)
                return (id = new DihedralGroup(group.Order / 2));
            if (group.Factors.Length == 2)
                // Non-abelian groups of order pq must have this structure (and q = 1 mod p)
                return (id = new SemidirectProduct(new CyclicGroup(group.Factors[1]), new CyclicGroup(group.Factors[0])));

            // Consider http://www.icm.tu-bs.de/ag_algebra/software/small/README
            string key = string.Join(",", group.OrderStats);
            bool success = GROUP_ID[group.Order].TryGetValue(key, out id);
            if (success) return id;
            throw new Exception("Couldn't identify");
        }

        private Dictionary<int, int[]> AbelianStructure()
        {
            Dictionary<int, int[]> structure = new Dictionary<int, int[]>();
            int[] primes = DiscreteMath.PrimeFactors(group.Order).Distinct().ToArray();
            foreach (int p in primes)
            {
                int k = group.POrderStats[p].Length;
                if (k == 2)
                {
                    // Elementary abelian
                    structure[p] = new int[] {
                        MathUtils.Multiplicity(group.POrderStats[p][1] + 1, p)
                    };
                } else if (group.POrderStats[p][k - 1] > 0) {
                    // Cyclic
                    structure[p] = new int[k - 1];
                    structure[p][k - 2] = 1;
                }
                else
                {
                    structure[p] = new int[k - 1];
                    int[] sums = new int[k];
                    int[] mults = new int[k + 1];
                    sums[0] = 1;
                    mults[0] = 0;
                    for (int i = 1; i < k; i++)
                    {
                        sums[i] = sums[i - 1] + group.POrderStats[p][i];
                        mults[i] = MathUtils.Multiplicity(sums[i], p);
                    }
                    mults[k] = mults[k - 1];
                    for (int i = 1; i < k - 1; i++)
                    {
                        structure[p][i - 1] = 2 * mults[i] - mults[i - 1] - mults[i + 1];
                    }
                }
            }
            return structure;
        }

        static Dictionary<int, StatsMap> GROUP_ID =
            new Dictionary<int, StatsMap>()
            {
                { 8, new StatsMap()
                {
                    { "1,1,6,0", new GeneralGroup("Q") },
                } },
                { 12, new StatsMap()
                {
                    { "1,1,2,6,2,0", new GeneralGroup("Dic12") }, // Change to presentation?
                    { "1,3,8,0,0,0", new AlternatingGroup(4) },
                } },
                { 16, new StatsMap()
                {
                    { "1,1,10,4,0", new GeneralGroup("Q16") },
                    { "1,3,4,8,0", new GeneralGroup("Z8:Z2") },
                    { "1,3,12,0,0", new GeneralGroup("Z4:Z4") },
                    // DISAMBIGUATE { "1,3,12,0,0", new DirectProduct(new CyclicGroup(2), new GeneralGroup("Q")) },
                    { "1,5,6,4,0", new GeneralGroup("QD16") },
                    { "1,7,8,0,0", new GeneralGroup("(Z4xZ2):Z2") },
                    // DISAMBIGUATE { "1,7,8,0,0", new GeneralGroup("(Z4xZ2):Z2") },
                    { "1,11,4,0,0", new DirectProduct(new CyclicGroup(2), new DihedralGroup(4)) },
                } },
                { 18, new StatsMap()
                {
                    { "1,3,8,6,0,0", new DirectProduct(new CyclicGroup(3), new SymmetricGroup(3)) },
                    { "1,9,8,0,0,0", new GeneralGroup("(Z3^2):Z2") },
                } },
                { 20, new StatsMap()
                {
                    { "1,1,10,4,4,0", new GeneralGroup("Z5:Z4") },
                    { "1,5,10,4,0,0", new GeneralGroup("Z5:Z4") },
                } },
                { 21, new StatsMap()
                {
                    { "1,14,6,0", new GeneralGroup("Z7:Z3") },
                } },
                { 24, new StatsMap()
                {
                    { "1,1,2,2,2,12,4,0", new GeneralGroup("Z3:Z8") },
                    { "1,1,2,6,2,0,12,0", new DirectProduct(new CyclicGroup(3), new GeneralGroup("Q")) },
                    { "1,1,2,14,2,0,4,0", new GeneralGroup("Z3:Q") },
                    { "1,1,8,6,8,0,0,0", new GeneralGroup("SL(2,3)") },
                    { "1,3,2,12,6,0,0,0", new DirectProduct(new CyclicGroup(2), new GeneralGroup("Z3:Z4")) },
                    { "1,5,2,2,10,0,4,0", new DirectProduct(new CyclicGroup(3), new DihedralGroup(4)) },
                    { "1,7,2,8,2,0,4,0", new DirectProduct(new CyclicGroup(4), new DihedralGroup(3)) },
                    { "1,7,8,0,8,0,0,0", new DirectProduct(new CyclicGroup(2), new AlternatingGroup(4)) },
                    { "1,9,2,6,6,0,0,0", new GeneralGroup("(Z6xZ2):Z2") },
                    { "1,9,8,6,0,0,0,0", new SymmetricGroup(4) },
                } },
                { 27, new StatsMap()
                {
                    { "1,8,18,0", new GeneralGroup("Z9:Z3") },
                    { "1,26,0,0", new GeneralGroup("(Z3xZ3):Z3") },
                } },
                { 28, new StatsMap()
                {
                    { "1,1,14,6,6,0", new GeneralGroup("Z7:Z4") },
                } },
                { 30, new StatsMap()
                {
                    { "1,3,2,4,0,12,8,0", new DirectProduct(new CyclicGroup(5), new DihedralGroup(3)) },
                    { "1,5,2,4,10,0,8,0", new DirectProduct(new CyclicGroup(3), new DihedralGroup(10)) },
                } },
                { 32, new StatsMap()
                {
                    // *sobs*
                } },
            };
    }
}
