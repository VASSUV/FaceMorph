using System;
using System.Collections.Generic;
using System.Text;
using Triangulator;
using Triangulator.Geometry;
namespace ppp
{
    public class BData
    {
        public BData(int i = 0) { j = i; }
        private int j = 0;
        public List<Point> Points1 = new List<Point>();
        public List<Point> Points2 = new List<Point>();
        public List<Point> Points3 = new List<Point>();
        public List<Triangle> Tris1 = new List<Triangle>();
        public List<Triangle> Tris2 = new List<Triangle>();
        public List<Triangle> Tris = new List<Triangle>();
        public void addPoint(Point d1, Point d2)
        {
            j++;
            Points1.Add(new Point(d1.X, d1.Y, j));
            Points2.Add(new Point(d2.X, d2.Y, j));
        }
        public void delPoint(Point d, int ID, int list)
        {
            int n = Points1.Count;
            for (int i = 0; i < n; i++)
            {
                if (list == 1)
                {
                    if (d.X == Points1[i].X && d.Y == Points1[i].Y)
                    {
                        Points1.RemoveAt(i);
                        Points2.RemoveAt(i);
                        delTriangle();
                        break;
                    }
                }
                else if (list == 2)
                {
                    if (d.X == Points2[i].X && d.Y == Points2[i].Y)
                    {
                        Points1.RemoveAt(i);
                        Points2.RemoveAt(i);
                        delTriangle();
                        break;
                    }
                }
            }
        }
        public void delTriangle()
        {
            Tris.Clear();
            Tris1.Clear();
            Tris2.Clear();
            ReTriangle();
        }
        public void Clear()
        {
            Points1.Clear();
            Points2.Clear();
            Points3.Clear();
            Tris.Clear();
            Tris1.Clear();
            Tris2.Clear();
        }
        public void ReTriangle()
        {
            this.Triangulate(Points1);
        }
        public void reTris(double Percent)
        {
            int n = Points1.Count;
            Points3 = new List<Point>();
            for (int i = 0; i < n; i++)
            {
                Points3.Add(new Point(Points1[i].X + (Points2[i].X - Points1[i].X) * Percent,
                                      Points1[i].Y + (Points2[i].Y - Points1[i].Y) * Percent,
                                      Points1[i].ID));
            }

        }
        private void Triangulate(List<Triangulator.Geometry.Point> X)
        {
            int nv = X.Count;
            if (nv < 3)
                return;

            int trimax = 4 * nv;

            // Find the maximum and minimum X bounds.
            // This is to allow calculation of the bounding supertriangle
            double xmin = X[0].X;
            double ymin = X[0].Y;
            double xmax = xmin;
            double ymax = ymin;
            for (int i = 1; i < nv; i++)
            {
                if (X[i].X < xmin) xmin = X[i].X;
                if (X[i].X > xmax) xmax = X[i].X;
                if (X[i].Y < ymin) ymin = X[i].Y;
                if (X[i].Y > ymax) ymax = X[i].Y;
            }

            double dx = xmax - xmin;
            double dy = ymax - ymin;
            double dmax = (dx > dy) ? dx : dy;

            double xmid = (xmax + xmin) * 0.5;
            double ymid = (ymax + ymin) * 0.5;


            // Set up the supertriangle
            // This is a triangle which encompasses all the sample points.
            // The supertriangle coordinates are added to the end of the
            // X list. The supertriangle is the first triangle in
            // the triangle list.
            X.Add(new Triangulator.Geometry.Point((xmid - 2 * dmax), (ymid - dmax)));
            X.Add(new Triangulator.Geometry.Point(xmid, (ymid + 2 * dmax)));
            X.Add(new Triangulator.Geometry.Point((xmid + 2 * dmax), (ymid - dmax)));
            Tris1 = new List<Triangle>();
            Tris2 = new List<Triangle>();
            Tris = new List<Triangle>();
            Tris1.Add(new Triangle(nv, nv + 1, nv + 2)); //SuperTriangle placed at index 0
            Tris2.Add(new Triangle(nv, nv + 1, nv + 2)); //SuperTriangle placed at index 0
            Tris.Add(new Triangle(nv, nv + 1, nv + 2)); //SuperTriangle placed at index 0

            // Include each point one at a time into the existing mesh
            for (int i = 0; i < nv; i++)
            {
                List<Edge> Edges = new List<Edge>(); //[trimax * 3];
                // Set up the edge buffer.
                // If the point (X(i).x,X(i).y) lies inside the circumcircle then the
                // three edges of that triangle are added to the edge buffer and the triangle is removed from list.
                for (int j = 0; j < Tris1.Count; j++)
                {
                    if (InCircle(X[i], X[Tris1[j].p1], X[Tris1[j].p2], X[Tris1[j].p3]))
                    {
                        Edges.Add(new Edge(Tris1[j].p1, Tris1[j].p2));
                        Edges.Add(new Edge(Tris1[j].p2, Tris1[j].p3));
                        Edges.Add(new Edge(Tris1[j].p3, Tris1[j].p1));
                        Tris1.RemoveAt(j);
                        Tris2.RemoveAt(j);
                        Tris.RemoveAt(j);
                        j--;
                    }
                }
                if (i >= nv) continue; //In case we the last duplicate point we removed was the last in the array

                // Remove duplicate edges
                // Note: if all triangles are specified anticlockwise then all
                // interior edges are opposite pointing in direction.
                for (int j = Edges.Count - 2; j >= 0; j--)
                {
                    for (int k = Edges.Count - 1; k >= j + 1; k--)
                    {
                        if (Edges[j].Equals(Edges[k]))
                        {
                            Edges.RemoveAt(k);
                            Edges.RemoveAt(j);
                            k--;
                            continue;
                        }
                    }
                }
                // Form new triangles for the current point
                // Skipping over any tagged edges.
                // All edges are arranged in clockwise order.
                for (int j = 0; j < Edges.Count; j++)
                {
                    if (Tris1.Count >= trimax)
                        throw new ApplicationException("Exceeded maximum edges");
                    Tris1.Add(new Triangle(Edges[j].p1, Edges[j].p2, i));
                    Tris2.Add(new Triangle(Edges[j].p1, Edges[j].p2, i));
                    Tris.Add(new Triangle(Edges[j].p1, Edges[j].p2, i));
                }
                Edges.Clear();
                Edges = null;
            }
            // Remove triangles with supertriangle vertices
            // These are triangles which have a X number greater than nv
            for (int i = Tris1.Count - 1; i >= 0; i--)
            {
                if (Tris1[i].p1 >= nv || Tris1[i].p2 >= nv || Tris1[i].p3 >= nv)
                {
                    Tris1.RemoveAt(i);
                    Tris2.RemoveAt(i);
                    Tris.RemoveAt(i);
                }
            }
            //Remove SuperTriangle vertices
            X.RemoveAt(X.Count - 1);
            X.RemoveAt(X.Count - 1);
            X.RemoveAt(X.Count - 1);
            Tris1.TrimExcess();
            Tris2.TrimExcess();
            Tris.TrimExcess();
        }

        /// <summary>
        /// Returns true if the point (p) lies inside the circumcircle made up by points (p1,p2,p3)
        /// </summary>
        /// <remarks>
        /// NOTE: A point on the edge is inside the circumcircle
        /// </remarks>
        /// <param name="p">Point to check</param>
        /// <param name="p1">First point on circle</param>
        /// <param name="p2">Second point on circle</param>
        /// <param name="p3">Third point on circle</param>
        /// <returns>true if p is inside circle</returns>
        private static bool InCircle(Point p, Point p1, Point p2, Point p3)
        {
            //Return TRUE if the point (xp,yp) lies inside the circumcircle
            //made up by points (x1,y1) (x2,y2) (x3,y3)
            //NOTE: A point on the edge is inside the circumcircle

            if (System.Math.Abs(p1.Y - p2.Y) < double.Epsilon && System.Math.Abs(p2.Y - p3.Y) < double.Epsilon)
            {
                //INCIRCUM - F - Points are coincident !!
                return false;
            }

            double m1, m2;
            double mx1, mx2;
            double my1, my2;
            double xc, yc;

            if (System.Math.Abs(p2.Y - p1.Y) < double.Epsilon)
            {
                m2 = -(p3.X - p2.X) / (p3.Y - p2.Y);
                mx2 = (p2.X + p3.X) * 0.5;
                my2 = (p2.Y + p3.Y) * 0.5;
                //Calculate CircumCircle center (xc,yc)
                xc = (p2.X + p1.X) * 0.5;
                yc = m2 * (xc - mx2) + my2;
            }
            else if (System.Math.Abs(p3.Y - p2.Y) < double.Epsilon)
            {
                m1 = -(p2.X - p1.X) / (p2.Y - p1.Y);
                mx1 = (p1.X + p2.X) * 0.5;
                my1 = (p1.Y + p2.Y) * 0.5;
                //Calculate CircumCircle center (xc,yc)
                xc = (p3.X + p2.X) * 0.5;
                yc = m1 * (xc - mx1) + my1;
            }
            else
            {
                m1 = -(p2.X - p1.X) / (p2.Y - p1.Y);
                m2 = -(p3.X - p2.X) / (p3.Y - p2.Y);
                mx1 = (p1.X + p2.X) * 0.5;
                mx2 = (p2.X + p3.X) * 0.5;
                my1 = (p1.Y + p2.Y) * 0.5;
                my2 = (p2.Y + p3.Y) * 0.5;
                //Calculate CircumCircle center (xc,yc)
                xc = (m1 * mx1 - m2 * mx2 + my2 - my1) / (m1 - m2);
                yc = m1 * (xc - mx1) + my1;
            }

            double dx = p2.X - xc;
            double dy = p2.Y - yc;
            double rsqr = dx * dx + dy * dy;
            //double r = Math.Sqrt(rsqr); //Circumcircle radius
            dx = p.X - xc;
            dy = p.Y - yc;
            double drsqr = dx * dx + dy * dy;

            return (drsqr <= rsqr);
        }
    }
}
