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
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace MTUtil.UI
{
	//not finished.......
	/*
	public class NumericSliderControl : UserControl
	{
		const int nameAreaWidth = 80;

		double val;
		double minVal;
		double maxVal;

		TrackBar slider;
		Label nameLabel;
		Label valueLabel;

		public double Value{
			get{ return val; }
			set{ val = value; }
		}

		public double MinVal
		{
			get{ return minVal; }
			set
			{
				minVal = value;
				slider.Value = ValToIntScale();
			}
		}

		public double MaxVal
		{
			get{ return maxVal; }
			set
			{
				maxVal = value;
				slider.Value = ValToIntScale();
			}
		}
		
		public NumericSliderControl()
		{
			val = 0.0;
			minVal = 0.0;
			maxVal = 0.0;

			slider = new TrackBar();
			nameLabel = new Label();
			valueLabel = new Label();

			nameLabel.Text = "";
			valueLabel.Text = val.ToString();

			slider.Minimum = 0;
			slider.Maximum = 100;

			slider.TickStyle = TickStyle.None;

			slider.Value = 0;
			slider.ValueChanged += handleSliderChanged;

			Controls.Add( nameLabel );
			Controls.Add( valueLabel );
			Controls.Add( slider );

			BorderStyle = BorderStyle.Fixed3D;
		}

		private int ValToIntScale()
		{
			return Util.Clamp((int)(100.0*(val-minVal)/(maxVal-minVal)), 0, 100);
		}

		private double IntScaleToVal()
		{
			return minVal + (double)slider.Value*100.0 * (maxVal-minVal);
		}

		private void handleSliderChanged( Object obj, EventArgs ea )
		{
			val = (double)slider.Value / 100.0;
			valueLabel.Text = val.ToString();
		}

		protected override Size DefaultSize
		{
			get{
				return new Size( 256, 24 );
			}
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			int labelHeight = (ClientSize.Height / 2) - 8;

			nameLabel.Location = new Point( 4, 4 );
			nameLabel.Size = new Size( nameAreaWidth - 8, labelHeight );

			valueLabel.Location = new Point( 4, labelHeight + 4 );
			valueLabel.Size = new Size( nameAreaWidth - 8, labelHeight );

			slider.Location = new Point( nameAreaWidth + 4, 4 );
			//slider.Size = new Size( ClientSize.Width - nameAreaWidth - 8, ClientSize.Height - 8 );
			slider.Size = new Size( ClientSize.Width - nameAreaWidth - 8, 14 );
			base.OnLayout(e);
		}

	}*/
}