using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KdTree;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Features2D;


public class PointCloud
{    
    public short[] rawDepth { get; private set; }
    public int depthX { get; private set; }
    public int depthY { get; private set; }
    public Color32[] colorizedDepth { get; private set; } // also has feature points marked in red

    public Color32[] rawColor { get; private set; }
    public int colorX { get; private set; }
    public int colorY { get; private set; }
    
    public List<CloudPoint> PointList { get; private set; }

    public KdTree<CloudPoint> FeatureTree { get; private set; }

    public Matrix<int> FeatureIndices;

    public float[] depthHistogramMap;
    public int MaxDepth = 10000; // this is something that the sensor knows too, don't mess
    Color32[] depthToColor;
    public Color32 BaseColor = Color.yellow;


    // here's the shitty way to do it.
    public PointCloud(short[] depth, int dWidth, int dHeight,
                      Color32[] color, int cWidth, int cHeight)
    {
        PointList = new List<CloudPoint>();

        // these are different sizes. use the depth image as the native index
        // we want to get as much depth data as possible
        this.rawColor = color;
        this.rawDepth = depth;

        this.depthX = dWidth;
        this.depthY = dHeight;
        this.colorX = cWidth;
        this.colorY = cHeight;

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
            
            PointList.Add(new CloudPoint(v.ToVector(), color[cIndex], Vector.Zeros(3)));
        }

        depthHistogramMap = new float[MaxDepth];
        depthToColor = new Color32[MaxDepth];
        colorizedDepth = new Color32[depthX * depthY];
		
		UpdateHistogram();
      }

    public void DetectFeatures()
    {
        const int numFeatures = 15;
        
        
        // note, the histogram is a property of the cloud itself and is updated when the cloud is created
        // load depth image into something that emgucv likes
        Image<Gray, Byte> depthImage = new Image<Gray, byte>(depthX, depthY);
        PointF hamburger = new PointF();

        // have to convert this to gray via luminosity
        var bytes = colorizedDepth.Select(x => (byte)(0.21*x.r + 0.71*x.g + 0.07*x.b));

        depthImage.Bytes = bytes.ToArray();

        // detect features of depth image using the harris thing
        // this also does the nonmax supression on the eigv image
        //hamburger = depthImage.GoodFeaturesToTrack(15, 
        
        

    }

    // adapted from ZigDepthViewer. this cleans up the depth image so the corner detector can get it easier.
    void UpdateHistogram()
    {
        int i, numOfPoints = 0;

        System.Array.Clear(depthHistogramMap, 0, depthHistogramMap.Length);

        int depthIndex = 0;

        // the textureSize below is the size of whatever thing you are trying to draw on
        // since this is for our own use this can be the same size

        // assume only downscaling
        // calculate the amount of source pixels to move per column and row in
        // output pixels
        //int factorX = depth.xres / textureSize.Width;
        //int factorY = ((depth.yres / textureSize.Height) - 1) * depth.xres;

        int factorX = 1;
        int factorY = 0; // this is weird, possibly supposed to be 1
        for (int y = 0; y < depthY; ++y, depthIndex += factorY)
        {
            for (int x = 0; x < depthX; ++x, depthIndex += factorX)
            {
                short pixel = rawDepth[depthIndex];
                if (pixel != 0)
                {
                    depthHistogramMap[pixel]++;
                    numOfPoints++;
                }
            }
        }
        depthHistogramMap[0] = 0;
        if (numOfPoints > 0)
        {
            for (i = 1; i < depthHistogramMap.Length; i++)
            {
                depthHistogramMap[i] += depthHistogramMap[i - 1];
            }
            depthToColor[0] = Color.black;
            for (i = 1; i < depthHistogramMap.Length; i++)
            {
                float intensity = (1.0f - (depthHistogramMap[i] / numOfPoints));
                //depthHistogramMap[i] = intensity * 255;
                depthToColor[i].r = (byte)(BaseColor.r * intensity);
                depthToColor[i].g = (byte)(BaseColor.g * intensity);
                depthToColor[i].b = (byte)(BaseColor.b * intensity);
                depthToColor[i].a = 255;//(byte)(BaseColor.a * intensity);
            }
        }
		depthIndex = 0;
        // now remap the depth into colors (from depthviewer)
        for (int y = depthY - 1; y >= 0; --y, depthIndex += factorY)
        {
            int outputIndex = y * depthX;
            for (int x = 0; x < depthX; ++x, depthIndex += factorX, ++outputIndex)
            {
                colorizedDepth[outputIndex] = depthToColor[rawDepth[depthIndex]];
            }
        }
    }
}