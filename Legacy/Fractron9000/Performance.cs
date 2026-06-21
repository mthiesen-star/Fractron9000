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
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics;
using Cuda;
using MTUtil;

namespace Fractron9000
{
	public static class Performance
	{
		public static ulong FrameCount;
		public static ulong IterationCount;
		public static ulong TotalDotCount;
		public static double MeanDensity;
		public static double PeakDensity;
		
		private static Dictionary<string,PerformanceCounter> counters = new Dictionary<string,PerformanceCounter>();
		private static Stopwatch sinceResetClock = new Stopwatch();


		public static double SecondsSinceReset{
			get{ return sinceResetClock.Elapsed.TotalSeconds; }
		}

		public static void Reset()
		{
			FrameCount = 0;
			IterationCount = 0;
			TotalDotCount = 0;
			PeakDensity = 0;
			foreach(var pc in counters)
				pc.Value.Reset();

			sinceResetClock.Reset();
			sinceResetClock.Start();
		}

		public static void Start(string str)
		{
			PerformanceCounter c;
			if(!counters.TryGetValue(str, out c))
			{
				c = new PerformanceCounter();
				counters.Add(str, c);
			}
			c.Start();
		}

		public static void Stop(string str)
		{
			PerformanceCounter c;
			if(!counters.TryGetValue(str, out c))
			{
				c = new PerformanceCounter();
				counters.Add(str, c);
			}
			c.Stop();
		}

		public static void RecordTime_s(string str, double timeInSeconds)
		{
			PerformanceCounter c;
			if(!counters.TryGetValue(str, out c))
			{
				c = new PerformanceCounter();
				counters.Add(str, c);
			}
			c.RecordTime_s(timeInSeconds);
		}

		public static double FrameRate{
			get{ return (double)FrameCount / SecondsSinceReset; }
		}

		public static double DotsPerSecond{
			get{ return (double)TotalDotCount / SecondsSinceReset; }
		}

		public static double IterationsPerSecond{
			get{ return (double)IterationCount / SecondsSinceReset; }
		}

		public static string GetPerformanceMessage()
		{
			var s = new System.IO.StringWriter();
			foreach(var pc in counters)
			{
				double pct = pc.Value.TotalTime_s / SecondsSinceReset;
				s.Write("{0}:{1,6:0.000}ms({2,6:0.00%}) ", pc.Key, pc.Value.AverageTime_ms, pct);
			}
			return s.ToString();
		}
	}

	public class PerformanceCounter
	{
		Stopwatch timer = new Stopwatch();
		double time_s;
		int count;

		public double AverageTime_s{
			get{
				return count > 0 ? TotalTime_s/(double)count : double.NaN;
			}
		}

		public double AverageTime_ms{
			get{
				return AverageTime_s * 1000.0;
			}
		}

		public double TotalTime_s{
			get{ return time_s; }
		}

		public int TotalCount{
			get{ return count; }
		}

		public void Reset()
		{
			timer.Reset();
			time_s = 0.0;
			count = 0;
		}

		public void Start()
		{
			timer.Reset();
			timer.Start();
		}

		public void Stop()
		{
			timer.Stop();
			time_s += timer.Elapsed.TotalSeconds;
			count++;
		}

		public void RecordTime_s(double timeInSeconds)
		{
			time_s += timeInSeconds;
			count++;
		}

		public void RecordTime_ms(double timeInMiliseconds)
		{
			RecordTime_s(timeInMiliseconds / 1000.0);
		}
	}
}