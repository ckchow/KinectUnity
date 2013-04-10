using UnityEngine;
using System.Collections;
using System.Collections.Generic;
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

    List<PointCloud> clouds;

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

        clouds = new List<PointCloud>();
    }

	void Update()
	{
		
	}

    void OnGUI()
    {
        #region DEBUG BUTTON
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
            for (int i = 0; i < rawImageMap.Length; i+=4)
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

                // the image and the depth are at different scale factors so you have to do something weird to fit them

                worldPoints.Add(ZigInput.ConvertImageToWorldSpace(new Vector3(x,y))); // TODO FIX THIS
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
        #endregion

        if (GUI.Button(new Rect(10,40,40,20), "fart"))
        {
			
            clouds.Add(new PointCloud(rawDepthMap, dWidth, dHeight, rawImageMap, imWidth, imHeight));
			Debug.Log("got a cloud");
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



