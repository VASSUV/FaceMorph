using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Triangulator.Geometry
{
	/// <summary>
	/// 2D Point with double precision
	/// </summary>
	public class Point
	{
		/// <summary>
		/// X component of point
		/// </summary>
		protected double _X;
		/// <summary>
		/// Y component of point
		/// </summary>
		protected double _Y;

        public int ID;

		/// <summary>
		/// Initializes a new instance of a point
		/// </summary>
		/// <param name="x"></param>
		/// <param name="y"></param>
		public Point(double x, double y, int id = 0)
		{
			_X = x;
			_Y = y;
            ID = id;
		}
	
		/// <summary>
		/// Gets or sets the X component of the point
		/// </summary>
		public double X
		{
			get { return _X; }
			set { _X = value; }
		}

		/// <summary>
		/// Gets or sets the Y component of the point
		/// </summary>
		public double Y
		{
			get { return _Y; }
			set { _Y = value; }
		}

		/// <summary>
		/// Makes a planar checks for if the points is spatially equal to another point.
		/// </summary>
		/// <param name="other">Point to check against</param>
		/// <returns>True if X and Y values are the same</returns>
		public bool Equals2D(Point other)
		{
			return (X == other.X && Y == other.Y);
		}

        #region ICloneable Members
        public Point Clone()
        {
            return new Point(X, Y,ID);
        }
        #endregion
	}
}
