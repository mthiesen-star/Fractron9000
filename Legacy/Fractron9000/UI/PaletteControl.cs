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
using System.Drawing;
using System.Windows.Forms;

using MTUtil;

namespace Fractron9000.UI
{
	public class ChromaControl : UserControl
	{
		public const int PaletteRes = 128;
		private static int ChromaWidth = PaletteRes;
		private static int ChromaHeight = PaletteRes;

		private bool mouseIsDown = false;

		private Bitmap background = null;
	
		public ChromaControl()
		{
			FractalManager.BranchSelected += (frac) =>
			{
				Invalidate();
			};
			FractalManager.PaletteChanged += (frac) =>
			{
				Palette bgPal = new Palette(PaletteRes, PaletteRes, frac.Palette);
				background = bgPal.ToBitmap();
				Invalidate();
			};
		}

		protected override Size DefaultSize{
			get{ return new Size(ChromaWidth,ChromaHeight); }
		}
		protected override Size DefaultMinimumSize{
			get{ return DefaultSize; }
		}

		private void DoColorSelection(int x, int y)
		{
			if(FractalManager.SelectedBranch != null)
			{
				float fx = Util.Clamp((float)(x) / (float)(PaletteRes-1), 0.0f, 1.0f);
				float fy = Util.Clamp((float)(y) / (float)(PaletteRes-1), 0.0f, 1.0f);

				FractalManager.SelectedBranch.Chroma = new Vec2(fx, fy);
				FractalManager.NotifyGeometryChanged();
				Invalidate();
			}
		}

		protected override void OnMouseDown(MouseEventArgs ea)
		{
			if(ea.Button == MouseButtons.Left)
			{
				DoColorSelection(ea.X, ea.Y);
				mouseIsDown = true;
			}
			base.OnMouseDown(ea);
		}

		protected override void OnMouseUp(MouseEventArgs ea)
		{
			if(ea.Button == MouseButtons.Left)
				mouseIsDown = false;

			base.OnMouseDown(ea);
		}

		protected override void OnMouseMove(MouseEventArgs ea)
		{
			if(mouseIsDown)
				DoColorSelection(ea.X,ea.Y);

			base.OnMouseMove(ea);
		}
		protected override void OnPaintBackground(PaintEventArgs e){}
		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics gfx = e.Graphics;
			gfx.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
			
			if(background != null)
				gfx.DrawImage(background, 0, 0, ClientSize.Width, ClientSize.Height);
			else
				gfx.FillRectangle(Brushes.Gray, 0, 0, ClientSize.Width, ClientSize.Height);

			if(FractalManager.SelectedBranch != null)
			{
				Vec2 colorVec = FractalManager.SelectedBranch.Chroma;
				int x = Util.Clamp((int)(colorVec.X * (float)(PaletteRes-1)), 0, PaletteRes-1);
				int y = Util.Clamp((int)(colorVec.Y * (float)(PaletteRes-1)), 0, PaletteRes-1);
				Pen outer = new Pen(Color.Black, 3.0f);
				Pen inner = new Pen(Color.White, 1.0f);

				gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
				gfx.DrawEllipse(outer, x - 3, y - 3, 6, 6);
				gfx.DrawLine(outer, x-8, y, x-3, y);
				gfx.DrawLine(outer, x+8, y, x+3, y);
				gfx.DrawLine(outer, x, y-8, x, y-3);
				gfx.DrawLine(outer, x, y+8, x, y+3);

				gfx.DrawEllipse(inner, x - 3, y - 3, 6, 6);
				gfx.DrawLine(inner, x-8, y, x-3, y);
				gfx.DrawLine(inner, x+8, y, x+3, y);
				gfx.DrawLine(inner, x, y-8, x, y-3);
				gfx.DrawLine(inner, x, y+8, x, y+3);

				gfx.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.Default;
			}
		}
	}
}
