using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Cayley
{
    public class Graph
    {
        public const int MAX_VERTICES = 63;
        public const int MAX_GENERATORS = 5;

        private int[,] outEdges;
        private int[,] inEdges;
        private Brush[] colors;
        private int order;
        private int degree;

        public Graph()
        {
            outEdges = new int[MAX_VERTICES, MAX_GENERATORS];
            inEdges = new int[MAX_VERTICES, MAX_GENERATORS];
            for (int i = 0; i < MAX_VERTICES; i++)
            {
                for (int j = 0; j < MAX_GENERATORS; j++)
                {
                    outEdges[i,j] = -1;
                    inEdges[i, j] = -1;
                }
            }

            colors = new Brush[MAX_GENERATORS];
            order = 0;
            degree = 0;
        }

        public int[,] OutEdges
        {
            get { return outEdges; }
            set { OutEdges = value; }
        }

        public int[,] InEdges
        {
            get { return inEdges; }
            set { InEdges = value; }
        }

        public Brush[] Colors
        {
            get { return colors; }
            set { colors = value; }
        }

        public int Order
        {
            get { return order; }
            set { order = value; }
        }

        public int Degree
        {
            get { return degree; }
            set { degree = value; }
        }

        internal void ClearAll()
        {
            for (int i = 0; i < MAX_VERTICES; i++)
            {
                for (int j = 0; j < MAX_GENERATORS; j++)
                {
                    outEdges[i, j] = -1;
                    inEdges[i, j] = -1;
                }
            }

            colors = new Brush[MAX_GENERATORS];
            order = 0;
            degree = 0;
        }

        public void AddEdge(int start, int end, Brush color)
        {
            int j;
            for (j = 0; colors[j] != color && j < degree; j++);
            if (j == degree)
            {
                colors[degree++] = color;
            }

            for (int i = 0; i < order; i++)
            {
                if (outEdges[i, j] == end)
                {
                    inEdges[end, j] = -1;
                    outEdges[i, j] = -1;
                }
            }

            outEdges[start, j] = end;
            inEdges[end, j] = start;
        }

        public void RemovePoint(int point)
        {
            for (int i = point; i < order - 1; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    inEdges[i, j] = inEdges[i + 1, j];
                    outEdges[i, j] = outEdges[i + 1, j];
                }
            }
            for (int j = 0; j < degree; j++)
            {
                inEdges[order - 1, j] = -1;
                outEdges[order - 1, j] = -1;
            }

            order--;

            bool[] count = new bool[degree];
            for (int i = 0; i < order; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    if (inEdges[i, j] == point) inEdges[i, j] = -1;
                    else if (inEdges[i, j] > point) inEdges[i, j]--;

                    if (outEdges[i, j] == point) outEdges[i, j] = -1;
                    else if (outEdges[i, j] > point) outEdges[i, j]--;

                    if (!count[j] && (inEdges[i, j] != -1 || outEdges[i, j] != -1)) count[j] = true;
                }
            }

            for (int i = 0; i < degree; i++)
            {
                if (!count[i])
                {
                    degree--;
                    for (int j = i; j < degree; j++)
                    {
                        colors[i] = colors[i + 1];
                        for (int k = 0; k < order; k++)
                        {
                            inEdges[k, j] = inEdges[k, j + 1];
                            outEdges[k, j] = outEdges[k, j + 1];
                        }
                    }

                    colors[degree] = null;
                    for (int k = 0; k < order; k++)
                    {
                        inEdges[k, degree] = -1;
                        outEdges[k, degree] = -1;
                    }
                }
            }
        }

        public Tuple<int[], int[]> BFS(int start)
        {
            int[] parents = new int[order];
            int[] d = new int[order];
            bool[] visited = new bool[order];
            for (int i = 0; i < order; i++)
            {
                parents[i] = -1;
                d[i] = 0;
                visited[i] = false;
            }

            Queue<int> Q = new Queue<int>();
            Q.Enqueue(start);

            int u, v, t = 0;
            while (Q.Count > 0)
            {
                u = Q.Dequeue();
                for (int i = 0; i < degree; i++)
                {
                    v = outEdges[u, i];
                    if (v != -1 && !visited[v])
                    {
                        visited[v] = true;
                        d[v] = t++;
                        parents[v] = i;
                        Q.Enqueue(v);
                    }
                }
                visited[u] = true;
            }

            return new Tuple<int[],int[]>(parents, d);
        }

        public Group ToCayleyGraph()
        {
            if (order == 1)
            {
                return new Group(this);
            }

            if (order == 0 || degree == 0)
            {
                return null;
            }

            for (int i = 0; i < order; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    if (outEdges[i,j] == -1)
                    {
                        // 1. Regularity 
                        return null;
                    }
                }
            }

            Tuple<int[], int[]> refer = BFS(0);
            Tuple<int[], int[]> compare;

            for (int i = 1; i < order; i++)
            {
                compare = BFS(i);
                for (int j = 0; j < order; j++)
                {
                    if (j != i && compare.Item1[j] == -1) {
                        // 2. Connectedness
                        return null;
                    }

                    int k = 0;
                    for (; k < order && (compare.Item1[j] != refer.Item1[k] || compare.Item2[j] != refer.Item2[k]); k++);
                    if (k == order)
                    {
                        // 3. Homogeneity (this is broken!)
                        return null;
                    }
                }
            }

            return new Group(this, refer);
        }
    }
}
