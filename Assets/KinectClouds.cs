using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;
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
    int cloudIndex = 0;
    string cloudIndexString;
    PointCloud curCloud;
    bool showLiveCloud = true;
    bool dumpList = false;

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

        cloudIndexString = "";
    }

	void Update()
	{
        // acquire cloud
        curCloud = new PointCloud(rawDepthMap, dWidth, dHeight, rawImageMap, imWidth, imHeight);

        // display it maybe
        PointCloudRenderer sebRenderer = (PointCloudRenderer)
                                                Object.FindObjectOfType(typeof(PointCloudRenderer));

        if (showLiveCloud)
        {
            sebRenderer.getCloudPoints(curCloud.PointList.ToArray());
        }
	}

    void OnGUI()
    {
        #region DEBUG BUTTON
        //// a button to capture the current point cloud into a KD tree
        //if (GUI.Button(new Rect(10, 10, 80, 20), "stringize"))
        //{
        //    // you have to fork a coroutine or something here.

        //    // TESTING
        //    // when you hit this button, it should save a screenshot and 3 CSV's

        //    // depth from array -> CSV
        //    string depthString = "";
        //    foreach (short depth in rawDepthMap)
        //    {
        //        depthString = string.Concat(depthString, depth.ToString(), ","); // has an extra comma, who care
        //    }
        //    string depthPath =
        //        System.IO.Path.Combine(filePath, "zigD" + index.ToString() + ".csv");
        //    File.WriteAllText(depthPath, depthString);

        //    // color from array -> bitmap
        //    string cPath =
        //        System.IO.Path.Combine(filePath, "zigC" + index.ToString() + ".bmp");
        //    // we have to do this in a smarter way or else the file will be huge.
        //    byte[] colors = new byte[4 * rawImageMap.Length]; // veryu lkarge
        //    for (int i = 0; i < rawImageMap.Length; i+=4)
        //    {
        //        colors[i] = rawImageMap[i].a;
        //        colors[i + 1] = rawImageMap[i].inc_transform;
        //        colors[i + 2] = rawImageMap[i].g;
        //        colors[i + 3] = rawImageMap[i].b;
        //    }
        //    File.WriteAllBytes(cPath, colors);

        //    // express color points as world points (fold this into the other iteration for speedup)
        //    ArrayList worldPoints = new ArrayList();
        //    for (int i = 0; i < rawImageMap.Length; i++)
        //    {
        //        int x = i % imWidth;
        //        int y = i / imWidth;

        //        // the image and the depth are at different scale factors so you have to do something weird to fit them

        //        worldPoints.Add(ZigInput.ConvertImageToWorldSpace(new Vector3(x,y))); // TODO FIX THIS
        //    }
        //    string worldString = "";
        //    foreach (Vector3 v in worldPoints)
        //    {
        //        worldString = string.Concat(worldString, v.ToString("G5"), ",");
        //    }
        //    string worldPath = 
        //        System.IO.Path.Combine(filePath, "zigW" + index.ToString() + ".csv");
        //    File.WriteAllText(worldPath, worldString);
        //}
        #endregion

        if (GUI.Button(new Rect(10,10,100,20), "add cloud"))
        {
            // match this new cloud onto the current cloud
            curCloud.DetectFeatures(); // debug
			clouds.Add(curCloud);

            double error = 0;

            if (clouds.Count > 1) // if this isn't the prime cloud, try to push onto the previous one
            {
				// debugging the normal calculation
				if (dumpList)
                {
                    string worldString = string.Join(";", clouds[cloudIndex].PointList.Select(x => x.location.ToString()).ToArray());
                    string worldPath =
                        System.IO.Path.Combine(filePath, "zigW" + index.ToString() + ".csv");
                    File.WriteAllText(worldPath, worldString);
                }
				
				
                error = curCloud.PushOntoCloud(clouds[clouds.Count - 1], 40, 300, 0.5);
            }

            Debug.Log("got cloud " + cloudIndex.ToString() + " e:" + error.ToString() + " R:" + curCloud.R.ToString() + " T:" + curCloud.T.ToString());
            
        }

        // choose which cloud to show
        cloudIndexString = GUI.TextField(new Rect(10, 30, 20, 20), cloudIndexString, 10);

        // have the renderer stop doing whatever
        showLiveCloud = GUI.Toggle(new Rect(10,50,50,20), showLiveCloud, "live?");
        dumpList = GUI.Toggle(new Rect(60, 50, 60, 20), dumpList, "DUMP");

        if (GUI.Button(new Rect(10, 70, 100, 20), "show this cloud"))
        {
            if (int.TryParse(cloudIndexString, out cloudIndex) && !showLiveCloud)
            {
                PointCloudRenderer sebRenderer = (PointCloudRenderer)
                                                    Object.FindObjectOfType(typeof(PointCloudRenderer));
                if (cloudIndex >= 0 && cloudIndex < clouds.Count)
                {
                    sebRenderer.getCloudPoints(clouds[cloudIndex].PointList.ToArray());

                }
                else if (cloudIndex == -1)
                {
                    // show all the fuckin clouds you piece of shit sHOW M<E THE CLODS
                    sebRenderer.getCloudPoints(clouds.SelectMany(x => x.PointList).ToArray());
                }
            }
        }

        if (GUI.Button(new Rect(10, 100, 80, 20), "clear clouds"))
        {
            clouds.Clear();
        }

        //if (GUI.Button(new Rect(10, 70, 80, 20), "RENDER"))
        //{
        //    Debug.Log("drawing a cloud");
        //    PointCloudRenderer sebRenderer = (PointCloudRenderer)
        //                                        Object.FindObjectOfType(typeof(PointCloudRenderer));
        //    sebRenderer.getCloudPoints(clouds[0].PointList.ToArray());          
            
        //}
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



