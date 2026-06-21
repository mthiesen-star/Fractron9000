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
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Windows.Forms;
using System.IO;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using Cuda;
using MTUtil;

namespace Fractron9000.UI
{
	public class RenderControl : GLControl
	{
		public enum SelectionType{ None, XAxis, YAxis, Translation };
		public enum DragState{ None, Panning, Dragging, RotateScale };
		[Flags] public enum DragModifier{ None = 0x00, Shift = 0x01, Ctrl = 0x02, Alt = 0x04 };

		public const float GridStep = 1.0f/8.0f;

		#region Member Vars
		MainForm mainForm = null;

		Point mousePos;
		Vec2 mouseDownWorldPos;
		Vec2 mouseWorldPos;
		DragState dragState = DragState.None;
		DragModifier dragModifier = 0;
		Affine2D dragSrcTransform;
		Vec2 dragHandleOffset;
		Vec2 dragHandlePos;
		bool editMode = true;
		
		Affine2D viewTransform;
		Affine2D projTransform;
		Affine2D screenTransform;

		Affine2D vpsTransform;
		Affine2D vpsInverse;

		private SelectionType hoverType = SelectionType.None;
		private Branch hoverBranch = null;
		Vec2 hoverHandlePos = new Vec2(0.0f,0.0f);
		#endregion

		#region Properties
		[Browsable(false)]
		public MainForm MainForm{
			get{ return mainForm; }
			set{ mainForm = value; }
		}

		[Browsable(false)]
		public Affine2D ViewTransform{
			get{ return viewTransform; }
		}

		[Browsable(false)]
		public Affine2D ProjTransform{
			get{ return projTransform; }
		}

		[Browsable(false)]
		public Affine2D ScreenTransform{
			get{ return screenTransform; }
		}

		[Browsable(false)]
		public Affine2D VPTransform{
			get{ return projTransform * viewTransform; }
		}

		[Browsable(false)]
		public Affine2D VPSTransform{
			get{ return vpsTransform; }
		}

		[Browsable(false)]
		public bool HasGraphics{
			get{ return this.Context != null; }
		}

		[Browsable(false)]
		public bool SafeToRender{
			get{
				return ClientSize.Width > 0 && ClientSize.Height > 0
					&& this.Visible && !this.DesignMode && mainForm != null
					&& mainForm.Context != null
					&& mainForm.EngineManager.EngineState == FractalEngineState.Online;
			}
		}

		[DefaultValue(true)]
		public bool EditMode{
			get{ return editMode; }
			set{ editMode = value; }
		}

		[Browsable(false)]
		public Branch HoverBranch{
			get{ return hoverBranch; }
		}

		[Browsable(false)]
		public SelectionType HoverType{
			get{ return hoverType; }
		}
		#endregion

		public RenderControl() : base()
		{
			ResizeRedraw = true;
			FractalManager.GeometryChanged += (frac)=>
			{
				UpdateTransforms();
			};

			viewTransform = Affine2D.Identity;
			projTransform = Affine2D.Identity;
			screenTransform = Affine2D.Identity;
		}

		#region Coordinate Transforms
		protected override void OnResize(EventArgs e)
		{
			base.OnResize(e);
			UpdateTransforms();
		}

		private void UpdateTransforms()
		{
			float invAspectRatio = (ClientSize.Width > 0) ? ((float)ClientSize.Height / (float)ClientSize.Width) : 0.0f;
			viewTransform = FractalManager.CameraTransform.Inverse;
			projTransform = new Affine2D(invAspectRatio, 0.0f, 0.0f, 1.0f, 0.0f, 0.0f);
			float xHalf = (float)ClientSize.Width / 2.0f;
			float yHalf = (float)ClientSize.Height / 2.0f;

			screenTransform = new Affine2D(xHalf, 0.0f, 0.0f, yHalf, xHalf, yHalf);

			vpsTransform = screenTransform * projTransform * viewTransform;
			vpsInverse = vpsTransform.Inverse;
		}

		private Vec2 WindowToWorld(int x, int y)
		{
			return vpsInverse * WindowToScreen(x, y);
		}

		private Vec2 WindowToScreen(int x, int y)
		{
			return new Vec2((float)x + 0.5f, (float)(ClientSize.Height-y-1) + 0.5f);
		}

		private Vec2 ScreenToView(Vec2 v)
		{
			return (screenTransform * projTransform).Inverse * v;
		}

		private Vec2 WorldToScreen(Vec2 v)
		{
			return vpsTransform * v;
		}

		private Vec2 ScreenToWorld(Vec2 v)
		{
			return vpsInverse * v;
		}

		#endregion

		#region Allocation

		public void InitContext()
		{
			if(this.Handle == IntPtr.Zero)
				CreateHandle();
		}

		/*
		public bool IsAllocated()
		{
			return engine != null;
		}

		public void Allocate()
		{
			if(!SafeToRender) return;

			Deallocate();

			UpdateTransforms();
		}
		public void Deallocate()
		{
			if(!SafeToRender) return;
		}
		*/
		#endregion

		#region Rendering

		protected override void OnPaintBackground(PaintEventArgs e){}
		protected override void OnPaint(PaintEventArgs e)
		{
			if(!SafeToRender){
				e.Graphics.FillRectangle(Brushes.Black, 0, 0, ClientSize.Width, ClientSize.Height);
			}
		}

		public void Render(int glOutputTexID)
		{
			if(!SafeToRender) return;
			
			GL.Viewport(0, 0, ClientSize.Width, ClientSize.Height);
			
			Vec4 bgCol = FractalManager.Fractal.BackgroundColor;
			GL.ClearColor(bgCol.X, bgCol.Y, bgCol.Z, bgCol.W);

			GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
			
			GL.Enable(EnableCap.LineSmooth);
			GL.Enable(EnableCap.PointSmooth);
			GL.Enable(EnableCap.Blend);
			GL.BlendFuncSeparate(BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha, BlendingFactorSrc.One, BlendingFactorDest.Zero);
			
			GL.MatrixMode(MatrixMode.Projection);
			GLUtil.GLLoadAffineMatrix(projTransform, -1.0f);
			GL.MatrixMode(MatrixMode.Modelview);
			GLUtil.GLLoadAffineMatrix(viewTransform);
			
			
			if(glOutputTexID != 0)
			{
				GL.MatrixMode(MatrixMode.Projection);
				GL.PushMatrix();
				GL.LoadIdentity();
				GL.Ortho(0, 1, 0, 1, -1, 1);
				GL.MatrixMode(MatrixMode.Modelview);
				GL.PushMatrix();
				GL.LoadIdentity();
				
				GL.BindTexture(TextureTarget.Texture2D, glOutputTexID);
				GL.Enable(EnableCap.Texture2D);

				GL.Begin(BeginMode.Quads);
				GL.Color4   (1.0f, 1.0f, 1.0f, 1.0f);
				GL.TexCoord2(0.0f, 0.0f);
				GL.Vertex2  (0.0f, 0.0f);
				GL.TexCoord2(1.0f, 0.0f);
				GL.Vertex2  (1.0f, 0.0f);
				GL.TexCoord2(1.0f, 1.0f);
				GL.Vertex2  (1.0f, 1.0f);
				GL.TexCoord2(0.0f, 1.0f);
				GL.Vertex2  (0.0f, 1.0f);
				GL.End();
				
				GL.BindTexture(TextureTarget.Texture2D, 0);
				
				GL.MatrixMode(MatrixMode.Projection);
				GL.PopMatrix();
				GL.MatrixMode(MatrixMode.Modelview);
				GL.PopMatrix();
			}
			if(EditMode && (dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
			{
				GL.LineWidth(1.0f);
				DrawGrid();
			}
			
			if(EditMode)
			{
				if(dragState == DragState.Dragging)
				{
					if(hoverBranch != null)
						DrawWidget(hoverBranch, true);

					GL.PointSize(6.0f);
					GL.Begin(BeginMode.Points);
					GL.Color4(1.0f,1.0f,0.5f,1.0f);
					GL.Vertex2(dragHandlePos.X, dragHandlePos.Y);
					GL.End();
					GL.PointSize(1.0f);
				}
				else
				{
					foreach(Branch branch in FractalManager.Branches)
						DrawWidget(branch, branch == FractalManager.SelectedBranch);

					if(hoverType != SelectionType.None)
					{
						GL.PointSize(6.0f);
						GL.Begin(BeginMode.Points);
						GL.Color4(0.5f,1.0f,1.0f,1.0f);
						GL.Vertex2(hoverHandlePos.X, hoverHandlePos.Y);
						GL.End();
						GL.PointSize(1.0f);
					}
				}
			}

			//GL.PointSize(6.0f);
			//GL.Begin(BeginMode.Points);
			//GL.Color4(1.0f,1.0f,1.0f,1.0f);
			//GL.Vertex2(debugPos.X, debugPos.Y);
			//GL.End();

			
			GL.Finish();
			SwapBuffers();
		}

		#region Render Helpers

		//TODO: these are getting messy, and really should be using display lists or something.
		private void DrawWidget(Branch branch, bool selected)
		{
			GL.PushMatrix();
			GLUtil.GLMultAffineMatrix(branch.DisplayTransform);

			GL.LineWidth(3.0f);
			if(selected)
				GL.Color4 (0.0f, 0.0f, 0.0f, 1.0f);
			else
				GL.Color4 (0.0f, 0.0f, 0.0f, 0.5f);
			DrawWidgetHelper(branch, selected);

			GL.LineWidth(1.0f);
			if(selected)
				GL.Color4 (1.0f, 1.0f, 1.0f, 1.0f);
			else
				GL.Color4 (1.0f, 1.0f, 1.0f, 0.75f);
			DrawWidgetHelper(branch, selected);

			GL.PointSize(5.0f);
			GL.Begin(BeginMode.Points);
			if(selected)
				GL.Color4(0.0f, 0.0f, 0.0f, 1.0f);
			else
				GL.Color4 (0.0f, 0.0f, 0.0f, 0.5f);
			GL.Vertex2(0.0f, 0.0f);
			GL.Vertex2(1.0f, 0.0f);
			GL.Vertex2(0.0f, 1.0f);
			GL.End();

			if(selected)
				GL.LineWidth(2.0f);
			else
				GL.LineWidth(1.0f);

			GL.Begin(BeginMode.Lines);
			GL.Color4(branch.GetChromaColor(FractalManager.Fractal.Palette));
			GL.Vertex2(0.0f, 0.0f);
			if(selected)
				GL.Color4(branch.GetChromaColor(FractalManager.Fractal.Palette));
			else
				GL.Color4 (1.0f, 1.0f, 1.0f, 0.375f);
			GL.Vertex2(0.0f, 1.0f);
			GL.End();

			GL.PointSize(3.0f);
			GL.Begin(BeginMode.Points);
			if(selected)
				GL.Color4(1.0f, 1.0f, 1.0f, 1.0f);
			else
				GL.Color4 (1.0f, 1.0f, 1.0f, 0.5f);
			GL.Vertex2(0.0f, 0.0f);
			GL.Vertex2(1.0f, 0.0f);
			GL.Vertex2(0.0f, 1.0f);
			GL.End();

			GL.PopMatrix();
		}

		private void DrawWidgetHelper(Branch branch, bool selected)
		{
			GL.Begin(BeginMode.Lines);
			float theta,x,y,px,py;
			px = 1.0f;
			py = 0.0f;

			if(selected)
			{
				for(int i = 1; i <= 64; i++)
				{
					theta = (float)Math.PI * 2.0f * (float)(i%64) / 64.0f;
					x = (float)Math.Cos(theta);
					y = (float)Math.Sin(theta);
					GL.Vertex2(px, py);
					GL.Vertex2( x,  y);
					px = x; py = y;
				}
			}
			GL.Vertex2(0.0f, 0.0f);
			GL.Vertex2(1.0f, 0.0f);
			GL.Vertex2(0.0f, 0.0f);
			GL.Vertex2(0.0f, 1.0f);
			GL.End();
		}

		private void DrawUnitSquare()
        {
			GL.Begin(BeginMode.Lines);
			GL.Color4(1.0f,1.0f,1.0f,0.25f);
			GL.Vertex2(-1,-1);
			GL.Vertex2( 1,-1);
			GL.Vertex2(-1, 1);
			GL.Vertex2( 1, 1);

			GL.Vertex2(-1,-1);
			GL.Vertex2(-1, 1);
			GL.Vertex2( 1,-1);
			GL.Vertex2( 1, 1);
			
			GL.Color4(1.0f,0.0f,0.0f,0.5f);
			GL.Vertex2(-1, 0);
			GL.Vertex2( 1, 0);
			GL.Color4(0.0f,1.0f,0.0f,0.5f);
			GL.Vertex2( 0,-1);
			GL.Vertex2( 0, 1);
			GL.End();
        }
		
		private void DrawGrid()
		{
			Vec2 center = FractalManager.Fractal.CameraTransform.Translation;
			float xspan = FractalManager.Fractal.CameraTransform.XAxis.Length;
			float wideMul = (float)Math.Max(ClientSize.Width, ClientSize.Height) / (float)Math.Min(ClientSize.Width, ClientSize.Height);
			float rad = (float)Math.Sqrt((wideMul*xspan)*(wideMul*xspan) + xspan*xspan);

			float xLow =  (float)Math.Floor  (center.X - rad);
			float xHigh = (float)Math.Ceiling(center.X + rad);
			float yLow =  (float)Math.Floor  (center.Y - rad);
			float yHigh = (float)Math.Ceiling(center.Y + rad);
			
			GL.Begin(BeginMode.Lines);

			if(rad <= 8.0f)
			{
				GL.Color4(0.5f,0.5f,0.5f,0.5f);
				for(float x = xLow; x <= xHigh; x += GridStep)
				{
					GL.Vertex2(x, yLow);
					GL.Vertex2(x, yHigh);
				}
				for(float y = yLow; y < yHigh; y += GridStep)
				{
					GL.Vertex2(xLow,  y);
					GL.Vertex2(xHigh, y);
				}
			}
			if(rad <= 64.0f)
			{
				GL.Color4(0.5f,0.5f,0.5f,1.0f);
				for(float x = xLow; x <= xHigh; x += 1.0f)
				{
					GL.Vertex2(x, yLow);
					GL.Vertex2(x, yHigh);
				}
				for(float y = yLow; y < yHigh; y += 1.0f)
				{
					GL.Vertex2(xLow,  y);
					GL.Vertex2(xHigh, y);
				}
			}
			GL.Color4(1.0f,0.0f,0.0f,1.0f);
			GL.Vertex2(0.0f,  0.0f);
			GL.Vertex2(xHigh, 0.0f);
			GL.Color4(0.5f,0.0f,0.0f,1.0f);
			GL.Vertex2(0.0f,  0.0f);
			GL.Vertex2(xLow,  0.0f);
			GL.Color4(0.0f,1.0f,0.0f,1.0f);
			GL.Vertex2(0.0f,  0.0f);
			GL.Vertex2(0.0f,  yHigh);
			GL.Color4(0.0f,0.5f,0.0f,1.0f);
			GL.Vertex2(0.0f,  0.0f);
			GL.Vertex2(0.0f,  yLow);
			GL.End();


			/*
			GL.Begin(BeginMode.Lines);
			for(int i = -32; i <= 32; i++)
			{
				if(i == 0)
					GL.Color4(1.0f,0.0f,0.0f,0.5f);
				else if(i % 8 == 0)
					GL.Color4(0.5f,0.5f,0.5f,1.0f);
				else
					GL.Color4(0.5f,0.5f,0.5f,0.5f);
				
				GL.Vertex2(-4.0f,(float)i / 8.0f);
				GL.Vertex2( 4.0f,(float)i / 8.0f);
			}
			for(int i = -32; i <= 32; i++)
			{
				if(i == 0)
					GL.Color4(0.0f,1.0f,0.0f,0.5f);
				else if(i % 8 == 0)
					GL.Color4(0.5f,0.5f,0.5f,1.0f);
				else
					GL.Color4(0.5f,0.5f,0.5f,0.5f);
				
				GL.Vertex2((float)i / 8.0f, -4.0f);
				GL.Vertex2((float)i / 8.0f,  4.0f);
			}
			GL.End();
			 * */
		}
		
		#endregion
		#endregion

		#region Input Handling
		protected override void OnKeyDown(KeyEventArgs e)
		{
			if(e.KeyCode == Keys.ShiftKey)
				dragModifier |= DragModifier.Shift;
			else if(e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.C)
				dragModifier |= DragModifier.Ctrl;
			else if(e.KeyCode == Keys.Menu || e.KeyCode == Keys.A)
				dragModifier |= DragModifier.Alt;
			else if(e.KeyCode == Keys.Space)
				FractalManager.SendBlip();
			else
				base.OnKeyDown(e);
			//Invalidate();
		}
		protected override void OnKeyUp(KeyEventArgs e)
		{
			if(e.KeyCode == Keys.ShiftKey)
				dragModifier &= ~DragModifier.Shift;
			else if(e.KeyCode == Keys.ControlKey || e.KeyCode == Keys.C)
				dragModifier &= ~DragModifier.Ctrl;
			else if(e.KeyCode == Keys.Menu || e.KeyCode == Keys.A)
				dragModifier &= ~DragModifier.Alt;
			else
				base.OnKeyUp(e);
			//Invalidate();
		}
		
		protected override void OnMouseEnter(EventArgs e)
		{
		}
		
		protected override void OnMouseLeave(EventArgs e)
		{
		}
		
		protected override void OnMouseDown(MouseEventArgs ea)
		{
			if(!SafeToRender) return;
			Focus();
			mouseWorldPos = WindowToWorld(ea.X, ea.Y);


			if(dragState == DragState.None)
			{
				if(EditMode && ea.Button == MouseButtons.Left)
				{
					UpdateHover(WindowToScreen(ea.X,ea.Y));
					FractalManager.SelectedBranch = hoverBranch;
					if(hoverBranch != null)
					{
						mouseDownWorldPos = mouseWorldPos;
						dragSrcTransform = hoverBranch.DisplayTransform;
						dragHandlePos = hoverHandlePos;
						dragHandleOffset = dragHandlePos - mouseWorldPos;
						dragState = DragState.Dragging;
						Capture = true;
					}
					else
					{
						dragState = DragState.Panning;
						Capture = true;
						mouseDownWorldPos = mouseWorldPos;
					}
				}
				else if(ea.Button == MouseButtons.Middle)
				{
					dragState = DragState.Panning;
					Capture = true;
					mouseDownWorldPos = mouseWorldPos;
				}
				else if(ea.Button == MouseButtons.Right)
				{
					if(EditMode){
						UpdateHover(WindowToScreen(ea.X,ea.Y));
						FractalManager.SelectedBranch = hoverBranch;
					}
					dragState = DragState.RotateScale;
					Capture = true;
					mouseDownWorldPos = mouseWorldPos;
				}
			}
		}
		
		protected override void OnMouseUp(MouseEventArgs ea)
		{
			if(!SafeToRender) return;
			mouseWorldPos = WindowToWorld(ea.X, ea.Y);

			if(dragState == DragState.Panning)
			{
				dragState = DragState.None;
				Capture = false;
			}
			else if(dragState == DragState.RotateScale)
			{
				dragState = DragState.None;
				Capture = false;
			}
			else if(dragState == DragState.Dragging && ea.Button == MouseButtons.Left)
			{
				dragState = DragState.None;
				Capture = false;
				UpdateHover(WindowToScreen(ea.X, ea.Y));
			}
		}
		

		protected override void OnMouseMove(MouseEventArgs ea)
		{
			if(!SafeToRender) return;
			if(ea.Location == mousePos) return; //the mouse didnt actually move, so bail out.

			mouseWorldPos = WindowToWorld(ea.X, ea.Y);
			Vec2 mouseDelta = mouseWorldPos - mouseDownWorldPos;

			if(EditMode && dragState == DragState.Dragging &&
				hoverBranch != null )
			{
				if(hoverType == SelectionType.Translation)
					hoverBranch.DisplayTransform = CalcDragTranslation();
				else if(hoverType == SelectionType.XAxis)
					hoverBranch.DisplayTransform = CalcDragXAxis();
				else if(hoverType == SelectionType.YAxis)
					hoverBranch.DisplayTransform = CalcDragYAxis();

				FractalManager.NotifyGeometryChanged();
			}
			else if(dragState == DragState.Panning)
			{
				Vec2 mouseScreenPos = WindowToScreen(ea.X,ea.Y);
				Pan(mouseScreenPos, mouseDownWorldPos);
				mouseWorldPos = WindowToWorld(ea.X, ea.Y);
			}
			else if(dragState == DragState.RotateScale)
			{
				Vec2 mouseScreenPos = WindowToScreen(ea.X,ea.Y);
				CameraRotateScale(mouseScreenPos, mouseDownWorldPos);
				mouseWorldPos = WindowToWorld(ea.X, ea.Y);
			}
			else
			{
				UpdateHover(WindowToScreen(ea.X, ea.Y));
			}

			//Vec2 mouseViewPos = ScreenToView(WindowToScreen(ea.X, ea.Y));
			//debugPos = cameraTransform * mouseViewPos;

			mousePos = ea.Location;
			base.OnMouseMove(ea);
		}

		//adjust the camera's position so that screenMousePos will align with the world target
		private void Pan(Vec2 screenMousePos, Vec2 worldTarget)
		{
			Vec2 viewMousePos = ScreenToView(screenMousePos);
			Affine2D ct = FractalManager.CameraTransform;

			ct.Translation.X = worldTarget.X - ct.XAxis.X*viewMousePos.X - ct.YAxis.X*viewMousePos.Y;
			ct.Translation.Y = worldTarget.Y - ct.XAxis.Y*viewMousePos.X - ct.YAxis.Y*viewMousePos.Y;

			FractalManager.CameraTransform = ct;
		}

		//adjust the camera's rotation/scale so that screenMousePos will align with the world target
		private void CameraRotateScale(Vec2 screenMousePos, Vec2 worldTarget)
		{
			Vec2 v = ScreenToView(screenMousePos);
			Affine2D ct = FractalManager.CameraTransform;

			//first figure out if the camera is flipped
			float k;
			float cz = ct.XAxis.X * ct.YAxis.Y - ct.XAxis.Y*ct.YAxis.X;
			if(cz > 0)
				k = 1.0f;
			else
				k = -1.0f;

			//compute the new cam matrix
			float rsq = v.X*v.X + v.Y*v.Y;
			float a =  k*(v.Y*(worldTarget.Y-ct.F) + k*v.X*(worldTarget.X-ct.C))/rsq;
			float d = -k*(v.Y*(worldTarget.X-ct.C) + k*v.X*(ct.F-worldTarget.Y))/rsq;

			ct.A = a;
			ct.D = d;
			ct.B = -k*d;
			ct.E = k*a;

			FractalManager.CameraTransform = ct;
		}


		private Affine2D CalcDragTranslation()
		{
			Affine2D result = dragSrcTransform;
			Vec2 newPos = new Vec2();

			if((dragModifier&DragModifier.Shift) == DragModifier.Shift)
			{
				Vec2 norm = mouseWorldPos.GetNormal();
				if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
					norm.SnapToNormalizedAngle(12);
				newPos = norm * dragSrcTransform.Translation.Length;
			}
			else
			{
				if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
					newPos = snapToGrid(mouseWorldPos);
				else
					newPos = mouseWorldPos + dragHandleOffset;
			}
			result.Translation = newPos;
			dragHandlePos = result.Translation;
			return result;
		}

		private Affine2D CalcDragXAxis()
		{
			Affine2D result = dragSrcTransform;
			Vec2 targetPos;
			Vec2 newAxis;

			if((dragModifier&DragModifier.Shift) == DragModifier.Shift)
			{
				//if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
				//	targetPos = snapToGrid(mouseWorldPos);
				
				Vec2 targetAxis = mouseWorldPos - dragSrcTransform.Translation;
				Vec2 norm = targetAxis.GetNormal();

				if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
					norm.SnapToNormalizedAngle(24);

				newAxis = norm * dragSrcTransform.XAxis.Length;
			}
			else
			{
				if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
					targetPos = snapToGrid(mouseWorldPos);
				else
					targetPos = mouseWorldPos + dragHandleOffset;

				newAxis = targetPos - dragSrcTransform.Translation;
			}

			if((dragModifier&DragModifier.Alt) == DragModifier.Alt)
				result.XAxis = newAxis;
			else
				result.RotateScaleXTo(newAxis);

			dragHandlePos = result.Translation + result.XAxis;
			return result;
		}

		private Affine2D CalcDragYAxis()
		{
			Affine2D result = dragSrcTransform;
			Vec2 targetPos;
			Vec2 newAxis;

			if((dragModifier&DragModifier.Shift) == DragModifier.Shift)
			{
				//if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
				//	targetPos = snapToGrid(mouseWorldPos);
				
				Vec2 targetAxis = mouseWorldPos - dragSrcTransform.Translation;
				Vec2 norm = targetAxis.GetNormal();

				if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
					norm.SnapToNormalizedAngle(24);

				newAxis = norm * dragSrcTransform.YAxis.Length;
			}
			else
			{
				if((dragModifier&DragModifier.Ctrl) == DragModifier.Ctrl)
					targetPos = snapToGrid(mouseWorldPos);
				else
					targetPos = mouseWorldPos + dragHandleOffset;

				newAxis = targetPos - dragSrcTransform.Translation;
			}

			if((dragModifier&DragModifier.Alt) == DragModifier.Alt)
				result.YAxis = newAxis;
			else
				result.RotateScaleYTo(newAxis);

			dragHandlePos = result.Translation + result.YAxis;
			return result;
		}

		private float snapToGrid(float n)
		{
			return (float)Math.Round(n/GridStep) * GridStep;
		}
		private Vec2 snapToGrid(Vec2 v)
		{
			return new Vec2(
				(float)Math.Round(v.X/GridStep) * GridStep,
				(float)Math.Round(v.Y/GridStep) * GridStep );
		}
		
		protected override void OnMouseWheel(MouseEventArgs ea)
		{
			if(!SafeToRender) return;

			if(ea.Delta > 0)
				FractalManager.ZoomIn();
			else if(ea.Delta < 0)
				FractalManager.ZoomOut();
		}

		private void UpdateHover(Vec2 mouseScreenPos)
		{
			hoverType = SelectionType.None;
			hoverBranch = null;
			float bestDistSq = 10.0f * 10.0f;

			//If there's a selected branch, check it first so it gets precedence over the others
			if(FractalManager.SelectedBranch != null)
				CheckBranchHover(FractalManager.SelectedBranch, mouseScreenPos, ref bestDistSq);

			foreach(Branch branch in FractalManager.Branches)
			{
				if(branch != FractalManager.SelectedBranch)
					CheckBranchHover(branch, mouseScreenPos, ref bestDistSq);
			}
		}

		private void CheckBranchHover(Branch branch, Vec2 mouseScreenPos, ref float bestDistSq)
		{
			Vec2 pos;
			Vec2 screenPos;
			Affine2D branchTransform = branch.DisplayTransform;

			float distSq = 0.0f;
			pos = branchTransform.Translation;
			screenPos = WorldToScreen(pos);
			distSq = (screenPos-mouseScreenPos).LengthSq;
			if(distSq < bestDistSq)
			{
				bestDistSq = distSq;
				hoverType = SelectionType.Translation;
				hoverBranch = branch;
				hoverHandlePos = pos;
			}

			pos = branch.DisplayTransform.Translation + branchTransform.XAxis;
			screenPos = WorldToScreen(pos);
			distSq = (screenPos-mouseScreenPos).LengthSq;
			if(distSq < bestDistSq)
			{
				bestDistSq = distSq;
				hoverType = SelectionType.XAxis;
				hoverBranch = branch;
				hoverHandlePos = pos;
			}

			pos = branchTransform.Translation + branchTransform.YAxis;
			screenPos = WorldToScreen(pos);
			distSq = (screenPos-mouseScreenPos).LengthSq;
			if(distSq < bestDistSq)
			{
				bestDistSq = distSq;
				hoverType = SelectionType.YAxis;
				hoverBranch = branch;
				hoverHandlePos = pos;
			}
		}

		#endregion
	}
}