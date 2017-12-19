using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using OpenCVForUnity;

namespace HGHandGesture 
{
	//手の認識を行う
	public static class HGEnoxSoftware
	{
		private static HGColorBlobDetector detector = new HGColorBlobDetector(); //色の検出器
		private static int numberOfFingers = 0;

		public static int depthThreashold = 8700; //検出の精度(0 ~ 30000)
		public static Color BetweenFingersColor = Color.yellow;
		public static Color HandRangeColor = Color.green;
		public static Color PalmsRangeColor = Color.blue;
		public static Color WristRangeColor = Color.red;

		//認識の開始
		public static void Cognition(Mat rgbaMat, Color handColor)
		{
			//色情報を手と認識して手を認識
			_handPoseEstimationProcess(rgbaMat, handColor);

			//認識した手の情報を文字で描画（英語のみ対応）
			Imgproc.putText(rgbaMat, "Finger Count:"+numberOfFingers, new Point(5, rgbaMat.rows()-10), Core.FONT_HERSHEY_SIMPLEX, 1.0, new Scalar(0, 0, 0, 255), 2, Imgproc.LINE_AA, false);
		}

		//手を検出して画像に描画する
		private static void _handPoseEstimationProcess(Mat rgbaMat, Color handColor)
		{
			Imgproc.GaussianBlur(rgbaMat, rgbaMat, new OpenCVForUnity.Size (3, 3), 1, 1);

			//検出器に色を設定
			detector.setHsvColor(HGColorSpuiter.ColorToScalar(handColor));

			List<MatOfPoint> contours = detector.getContours();
			detector.process(rgbaMat);
			if (contours.Count <= 0) return;

			//手の角度に傾いた外接矩形を作成
			RotatedRect rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[0].toArray()));

			double boundWidth = rect.size.width;
			double boundHeight = rect.size.height;
			int boundPos = 0;

			for (int i = 1; i < contours.Count; i++)
			{
				rect = Imgproc.minAreaRect(new MatOfPoint2f(contours[i].toArray()));
				if (rect.size.width * rect.size.height > boundWidth * boundHeight) 
				{
					boundWidth = rect.size.width;
					boundHeight = rect.size.height;
					boundPos = i;
				}
			}

			OpenCVForUnity.Rect boundRect = Imgproc.boundingRect(new MatOfPoint(contours[boundPos].toArray()));
			//手首までの範囲を描画
			Imgproc.rectangle(rgbaMat, boundRect.tl(), boundRect.br(), HGColorSpuiter.ColorToScalar(WristRangeColor), 2, 8, 0);

			double a = boundRect.br().y-boundRect.tl().y;
			a = a*0.7;
			a = boundRect.tl().y+a;

			//手のひらの範囲を描画
			Imgproc.rectangle(rgbaMat, boundRect.tl(), new Point(boundRect.br().x, a), HGColorSpuiter.ColorToScalar(PalmsRangeColor), 2, 8, 0);

			//折れ線カーブまたはポリゴンを，互いの距離が指定された精度以下になるように，より少ない頂点数のカーブやポリゴンで近似します
			MatOfPoint2f pointMat = new MatOfPoint2f();
			Imgproc.approxPolyDP(new MatOfPoint2f(contours[boundPos].toArray()), pointMat, 3, true);
			contours[boundPos] = new MatOfPoint(pointMat.toArray());

			//点とポリゴンの最短距離を計算
			MatOfInt hull = new MatOfInt();
			MatOfInt4 convexDefect = new MatOfInt4();
			Imgproc.convexHull(new MatOfPoint(contours[boundPos].toArray()), hull);
			if (hull.toArray().Length < 3) return;
			Imgproc.convexityDefects(new MatOfPoint(contours[boundPos].toArray()), hull, convexDefect);

			//手の範囲を取得
			List<MatOfPoint> hullPoints = new List<MatOfPoint>();
			List<Point> listPo = new List<Point>();
			for (int j = 0; j < hull.toList().Count; j++)
				listPo.Add(contours[boundPos].toList()[hull.toList()[j]]);

			MatOfPoint e = new MatOfPoint();
			e.fromList(listPo);
			hullPoints.Add(e);

			//手の範囲を描画
			Imgproc.drawContours(rgbaMat, hullPoints, -1, HGColorSpuiter.ColorToScalar(HandRangeColor), 3);

			//指と認識した場所を取得
			List<MatOfPoint> defectPoints = new List<MatOfPoint>();
			List<Point> listPoDefect = new List<Point>();
			for (int j = 0; j < convexDefect.toList().Count; j = j+4) 
			{
				Point farPoint = contours[boundPos].toList()[convexDefect.toList()[j+2]];
				int depth = convexDefect.toList()[j+3];
				if (depth > depthThreashold && farPoint.y < a)
					listPoDefect.Add(contours[boundPos].toList()[convexDefect.toList()[j+2]]);
			}

			MatOfPoint e2 = new MatOfPoint();
			e2.fromList(listPo);
			defectPoints.Add(e2);

			//検出した指の本数を更新
			numberOfFingers = listPoDefect.Count;
			if (numberOfFingers > 5) numberOfFingers = 5;

			//指の間に点を描画
			foreach (Point p in listPoDefect)
				Imgproc.circle(rgbaMat, p, 6, HGColorSpuiter.ColorToScalar(BetweenFingersColor), -1);
		}
	}
}