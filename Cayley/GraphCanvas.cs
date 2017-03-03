using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Cayley
{
    class GraphCanvas : Canvas
    {
        private const double BUBBLE_DIST = 25.0;

        private Point[] vertices;
        private Brush[] colors;
        private int nearbyVertex;
        private int focusedVertex;
        private Brush edgeColor;
        private Graph graph;

        private Ellipse previewPoint = new Ellipse() { Visibility = Visibility.Hidden, Width = 12, Height = 12 };
        private Line previewLine = new Line() { Visibility = Visibility.Hidden, StrokeThickness = 2 };

        public GraphCanvas()
        {
            vertices = new Point[Graph.MAX_VERTICES];
            colors = new Brush[Graph.MAX_GENERATORS];
            edgeColor = Brushes.Black;
            graph = new Graph();

            Children.Add(previewPoint);
            Children.Add(previewLine);
            SetZIndex(previewPoint, 0);
            SetZIndex(previewLine, 0);

            nearbyVertex = focusedVertex = -1;

            // Mouse events
            //   Right click to add a point
            //   Left click and drag to draw a line
            MouseLeftButtonDown += StartLine;
            MouseLeftButtonUp += EndLine;
            MouseRightButtonDown += AddRemovePoint;
            MouseMove += DrawPreviewShapes;
        }

        public Graph Graph { get { return graph; } }
        public Point[] Vertices { get { return vertices; } }
        public Brush[] Colors { get { return colors; } }

        public Brush EdgeColor
        {
            get { return edgeColor; }
            set { edgeColor = value; }
        }
        
        /// <summary>
        /// Add concentric polygons to the graph. Quick way to add lots of points neatly.
        /// </summary>
        public void AddShape(int nPolygons, int nPointsOnPolygons)
        {
            if (nPolygons * nPointsOnPolygons > Graph.MAX_VERTICES) return;
            if (Children.Count > 2) ClearAll();

            double D = Math.Min(ActualHeight, ActualWidth);
            double rmin, rmax, h;
            if (nPolygons == 1)
            {
                rmin = 0.4 * D;
                rmax = rmin + 0.5;
                h = 1.0;
            }
            else
            {
                rmin = 0.2 * D;
                rmax = 0.45 * D;
                h = (rmax - rmin) / (nPolygons - 1);
            }

            double x, y, r = rmin;
            while (r < rmax + 0.1)
            {
                for (int i = 0; i < nPointsOnPolygons; i++)
                {
                    x = ActualWidth / 2 + r * Math.Cos(2 * Math.PI * i / nPointsOnPolygons);
                    y = ActualHeight / 2 + r * Math.Sin(2 * Math.PI * i / nPointsOnPolygons);
                    AddRemovePoint(new Point(x, y));
                }
                r += h;
            }
        }

        /// <summary>
        /// This handles the right click, passes immediately to the other overload
        /// </summary>
        public void AddRemovePoint(object sender, MouseButtonEventArgs e)
        {
            AddRemovePoint(e.GetPosition(this));
        }

        /// <summary>
        /// Adds or removes a point at a Point
        /// </summary>
        public void AddRemovePoint(Point pos)
        {
            if (nearbyVertex != -1)
            {
                // Remove point
                previewPoint.Visibility = Visibility.Hidden;
                Point q = vertices[nearbyVertex];

                // First, remove from the visual tree
                int i;
                for (i = 2; i < Children.Count; i++)
                {
                    Ellipse e = Children[i] as Ellipse;
                    if (e != null && GetLeft(e) + 4 == q.X && GetTop(e) + 4 == q.Y)
                    {
                        Children.Remove(e);
                        break;
                    }
                }
                for (i = 2; i < Children.Count; i++)
                {
                    Line l = Children[i] as Line;
                    if (l != null && ((l.X1 == q.X && l.Y1 == q.Y) || (l.X2 == q.X && l.Y2 == q.Y)))
                    {
                        Children.RemoveRange(i, 3);
                        i--;
                    }
                }
                for (i = nearbyVertex; i < graph.Order - 1; i++)
                {
                    vertices[i] = vertices[i + 1];
                }
                vertices[graph.Order - 1] = new Point();

                // Send call to graph to remove from its data
                bool[] colorsKept = graph.RemovePoint(nearbyVertex);
                
                // Fix the colors array
                int j = 0;
                Brush[] newColors = new Brush[Graph.MAX_GENERATORS];
                for (i = 0; i < colorsKept.Length; i++)
                {
                    if (colorsKept[i])
                    {
                        newColors[j++] = colors[i];
                    }
                }
                colors = newColors;
            }
            else if (graph.Order < Graph.MAX_VERTICES)
            {
                // Add point
                Ellipse newPoint = new Ellipse() { Width = 8, Height = 8, Fill = Brushes.Black };
                SetZIndex(newPoint, 2);
                SetLeft(newPoint, pos.X - 4);
                SetTop(newPoint, pos.Y - 4);
                Children.Add(newPoint);

                vertices[graph.Order] = pos;
                graph.AddPoint();
            }
        }

        public void RemoveColor(Brush brush)
        {
            int i = 0;
            for (; i < graph.Degree && colors[i] != brush; i++);
            if (i != graph.Degree)
            {
                graph.RemoveColor(i);
                for (; i < Graph.MAX_GENERATORS - 1; i++) colors[i] = colors[i + 1];

                for (i = 2; i < Children.Count; i++)
                {
                    Line l = Children[i] as Line;
                    if (l != null && l.Stroke == brush)
                    {
                        Children.RemoveRange(i, 3);
                        i--;
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for left click. If applicable, it begins a line at the nearby vertex.
        /// </summary>
        private void StartLine(object sender, MouseButtonEventArgs e)
        {
            if (nearbyVertex == -1) return;

            focusedVertex = nearbyVertex;
            previewLine.X1 = vertices[focusedVertex].X;
            previewLine.Y1 = vertices[focusedVertex].Y;
        }

        /// <summary>
        /// Event handler for left click release. It draws a line and removes conflicting lines
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EndLine(object sender, MouseButtonEventArgs e)
        {
            previewLine.Visibility = Visibility.Hidden;

            if (nearbyVertex == -1 || focusedVertex == -1 || nearbyVertex == focusedVertex)
            {
                focusedVertex = -1;
                return;
            }

            Point start = vertices[focusedVertex];
            Point end = vertices[nearbyVertex];
            AddEdgeToGraph(focusedVertex, nearbyVertex, edgeColor);

            Line newLine = null, arrowLine1 = null, arrowLine2 = null;
            bool startMatch, endMatch;
            for (int i = 2; i < Children.Count; i++)
            {
                Line l = Children[i] as Line;
                if (l != null && edgeColor == l.Stroke && l.Visibility == Visibility.Visible)
                {
                    startMatch = l.X1 == start.X && l.Y1 == start.Y;
                    endMatch = l.X2 == end.X && l.Y2 == end.Y;
                    if (startMatch && endMatch)
                    {
                        focusedVertex = -1;
                        return;
                    }
                    else if (startMatch)
                    {
                        // Use the conflicting line
                        newLine = l;
                        arrowLine1 = (Line)Children[i + 1];
                        arrowLine2 = (Line)Children[i + 2];
                    }
                    else if (endMatch)
                    {
                        // Remove the conflicting line
                        Children.RemoveRange(i, 3);
                        i--;
                    }
                }
            }

            if (newLine == null)
            {
                newLine = new Line() { Stroke = edgeColor, StrokeThickness = 2 };
                arrowLine1 = new Line() { Stroke = edgeColor, StrokeThickness = 2 };
                arrowLine2 = new Line() { Stroke = edgeColor, StrokeThickness = 2 };

                SetZIndex(newLine, 1);
                SetZIndex(arrowLine1, 1);
                SetZIndex(arrowLine2, 1);

                Children.Add(newLine);
                Children.Add(arrowLine1);
                Children.Add(arrowLine2);
            }

            newLine.X1 = start.X;
            newLine.Y1 = start.Y;
            newLine.X2 = end.X;
            newLine.Y2 = end.Y;

            // Gotta draw an arrowhead
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double r = Math.Sqrt(Math.Pow(dx, 2) + Math.Pow(dy, 2));
            Point tip = new Point(end.X - 4 * dx / r, end.Y - 4 * dy / r);
            double alpha = dx >= 0 ? Math.Atan(dy / dx) : Math.Atan(dy / dx) + Math.PI;

            arrowLine1.X1 = tip.X;
            arrowLine1.Y1 = tip.Y;
            arrowLine2.X1 = tip.X;
            arrowLine2.Y1 = tip.Y;
            arrowLine1.X2 = tip.X - 10 * Math.Cos(alpha + 0.3);
            arrowLine1.Y2 = tip.Y - 10 * Math.Sin(alpha + 0.3);
            arrowLine2.X2 = tip.X - 10 * Math.Cos(alpha - 0.3);
            arrowLine2.Y2 = tip.Y - 10 * Math.Sin(alpha - 0.3);

            focusedVertex = -1;
        }

        /// <summary>
        /// Calls the graph to add an edge
        /// </summary>
        public void AddEdgeToGraph(int start, int end, Brush color)
        {
            int j;
            for (j = 0; colors[j] != color && j < graph.Degree; j++) ;
            if (j == graph.Degree) colors[graph.Degree] = color;
            graph.AddEdge(start, end, j);
        }

        /// <summary>
        /// When the mouse moves around we need to know where the nearby vertex is and whether to draw preview shapes.
        /// </summary>
        private void DrawPreviewShapes(object sender, MouseEventArgs e)
        {
            int minidx = -1;
            double min = double.PositiveInfinity;
            double dist;
            Point mousePosition = e.GetPosition(this);
            Point q;

            // Find nearby vertex
            for (int i = 0; i < graph.Order; i++)
            {
                q = vertices[i];
                dist = Math.Pow(mousePosition.X - q.X, 2) + Math.Pow(mousePosition.Y - q.Y, 2);
                if (dist < min)
                {
                    min = dist;
                    minidx = i;
                }
            }

            nearbyVertex = min < BUBBLE_DIST * BUBBLE_DIST ? minidx : -1;

            Brush previewColor = edgeColor.Clone();
            previewColor.Opacity = 0.5;

            // Draw preview point and preview line
            // The preview point will only be shown if the mouse is near a vertex.
            if (nearbyVertex != -1)
            {
                previewPoint.Fill = previewColor;
                SetLeft(previewPoint, vertices[nearbyVertex].X - 6);
                SetTop(previewPoint, vertices[nearbyVertex].Y - 6);
                previewPoint.Visibility = Visibility.Visible;
            }
            else
            {
                previewPoint.Visibility = Visibility.Hidden;
            }

            // The preview line will only be shown if the mouse is down and another vertex is focused
            if (focusedVertex != -1)
            {
                previewLine.Stroke = previewColor;

                if (nearbyVertex != -1)
                {
                    previewLine.X2 = vertices[nearbyVertex].X;
                    previewLine.Y2 = vertices[nearbyVertex].Y;
                }
                else
                {
                    previewLine.X2 = mousePosition.X;
                    previewLine.Y2 = mousePosition.Y;
                }

                previewLine.Visibility = Visibility.Visible;
            }
            else
            {
                previewLine.Visibility = Visibility.Hidden;
            }
        }

        /// <summary>
        /// Remove all elements from the Canvas (except the preview shapes)
        /// </summary>
        public void ClearAll()
        {
            Children.RemoveRange(2, Children.Count - 2);
            colors = new Brush[Graph.MAX_GENERATORS];
            graph.ClearAll();
        }
    }
}
