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
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using MTUtil;

namespace Fractron9000
{
	public delegate void FractalChangedHandler(Fractal fractal);

	public static class FractalManager
	{
		public static event FractalChangedHandler BranchAdded;
		public static event FractalChangedHandler BranchRemoved;
		public static event FractalChangedHandler BranchSelected;
		public static event FractalChangedHandler CurrentFractalChanged;
		public static event FractalChangedHandler GeometryChanged;
		public static event FractalChangedHandler ToneMapChagned;
		public static event FractalChangedHandler PaletteChanged;

		public static event EventHandler Blip; //used for debugging
		public static void SendBlip()
		{
			if(Blip != null)
				Blip(null, new EventArgs());
		}

		private static FractalList fractals = new FractalList();
		private static Fractal currentFractal = null;
		private static double t;
		
		//private static Fractal fractal = null;
		private static int selectedBranchIndex = -1;

		public static FractalList Fractals{
			get{ return fractals; }
		}

		public static void Init()
		{
			currentFractal = Fractal.BuildDefault();
		}

		public static Fractal Fractal{
			get{ return currentFractal; }
			//set{
			//	currentFractal = value;
			//	selectedBranchIndex = -1;
			//	NotifyFractalChanged();
			//	NotifyBranchSelected();
			//	NotifyGeometryChanged();
			//	NotifyToneMapChanged();
			//	NotifyPaletteChanged();
			//}
		}

		public static double Time{
			get{ return t; }
			set{ t = value; }
		}

		public static void StepAnim(double dt)
		{
			if(fractals.Count == 0) return;
			t += dt;
			double dblCount = (double)fractals.Count;
			while(t < 0.0) t += dblCount;
			while(t > dblCount)	t -= dblCount;
			InterpolateCurrentFractal();
		}

		public static void InterpolateCurrentFractal()
		{
			if(fractals.Count == 0)
				return;

			Fractal l = null;
			Fractal r = null;

			int fi = (int)t;
			float alpha = (float)( t - (double)fi );
			//float alpha = Util.SCurve((float)( t - (double)fi ));

			//Highly inefficient animation looping code. TODO: Fix this!
			while(fi < 0) fi += fractals.Count;
			while(fi >= fractals.Count)	fi -= fractals.Count;

			l = fractals[fi];
			r = (fi+1) < fractals.Count ? fractals[fi+1] : fractals[0];

			currentFractal.FillBlended(l, r, alpha);

			NotifyFractalChanged();
			if(selectedBranchIndex >= currentFractal.Branches.Count){
				selectedBranchIndex = -1;
				NotifyBranchSelected();
			}
			NotifyGeometryChanged();
			NotifyToneMapChanged();
			NotifyPaletteChanged();
		}

		public static void SetCurrentCopy(Fractal fractal)
		{
			if(fractal == null) return;
			currentFractal = new Fractal(fractal);
			currentFractal.MarkVersionAsFractron();
			selectedBranchIndex = -1;

			NotifyFractalChanged();
			NotifyBranchSelected();
			NotifyGeometryChanged();
			NotifyToneMapChanged();
			NotifyPaletteChanged();
		}

		public static void NextFractal()
		{
			if(fractals.Count <= 0) return;

			int idx = fractals.GetIndexByName(currentFractal.Name);

			if(idx == -1)
			{
				SetCurrentCopy(fractals[0]);
				return;
			}

			int next = idx+1;
			if(next >= fractals.Count)
				next = 0;

			SetCurrentCopy(fractals[next]);
		}

		public static void PrevFractal()
		{
			if(fractals.Count <= 0) return;

			int idx = fractals.GetIndexByName(currentFractal.Name);

			if(idx == -1)
			{
				SetCurrentCopy(fractals[fractals.Count-1]);
				return;
			}

			int prev = idx-1;
			if(prev < 0)
				prev = fractals.Count-1;

			SetCurrentCopy(fractals[prev]);
		}

		public static Affine2D CameraTransform{
			get{
				if(Fractal == null)
					return Affine2D.Identity;
				else
					return Fractal.CameraTransform;
			}
			set{
				Fractal.CameraTransform = value;
				NotifyGeometryChanged();
			}
		}

		public static void ChangeZoom(float ammount)
		{
			Affine2D ct = FractalManager.CameraTransform;
			ct.XAxis /= ammount;
			ct.YAxis /= ammount;
			FractalManager.CameraTransform = ct;
		}

		public static void ZoomIn()
		{
			ChangeZoom(1.189207115f);
		}

		public static void ZoomOut()
		{
			ChangeZoom(0.840896415f);
		}

		public static void FlipVertical()
		{
			if(Fractal != null)
			{
				Fractal.CameraTransform.YAxis *= -1.0f;
				NotifyGeometryChanged();
			}
		}

		public static void SetPalette(Palette pal)
		{
			Fractal.Palette = pal;
			NotifyPaletteChanged();
		}

		public static IList<Branch> Branches{
			get{ return Fractal.Branches; }
		}

		public static void AddBranch()
		{
			Branch branch = new Branch();
			branch.PreTransform = new Affine2D(0.5f, 0.0f, 0.0f, 0.5f, 0.0f, 0.0f);
			branch.Chroma = new Vec2(0.5f, 0.5f);
			Branches.Add( branch );
			SelectedBranch = branch;
			if(BranchAdded != null) BranchAdded(Fractal);
			NotifyGeometryChanged();
		}

		public static void RemoveSelectedBranch()
		{
			if(SelectedBranch != null)
			{
				Fractal.Branches.Remove(SelectedBranch);
				SelectedBranchIndex = -1;
				if(BranchRemoved != null)
					BranchRemoved(Fractal);
				NotifyGeometryChanged();
			}
		}

		public static void DuplicateSelectedBranch()
		{
			if(SelectedBranch != null)
			{
				Branch branch = new Branch(SelectedBranch);
				Branches.Add( branch );
				SelectedBranch = branch;
				if(BranchAdded != null) BranchAdded(Fractal);
				NotifyGeometryChanged();
			}
		}

		public static void InvertSelectedBranch()
		{
			if(FractalManager.SelectedBranch != null)
			{
				FractalManager.SelectedBranch.PreTransform = FractalManager.SelectedBranch.PreTransform.Inverse;
				FractalManager.SelectedBranch.PostTransform = FractalManager.SelectedBranch.PostTransform.Inverse;
				FractalManager.NotifyGeometryChanged();
			}
		}

		public static int SelectedBranchIndex{
			get{ return selectedBranchIndex; }
			set
			{
				if(value >= 0 && value < Fractal.Branches.Count)
					selectedBranchIndex = value;
				else
					selectedBranchIndex = -1;

				if(BranchSelected != null)
					BranchSelected(Fractal);
			}
		}

		public static Branch SelectedBranch
		{
			get
			{
				if(selectedBranchIndex >= 0 && selectedBranchIndex < Fractal.Branches.Count)
					return Fractal.Branches[selectedBranchIndex];
				else
					return null;
			}
			set
			{
				selectedBranchIndex = Fractal.Branches.IndexOf(value);
				if(BranchSelected != null)
					BranchSelected(Fractal);
			}
		}

		public static void NotifyFractalChanged()
		{
			if(CurrentFractalChanged != null) CurrentFractalChanged(Fractal);
		}
		public static void NotifyBranchSelected()
		{
			if(BranchSelected != null) BranchSelected(Fractal);
		}
		public static void NotifyGeometryChanged()
		{
			if(GeometryChanged != null) GeometryChanged(Fractal);
		}
		public static void NotifyToneMapChanged()
		{
			if(ToneMapChagned != null) ToneMapChagned(Fractal);
		}
		public static void NotifyPaletteChanged()
		{
			if(PaletteChanged != null) PaletteChanged(Fractal);
		}

		public static bool IsBranchIndex(int i)
		{
			return i >= 0 && i < Fractal.Branches.Count;
		}

		public static Color SampleBranchColor(int i)
		{
			if(!IsBranchIndex(i))
				return Color.Black;
			else
				return Branches[i].GetChromaColor(Fractal.Palette);
		}

		public static void LoadNewFractal()
		{
			SetCurrentCopy(Fractal.BuildDefault());
		}

		public static void ReadFromFlameFile(string filename, FractronConfig conf)
		{
			FractalList newFractals = FlameFileIO.ReadFlameFile(filename, conf);

			fractals = newFractals;
		}
	}
}
