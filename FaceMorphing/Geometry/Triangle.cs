using System;
using System.Collections.Generic;
using System.Text;

namespace Triangulator.Geometry
{
	/// <summary>
	/// Triangle made from three point indexes
	/// </summary>
	public struct Triangle
	{
		/// <summary>
		/// First vertex index in triangle
		/// </summary>
        public int p1; 
		/// <summary>
		/// Second vertex index in triangle
		/// </summary>
        public int p2; 
		/// <summary>
		/// Third vertex index in triangle
		/// </summary>
        public int p3; 
		/// <summary>
		/// Initializes a new instance of a triangle
		/// </summary>
		/// <param name="point1">Vertex 1</param>
		/// <param name="point2">Vertex 2</param>
		/// <param name="point3">Vertex 3</param>
        public Triangle(int po1, int po2, int po3)
		{
            p1 = po1; p2 = po2; p3 = po3;
		}
	}
}
