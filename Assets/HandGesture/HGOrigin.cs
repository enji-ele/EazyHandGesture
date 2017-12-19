using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

namespace HGHandGesture 
{
	//手の認識を行う
	public static class HGOrigin
	{
		public static int depthThreashold = 9000; //検出の精度 (0 ~ 30000)
		public static Color ContourRangeColor = Color.green;
		public static Color ArmRangeColor = Color.blue;
		public static Color HandRangeColor = Color.cyan;
		public static Color PalmRangeColor = Color.yellow;
		public static Color PalmCenterColor = Color.grey;
		public static Color FingerRangeColor = Color.red;

		//認識の開始
		public static void Cognition(Mat rgbaMat, Color handColor)
		{
			//指定色と同じ輪郭を取得する
			Mat mDilatedMask = new Mat();
			_makeColorMask(rgbaMat, handColor, mDilatedMask);

			//マスクの輪郭の頂点を取得する
			List<MatOfPoint> contours = new List<MatOfPoint> ();
			Imgproc.findContours(mDilatedMask, contours, new Mat(), Imgproc.RETR_EXTERNAL, Imgproc.CHAIN_APPROX_SIMPLE);

			//輪郭ごとの頂点を取得し、手を判別する
			foreach (MatOfPoint contour in contours)
				_contourToHandGesture(rgbaMat, contour);
		}

		/*=============================================*
		 * 画像から輪郭を取得するまで
		 *=============================================*/
		/// <summary>
		/// Makes the color mask.
		/// </summary>
		/// <param name="rgbaMat">Rgba mat.</param>
		/// <param name="handColor">Hand color.</param>
		/// <param name="mDilatedMask">M dilated mask.</param>
		private static void _makeColorMask(Mat rgbaMat, Color handColor, Mat mDilatedMask) 
		{
			//色の範囲を指定する
			Scalar mLowerBound = new Scalar(0);
			Scalar mUpperBound = new Scalar(0);
			_getApproximateScalarFromColor(handColor, mLowerBound, mUpperBound);

			//ガウシアンピラミッドを利用して画像を周波数ごとに分解した（小さくした）HSV形式の画像を作成
			Mat mHsvMat = new Mat();
			_getGaussianPyramidHSVMat(rgbaMat, mHsvMat);

			//inRangeで色による探索を行い、mMaskに指定色だけが残った画像（マスク）を作成する
			Mat mMask = new Mat();
			Core.inRange (mHsvMat, mLowerBound, mUpperBound, mMask);

			//dilateで画像の膨張を行い、マスクのノイズ除去を行う
			Imgproc.dilate (mMask, mDilatedMask, new Mat ());
		}

		/// <summary>
		/// Gets the color of the approximate scalar from.
		/// </summary>
		/// <param name="handColor">Hand color.</param>
		/// <param name="mLowerBound">M lower bound.</param>
		/// <param name="mUpperBound">M upper bound.</param>
		private static void _getApproximateScalarFromColor(Color handColor, Scalar mLowerBound, Scalar mUpperBound) 
		{
			//色の範囲を指定する
			Scalar mColorRadius = new Scalar(25, 50, 50, 0);

			Scalar hsvColor = HGColorSpuiter.ColorToScalar(handColor);
			double minH = (hsvColor.val [0] >= mColorRadius.val [0]) ? hsvColor.val [0] - mColorRadius.val [0] : 0;
			double maxH = (hsvColor.val [0] + mColorRadius.val [0] <= 255) ? hsvColor.val [0] + mColorRadius.val [0] : 255;

			mLowerBound.val [0] = minH;
			mUpperBound.val [0] = maxH;

			mLowerBound.val [1] = hsvColor.val [1] - mColorRadius.val [1];
			mUpperBound.val [1] = hsvColor.val [1] + mColorRadius.val [1];

			mLowerBound.val [2] = hsvColor.val [2] - mColorRadius.val [2];
			mUpperBound.val [2] = hsvColor.val [2] + mColorRadius.val [2];

			mLowerBound.val [3] = 0;
			mUpperBound.val [3] = 255;
		}

		/// <summary>
		/// Gets the gaussian pyramid HSV mat.
		/// </summary>
		/// <param name="rgbaMat">Rgba mat.</param>
		/// <param name="mHsvMat">M hsv mat.</param>
		private static void _getGaussianPyramidHSVMat(Mat rgbaMat, Mat mHsvMat) 
		{
			Mat mPyrDownMat = new Mat();
			Imgproc.pyrDown (rgbaMat, mPyrDownMat);
			Imgproc.pyrDown (mPyrDownMat, mPyrDownMat);
			Imgproc.cvtColor (mPyrDownMat, mHsvMat, Imgproc.COLOR_RGB2HSV_FULL);
		}

		/*=============================================*
		 * 輪郭ごとの頂点から手を判別するまで
		 *=============================================*/
		/// <summary>
		/// Contours to hand gesture.
		/// </summary>
		/// <param name="rgbaMat">Rgba mat.</param>
		/// <param name="contour">Contour.</param>
		private static void _contourToHandGesture(Mat rgbaMat, MatOfPoint contour) 
		{
			try 
			{
				//頂点を調査する準備をする
				_pointOfVertices(rgbaMat, contour);

				//基準輪郭のサイズの取得と描画（長方形）
				OpenCVForUnity.Rect boundRect = Imgproc.boundingRect(new MatOfPoint(contour.toArray()));
				Imgproc.rectangle(rgbaMat, boundRect.tl(), boundRect.br(), HGColorSpuiter.ColorToScalar(ContourRangeColor), 2, 8, 0);

				/*=============================================*
				* 腕まで含んだ手の大きさを取得する
				**=============================================*/
				//腕まで含んだ手の大きさを識別する
				MatOfInt hull = new MatOfInt();
				Imgproc.convexHull(new MatOfPoint(contour.toArray()), hull);

				//腕まで含んだ手の範囲を取得
				List<Point> armPointList = new List<Point>();
				for (int j = 0; j < hull.toList().Count; j++)
				{
					Point armPoint = contour.toList()[hull.toList()[j]];
					bool addFlag = true;
					foreach (Point point in armPointList.ToArray()) 
					{
						//輪郭の1/10より近い頂点は誤差としてまとめる
						double distance = Mathf.Sqrt((float)((armPoint.x-point.x)*(armPoint.x-point.x)+(armPoint.y-point.y)*(armPoint.y-point.y)));
						if (distance <= Mathf.Min((float)boundRect.width, (float)boundRect.height)/10) 
						{
							addFlag = false;
							break;
						}
					}
					if (addFlag) armPointList.Add(armPoint);	
				}

				MatOfPoint armMatOfPoint = new MatOfPoint();
				armMatOfPoint.fromList(armPointList);
				List<MatOfPoint> armPoints = new List<MatOfPoint>();
				armPoints.Add(armMatOfPoint);

				//腕まで含んだ手の範囲を描画
				Imgproc.drawContours(rgbaMat, armPoints, -1, HGColorSpuiter.ColorToScalar(ArmRangeColor), 3);

				//腕まで含んだ手が三角形の場合はそれ以上の識別が難しい
				if (hull.toArray().Length < 3) return;

				/*=============================================*
				* 掌の大きさを取得する
				**=============================================*/
				//凸面の頂点から凹面の点のみを取得し、掌の範囲を取得する
				MatOfInt4 convexDefect = new MatOfInt4();
				Imgproc.convexityDefects(new MatOfPoint(contour.toArray()), hull, convexDefect);

				//凹面の点をフィルタリングして取得
				List<Point> palmPointList = new List<Point>();
				for (int j = 0; j < convexDefect.toList().Count; j = j+4) 
				{
					Point farPoint = contour.toList()[convexDefect.toList()[j+2]];
					int depth = convexDefect.toList()[j+3];
					if (depth > depthThreashold && farPoint.y < boundRect.br().y-boundRect.tl().y)
						palmPointList.Add(contour.toList()[convexDefect.toList()[j+2]]);
				}

				MatOfPoint palmMatOfPoint = new MatOfPoint();
				palmMatOfPoint.fromList(palmPointList);
				List<MatOfPoint> palmPoints = new List<MatOfPoint>();
				palmPoints.Add(palmMatOfPoint);

				//掌の範囲を描画
				Imgproc.drawContours(rgbaMat, palmPoints, -1, HGColorSpuiter.ColorToScalar(PalmRangeColor), 3);

				/*=============================================*
				* 掌+指先の大きさを取得する
				**=============================================*/
				//掌の位置を元に手首を除いた範囲を取得する
				List<Point> handPointList = new List<Point>();
				handPointList.AddRange(armPointList.ToArray());
				handPointList.Reverse();
				handPointList.RemoveAt(0);
				handPointList.Insert(0, palmPointList.ToArray()[0]);
				handPointList.RemoveAt(handPointList.Count-1);
				handPointList.Insert(handPointList.Count, palmPointList.ToArray()[palmPointList.Count-1]);

				MatOfPoint handMatOfPoint = new MatOfPoint();
				handMatOfPoint.fromList(handPointList);
				List<MatOfPoint> handPoints = new List<MatOfPoint>();
				handPoints.Add(handMatOfPoint);

				Imgproc.drawContours(rgbaMat, handPoints, -1, HGColorSpuiter.ColorToScalar(HandRangeColor), 3);

				/*=============================================*
				* 指先の位置を取得する
				**=============================================*/
				//掌の各頂点の中心を求める
				List<Point> palmCenterPoints = new List<Point>();
				for (int i = 0; i < palmPointList.Count; i++)
				{
					Point palmPoint = palmPointList.ToArray()[i];
					Point palmPointNext = new Point();
					if (i+1 < palmPointList.Count) 
						palmPointNext = palmPointList.ToArray()[i+1];
					else palmPointNext = palmPointList.ToArray()[0];
	
					Point palmCenterPoint = new Point((palmPoint.x+palmPointNext.x)/2, (palmPoint.y+palmPointNext.y)/2);
					palmCenterPoints.Add(palmCenterPoint);
				}
	
				//掌の頂点から最も近い手の頂点を求める
				for (int i = 0; i < palmCenterPoints.Count && i+1 < handPointList.Count && i < 5; i++) 
				{
					Point palmPoint = palmCenterPoints.ToArray()[i];


					List<Point> fingerList = new List<Point>();
					fingerList.Add(palmPoint);
					fingerList.Add(handPointList.ToArray()[i+1]);
	
					MatOfPoint fingerPoint = new MatOfPoint();
					fingerPoint.fromList(fingerList);
	
					List<MatOfPoint> fingerPoints = new List<MatOfPoint>();
					fingerPoints.Add(fingerPoint);
	
					Imgproc.drawContours(rgbaMat, fingerPoints, -1, HGColorSpuiter.ColorToScalar(FingerRangeColor), 3);
				}

//				Imgproc.putText(rgbaMat, "", new Point(2, rgbaMat.rows()-30), Core.FONT_HERSHEY_SIMPLEX, 1.0, HGColorSpuiter.ColorToScalar(Color.black), 2, Imgproc.LINE_AA, false);
			}
			catch (System.Exception e) 
			{
				Debug.Log(e.Message);
			}
		}

		/// <summary>
		/// Points the of vertices.
		/// </summary>
		/// <param name="contour">Contour.</param>
		private static void _pointOfVertices(Mat rgbaMat, MatOfPoint contour) 
		{
			//multiplyでガウシアンピラミッドで分解されたサイズを掛け算で実画像サイズに戻す
			Core.multiply(contour, new Scalar(4, 4), contour);

			//輪郭の頂点がまだらにあるので識別しやすいようにポリゴン近似でサンプリングする。
			MatOfPoint2f pointMat = new MatOfPoint2f();
			Imgproc.approxPolyDP(new MatOfPoint2f(contour.toArray()), pointMat, 3, true);
			contour = new MatOfPoint(pointMat.toArray());
		}
	}
}
