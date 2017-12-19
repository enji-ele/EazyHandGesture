using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;
using HGHandGesture;

public class CameraTracking : HGCamera 
{
	/*======================================
    * Override Method
	======================================*/
	// Use this for initialization
	protected override void Start()
	{
		base.Start();
	}
	
	// Update is called once per frame
	protected override void Update () {
		base.Update();
	}

	protected override void MatRetouch(Mat _rgbaMat)
	{
		base.MatRetouch(_rgbaMat);
	}

	protected override void Dispose()
	{
		base.Dispose();
	}
}
