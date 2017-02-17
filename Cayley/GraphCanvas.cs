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
        private int nearbyVertex;
        private int focusedVertex;
        private Brush edgeColor;
        private Graph graph;

        private Ellipse previewPoint = new Ellipse() { Visibility = Visibility.Hidden, Width = 12, Height = 12 };
        private Line previewLine = new Line() { Visibility = Visibility.Hidden, StrokeThickness = 2 };

        public GraphCanvas()
        {
            vertices = new Point[Graph.MAX_VERTICES];
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

        public Graph Graph
        {
            get { return graph; }
        }

        public Point[] Vertices
        {
            get { return vertices; }
        }

        public Brush EdgeColor
        {
            get { return edgeColor; }
            set { edgeColor = value; }
        }
        
        // So this is a debug thing. The code might be used eventually but don't base your design on it
        // Hack hack hack
        public void AddShape(int m, int n)
        {
            if (m * n > Graph.MAX_VERTICES)
            {
                return;
            }

            if (Children.Count > 2)
            {
                ClearAll();
            }

            double D = Math.Min(ActualHeight, ActualWidth);
            double rmin, rmax, h;
            if (m == 1)
            {
                rmin = 0.4 * D;
                rmax = rmin + 0.5;
                h = 1.0;
            }
            else
            {
                rmin = 0.2 * D;
                rmax = 0.4 * D;
                h = (rmax - rmin) / (m - 1);
            }

            double x, y, r = rmin;
            while (r < rmax + 0.1)
            {
                for (int i = 0; i < n; i++)
                {
                    x = ActualWidth / 2 + r * Math.Cos(2 * Math.PI * i / n);
                    y = ActualHeight / 2 + r * Math.Sin(2 * Math.PI * i / n);
                    AddRemovePoint(new Point(x, y));
                }
                r += h;
            }
        }

        private void StartLine(object sender, MouseButtonEventArgs e)
        {
            if (nearbyVertex == -1)
            {
                return;
            }

            focusedVertex = nearbyVertex;
            previewLine.X1 = vertices[focusedVertex].X;
            previewLine.Y1 = vertices[focusedVertex].Y;
        }

        // This is really long. Tons of functionality is crammed here. Oops.
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
            graph.AddEdge(focusedVertex, nearbyVertex, edgeColor);

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

        // This handles the click, passes immediately to the other overload
        public void AddRemovePoint(object sender, MouseButtonEventArgs e)
        {
            AddRemovePoint(e.GetPosition(this));
        }

        // Adds a point
        public void AddRemovePoint(Point p)
        {
            if (nearbyVertex != -1)
            {
                previewPoint.Visibility = Visibility.Hidden;
                Point q = vertices[nearbyVertex];

                for (int i = 2; i < Children.Count; i++)
                {
                    Ellipse e = Children[i] as Ellipse;
                    if (e != null && GetLeft(e) + 4 == q.X && GetTop(e) + 4 == q.Y)
                    {
                        Children.Remove(e);
                        break;
                    }
                }

                for (int i = 2; i < Children.Count; i++)
                {
                    Line l = Children[i] as Line;
                    if (l != null && ((l.X1 == q.X && l.Y1 == q.Y) || (l.X2 == q.X && l.Y2 == q.Y)))
                    {
                        Children.RemoveRange(i, 3);
                        i--;
                    }
                }

                // This also decrements graph.Order
                graph.RemovePoint(nearbyVertex);

                for (int i = nearbyVertex; i < graph.Order; i++)
                {
                    vertices[i] = vertices[i + 1];
                }
                vertices[graph.Order] = new Point();
            }
            else if (graph.Order < Graph.MAX_VERTICES)
            {
                Ellipse newPoint = new Ellipse() { Width = 8, Height = 8, Fill = Brushes.Black };
                SetZIndex(newPoint, 2);
                SetLeft(newPoint, p.X - 4);
                SetTop(newPoint, p.Y - 4);
                Children.Add(newPoint);

                vertices[graph.Order++] = p;
            }
        }

        // Updates the index of the vertex closest to p (and draws preview shapes)
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

        public void ClearAll()
        {
            Children.RemoveRange(2, Children.Count - 2);
            graph.ClearAll();
        }
    }
}
