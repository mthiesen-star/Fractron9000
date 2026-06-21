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
	public class SettingsCtl : UserControl
	{
		SettingCollection settings;

		//gives the control a list of settings to manage
		public SettingsCtl( SettingCollection settings )
		{
			this.settings = settings;
			SettingSubCtl sub;

			foreach( Setting s in settings )
			{
				sub = new SettingSubCtl( s );
				Controls.Add( sub );
			}
		
		}

		protected override void OnLayout(LayoutEventArgs e)
		{
			int y = 0;
			foreach( Control c in Controls )
			{
				SettingSubCtl sub = c as SettingSubCtl;
				if( sub != null )
				{
					sub.Location = new Point( 0, y );
					sub.Width = ClientSize.Width;

					y += sub.Height;
				}
			}

			base.OnLayout(e);
		}

		private class SettingSubCtl : UserControl
		{
			const int nameAreaWidth = 80;
			const int maxTicks = 64;

			Setting setting;
			TrackBar slider;
			Label nameLabel;
			Label valueLabel;
			
			public SettingSubCtl( Setting setting )
			{
				this.setting = setting;
				
				slider = new TrackBar();
				nameLabel = new Label();
				valueLabel = new Label();

				nameLabel.Text = setting.Name + ":";
				valueLabel.Text = setting.IntValue.ToString();

				slider.Minimum = setting.SliderMin();
				slider.Maximum = setting.SliderMax();

				if(slider.Maximum-slider.Minimum > maxTicks)
					slider.TickStyle = TickStyle.None;
				else
					slider.TickStyle = TickStyle.BottomRight;

				slider.Value = Util.Clamp(setting.ToSlider(), slider.Minimum, slider.Maximum);
				slider.ValueChanged += handleSliderChanged;

				//setting.Changed += handleSettingChanged;

				Controls.Add( nameLabel );
				Controls.Add( valueLabel );
				Controls.Add( slider );

				BorderStyle = BorderStyle.Fixed3D;
			}

			private void handleSettingChanged( Setting sender )
			{
				valueLabel.Text = setting.StringValue;
				slider.Value = Util.Clamp(setting.ToSlider(), slider.Minimum, slider.Maximum);
				Invalidate( true );
			}

			private void handleSliderChanged( Object obj, EventArgs ea )
			{
				setting.FromSlider(slider.Value);
				valueLabel.Text = setting.StringValue;
			}

			protected override Size DefaultSize
			{
				get{
					return new Size( 256, 50 );
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
				slider.Size = new Size( ClientSize.Width - nameAreaWidth - 8, ClientSize.Height - 8 );

				base.OnLayout(e);
			}

		}

	}
}
