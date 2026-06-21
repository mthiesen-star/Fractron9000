using System;
using System.Drawing;
using System.IO;
using MTUtil;

namespace Fractron9000
{
	public class Palette
	{
		public enum PaletteType{ Default, OneDimensional, TwoDimensional, FromFile };

		public const int DefaultRes = 128;

		private static Palette defaultPalette = null;

		public static Palette DefaultPalette{
			get{
				if(defaultPalette == null)
					defaultPalette = Palette.GenerateDefaultPalette();
				return defaultPalette;
			}
		}

		private Color[,] pix;

		public int Width{
			get{ return pix.GetLength(1); }
		}
		public int Height{
			get{ return pix.GetLength(0); }
		}

		private PaletteType type;
		public PaletteType Type{
			get{ return type;}
		}

		private string name = null;
		public string Name{
			get{ return name; }
			set{ name = value; }
		}

		public Palette(int width, int height, PaletteType type)
		{
			this.pix = new Color[height,width];
			this.type = type;
		}

		public Palette(string fullFileName)
		{
			this.name = Path.GetFileName(fullFileName);
			Bitmap bmp = (Bitmap)Image.FromFile(fullFileName);
			this.type = PaletteType.FromFile;
			fillFromBitmap(bmp);
		}

		public Palette(Bitmap bmp)
		{
			this.type = PaletteType.TwoDimensional;
			fillFromBitmap(bmp);
		}

		private void fillFromBitmap(Bitmap bmp)
		{
			this.pix = new Color[bmp.Height, bmp.Width];

			for(int y = 0; y < Height; y++)
				for(int x = 0; x < Width; x++)
					pix[y,x] = bmp.GetPixel(x,y);
		}

		//Resample the src pallete to this size
		public Palette(int width, int height, Palette src)
		{
			pix = new Color[height,width];
			type = PaletteType.TwoDimensional;
			float fx, fy;

			for(int y = 0; y < Height; y++)
			{
				fy = ((float)y + 0.5f) / Height;
				
				for(int x = 0; x < Width; x++)
				{
					fx = ((float)x + 0.5f) / Width;
					pix[y,x] = src.SampleColor(fx,fy);
				}
			}
		}

		public Color GetPixel(int x, int y)
		{
			return pix[y,x];
		}

		public void SetPixel(int x, int y, Color c)
		{
			pix[y,x] = c;
		}
		
		public void SetPixel(int x, int y, float fr, float fg, float fb)
		{
			pix[y,x] = Util.ColorFromVec4(new Vec4(fr, fg, fb, 1.0f));
		}

		/*
		public void SetPixel(int x, int y, int r, int g, int b)
		{
			pix[y,x] = Color.FromArgb(
				Util.Clamp(r,0,255),
				Util.Clamp(g,0,255),
				Util.Clamp(b,0,255));
		}*/

		public Color Sample(float x, float y)
		{
			int sx = Util.Clamp(
				(int)(x * (float)(Width-1)),
				0, Width-1);
			int sy = Util.Clamp(
				(int)(y * (float)(Height-1)),
				0, Height-1);
			return pix[sy,sx];
		}

		//x and y are normalized to the range [0,1]
		public Vec4 SampleVec4(float x, float y)
		{
			float xb, yb;
			int i0, i1, j0, j1;
			float a, b;

			xb = (float)x * (float)Width - 0.5f;
			i0 = (int)Math.Floor(xb);
			a = xb - (float)i0;
			i1 = Util.Clamp(i0+1, 0, Width-1);
			i0 = Util.Clamp(i0,   0, Width-1);

			yb = (float)y * (float)Height - 0.5f;
			j0 = (int)Math.Floor(yb);
			b = yb - (float)j0;
			j1 = Util.Clamp(j0+1, 0, Height-1);
			j0 = Util.Clamp(j0,   0, Height-1);

			return Vec4.Lerp(
						Vec4.Lerp(Util.Vec4FromColor(pix[j0,i0]), Util.Vec4FromColor(pix[j0,i1]), a),
						Vec4.Lerp(Util.Vec4FromColor(pix[j1,i0]), Util.Vec4FromColor(pix[j1,i1]), a),
						b);
		}

		//x and y are normalized to the range [0,1]
		public Color SampleColor(float x, float y)
		{
			return Util.ColorFromVec4(SampleVec4(x,y));
		}

		public Bitmap ToBitmap()
		{
			Bitmap result = new Bitmap(Width,Height);
			for(int y = 0; y < Height; y++)
				for(int x = 0; x < Width; x++)
					result.SetPixel(x, y, pix[y,x]);
			return result;
		}

		public static Palette Build2DFrom1D(Palette src)
		{
			Palette result;

			if(src.Height != 1)
				return src;

			result = new Palette(src.Width, 15, PaletteType.TwoDimensional);
			int x;
			for(int y = 0; y < result.Height; y++)
			{
				double gamma = Math.Pow(1.44,(double)(7 - y));
				double invGamma = 1.0f / gamma;

				for(x = 0; x < src.Width; x++)
				{
					Vec4 sc = Util.Vec4FromColor(src.pix[0,x]);
					Vec4 c;

					c.X = (float)Math.Pow(sc.X, invGamma);
					c.Y = (float)Math.Pow(sc.Y, invGamma);
					c.Z = (float)Math.Pow(sc.Z, invGamma);
					c.W = sc.W;

					result.pix[y,x] = Util.ColorFromVec4(c);
				}
			}

			return result;
		}

		private static Vec4 lighten(Vec4 v)
		{
			float max = Math.Max(Math.Max(v.X,v.Y), v.Z);
			if(max <= 0.0f) return v;
			float k = Math.Min(4.0f,1.0f/max);
			return new Vec4( k*v.X, k*v.Y, k*v.Z, v.W);
		}

		

		public static Palette[] Read1DArray(BinaryReader input)
		{
			Palette[] result = null;

			int count = input.ReadInt32();
			result = new Palette[count];

			for(int i = 0; i < count; i++)
			{
				result[i] = Read1D(input);
			}

			return result;
		}
		
		private static Palette Read1D(BinaryReader input)
		{
			Palette result = null;

			StringWriter sw = new StringWriter();
			int len = input.ReadInt32();
			for(int i = 0; i < len; i++)
				sw.Write(input.ReadChar());
			sw.Flush();

			int width = input.ReadInt32();

			result = new Palette(width, 1, PaletteType.OneDimensional);
			result.Name = sw.ToString();
			byte r, g, b;
			Color c;
			for(int i = 0; i < width; i++)
			{
				r = input.ReadByte();
				g = input.ReadByte();
				b = input.ReadByte();
				c = Color.FromArgb(255, (int)r, (int)g, (int)b);
				result.SetPixel(i, 0, c);
			}

			return result;
		}


		public static Palette GenerateDefaultPalette()
		{
			Palette pal = new Palette(DefaultRes,DefaultRes,PaletteType.Default);

			for(int y = 0; y < pal.Height; y++)
			{
				float fy = 1.0f - 2.0f * (float)y / (float)(pal.Height-1);
				for(int x = 0; x < pal.Width; x++)
				{
					float fx = 2.0f * (float)x / (float)(pal.Width-1) - 1.0f;
					pal.pix[y,x] = ChromaToColor(fx, fy);
				}
			}

			return pal;
		}

		const double Sqrt3 = 1.7320508075688772935274463415059;
		private static Color ChromaToColor(double x, double y)
		{
			double r = 0.0;
			double g = 0.0;
			double b = 0.0;
			double len = x*x + y*y;
			double theta;
			double f;

			if(len == 0.0)
				return Color.FromArgb(255,255,255);
			
			len = Math.Sqrt(len);
			x /= len;
			y /= len;

			double s = Math.Min(len,1.0);

			if(y >= 0)
			{
				if(y < Sqrt3*x )
				{
					theta = Math.Asin(y);
					f = 3.0 * theta / Math.PI;
					r = 1.0;
					g = 1.0 - (1.0-f)*s;
					b = 1.0 - s;
				}
				else if(y > -Sqrt3*x)
				{
					theta = Math.Acos(x);
					f = 3.0 * theta / Math.PI - 1.0;
					r = 1.0 - f*s;
					g = 1.0;
					b = 1.0 - s;
				}
				else
				{
					theta = Math.PI - Math.Asin(y);
					f = 3.0 * theta / Math.PI - 2.0;
					r = 1.0 - s;
					g = 1.0;
					b = 1.0 - (1.0-f)*s;
				}
			}
			else
			{
				if(y > Sqrt3*x )
				{
					theta = Math.PI - Math.Asin(y);
					f = 3.0 * theta / Math.PI - 3.0;
					r = 1.0 - s;
					g = 1.0 - f*s;
					b = 1.0;
				}
				else if(y < -Sqrt3*x)
				{
					theta = 2.0f*Math.PI - Math.Acos(x);
					f = 3.0 * theta / Math.PI - 4.0;
					r = 1.0 - (1.0-f)*s;
					g = 1.0 - s;
					b = 1.0;
				}
				else
				{
					theta = 2.0*Math.PI + Math.Asin(y);
					f = 3.0 * theta / Math.PI - 5.0;
					r = 1.0;
					g = 1.0 - s;
					b = 1.0 - f*s;
				}
			}

			int br = Util.Clamp((int)(r*255.0), 0, 255);
			int bg = Util.Clamp((int)(g*255.0), 0, 255);
			int bb = Util.Clamp((int)(b*255.0), 0, 255);
			return Color.FromArgb(br, bg, bb);
		}
	}
}
