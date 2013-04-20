using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;

public static class Extensions
{
    public static Vector3 ToVector3(this Vector v)
    {
        return new Vector3((float)v[0], (float)v[1], (float)v[2]); // agh this is kind of bad
    }

    public static Vector ToVector(this Vector3 v)
    {
        return new Vector(new double[] { v[0], v[1], v[2] });
    }

   
    // this extension was a little unclear
    ////public static Vector ColorLocation(this Vector v1, Color32 ci)
    //{
    //    // maybe compare these colors grayscale otherwise the norm gets really high dim
    //    double [] g = new double[]{((Color) ci).grayscale};
    //    double[] d = v1.CopyToArray();

    //    return new Vector(d.Concat(g).ToArray());
    //}

    // it's possible that this extension is inefficient and we should just store the augmented vector somewhere
    public static Vector ColorLocation(this CloudPoint c)
    {
        double[] g = new double[] { ((Color)c.color).grayscale };
        double[] d = c.location.CopyToArray();

        return new Vector(d.Concat(g).ToArray());
    }
}
