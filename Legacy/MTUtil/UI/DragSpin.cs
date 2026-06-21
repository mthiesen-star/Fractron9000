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

namespace MTUtil.UI
{
	public class DragSpin : UserControl
	{
		const int DefaultTextBoxWidth = 40;
		const int MinTextBoxWidth = 20;

		private static Bitmap background = null;

		public event EventHandler ValueChanged;

		double val = 1.0;
		[Browsable(true)]
		[DefaultValue(1.0)]
		public double Value{
			get{ return val; }
			set{
				changeValByCode(value);
				dirty = true;
			}
		}

		double minVal = 0.0;
		[Browsable(true)]
		[DefaultValue(0.0)]
		public double MinVal
		{
			get{ return minVal; }
			set{
				minVal = value;
				dirty = true;
			}
		}

		double maxVal = 4.0;
		[Browsable(true)]
		[DefaultValue(4.0)]
		public double MaxVal
		{
			get{ return maxVal; }
			set{
				maxVal = value;
				dirty = true;
			}
		}

		double majorTickStep = 1.0;
		[Browsable(true)]
		[DefaultValue(1.0)]
		public double MajorTickStep{
			get{ return majorTickStep; }
			set{
				majorTickStep = value;
				dirty = true;
			}
		}

		int minorTicksPerMajorTick = 16;
		[Browsable(true)]
		[DefaultValue(16)]
		public int MinorTicksPerMajorTick{
			get{ return minorTicksPerMajorTick; }
			set{
				minorTicksPerMajorTick = value;
				dirty = true;
			}
		}

		int pixelsPerMinorTick = 16;
		[Browsable(true)]
		[DefaultValue(16)]
		public int PixelsPerMinorTick{
			get{ return pixelsPerMinorTick; }
			set{
				pixelsPerMinorTick = value;
				dirty = true;
			}
		}
		
		string formatString = "0.###";
		[Browsable(true)]
		[DefaultValue("0.###")]
		public string FormatString{
			get{ return formatString; }
			set{ formatString = value; }
		}

		public double MinorTickStep{
			get{ return majorTickStep / minorTicksPerMajorTick; }
		}

		public double PixStep{
			get{ return majorTickStep / (minorTicksPerMajorTick*pixelsPerMinorTick); }
		}

		protected override Size DefaultMinimumSize
		{
			get{ return new Size( MinTextBoxWidth+20, 20 ); }
		}

		protected override Size DefaultMaximumSize
		{
			get{ return new Size(0, 20); }
		}

		protected override Size DefaultSize
		{
			get{ return new Size( DefaultTextBoxWidth+20, 20 ); }
		}

		TextBox textBox;
		DragControl dragger;
		Bitmap bitmap;
		bool dirty;

		private bool dragging;
		double dragStartVal;
		double dragDelta;
		bool dragSnap = false;
		Point origMousePos;
		Point prevMousePos;

		Rectangle dragZone;
		Point dragZoneCenter;
		
		public DragSpin()
		{
			bitmap = new Bitmap(20,20);
			dirty = true;
			textBox = new TextBox();
			dragger = new DragControl();
			dragger.Cursor = Cursors.SizeNS;

			if(background == null)
			{
				try{
					background = new Bitmap(this.GetType(), "DragSpin.bmp");
				}
				catch(ArgumentException)
				{
					background = new Bitmap(20,20);
					Graphics gfx = Graphics.FromImage(background);
					gfx.FillRectangle(Brushes.White, 0, 0, 20, 20);
					gfx.DrawLine(Pens.Black, 0, 0, 20, 20);
					gfx.DrawLine(Pens.Black, 0, 20, 20, 0);
				}
			}		

			dragger.Paint += draggerPaint;
			dragger.KeyDown += draggerKeyDown;
			dragger.KeyUp += draggerKeyUp;
			dragger.MouseDown += draggerMouseDown;
			dragger.MouseUp += draggerMouseUp;
			dragger.MouseMove += draggerMouseMove;
			dragger.LostFocus += draggerLostFocus;

			dragger.MouseWheel += handleMouseWheel;
			textBox.MouseWheel += handleMouseWheel;

			textBox.Text = val.ToString(formatString);
			textBox.TextChanged += handleTextChanged;
			textBox.LostFocus += handleTextLostFocus;

			Controls.Add( textBox );
			Controls.Add( dragger );
		}

		/// <summary>
		/// Change the spinner's value without triggering the ValueChanged event.
		/// </summary>
		/// <param name="newVal">The new value</param>
		public void SetValueStealth(double newVal)
		{
			val = newVal;
			dirty = true;
			dragger.Invalidate();
			setTextStealth(val.ToString(formatString));
		}

		private void setTextStealth(string text)
		{
			textBox.TextChanged -= handleTextChanged;
			if(textBox != null) textBox.Text = text;
			textBox.TextChanged += handleTextChanged;
		}

		private void changeValUI(double newVal)
		{
			newVal = Util.Clamp(newVal, minVal, maxVal);
			if(newVal != val)
			{
				val = newVal;
				textBox.Text = val.ToString(formatString);
				dirty = true;
				if(ValueChanged != null)
					ValueChanged(this,EventArgs.Empty);
			}
		}

		private void changeValByCode(double newVal)
		{
			newVal = Util.Clamp(newVal, minVal, maxVal);
			if(newVal != val)
			{
				val = newVal;
				dirty = true;
				if(textBox != null)
					textBox.Text = val.ToString(formatString);
			}
		}

		private void updateTickSteps()
		{
		}

		private void handleTextChanged(object sender, EventArgs e)
		{
			double textVal;

			if(double.TryParse(textBox.Text, out textVal) &&
				textVal <= maxVal && textVal >= minVal && textVal != val)
			{
				val = textVal;
				dirty = true;
				if(ValueChanged != null)
					ValueChanged(this,EventArgs.Empty);
			}
		}
		private void handleTextLostFocus(object sender, EventArgs e)
		{
			double textVal;
			bool ok = double.TryParse(textBox.Text, out textVal);

			if(!ok)
			{
				textVal = val;
				textBox.Text = textVal.ToString(formatString);
			}
			else if(textVal > maxVal)
			{
				textVal = maxVal;
				textBox.Text = textVal.ToString(formatString);
			}
			else if(textVal < minVal)
			{
				textVal = minVal;
				textBox.Text = textVal.ToString(formatString);
			}

			if(val != textVal)
			{
				val = textVal;
				dirty = true;
				if(ValueChanged != null)
					ValueChanged(this,EventArgs.Empty);
			}
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			if(!Enabled)
			{
				if(dragger.Capture)
					dragger.Capture = false;
				if(dragging)
				{
					dragging = false;
					Cursor.Show();
				}
			}
			base.OnEnabledChanged(e);
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			int pixelBudget = ClientSize.Width;
			pixelBudget -= 20;
			dragger.Location = new Point(pixelBudget,0);
			dragger.Size = new Size(20,20);

			pixelBudget -= textBox.Width;
			textBox.Location = new Point( 0, 0 );
			textBox.Size = new Size(ClientSize.Width-20, 20);
		}

		private void draggerPaint(object sender, PaintEventArgs e)
		{
			if(dirty)
				updateBitmap();

			Graphics gfx = e.Graphics;
			if(Enabled && bitmap != null)
				gfx.DrawImage(bitmap, 0, 0, 20, 20);
			else
				gfx.FillRectangle(Brushes.Gray, 0, 0, ClientSize.Height, ClientSize.Width);
		}

		private void updateBitmap()
		{
			int pixelsPerMajorTick = pixelsPerMinorTick * minorTicksPerMajorTick;
			double pixStep = majorTickStep / pixelsPerMajorTick;
			int pixelsPerMicroTick = 4;

			pixStep = majorTickStep / (double)pixelsPerMajorTick;
			
			Graphics gfx = Graphics.FromImage(bitmap);

			if(background != null)
				gfx.DrawImage(background, 0, 0, 20, 20);
			else
				gfx.FillRectangle(Brushes.Gray, 0, 0, ClientSize.Height, ClientSize.Width);

			int valPos = (int)Math.Round(val/pixStep);
			int minPos = (int)Math.Round(minVal/pixStep);
			int maxPos = (int)Math.Round(maxVal/pixStep);

			int lowBound  = valPos - 10;
			int highBound = valPos + 10;

			int left = 5;
			int right = 15;

			for(int i = 1; i < 19; i++)
			{
				int pixPos = i + lowBound;
				
				if(pixPos >= minPos && pixPos <= maxPos)
				{
					if(pixPos % pixelsPerMajorTick == 0){
						left = 2;
						right = 17;
						gfx.DrawLine(Pens.Black, left, i, right, i);
					}else if(pixPos % pixelsPerMinorTick == 0){
						left = 4;
						right = 15;
						gfx.DrawLine(Pens.Black, left, i, right, i);
					}else if(pixPos % pixelsPerMicroTick == 0){
						left = 6;
						right = 13;
						gfx.DrawLine(Pens.Black, left, i, right, i);
					}
				}
			}
		}

		private void handleMouseWheel(object sender, MouseEventArgs e)
		{
			if(e.Delta < 0)
			{
				changeValUI(Util.SnapToGrid(val, MinorTickStep) - MinorTickStep);
			}
			else if(e.Delta > 0)
			{
				changeValUI(Util.SnapToGrid(val, MinorTickStep) + MinorTickStep);
			}
		}

		private void draggerKeyDown(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.ControlKey)
				dragSnap = true;
		}

		private void draggerKeyUp(object sender, KeyEventArgs e)
		{
			if(e.KeyCode == Keys.ControlKey)
				dragSnap = false;
		}

		private void draggerMouseDown(object sender, MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left)
				draggerCapture();
		}

		private void draggerCapture()
		{
			Rectangle screenRect = Screen.PrimaryScreen.Bounds;

			dragZone.Location = new Point(
				screenRect.Location.X + screenRect.Width/4,
				screenRect.Location.Y + screenRect.Height/4);

			dragZone.Size = new Size(screenRect.Size.Width/2, screenRect.Size.Height/2);
			dragZoneCenter = new Point(dragZone.Left+dragZone.Width/2, dragZone.Top+dragZone.Height/2);
			
			dragStartVal = val;
			dragDelta = 0.0;

			origMousePos = Cursor.Position;
			dragging = true;
			Cursor.Hide();
			Cursor.Position = dragZoneCenter;
			dragger.Capture = true;
			prevMousePos = dragZoneCenter;
		}

		private void draggerMouseUp(object sender, MouseEventArgs e)
		{
			if(e.Button == MouseButtons.Left)
				draggerRelease();
		}

		private void draggerRelease()
		{
			dragger.Capture = false;
			if(dragging)
			{
				dragging = false;
				Cursor.Position = origMousePos;
				Cursor.Show();
			}
		}

		private void draggerMouseMove(object sender, MouseEventArgs e)
		{
			if(!dragging) return;

			int dx = Cursor.Position.X - prevMousePos.X;
			int dy = Cursor.Position.Y - prevMousePos.Y;

			if(!dragZone.Contains(Cursor.Position))
			{
				dragger.Capture = false;
				Cursor.Position = dragZoneCenter;
				dragger.Capture = true;
			}

			prevMousePos = Cursor.Position;
			
			if(dy != 0)
			{
				dragDelta -= PixStep*(double)dy;
				double newVal = dragStartVal + dragDelta;

				if(dragSnap)
					newVal = Util.SnapToGrid(newVal, MinorTickStep);

				if(newVal < minVal){
					newVal = minVal;
					dragStartVal = val;
					dragDelta = 0;
				}
				else if(newVal > maxVal){
					newVal = maxVal;
					dragStartVal = val;
					dragDelta = 0;
				}

				if(newVal != val)
				{
					val = newVal;
					dragStartVal = val;
					dragDelta = 0;
					setTextStealth(val.ToString(formatString));
					dirty = true;
					dragger.Invalidate();
					if(ValueChanged != null)
						ValueChanged(this,EventArgs.Empty);
				}
			}
		}

		private void draggerLostFocus(object sender, EventArgs e)
		{
			draggerRelease();
		}

		private class DragControl : UserControl
		{
			public DragControl(){}
			protected override void OnPaintBackground(PaintEventArgs e){}
		}
	}
}