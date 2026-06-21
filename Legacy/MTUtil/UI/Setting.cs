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
using MTUtil;

namespace MTUtil.UI
{
	public delegate void SettingChangedDelegate( Setting sender );

	public class Setting
	{
		private string name;
		private double val = 0;
		private double minVal = 0;
		private double maxVal = 100;

		public event SettingChangedDelegate Changed;

		public Setting( string name, double val, double min, double max )
		{
			this.name = name;
			this.minVal = min;
			this.maxVal = max;
			this.val = Constrain(val);
		}

		public string Name
		{
			get{
				return name;
			}
		}

		public double MinVal{
			get{ return minVal; }
		}

		public double MaxVal{
			get{ return maxVal; }
		}

		public double Value
		{
			get{
				return val;
			}
			set{
				double oldVal = val;
				val = Constrain(value);
				if( val != oldVal && Changed != null )
					Changed( this );
			}
		}

		public int IntValue{
			get{ return (int)val; }
			set{ Value = (double)value; }
		}

		public float FloatValue{
			get{ return (float)val; }
			set{ Value = (float)value; }
		}

		public string StringValue{
			get{ return val.ToString("f5"); }
		}

		virtual protected double Constrain( double n )
		{
			return Util.Clamp(n, minVal, maxVal);
		}

		virtual public int SliderMin()
		{
			return (int)MinVal;
		}

		virtual public int SliderMax()
		{
			return (int)MaxVal;
		}

		virtual public int ToSlider()
		{
			return (int)val;
		}

		virtual public void FromSlider( int n )
		{
			val = (double)n;
		}

		public override bool Equals(object obj)
		{
			return base.Equals(obj as Setting);
		}

		public bool Equals( Setting s )
		{
			return s != null && s.name.Equals( name );
		}

		public override int GetHashCode()
		{
			return name.GetHashCode();
		}
	}

	public class IntSetting : Setting
	{
		public IntSetting( string name, int val, int minVal, int maxVal )
			: base( name, val, minVal, maxVal )
		{}

		protected override double Constrain( double n )
		{
			return Util.Clamp(Math.Round(n,0), MinVal, MaxVal);
		}
	}

	public class DoubleSetting : Setting
	{
		public DoubleSetting( string name, double val, double minVal, double maxVal )
			: base( name, val, minVal, maxVal )
		{}

		public override int SliderMin(){
			return 0;
		}

		public override int SliderMax(){
			return 1000;
		}

		public override int ToSlider()
		{
			return (int)((Value-MinVal)/(MaxVal-MinVal)*1000);
		}

		public override void FromSlider(int n)
		{
			Value = MinVal + (MaxVal-MinVal)*(double)n/1000;
		}
	}

	public class LogSetting : Setting
	{
		private int ticksPerBase = 4;

		public LogSetting( string name, double val, double minVal, double maxVal )
			: base( name, val, minVal, maxVal )
		{}

		public LogSetting( string name, double val, double minVal, double maxVal, int ticksPerBase )
			: base( name, val, minVal, maxVal )
		{
			this.ticksPerBase = ticksPerBase;
		}

		public override int SliderMin(){
			return (int)(Math.Round((double)ticksPerBase*Math.Log(MinVal,2)));
		}

		public override int SliderMax(){
			return (int)(Math.Round((double)ticksPerBase*Math.Log(MaxVal,2)));
		}

		public override int ToSlider()
		{
			return (int)(Math.Round((double)ticksPerBase*Math.Log(Value,2)));
		}

		public override void FromSlider(int n)
		{
			Value = Math.Pow(2, (double)n/(double)ticksPerBase);
		}
	}
}
