using System.Collections;
using UnityEngine;
using OpenCVForUnity;

namespace HGHandGesture 
{
	public class HGCamera : HGMat 
	{
		[System.Serializable] public class WebCameraData 
		{
			public string DeviceName = null;
			public Size Size = new Size(1136, 640);
			public bool IsFrontFacing = false;
		}
		[SerializeField] private WebCameraData _webCameraData = new WebCameraData();

		private WebCamTexture _webCamTexture;
		private WebCamDevice _webCamDevice;

		private bool _isInitWaiting = false;
		private bool _hasInitDone = false;

		/*======================================
	    * Override Method
		======================================*/
		// Use this for initialization
		protected override void Start() {
			base.Start();
			Initialize();
		}

		// Update is called once per frame
		protected override void Update() {
			base.Update();
			if (_hasInitDone) base.webCamTexture = _webCamTexture;
		}

		/// <summary>
		/// Mat the retouch.
		/// </summary>
		/// <param name="rgbaMat">Mat.</param>
		protected override void MatRetouch(Mat _rgbaMat)
		{
			base.MatRetouch(_rgbaMat);
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
			_isInitWaiting = false;
			_hasInitDone = false;

			if (_webCamTexture != null) 
			{
				_webCamTexture.Stop ();
				_webCamTexture = null;
			}

			base.Dispose();
		}

		/*======================================
	    * Public Method
		======================================*/

		public void Play()
		{
			if (_hasInitDone) webCamTexture.Play();
		}

		public void Pause()
		{
			if (_hasInitDone) webCamTexture.Pause();
		}

		public void Stop()
		{
			if (_hasInitDone) webCamTexture.Stop();
		}

		public void ChangeCamera()
		{
			if (_hasInitDone && _webCameraData != null)
			{
				_webCameraData.IsFrontFacing = !_webCameraData.IsFrontFacing;
				Initialize();
			}
		}

		/*======================================
	    * Private Method
		======================================*/

		private void Initialize()
		{
			if (_isInitWaiting) return;
			StartCoroutine(_Initialize());
		}

		private IEnumerator _Initialize ()
		{
			if (_webCameraData == null)
				yield return null;
			
			if (_hasInitDone) Dispose();
			_isInitWaiting = true;

			if (!string.IsNullOrEmpty(_webCameraData.DeviceName))
				_webCamTexture = new WebCamTexture(_webCameraData.DeviceName, (int)_webCameraData.Size.width, (int)_webCameraData.Size.height);

			if (_webCamTexture == null)
			{
				for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++) 
				{
					if (WebCamTexture.devices[cameraIndex].isFrontFacing == _webCameraData.IsFrontFacing)
					{
						_webCamDevice = WebCamTexture.devices[cameraIndex];
						_webCamTexture = new WebCamTexture (_webCamDevice.name, (int)_webCameraData.Size.width, (int)_webCameraData.Size.height);
						break;
					}
				}
			}

			if (_webCamTexture == null) 
			{
				if (WebCamTexture.devices.Length > 0) 
				{
					_webCamDevice = WebCamTexture.devices [0];
					_webCamTexture = new WebCamTexture(_webCamDevice.name, (int)_webCameraData.Size.width, (int)_webCameraData.Size.height);
				} 
				else _webCamTexture = new WebCamTexture((int)_webCameraData.Size.width, (int)_webCameraData.Size.height);
			}

			if (_webCamTexture != null)
			{
				_webCamTexture.Play();

				while (true) 
				{
					if (_webCamTexture.didUpdateThisFrame)
					{
						_isInitWaiting = false;
						_hasInitDone = true;
						OnInited();
						break;
					} 
					else yield return 0;
				}
			}
		}

		private void OnInited ()
		{
			float width = _webCamTexture.height;
			float height = _webCamTexture.width;

			float widthScale = (float)Screen.width/width;
			float heightScale = (float)Screen.height/height;
			if (widthScale < heightScale)
				Camera.main.orthographicSize = (width*(float)Screen.height/(float)Screen.width)/2;
			else Camera.main.orthographicSize = height/2;
		}
	}
}