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
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;

using MTUtil;

namespace Fractron9000
{
	public class Fractal
	{
		public string Name = null;
		public string Version = null;

		public Affine2D CameraTransform; //The inverse of a view transform
		public float Brightness = 1.0f;
		public float Gamma = 2.0f;
		public float Vibrancy = 1.0f;
		public Vec4 BackgroundColor = new Vec4(0,0,0,1);
		public List<Branch> Branches;
		private Palette palette = null;

		public Palette Palette{
			get{ return palette != null ? palette : Palette.DefaultPalette; }
			set{ palette = value; }
		}

		public Fractal()
		{
			CameraTransform = new Affine2D(2.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f);
			Branches = new List<Branch>();
		}

		public Fractal(Fractal src)
		{
			this.Name = src.Name;
			this.Version = src.Version;

			this.CameraTransform = src.CameraTransform;
			this.Brightness = src.Brightness;
			this.Gamma = src.Gamma;
			this.Vibrancy = src.Vibrancy;
			this.BackgroundColor = src.BackgroundColor;

			this.Branches = new List<Branch>();
			foreach(Branch branch in src.Branches)
				Branches.Add(new Branch(branch));

			this.palette = src.Palette;
		}

		public Fractal(Fractal l, Fractal r, float alpha)
		{
			FillBlended(l, r, alpha);
		}

		//Returns false if this fractal was read from a file not created by Fractron 9000
		public bool IsFromFractron()
		{
			return Version.ToLower().Contains("fractron");
		}

		//Stamps this fractal with the Fractron9000 Seal of Approval
		public void MarkVersionAsFractron()
		{
			Version = FlameFileIO.IOVersion;
		}

		//invents some size/scale stuff that some renderers need based on the current camera
		public void GetFlameFromCamera(
			out int xSize, out int ySize,
			out float xCenter, out float yCenter,
			out float scale, out float zoom, out float rotate)
		{
			xSize = 800;
			ySize = 600;
			xCenter = CameraTransform.Translation.X;
			yCenter = CameraTransform.Translation.Y;
			float minSize = (float)Math.Min(xSize,ySize);
			float camSpan = CameraTransform.XAxis.Length * 2.0f;
			if(camSpan == 0.0f)
				scale = 1.0f;
			else
				scale = minSize / camSpan;
			zoom = 0.0f;

			double theta = Math.Atan2(CameraTransform.XAxis.Y, CameraTransform.XAxis.X);
			rotate = (float)(theta * 180.0 / Math.PI);
		}

		public void SetCameraFromFlame(float xSize, float ySize,
			float xCenter, float yCenter,
			float scale, float zoom, float rotate)
		{
			float minSize = (float)Math.Min(xSize,ySize);
			if(scale <= 0.0f)
				scale = 1.0f;
			float camSpan = minSize / scale;
			float camScale = (camSpan/2.0f) * (float)Math.Pow(0.5f, zoom);

			double theta = (double)rotate * Math.PI / 180.0;

			float xx = (float)Math.Cos(theta) * camScale;
			float xy = (float)Math.Sin(theta) * camScale;

			CameraTransform.XAxis = new Vec2(xx,xy);
			CameraTransform.YAxis = new Vec2(-xy,xx);
			CameraTransform.Translation = new Vec2(xCenter, yCenter);
		}

		public static Fractal BuildDefault()
		{
			Fractal result = new Fractal();
			result.Name = "New Fractal";

			Branch br;
			br = new Branch();
			br.PreTransform = new Affine2D( 0.5f,  0.0f,  0.0f,  0.5f,  0.433f, -0.25f );
			br.Chroma = new Vec2(1.0f,  0.5f);
			result.Branches.Add(br);

			br = new Branch();
			br.PreTransform = new Affine2D( 0.5f,  0.0f,  0.0f,  0.5f, -0.433f, -0.25f );
			br.Chroma = new Vec2(0.25f, 0.9f );
			result.Branches.Add(br);

			br = new Branch();
			br.PreTransform = new Affine2D( 0.5f,  0.0f,  0.0f,  0.5f,  0.0f,  0.5f );
			br.Chroma = new Vec2(0.25f, 0.1f );
			result.Branches.Add(br);

			return result;
		}

		public void FillBlended(Fractal l, Fractal r, float alpha)
		{
			if(l == null) l = BuildDefault();
			if(r == null) r = BuildDefault();

			this.Name = "";
			this.Version = l.Version;

			this.CameraTransform = Affine2D.Lerp(l.CameraTransform, r.CameraTransform, alpha);
			this.Brightness = Util.Lerp(l.Brightness, r.Brightness, alpha);
			this.Gamma = Util.Lerp(l.Gamma, r.Gamma, alpha);
			this.Vibrancy = Util.Lerp(l.Vibrancy, r.Vibrancy, alpha);
			this.BackgroundColor = Vec4.Lerp(l.BackgroundColor, r.BackgroundColor, alpha);

			int numBranches = Math.Max(l.Branches.Count, r.Branches.Count);
			this.Branches.Clear();
			for(int i = 0; i < numBranches; i++){
				Branch lb = i < l.Branches.Count ? l.Branches[i] : null;
				Branch rb = i < r.Branches.Count ? r.Branches[i] : null;
				this.Branches.Add( new Branch(lb, rb, alpha)  );
			}

			if(alpha < 0.5f)
				this.palette = l.palette;
			else
				this.palette = r.palette;
		}

		public Affine2D GetVPSTransform(int xRes, int yRes)
		{
			float invAspectRatio = (xRes > 0) ? ((float)yRes / (float)xRes) : 0.0f;
			Affine2D viewTransform = this.CameraTransform.Inverse;
			Affine2D projTransform = new Affine2D(invAspectRatio, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
			float xHalf = (float)xRes / 2.0f;
			float yHalf = (float)yRes / 2.0f;
			Affine2D screenTransform = new Affine2D(xHalf, 0.0f, 0.0f, yHalf, xHalf, yHalf);
			return screenTransform * projTransform * viewTransform;
		}

		public static void GetNativeFractalSizes(out int nFractalSize, out int nBranchesSize, out int nVariWeightsSize)
		{
			nFractalSize = Marshal.SizeOf(typeof(NativeFractal));
			nBranchesSize = Marshal.SizeOf(typeof(NativeBranch)) * NativeFractal.MaxBranches;
			nVariWeightsSize = Marshal.SizeOf(typeof(float)) * NativeFractal.MaxBranches * NativeFractal.MaxVariations;
		}

		unsafe public void FillNativeFractal(int xRes, int yRes, NativeFractal* nvFractal, NativeBranch* nvBranches, float* nvVariWeights)
		{			
			nvFractal->BranchCount = (uint)Math.Min(NativeFractal.MaxBranches, this.Branches.Count);
			nvFractal->VpsTransform = this.GetVPSTransform(xRes, yRes);
			nvFractal->Brightness = this.Brightness;
			nvFractal->InvGamma = 1.0f / this.Gamma;
			nvFractal->Vibrancy = this.Vibrancy;
			nvFractal->BgColor = this.BackgroundColor;
		
			fillNativeBranches(this.Branches, nvBranches, nvVariWeights);
		}

		unsafe private static void fillNativeBranches(List<Branch> branches, NativeBranch* nvBranches, float* nvVariWeights)
		{
			Int32 branchCount = Math.Min(NativeFractal.MaxBranches, branches.Count);
			for(int bi = 0; bi < branchCount; bi++)
				nvBranches[bi].NormWeight = (UInt32)0x00010000;

			float branchWeightSum = 0.0f;
			for(int bi = 0; bi < branchCount; bi++)
				branchWeightSum += branches[bi].Weight;
			
			for(int i = 0; i < NativeFractal.MaxBranches*NativeFractal.MaxVariations; i++)
				nvVariWeights[i] = 0.0f;

			UInt32 runningSum = 0;
			for(int bi = 0; bi < branchCount; bi++)
			{
				Branch branch = branches[bi];

				runningSum += (UInt32)(branch.Weight / branchWeightSum * 65536.0f);
				if(bi < branchCount-1)
					nvBranches[bi].NormWeight = runningSum;
				else
					nvBranches[bi].NormWeight = 0x00010000;

				nvBranches[bi].ColorWeight =   branch.ColorWeight;
				nvBranches[bi].Chroma =        branch.Chroma;
				nvBranches[bi].PreTransform =  branch.PreTransform;
				nvBranches[bi].PostTransform = branch.PostTransform;
	
				foreach(Branch.VariEntry ve in branch.Variations)
					if(ve.Index >= 0 && ve.Index < NativeFractal.MaxVariations)
						nvVariWeights[bi*NativeFractal.MaxVariations + ve.Index] += ve.Weight;
			}
		}

		/// <summary>
		/// Converts this fractal into a format that can be easily passed to the GPU
		/// </summary>
		public void GetNativeFractal(int xRes, int yRes, out NativeFractal nvFractal, out NativeBranch[] nvBranches, out float[] nvVariWeights)
		{
			nvFractal = new NativeFractal();
			nvBranches = new NativeBranch[NativeFractal.MaxBranches];
			nvVariWeights = new float[NativeFractal.MaxBranches * NativeFractal.MaxVariations];
			unsafe{
				fixed(NativeFractal* p_fractal = &nvFractal){
					fixed(NativeBranch* p_branches = nvBranches){
						fixed(float* p_variWeights = nvVariWeights){
							FillNativeFractal(xRes, yRes, p_fractal, p_branches, p_variWeights);
						}
					}
				}
			}
		}
	}

	public class Branch
	{
		public Affine2D PreTransform;
		public Affine2D PostTransform;
		public Vec2 Chroma;
		public float Weight;
		public float ColorWeight;

		private bool localized;
		private List<VariEntry> variations;
		public IList<VariEntry> Variations{
			get{ return variations; }
		}

		public Affine2D DisplayTransform{
			get{
				if(Localized)
					return PostTransform;
				else
					return PreTransform;
			}
			set{
				if(Localized){
					PreTransform = value.Inverse;
					PostTransform = value;
				}
				else{
					PreTransform = value;
					PostTransform = Affine2D.Identity;
				}
			}
		}
		
		public bool Localized{
			get{ return localized; }
			set{ localized = value; }
		}

		public void Localize()
		{
			if(!localized){
				PostTransform = PreTransform;
				PreTransform = PreTransform.Inverse;
				localized = true;
			}
		}

		public void Delocalize()
		{
			if(localized){
				PreTransform = PostTransform;
				PostTransform = Affine2D.Identity;
				localized = false;
			}
		}

		public Branch()
		{
			FillDefault();
		}

		public Branch(Branch src)
		{
			Fill(src);
		}

		public Branch(Branch l, Branch r, float alpha)
		{
			FillBlended(l, r, alpha);
		}
		
		public void FillDefault()
		{
			PreTransform = new Affine2D(0.5f,0.0f,0.0f,0.5f,0.0f,0.0f);
			PostTransform = Affine2D.Identity;
			localized = false;
			Chroma = new Vec2(0.5f,0.5f);
			Weight = 1.0f;
			ColorWeight = 0.5f;
			variations = new List<VariEntry>();
			variations.Add(new VariEntry(0, 1.0f));
		}

		public void Fill(Branch src)
		{
			PreTransform = src.PreTransform;
			PostTransform = src.PostTransform;
			localized = src.localized;
			Chroma = src.Chroma;
			Weight = src.Weight;
			ColorWeight = src.ColorWeight;
			variations = new List<VariEntry>();
			foreach(VariEntry v in src.Variations)
				variations.Add(v);
		}

		public Color GetChromaColor(Palette palette)
		{
			if(palette == null)
				return Color.Black;
			return palette.Sample(Chroma.X, Chroma.Y);
		}

		public struct VariEntry
		{
			public int Index;
			public float Weight;

			public VariEntry(int index, float weight){
				this.Index = index;
				this.Weight = weight;
			}
		}

		public void FillBlended(Branch l, Branch r, float alpha)
		{
			if(l == null && r == null){
				FillDefault();
			}
			else if(l == null){
				Fill(r);
				Weight = alpha;
			}
			else if(r == null){
				Fill(l);
				Weight = 1.0f - alpha;
			}
			else
			{
				this.PreTransform = Affine2D.Lerp(l.PreTransform, r.PreTransform, alpha);
				this.PostTransform = Affine2D.Lerp(l.PostTransform, r.PostTransform, alpha);
				this.Chroma = Vec2.Lerp(l.Chroma, r.Chroma, alpha);
				this.ColorWeight = Util.Lerp(l.ColorWeight, r.ColorWeight, alpha);
				this.Weight = Util.Lerp(l.Weight, r.Weight, alpha);
				if(alpha < 0.5f)
					localized = l.localized;
				else
					localized = r.localized;

				float invAlpha = 1.0f - alpha;

				variations = new List<VariEntry>();
				foreach(VariEntry ve in l.Variations)
					addOrMergeVariation(new VariEntry(ve.Index, invAlpha*ve.Weight));
				foreach(VariEntry ve in r.Variations)
					addOrMergeVariation(new VariEntry(ve.Index, alpha*ve.Weight));
			}
		}
		private void addOrMergeVariation(VariEntry ve)
		{
			for(int i = 0; i < variations.Count; i++){
				if(variations[i].Index == ve.Index){
					variations[i] = new VariEntry(variations[i].Index, variations[i].Weight + ve.Weight);
					return;
				}
			}
			variations.Add(ve);
		}
	}
}
