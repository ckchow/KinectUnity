using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MathNet.Numerics.LinearAlgebra;

class SuperCloud
{
    public List<CloudPoint> Points { get; private set; }

    List<RT> Transforms { get; private set; }

    public SuperCloud(PointCloud root)
    {
        this.Points = root.PointList;
    }

    public void AddCloud(PointCloud cloud)
    {
        var transformedPoints = cloud.PointList;

        Transforms.Add(new RT(cloud.R, cloud.T));

        // do the transforms backwards, and no don't reverse it in place
        foreach (RT rt in ((IEnumerable<RT>) Transforms).Reverse())
        {
            transformedPoints = transformedPoints.Select(x => x.ApplyTransform(rt.R, rt.T)).ToList();
            // SO FRESH
        }


        // cull points that are redundant
    }

    public void Clear()
    {
        Points.Clear();
        Transforms.Clear();
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