#region License
/*
    Fractron 9000
    Copyright (C) 2009 Michael J. Thiesen
	http://fractron9000.sourceforge.net
	mike@thiesen.us

    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.
*/
#endregion

using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Drawing.Imaging;

using OpenTK.Graphics.OpenGL;

namespace Fractron9000
{
	public struct InfoEntry{
		public string Name;
		public object Value;
		public InfoEntry(string name, object value){
			this.Name = name;
			this.Value = value;
		}
	}

	abstract public class FractalEngine : IDisposable
	{
		private OpenTK.Graphics.IGraphicsContext context;
		
		public OpenTK.Graphics.IGraphicsContext Context{
			get{ return context; }
			set{ context = value; }
		}

		abstract public int XRes{
			get;
		}
		abstract public int YRes{
			get;
		}

		public FractalEngine()
		{
		}

		virtual public bool IsAllocated()
		{
			return true;
		}

		virtual public void Allocate(int xRes, int yRes){}
		virtual public void Deallocate(){}
		virtual public void Destroy(){}

		abstract public bool IsBusy();
		abstract public void Synchronize();

		abstract public void ApplyParameters(Fractal fractal);
		abstract public void ApplyPalette(Palette palette);
		abstract public void ResetOutput();
		abstract public void DoIterationCycle(int numIterationsPerThread);
		abstract public void CalcToneMap();
		abstract public void CopyToneMap();
		abstract public Stats GatherStats();
		abstract public int GetOutputTextureId();
		abstract public Color[,] GetOutputPixels();

		//virtual public string GetDeviceName(){ return "Unknown video device"; }
		//virtual public IEnumerable<KeyValuePair<string,object>> GetDeviceInfo()
		//{
		//	yield break;
		//}

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if(disposing){
				if(IsAllocated())
					Deallocate();
				Destroy();
			}
		}

		#endregion

		//the result will be a 2d array of size [yRes,xRes]
		protected static Color[,] GetPixelsFromBuffer(int xRes, int yRes, uint bufferID)
		{
			Color[,] result = new Color[yRes,xRes];

			GL.BindBuffer(BufferTarget.PixelUnpackBuffer, bufferID);
			Color color = Color.Black;
			unsafe
			{
				byte* basePtr = (byte*)GL.MapBuffer(BufferTarget.PixelUnpackBuffer, BufferAccess.ReadOnly);
				byte* pixPtr = basePtr;
				for(int y = 0; y < yRes; y++)
				{
					int flipY = yRes-1-y;  //used to vertically flip the output
					for(int x = 0; x < xRes; x++)
					{
						result[flipY,x] = Color.FromArgb((int)pixPtr[3], (int)pixPtr[0], (int)pixPtr[1], (int)pixPtr[2]);
						pixPtr += 4;
					}
				}
				GL.UnmapBuffer(BufferTarget.PixelUnpackBuffer);
			}
			GL.BindBuffer(BufferTarget.PixelUnpackBuffer,0);

			return result;
		}

		//the result will be a 2d array of size [texWidth,texHeight]
		unsafe protected static Color[,] GetPixelsFromTexture(int texID)
		{
			int xRes;
			int yRes;

			GL.BindTexture(TextureTarget.Texture2D, texID);
			GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureWidth,  &xRes);
			GL.GetTexLevelParameter(TextureTarget.Texture2D, 0, GetTextureParameter.TextureHeight, &yRes);

			Color[,] result = new Color[yRes,xRes];
			uint[] pix = new uint[yRes*xRes];
			uint val;
			uint r,g,b,a;
			GL.GetTexImage(TextureTarget.Texture2D, 0, OpenTK.Graphics.OpenGL.PixelFormat.Rgba, PixelType.UnsignedInt8888, pix);

			for(int y = 0; y < yRes; y++)
			{
				int flipY = yRes-1-y;  //used to vertically flip the output
				for(int x = 0; x < xRes; x++)
				{
					val = pix[y*xRes + x];
					r = (val & 0xFF000000) >> 24;
					g = (val & 0x00FF0000) >> 16;
					b = (val & 0x0000FF00) >> 8;
					a = (val & 0x000000FF);

					result[flipY,x] = Color.FromArgb((int)a, (int)r, (int)g, (int)b);
				}
			}
			GL.BindTexture(TextureTarget.Texture2D, 0);

			return result;
		}

		public struct Stats{
			public UInt64 TotalIterCount;
			public UInt64 TotalDotCount;
			public float meanDotsPerSubpixel;
		}
	}
}
