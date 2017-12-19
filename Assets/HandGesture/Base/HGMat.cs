using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using OpenCVForUnity;

namespace HGHandGesture 
{
	public class HGMat : MonoBehaviour {

		[SerializeField] private Image TextureImage;
		[HideInInspector] public Mat rgbaMat;
		[HideInInspector] public Color32[] colors;
		private Texture2D _convertTexture;

		/*======================================
		* Default Method
		======================================*/
		/// <summary>
		/// Raises the destroy event.
		/// </summary>
		void OnDestroy()
		{
			Dispose();
		}

		/*======================================
		* Override Method
		======================================*/
		// Use this for initialization
		protected virtual void Start() {
			
		}

		// Update is called once per frame
		protected virtual void Update() {
			
		}

		/// <summary>
		/// Mat the retouch.
		/// </summary>
		/// <param name="rgbaMat">Mat.</param>
		protected virtual void MatRetouch(Mat _rgbaMat) 
		{

		}

		/// <summary>
		/// Releases all resource used by the <see cref="HGTexture2DToMat"/> object.
		/// </summary>
		/// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="HGTexture2DToMat"/>. The
		/// <see cref="Dispose"/> method leaves the <see cref="HGTexture2DToMat"/> in an unusable state. After calling
		/// <see cref="Dispose"/>, you must release all references to the <see cref="HGTexture2DToMat"/> so the garbage
		/// collector can reclaim the memory that the <see cref="HGTexture2DToMat"/> was occupying.</remarks>
		protected virtual void Dispose()
		{
			if (_texture2D != null) 
				_texture2D = null;
			if (colors != null)
				colors = null;
			if (rgbaMat != null)
			{
				rgbaMat.Dispose();
				rgbaMat = null;
			}
		}

		/*======================================
	    * Private Method
		======================================*/
		/// <summary>
		/// Sprites from _texture2D.
		/// </summary>
		/// <returns>The from _texture2D.</returns>
		/// <param name="texture">Texture2D.</param>
		private Sprite _spriteFromTexture2D(Texture2D texture)
		{
			//Texture2DからSprite作成
			Sprite sprite = null;
			if (texture != null)
				sprite = Sprite.Create(texture, new UnityEngine.Rect(0, 0, texture.width, texture.height), Vector2.zero);
			return sprite;
		}

		/*======================================
	    * Convert Method
		======================================*/
		/// <summary>
		/// Shows the retouch Mat.
		/// </summary>
		private void _showRetouchMat() 
		{
			MatRetouch(rgbaMat);
			if (TextureImage != null)
			{
				Utils.matToTexture2D(rgbaMat, _convertTexture, colors);
				Sprite _sprite = _spriteFromTexture2D(_convertTexture);
				TextureImage.sprite = _sprite;
			}
		}

		/// <summary>
		/// Texture2D to Mat.
		/// </summary>
		private Texture2D _texture2D = null;
		public Texture2D texture2D 
		{
			get { return _texture2D; }
			set {
				_texture2D = value;
				_texture2DToMat();
			}
		}
		private void _texture2DToMat() 
		{
			if (_texture2D != null)
			{
				//Texture2DをMatに変換する
				if (colors == null || colors.Length != _texture2D.width * _texture2D.height)
					colors = new Color32[_texture2D.width * _texture2D.height];
				if (_convertTexture == null || _convertTexture.width != _texture2D.width || _convertTexture.height != _texture2D.height)
					_convertTexture = new Texture2D (_texture2D.width, _texture2D.height, TextureFormat.RGBA32, false);
				rgbaMat = new Mat(_texture2D.height, _texture2D.width, CvType.CV_8UC4);
				Utils.texture2DToMat(_texture2D, rgbaMat);

				//Matを修正して画面に描画
				_showRetouchMat();
			}
		}

		/// <summary>
		/// WebCamTexture to Mat.
		/// </summary>
		private WebCamTexture _webCamTexture = null;
		public WebCamTexture webCamTexture 
		{
			get { return _webCamTexture; }
			set {
				_webCamTexture = value;
				_webCamTextureToMat();
			}
		}
		private void _webCamTextureToMat()
		{
			if (_webCamTexture != null && _webCamTexture.isPlaying && _webCamTexture.didUpdateThisFrame)
			{
				//WebCamTextureをMatに変換する
				if (colors == null || colors.Length != _webCamTexture.width * _webCamTexture.height)
					colors = new Color32[_webCamTexture.width * _webCamTexture.height];
				if (_convertTexture == null || _convertTexture.width != _webCamTexture.width || _convertTexture.height != _webCamTexture.height)
					_convertTexture = new Texture2D (_webCamTexture.width, _webCamTexture.height, TextureFormat.RGBA32, false);
				rgbaMat = new Mat(_webCamTexture.height, _webCamTexture.width, CvType.CV_8UC4);
				Utils.webCamTextureToMat(_webCamTexture, rgbaMat, colors);

				//Matを修正して画面に描画
				_showRetouchMat();
			}
		}
	}
}