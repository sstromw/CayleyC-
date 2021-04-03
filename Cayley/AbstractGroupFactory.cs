using System;
using System.Collections.Generic;

namespace Cayley
{
    public abstract class AbstractGroupFactory
    {
        public abstract override string ToString();
    }

    public class GeneralGroup : AbstractGroupFactory
    {
        private string descriptor;
        public GeneralGroup(string d) { descriptor = d; }
        public override string ToString() { return descriptor; }
    }

    public class Presentation : AbstractGroupFactory
    {
        private char[] generators;
        private string[] relations;
        public Presentation(char[] generators, string[] relations)
        {
            this.generators = generators;
            this.relations = relations;
            if (!Validate()) throw new Exception("Invalid presentation");
        }

        private bool Validate()
        {
            // TODO
            return false;
        }

        public override string ToString()
        {
            return string.Format("<{0} | {1}>",
                string.Join(",", generators),
                string.Join(",", relations));
        }
    }

    public class DirectProduct : AbstractGroupFactory
    {
        private AbstractGroupFactory[] groups;

        public DirectProduct(params AbstractGroupFactory[] groups) { this.groups = groups; }

        public override string ToString()
        {
            return string.Join(" × ", groups.ToString());
        }
    }

    public class SemidirectProduct : AbstractGroupFactory
    {
        private AbstractGroupFactory[] groups;

        public SemidirectProduct(params AbstractGroupFactory[] groups) { this.groups = groups; }

        public override string ToString()
        {
            return string.Join(" ⋊ ", groups.ToString());
        }
    }

    public class AbelianGroup : AbstractGroupFactory
    {
        private Dictionary<int, int[]> factors;

        public AbelianGroup(Dictionary<int, int[]> factors) { this.factors = factors; }

        public override string ToString()
        {
            string product = "";
            foreach (KeyValuePair<int, int[]> kv in factors)
            {
                int n = kv.Key;
                for (int i = 0; i < kv.Value.Length; i++)
                {
                    // TODO subscripts
                    if (kv.Value[i] > 0)
                    {
                        if (product != "") product += " × ";
                        product += string.Format("ℤ{0}", n);
                    }
                    if (kv.Value[i] > 1) product += string.Format("^{0}", kv.Value[i]);
                    n *= kv.Key;
                }
            }
            return product;
        }
    }

    public abstract class GroupSequence : AbstractGroupFactory
    {
        protected char identifier;
        private int index;
        public GroupSequence(int index) { this.index = index; }

        public override string ToString() { return identifier + index.ToString(); }
    }

    public class CyclicGroup : GroupSequence { public CyclicGroup(int index) : base(index) { identifier = 'ℤ'; } }
    public class SymmetricGroup : GroupSequence { public SymmetricGroup(int index) : base(index) { identifier = 'S'; } }
    public class AlternatingGroup : GroupSequence { public AlternatingGroup(int index) : base(index) { identifier = 'A'; } }
    public class DihedralGroup : GroupSequence { public DihedralGroup(int index) : base(index) { identifier = 'D'; } }
}
