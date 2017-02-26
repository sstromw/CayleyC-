using System;
using System.IO;

namespace Cayley
{
#if SAVE_DATA
    // I really gotta make this class better
    class DataReader
    {
        private static string filename = "found_groups.dat";
        public static ushort[] GroupCounts = { 1, 1, 1, 2, 1, 2, 1, 5, 2, 2, 1, 5, 1, 2, 1, 14, 1, 5, 1, 5, 2, 2, 1, 15, 2, 2, 5, 4, 1, 4, 1, 51, 1, 2, 1, 14, 1, 2, 2, 14, 1, 6, 1, 4, 2, 2, 1, 52, 2, 5, 1, 5, 1, 15, 2, 13, 2, 2, 1, 13, 1, 2, 4, 267, 1, 4, 1, 5, 1, 4, 1, 50, 1, 2, 3, 4, 1, 6, 1, 52, 15, 2, 1, 15, 1, 2, 1, 12, 1, 10, 1, 4, 2 };
        
        ulong[] groups;

        public DataReader()
        {
            if (File.Exists(filename))
            {
                if (File.ReadAllLines(filename).Length != Graph.MAX_VERTICES)
                {
                    throw new Exception("The file is wrong");
                }
                groups = Array.ConvertAll(File.ReadAllLines(filename), b => ulong.Parse(b));
            }
            else
            {
                File.Create(filename);
                groups = new ulong[Graph.MAX_VERTICES];
            }
        }

        ~DataReader()
        {
            File.WriteAllLines(filename, Array.ConvertAll(groups, u => u.ToString()));
        }

        public int GetCount(int order)
        {
            int i;
            ulong n = groups[order - 1];
            for (i = 0; n > 0; i++) n &= (n - 1);
            return i;
        }

        public bool IsGroupFound(int order, int id)
        {
            if (id < 0 || id > GroupCounts[order - 1])
            {
                throw new ArgumentException("Invalid group ID");
            }
            return ((groups[order - 1] >> id) & 0x1) == 1;
        }

        public void AddGroup(int order, int id)
        {
            groups[order - 1] |= (ulong)0x1 << id;
        }
    }
#endif
}