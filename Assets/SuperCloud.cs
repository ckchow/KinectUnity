using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;
using UnityEngine;
using DelaunayTriangulator;

class SuperCloud
{
    public List<CloudPoint> Points { get; private set; }

    public List<RT> Transforms { get; private set; }

    private Triangulator triangler;

    public List<Triad> SuperTriads { get; private set; }
    

    /// <summary>
    /// Number of points acquired each time you add a cloud. used for coloring purposes
    /// </summary>
    public List<int> PointsPerCloud;

    private Color32 colorStart = Color.magenta;
    private Color32 colorEnd = Color.blue;

    public SuperCloud(PointCloud root)
    {
        this.Points = root.PointList;
        Transforms = new List<RT>();
        Transforms.Add(new RT(root.R, root.T));
        PointsPerCloud = new List<int>();
        PointsPerCloud.Add(root.PointList.Count);
    }

    public SuperCloud()
    {
        Points = new List<CloudPoint>();
        Transforms = new List<RT>();
        PointsPerCloud = new List<int>();
        triangler = new Triangulator();
    }

    public void AddCloud(PointCloud cloud)
    {
        var transformedPoints = cloud.PointList;

        Transforms.Add(new RT(cloud.R, cloud.T));

        // do the transforms backwards, and no don't reverse it in place
        foreach (RT rt in ((IEnumerable<RT>) Transforms).Reverse())
        {
            transformedPoints = transformedPoints.Select(x => x.ApplyTransform(rt.R, rt.T)).ToList();
        }

		Points = Points.Concat(transformedPoints).ToList();
		
		var blah = Transforms.ToArray();

        PointsPerCloud.Add(transformedPoints.Count);

        // TODO cull points that are redundant
    }
	
	//TODO probably don't put this here, but it's ok for the demo
	public void Triangulate()
	{
		// project supercloud onto vertex list, truncating Z
        var vertices = Points.Select(x => new Vertex((float)x.location[0], (float)x.location[1])).ToList();

        SuperTriads = triangler.Triangulation(vertices);
	}

    public void Clear()
    {
        Points.Clear();
        Transforms.Clear();
        PointsPerCloud.Clear();
    }

    /// <summary>
    /// Returns the CloudPoints recolored according to their acquisition order.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<CloudPoint> GetPriorityClouds()
    {
        float step = 1f / PointsPerCloud.Count;

        List<CloudPoint> newPoints = new List<CloudPoint>();

        for (int i = 0; i < PointsPerCloud.Count; i++)
        {
            var color = Color32.Lerp(colorStart, colorEnd, i * step);
            var pointsPassed = PointsPerCloud.Take(i).Sum();

            newPoints.AddRange(Points.Skip(pointsPassed).Select(x => new CloudPoint(x.location, color, x.normal)));
        }

        return newPoints;
    }
}

public class RT
{
    public Matrix R;
    public Vector T;

    public RT(Matrix R, Vector T)
    {
        this.R = R;
        this.T = T;
    }
}