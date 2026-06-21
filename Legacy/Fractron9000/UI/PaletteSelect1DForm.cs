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
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fractron9000.UI
{
	public partial class PaletteSelect1DForm : Form
	{
		private Palette palette = null;
		public Palette Palette{
			get{ return palette; }
		}

		private Palette[] palettes = null;

		public PaletteSelect1DForm()
		{
			InitializeComponent();
			paletteList.LargeImageList = new ImageList();
			paletteList.LargeImageList.ImageSize = new Size(256,16);
			paletteList.LargeImageList.ColorDepth = ColorDepth.Depth32Bit;

			paletteList.SmallImageList = new ImageList();
			paletteList.SmallImageList.ImageSize = new Size(256,16);
			paletteList.SmallImageList.ColorDepth = ColorDepth.Depth32Bit;

			MemoryStream ms = new MemoryStream(Properties.Resources.palette_data_1d, false);

			BinaryReader input = new BinaryReader(ms);

			palettes = Palette.Read1DArray(input);
			ms.Close();

			for(int i = 0; i < palettes.Length; i++)
			{
				Bitmap bmp = getBitmapFromPalette(palettes[i]);
				paletteList.LargeImageList.Images.Add(bmp);
				paletteList.SmallImageList.Images.Add(bmp);

				ListViewItem item = new ListViewItem(palettes[i].Name, i);
				item.Tag = (object)i;

				paletteList.Items.Add(item);
			}

			/*
			for(int i = 0; i < PaletteData.Names.Length; i++)
			{
				Bitmap bmp = getBitmapFromData(i);
				paletteList.LargeImageList.Images.Add(bmp);
				paletteList.SmallImageList.Images.Add(bmp);

				ListViewItem item = new ListViewItem(PaletteData.Names[i], i);
				item.Tag = (object)i;

				paletteList.Items.Add(item);
			}*/
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			okButton.Enabled = false;
		}

		private void paletteList_SelectedIndexChanged(object sender, EventArgs e)
		{
			okButton.Enabled = (paletteList.SelectedIndices.Count == 1);
		}

		private void handleSelectionConfirmed(object sender, EventArgs e)
		{
			if(paletteList.SelectedItems.Count != 1 || !(paletteList.SelectedItems[0].Tag is int))
				return;

			int idx = (int)paletteList.SelectedItems[0].Tag;
			this.palette = makePalette(idx);
			this.DialogResult = DialogResult.OK;
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			this.DialogResult = DialogResult.Cancel;
			Close();
		}

		private static unsafe Bitmap getBitmapFromPalette(Palette palette)
		{
			Bitmap bmp = new Bitmap(256,16,PixelFormat.Format32bppArgb);
			//byte[,,] cd = PaletteData.ColorData;
			BitmapData dat = bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format32bppPArgb);

			byte* rowPtr = (byte*)dat.Scan0;
			uint* pixPtr;
			for(int y = 0; y < bmp.Height; y++)
			{
				pixPtr = (uint*)rowPtr;
				for(int x = 0; x < bmp.Width; x++)
				{
					//pixPtr[x] = 0xFF00FFFF;
					pixPtr[x] = (uint)palette.GetPixel(x,0).ToArgb();
				}
				rowPtr += dat.Stride;
			}
			bmp.UnlockBits(dat);
			return bmp;
		}

		private Palette makePalette(int idx)
		{
			if(idx > 0 && idx < palettes.Length)
				return palettes[idx];
			else
				return null;
		}
	}
}
