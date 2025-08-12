
using Engine;
using Engine.Graphics;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Game
{
	public class TextureAtlas
	{
		public Texture2D m_texture;

		public Dictionary<string, Rectangle> m_rectangles = [];

		public Texture2D Texture => m_texture;

		public TextureAtlas(Texture2D texture, string atlasDefinition, string prefix)
		{
			m_texture = texture;
			string[] array = atlasDefinition.Split(new char[2]
			{
				'\n',
				'\r'
			}, StringSplitOptions.RemoveEmptyEntries);
			int num = 0;
			while (true)
			{
				if (num < array.Length)
				{
					string[] array2 = array[num].Split(new char[1]
					{
						' '
					}, StringSplitOptions.RemoveEmptyEntries);
					if (array2.Length < 5)
					{
						break;
					}
					string key = prefix + array2[0];
					var value = new Rectangle
					{
						Left = int.Parse(array2[1], CultureInfo.InvariantCulture),
						Top = int.Parse(array2[2], CultureInfo.InvariantCulture),
						Width = int.Parse(array2[3], CultureInfo.InvariantCulture),
						Height = int.Parse(array2[4], CultureInfo.InvariantCulture)
					};
					m_rectangles.Add(key, value);
					num++;
					continue;
				}
				return;
			}
			throw new InvalidOperationException("Invalid texture atlas definition.");
		}

		public bool ContainsTexture(string textureName)
		{
			return m_rectangles.ContainsKey(textureName);
		}

		public Vector4? GetTextureCoordinates(string textureName)
		{
			if (m_rectangles.TryGetValue(textureName, out Rectangle value))
			{
				Vector4 value2 = default;
				value2.X = value.Left / (float)m_texture.Width;
				value2.Y = value.Top / (float)m_texture.Height;
				value2.Z = value.Right / (float)m_texture.Width;
				value2.W = value.Bottom / (float)m_texture.Height;
				return value2;
			}
			return null;
		}
	}
}
