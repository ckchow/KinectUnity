using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KdTree;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;


public class PointCloud
{
    // our KD tree of points
    KdTree<CloudPoint> points;
    short[] rawDepth;
    Color32[] rawColor;

    // the Zig library handles alignment and stuff so it's ok
    // probably never use this constructor because it's horrible
    //public PointCloud(short[] depthArray, int depthWidth, int depthheight,
    //                  Color32[] colorArray, int colorWidth, int colorHeight)
    //{
    //    // we have to downsample or else this point cloud is really, really big.
    //    ArrayList cloudPoints = new ArrayList();

    //    // use as much depth information as possible, don't 

    //    for (int i = 0; i < colorArray.Length; i++)
    //    {
    //        Color32 c = colorArray[i];
    //        // use the mapping function to go from point to point
    //    }
    //}


    // here's the shitty way to do it.
    public PointCloud(short[] depth, int dWidth, int dHeight,
                      Color32[] color, int cWidth, int cHeight)
    {
        List<CloudPoint> cloudpoints = new List<CloudPoint>();

        // these are different sizes. use the depth image as the native index
        // we want to get as much depth data as possible
        this.rawColor = color;
        this.rawDepth = depth;

        // express scaling factors to take x,y positions in depth to color
        // also we are assuming that the color image is larger than the depth one
        int factorX = cWidth / dWidth;
        int factorY = cHeight / dHeight;

        for (int i = 0; i < depth.Length; i++)
        {
            // the transform that the Zig thing provides expects coordinates in the image plane though
            if (depth[i] == 0)
                continue; // this is a garbage point

            int x = (i % dWidth) * factorX;
            int y = (i / dWidth) * factorY;
            int z = depth[i];

            Vector3 v = new Vector3(x, y, z);
            v = ZigInput.ConvertImageToWorldSpace(v);

            int cIndex = x + y * cWidth;
            
            cloudpoints.Add(new CloudPoint(v.ToVector(), color[cIndex], Vector.Zeros(3)));
        }

        // FORM TREE
        points = KdTree<CloudPoint>.Construct(4, cloudpoints, x => x.location.ColorLocation(x.color));
    }
}