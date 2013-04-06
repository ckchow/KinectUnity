using UnityEngine;
using System.Collections;
using MathNet.Numerics.LinearAlgebra;
using KdTree;
using System.IO;


public class KinectClouds : MonoBehaviour
{
    Color32[] rawImageMap;
    short[] rawDepthMap;
    int imWidth;
    int imHeight;
    int dWidth;
    int dHeight;

    int index;
    string filePath = @"C:\temp\ZIG\";

    // Use this for initialization
    void Start()
    {
        index = 0;
        imWidth = 0;
        imHeight = 0;
        dWidth = 0;
        dHeight = 0;
        ZigInput.Instance.AddListener(gameObject); // I don't know what this does
    }



    void OnGUI()
    {
        // a button to capture the current point cloud into a KD tree
        if (GUI.Button(new Rect(10, 10, 80, 20), "Yay Cloud"))
        {
            // you have to fork a coroutine or something here.

            // TESTING
            // when you hit this button, it should save a screenshot and 3 CSV's

            // depth from array -> CSV
            string depthString = "";
            foreach (short depth in rawDepthMap)
            {
                depthString = string.Concat(depthString, depth.ToString(), ","); // has an extra comma, who care
            }
            string depthPath =
                System.IO.Path.Combine(filePath, "zigD" + index.ToString() + ".csv");
            File.WriteAllText(depthPath, depthString);

            // color from array -> bitmap
            string cPath =
                System.IO.Path.Combine(filePath, "zigC" + index.ToString() + ".bmp");
            // we have to do this in a smarter way or else the file will be huge.
            byte[] colors = new byte[4 * rawImageMap.Length]; // veryu lkarge
            for (int i = 0; i < rawImageMap.Length; i++)
            {
                colors[i] = rawImageMap[i].a;
                colors[i + 1] = rawImageMap[i].r;
                colors[i + 2] = rawImageMap[i].g;
                colors[i + 3] = rawImageMap[i].b;
            }
            File.WriteAllBytes(cPath, colors);

            // express color points as world points (fold this into the other iteration for speedup)
            ArrayList worldPoints = new ArrayList();
            for (int i = 0; i < rawImageMap.Length; i++)
            {
                int x = i % imWidth;
                int y = i / imWidth;

                worldPoints.Add(ZigInput.ConvertImageToWorldSpace(new Vector3(x,y)));
            }
            string worldString = "";
            foreach (Vector3 v in worldPoints)
            {
                worldString = string.Concat(worldString, v.ToString("G5"), ",");
            }
            string worldPath = 
                System.IO.Path.Combine(filePath, "zigW" + index.ToString() + ".csv");
            File.WriteAllText(worldPath, worldString);
        }

    }


    void Zig_Update(ZigInput input)
    {
        // when Zig.cs tells us to we acquire a new scene
        rawDepthMap = ZigInput.Depth.data;
        rawImageMap = ZigInput.Image.data;

        dWidth = ZigInput.Depth.xres;
        dHeight = ZigInput.Depth.yres;
        imWidth = ZigInput.Image.xres;
        imHeight = ZigInput.Image.yres;
    }
}

public class CloudPoint
{
    public Color32 color { get; set; }
    public Vector location { get; set; }
    public Vector normal { get; set; }

    public CloudPoint(Vector location, Color32 color, Vector normal)
    {
        this.location = location;
        this.color = color;
        this.normal = normal;
    }
}

public class PointCloud
{
    // our KD tree of points
    KdTree<CloudPoint> points;

    // the Zig library handles alignment and stuff so it's ok
    public PointCloud(short[] depthArray, int depthWidth, int depthheight,
                      Color32[] colorArray, int colorWidth, int colorHeight)
    {
        // we have to downsample or else this point cloud is really, really big.
        ArrayList cloudPoints = new ArrayList();

        for (int i = 0; i < colorArray.Length; i++)
        {
            Color32 c = colorArray[i];
            // use the mapping function to go from point to point maybe
        }
    }
}