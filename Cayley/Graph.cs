using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Cayley
{
    public class Graph
    {
        public const int MAX_VERTICES = 63;
        public const int MAX_GENERATORS = 9;

        private int[,] outEdges;
        private int[,] inEdges;
        private int order;
        private int degree;

        public Graph() : this(0) { }

        public Graph(int order) 
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

            this.order = order;
            degree = 0;
        }

        public int[,] OutEdges { get { return outEdges; } }
        public int[,] InEdges { get { return inEdges; } }
        public int Degree { get { return degree; } }
        public int Order { get { return order; } }

        public void AddEdge(int start, int end, int color)
        {
            if (start == end)
            {
                throw new Exception("I don't think this should happen");
            }

            if (color >= degree) degree++;
            for (int i = 0; i < order; i++)
            {
                if (outEdges[i, color] == end)
                {
                    inEdges[end, color] = -1;
                    outEdges[i, color] = -1;
                }
            }

            outEdges[start, color] = end;
            inEdges[end, color] = start;
        }

        /// <summary>
        /// Yeah this only exists for one call in GraphCanvas. I want Order to be read only.
        /// </summary>
        public void AddPoint()
        {
            order++;
        }

        /// <summary>
        /// Removes a point. It returns a boolean list of which colors were removed.
        /// </summary>
        /// <param name="point"></param>
        public bool[] RemovePoint(int point)
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

            bool[] colorsKept = new bool[degree];
            for (int i = 0; i < order; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    if (inEdges[i, j] == point) inEdges[i, j] = -1;
                    else if (inEdges[i, j] > point) inEdges[i, j]--;

                    if (outEdges[i, j] == point) outEdges[i, j] = -1;
                    else if (outEdges[i, j] > point) outEdges[i, j]--;

                    if (inEdges[i, j] != -1 || outEdges[i, j] != -1) colorsKept[j] = true;
                }
            }

            for (int i = 0; i < degree; i++)
            {
                if (!colorsKept[i])
                {
                    degree--;
                    for (int j = i; j < degree; j++)
                    {
                        for (int k = 0; k < order; k++)
                        {
                            inEdges[k, j] = inEdges[k, j + 1];
                            outEdges[k, j] = outEdges[k, j + 1];
                        }
                    }
                    
                    for (int k = 0; k < order; k++)
                    {
                        inEdges[k, degree] = -1;
                        outEdges[k, degree] = -1;
                    }
                }
            }

            return colorsKept;
        }

        public void RemoveColor(int color)
        {
            degree--;
            for(int i = 0; i < order; i++)
            {
                for (int j = color; j < degree; j++)
                {
                    outEdges[i, j] = outEdges[i, j + 1];
                    inEdges[i, j] = inEdges[i, j + 1];
                }
                outEdges[i, degree] = -1;
                inEdges[i, degree] = -1;
            }
        }

        /// <summary>
        /// Returns a pair of arrays. The first contains the color of the edge from a node's parent to it. The second contains the order at which the nodes were found.
        /// </summary>
        /// <param name="start"></param>
        /// <returns></returns>
        public Tuple<int[], int[]> BFS(int start)
        {
            int[] parents = new int[order];
            int[] d = new int[order];
            bool[] visited = new bool[order];
            for (int i = 0; i < order; i++)
            {
                parents[i] = -1;
                visited[i] = false;
            }

            Queue<int> Q = new Queue<int>();
            Q.Enqueue(start);

            int u, v, t = 1;
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
                        // 3. Homogeneity (vertex transitive?)
                        return null;
                    }
                }
            }

            return new Group(this, refer);
        }

        public void ClearAll()
        {
            for (int i = 0; i < MAX_VERTICES; i++)
            {
                for (int j = 0; j < MAX_GENERATORS; j++)
                {
                    outEdges[i, j] = -1;
                    inEdges[i, j] = -1;
                }
            }

            order = 0;
            degree = 0;
        }
    }
}
