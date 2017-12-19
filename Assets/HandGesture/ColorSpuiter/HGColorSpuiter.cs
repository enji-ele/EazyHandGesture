using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

namespace HGHandGesture 
{
	public static class HGColorSpuiter
	{
		public static Point storedTouchPoint = null;

		//ColorをScalarに変換
		public static Scalar ColorToScalar(Color color) 
		{
			//   0,   0,   0, 255：黒
			// 255,   0,   0, 255：赤
			//   0, 255,   0, 255：緑
			//   0,   0, 255, 255：青
			// 255, 255, 255, 255：白
			return new Scalar(color.r*255f, color.g*255f, color.b*255f, color.a*255f);
		}

		//ScalarをColorに変換
		public static Color ScalarToColor(Scalar scalar) 
		{
			float r = 0;
			if (0 < scalar.val.Length) r = (float)scalar.val[0]/255f;
			float g = 0;
			if (1 < scalar.val.Length) g = (float)scalar.val[1]/255f;
			float b = 0;
			if (2 < scalar.val.Length) b = (float)scalar.val[2]/255f;
			float a = 0;
			if (3 < scalar.val.Length) a = (float)scalar.val[3]/255f;
			return new Color(r, g, b, a);
		}

		//タップした位置の色を返す
		public static Color GetTapPointColor(Mat rgbaMat)
		{
			Color tapColor = new Color(0.031f, 0.326f, 0.852f, 1f);

			//タップ座標の取得
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR)
			//Touch
			int touchCount = Input.touchCount;
			if (touchCount == 1)
			{
				Touch t = Input.GetTouch(0);
				if(t.phase == TouchPhase.Ended && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject(t.fingerId))
					storedTouchPoint = new Point (t.position.x, t.position.y);
			}
#else
			//Mouse
			if (Input.GetMouseButtonUp(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
				storedTouchPoint = new Point(Input.mousePosition.x, Input.mousePosition.y);
#endif

			//タップされていればタップされている場所を取得
			if(storedTouchPoint != null)
			{
				Point touchPoint = _convertScreenPoint(rgbaMat, storedTouchPoint);

				//タップされている場所から色情報を取得
				Scalar blobColorHsv = _onTouch(rgbaMat, touchPoint);
				if (blobColorHsv != null) tapColor = ScalarToColor(blobColorHsv);
			}

			return tapColor;
		}

		//タップ座標を取得
		private static Point _convertScreenPoint(Mat rgbaMat, Point screenPoint)
		{
			//台形補正を行いタップされた位置の座標を正確に取得する
			Vector2 tl = Camera.main.WorldToScreenPoint(new Vector3(-rgbaMat.width()/2,  rgbaMat.height()/2));
			Vector2 tr = Camera.main.WorldToScreenPoint(new Vector3( rgbaMat.width()/2,  rgbaMat.height()/2));
			Vector2 br = Camera.main.WorldToScreenPoint(new Vector3( rgbaMat.width()/2, -rgbaMat.height()/2));
			Vector2 bl = Camera.main.WorldToScreenPoint(new Vector3(-rgbaMat.width()/2, -rgbaMat.height()/2));

			Mat srcRectMat = new Mat(4, 1, CvType.CV_32FC2);
			Mat dstRectMat = new Mat(4, 1, CvType.CV_32FC2);

			srcRectMat.put(0, 0, tl.x, tl.y, tr.x, tr.y, br.x, br.y, bl.x, bl.y);
			dstRectMat.put(0, 0, 0.0, 0.0, rgbaMat.width(), 0.0, rgbaMat.width(), rgbaMat.height(), 0.0, rgbaMat.height());

			Mat perspectiveTransform = Imgproc.getPerspectiveTransform(srcRectMat, dstRectMat);

			MatOfPoint2f srcPointMat = new MatOfPoint2f(screenPoint);
			MatOfPoint2f dstPointMat = new MatOfPoint2f();

			Core.perspectiveTransform (srcPointMat, dstPointMat, perspectiveTransform);

			return dstPointMat.toArray()[0];
		}

		//タッチ座標からその場所の平均色を取得
		private static Scalar _onTouch(Mat rgbaMat, Point touchPoint)
		{
			int cols = rgbaMat.cols();
			int rows = rgbaMat.rows();
			int x = (int)touchPoint.x;
			int y = (int)touchPoint.y;
			if ((x < 0) || (y < 0) || (x > cols) || (y > rows)) return null;

			OpenCVForUnity.Rect touchedRect = new OpenCVForUnity.Rect();

			touchedRect.x = (x > 5) ? x - 5 : 0;
			touchedRect.y = (y > 5) ? y - 5 : 0;

			touchedRect.width = (x + 5 < cols) ? x + 5 - touchedRect.x : cols - touchedRect.x;
			touchedRect.height = (y + 5 < rows) ? y + 5 - touchedRect.y : rows - touchedRect.y;

			//タップ座標のみを切り抜く
			Mat touchedRegionRgba = rgbaMat.submat(touchedRect);

			Mat touchedRegionHsv = new Mat();
			Imgproc.cvtColor(touchedRegionRgba, touchedRegionHsv, Imgproc.COLOR_RGB2HSV_FULL);

			//タップされた位置の平均色を計算する
			Scalar blobColorHsv = Core.sumElems(touchedRegionHsv);
			int pointCount = touchedRect.width * touchedRect.height;
			for (int i = 0; i < blobColorHsv.val.Length; i++)
				blobColorHsv.val [i] /= pointCount;

			touchedRegionRgba.release();
			touchedRegionHsv.release();

			return blobColorHsv;
		}
	}
}
