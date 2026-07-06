using System;
using UnityEngine;

	public static class ImageStringConverter
	{
		/// <summary>
		/// Get string image representation
		/// </summary>
		public static Texture2D ImageFromString(string source, int width, int height)
		{
			var bytes = Convert.FromBase64String(source);
			var texture = new Texture2D(width, height);
			texture.LoadImage(bytes);
			return texture;
		}
	}
