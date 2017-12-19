using UnityEngine;
using OpenCVForUnity;
using HGHandGesture;

public class ImageTracking : HGMat 
{
	public Texture2D HandImage;
	public TrackingType trackingType = TrackingType.HGOrigin;
	public enum TrackingType 
	{
		HGEnoxSoftware,
		HGOrigin
	}

	/*======================================
    * Override Method
	======================================*/
	// Use this for initialization
	protected override void Start()
	{
		base.Start();
	}

	// Update is called once per frame
	protected override void Update()
	{
		base.Update();

		//加工用のTexture2Dを送りMatへの変換を待つ
		base.texture2D = HandImage;
	}

	/// <summary>
	/// Mat the retouch.
	/// </summary>
	/// <param name="rgbaMat">Mat.</param>
	protected override void MatRetouch(Mat _rgbaMat)
	{
		//変換されたMatを加工する
		base.MatRetouch(_rgbaMat);

		//タップ位置の色を取得
		Color color = HGColorSpuiter.GetTapPointColor(_rgbaMat);

		switch(trackingType)
		{
			case TrackingType.HGEnoxSoftware:
				HGEnoxSoftware.Cognition(_rgbaMat, color);
				break;
			case TrackingType.HGOrigin:
				HGOrigin.Cognition(_rgbaMat, color);
				break;
		}
	}

	/// <summary>
	/// Releases all resource used by the <see cref="HGTexture2DToMat"/> object.
	/// </summary>
	/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="HGTexture2DToMat"/>. The
	/// <see cref="Dispose"/> method leaves the <see cref="HGTexture2DToMat"/> in an unusable state. After calling
	/// <see cref="Dispose"/>, you must release all references to the <see cref="HGTexture2DToMat"/> so the garbage
	/// collector can reclaim the memory that the <see cref="HGTexture2DToMat"/> was occupying.</remarks>
	protected override void Dispose()
	{
		//破棄処理を記載
		base.Dispose();
	}
}