#region License
/*
    MTUtil - Miscellaneous utility classes 
    Copyright (C) 2009 Michael J. Thiesen
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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace Fractron9000
{
	public class ToolButton : UserControl
	{
		private bool hasMouse = false;
		private bool pressed = false;

		private static Pen edgePenLight = new Pen(Color.FromArgb(128,128,128,128));
		private static Pen edgePenDark =  new Pen(Color.FromArgb(255,64,64,64));

		private static Pen shadow1 = new Pen(Color.FromArgb(240, 0, 0, 0));
		private static Pen shadow2 = new Pen(Color.FromArgb(128, 0, 0, 0));
		private static Pen shadow3 = new Pen(Color.FromArgb(64 , 0, 0, 0));

		private Image image;
		[Browsable(true), Description("The image associated with the button"), Category("Appearance")]
		public Image Image
		{
			get{ return image; }
			set{
				image = value;
				Invalidate();
			}
		}


		protected override Size DefaultMinimumSize
		{
			get{ return new Size( 22, 22 ); }
		}

		protected override Size DefaultMaximumSize
		{
			get{ return new Size( 22, 22); }
		}

		protected override Size DefaultSize
		{
			get{ return new Size( 22, 22 ); }
		}
		
		public ToolButton()
		{
			DoubleBuffered = true;
		}

		protected override void OnPaintBackground(PaintEventArgs e)
		{
			base.OnPaintBackground(e);

			if(pressed)
			{
				e.Graphics.DrawRectangle(shadow1, 0, 0, ClientSize.Width-1, ClientSize.Height-1);
				e.Graphics.DrawRectangle(shadow2, 1, 1, ClientSize.Width-3, ClientSize.Height-3);
				e.Graphics.DrawRectangle(shadow3, 2, 2, ClientSize.Width-5, ClientSize.Height-5);
			}
			else if(hasMouse)
			{
				e.Graphics.DrawRectangle(edgePenLight, 0, 0, ClientSize.Width-1, ClientSize.Height-1);
			}
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			Graphics gfx = e.Graphics;

			if(image == null)
				gfx.FillRectangle(Brushes.Black, 3, 3, 16, 16);
			else
				gfx.DrawImage(image, 3, 3, 16, 16);

			base.OnPaint(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			base.OnMouseDown(e);
			if(e.Button == MouseButtons.Left)
			{
				pressed = true;
				Invalidate();
			}
		}

		protected override void OnMouseUp(MouseEventArgs e)
		{
			base.OnMouseUp(e);
			if(e.Button == MouseButtons.Left)
			{
				pressed = false;
				Invalidate();
			}
		}

		protected override void OnMouseEnter(EventArgs e)
		{
			base.OnMouseEnter(e);
			hasMouse = true;
			Invalidate();
		}

		protected override void OnMouseLeave(EventArgs e)
		{
			base.OnMouseLeave(e);
			hasMouse = false;
			pressed = false;
			Invalidate();
		}

	}
}