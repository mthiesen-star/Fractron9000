#region License
/*
    Fractron 9000
    Copyright (C) 2009 Michael J. Thiesen
	http://mike.thiesen.us/projects/fractron-9000
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
using System.IO;
using System.Xml;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using MTUtil;

namespace Fractron9000
{
	//Used to export the giant palette data array form apophysis to image files
	public static class PaletteDataExport
	{
		public static void ExportPaletteImages()
		{
			string outDir = "D:\\projects\\fractron9000\\Fractron9000\\palettes_1d";
			Bitmap bmp = new Bitmap(256,1,PixelFormat.Format24bppRgb);

			for(int i = 0; i < PaletteData.Names.Length; i++)
			{
				string name = string.Format("{0}.png", PaletteData.Names[i]);
				string fileName = Path.Combine(outDir, name);

				for(int j = 0; j < 256; j++)
				{
					Color c = Color.FromArgb(255,
						(int)PaletteData.ColorData[i,j,0], (int)PaletteData.ColorData[i,j,1], (int)PaletteData.ColorData[i,j,2]);
					bmp.SetPixel(j, 0, c);
				}

				
				bmp.Save(fileName, ImageFormat.Png);
			}
		}

		public static void ExportPaletteFile()
		{
			string fileName = "D:\\projects\\fractron9000\\Fractron9000\\Resources\\palette_data_1d";

			Stream fs = File.Open(fileName, FileMode.Create, FileAccess.Write);
			BinaryWriter writer = new BinaryWriter(fs);

			int count = PaletteData.Names.Length;
			writer.Write(count);

			for(int i = 0; i < count; i++)
			{
				string name = PaletteData.Names[i];
				int len = name.Length;
				writer.Write(len);
				foreach(char ch in name)
					writer.Write(ch);

				writer.Write((int)256);

				for(int j = 0; j < 256; j++)
				{
					writer.Write( PaletteData.ColorData[i,j,0] );
					writer.Write( PaletteData.ColorData[i,j,1] );
					writer.Write( PaletteData.ColorData[i,j,2] );
				}
			}

			writer.Close();
		}

	}
}