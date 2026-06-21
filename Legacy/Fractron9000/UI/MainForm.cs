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
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using OpenTK.Graphics;
using MTUtil;
using MTUtil.UI;
using Fractron9000.CPUEngine;
using Fractron9000.CudaEngine;
using Fractron9000.OpenCLEngine;
using Fractron9000.HybridEngine;

namespace Fractron9000.UI
{
	public partial class MainForm : Form
	{
		#region Properties and member vars
		private FractronConfig config = new FractronConfig();
		public FractronConfig Config{
			get{ return config; }
			set{ config = value; }
		}

		private EngineManager engineMgr;
		public EngineManager EngineManager{
			get{ return engineMgr; }
		}

		public Size RendererSize{
			get{ return renderer == null ? new Size(0,0) : renderer.Size; }
		}

		private bool readyToRender = false;
		public bool ReadyToRender{
			get{ return readyToRender; }
		}

		private bool resizing = false;
		public bool Resizing{
			get{ return resizing; }
		}

		private bool dlgActive = false;
		public bool DlgActive{
			get{ return dlgActive; }
		}

		public RenderControl Renderer{
			get{ return renderer; }
		}

		public IGraphicsContext Context{
			get{ return renderer == null ? null : renderer.Context; }
		}

		private PaletteSelect1DForm paletteSelect1DForm = null;
		private const int variControlCount = 4;
		private ComboBox[] variDropBoxes;
		private DragSpin[] variSpinners;
		private string helpMessage = null;

		private bool engineErrorShown = false; //set to true the first time an engine error dialog is shown

		#endregion
		
		#region Constructor of DOOOOOM
		public MainForm()
		{
			engineMgr = new EngineManager(this);

			InitializeComponent();

			if(this.DesignMode)
				return;

			FractronConfig.DoInitialSetup();
			try{
				config = FractronConfig.Load();
			}
			catch(Exception ex)
			{
				ErrorForm.Show(Narratives.Error_InitConfigLoadFailed, ex);
				config = new FractronConfig();
			}

			renderer.MainForm = this;
			renderer.HandleCreated += new EventHandler(renderer_HandleCreated);
			renderer.InitContext();
			
			FractalManager.Init();
			try{
				FractalManager.ReadFromFlameFile(config.CurrentLibraryFile, config);
				libraryNameLabel.Text = Path.GetFileName(config.CurrentLibraryFile);
			}
			catch(Exception ex)
			{
				string msg = string.Format(Narratives.Error_FlameLoadFailed, config.CurrentLibraryFile);
				ErrorForm.Show(msg, ex);
			}
			if(FractalManager.Fractals.Count > 0)
				FractalManager.SetCurrentCopy(FractalManager.Fractals[0]);

			brightnessSpinner.ValueChanged += (sender, e) =>
			{
				FractalManager.Fractal.Brightness = (float)brightnessSpinner.Value;
				FractalManager.NotifyToneMapChanged();
			};

			gammaSpinner.ValueChanged += (sender, e) =>
			{
				FractalManager.Fractal.Gamma = (float)gammaSpinner.Value;
				FractalManager.NotifyToneMapChanged();
			};

			vibrancySpinner.ValueChanged += (sender, e) =>
			{
				FractalManager.Fractal.Vibrancy = (float)vibrancySpinner.Value;
				FractalManager.NotifyToneMapChanged();
			};

			weightSpinner.ValueChanged += (sender, e) =>
			{
				if(FractalManager.SelectedBranch != null){
					FractalManager.SelectedBranch.Weight = (float)weightSpinner.Value;
					FractalManager.NotifyGeometryChanged();
				}
			};

			colorWeightSpinner.ValueChanged += (sender, e) =>
			{
				if(FractalManager.SelectedBranch != null){
					FractalManager.SelectedBranch.ColorWeight = (float)colorWeightSpinner.Value;
					FractalManager.NotifyGeometryChanged();
				}
			};

			localizedCheckbox.CheckedChanged += applyLocalized;

			variDropBoxes = new ComboBox[variControlCount];
			variSpinners = new DragSpin[variControlCount];
			int variDropWidth = variGroupBox.ClientSize.Width - 76;
			int y = 38;
			for(int i = 0; i < variControlCount; i++)
			{
				ComboBox variDropBox = new ComboBox();
				DragSpin variSpinner = new DragSpin();

				variDropBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
				variDropBox.Size = new System.Drawing.Size(variDropWidth, 21);
				variDropBox.Location = new System.Drawing.Point(6, y);
				variDropBox.TabIndex = 2*i;
				
				if(i > 0)
					variDropBox.Items.Add("<none>");
				foreach(Variation vari in Variation.Variations)
					variDropBox.Items.Add(vari);
				variDropBox.SelectedIndexChanged += applyVariControls;


				variSpinner.Size = new System.Drawing.Size(58, 20);
				variSpinner.Location = new System.Drawing.Point(variGroupBox.ClientSize.Width - 64, y);
				variSpinner.FormatString = "0.###";
				variSpinner.MinVal = 0.0;
				variSpinner.MaxVal = 1.0;
				variSpinner.MinorTicksPerMajorTick = 12;
				variSpinner.PixelsPerMinorTick = 24;
				variSpinner.TabIndex = 2*i + 1;

				variSpinner.ValueChanged += applyVariControls;

				y += variDropBox.Height + 6;

				helpProvider.SetHelpString(variDropBox, "Selects a variation to apply to the current branch.");
				helpProvider.SetHelpString(variSpinner, "Adjusts the weight of a variation.");

				variGroupBox.Controls.Add(variDropBox);
				variGroupBox.Controls.Add(variSpinner);

				variDropBoxes[i] = variDropBox;
				variSpinners[i] = variSpinner;
			}

			variGroupBox.Size = new Size(variGroupBox.Width, y);
			
			helpifyControl(this, null);

			FractalManager.CurrentFractalChanged += handleCurrentFractalChange;

			FractalManager.BranchSelected += (frac)=>{
				updateBranchControls();
			};

			//foreach(var spin in branchOptsPanel.Controls.OfType<DragSpin>())
			//	spin.ValueChanged += HandleFactorSpinnerValueChange;
			FractalManager.PaletteChanged  += (frac)=>
			{
				engineMgr.MarkPaletteDirty();
			};

			FractalManager.ToneMapChagned  += (frac)=>
			{
				engineMgr.MarkToneMapDirty();
			};

			FractalManager.GeometryChanged += (frac)=>
			{
				engineMgr.MarkGeometryDirty();
			};

			nameTextBox.TextChanged += applyFractalName;
			updateLibraryView();

			updateToneControls();
			handleCurrentFractalChange(FractalManager.Fractal);
			updateBranchControls();

			chooseDesiredEngineState();
		}

		private void helpifyControl(Control ctl, string parentHelpText)
		{
			string helpText = helpProvider.GetHelpString(ctl);

			if(helpText == null && parentHelpText != null)
			{
				helpText = parentHelpText;
				helpProvider.SetHelpString(ctl, helpText);
				helpProvider.SetShowHelp(ctl, true);
			}

			if(helpText != null)
			{
				ctl.MouseEnter += handleHelpCtlMouseEnter;
				ctl.MouseLeave += handleHelpCtlMouseLeave;
			}
			
			foreach(Control child in ctl.Controls)
				helpifyControl(child, helpText);
		}
		#endregion

		#region Control initialization and event handling
		private void setHelpMessage(string message)
		{
			helpMessage = message;
			dotCountStatusLabel.Visible = (helpMessage == null);
			dotRateStatusLabel.Visible = (helpMessage == null);
			qualityStatusLabel.Visible = (helpMessage == null);
			iterStatusLabel.Visible = (helpMessage == null);
			timeStatusLabel.Visible = (helpMessage == null);
			helpStatusLabel.Visible = (helpMessage != null);
			helpStatusLabel.Text = helpMessage ?? "";
		}

		private void handleHelpCtlMouseEnter(object sender, EventArgs ea)
		{
			Control ctl = sender as Control;
			if(ctl == null) return;
			string helpText = helpProvider.GetHelpString(ctl);
			setHelpMessage(helpText);
		}
		private void handleHelpCtlMouseLeave(object sender, EventArgs ea)
		{
			setHelpMessage(null);
		}

		private void handleRendererMouseMove(object sender, MouseEventArgs ea)
		{
			if(renderer.HoverType == RenderControl.SelectionType.None)
				setHelpMessage(null);
			else if(renderer.HoverType == RenderControl.SelectionType.Translation)
				setHelpMessage("Branch Center: Drag to move. Ctrl+Drag to snap to grid. Shift+Drag to rotate.");
			else if(renderer.HoverType == RenderControl.SelectionType.XAxis)
				setHelpMessage("Branch X-Axis: Drag to rotate and scale. Ctrl+Drag to snap to grid. Alt+Drag to skew. Shift+Drag for rotation only.");
			else if(renderer.HoverType == RenderControl.SelectionType.YAxis)
				setHelpMessage("Branch Y-Axis: Drag to rotate and scale. Ctrl+Drag to snap to grid. Alt+Drag to skew. Shift+Drag for rotation only.");
		}

		private void handleCurrentFractalChange(Fractal frac)
		{
			nameTextBox.TextChanged -= applyFractalName;
			nameTextBox.Text = FractalManager.Fractal.Name ?? "unnamed";
			nameTextBox.TextChanged += applyFractalName;
			updateToneControls();
			updateBranchControls();
		}

		private void updateToneControls()
		{
			Fractal frac = FractalManager.Fractal;
			brightnessSpinner.SetValueStealth((double)frac.Brightness);
			gammaSpinner.SetValueStealth((double)frac.Gamma);
			vibrancySpinner.SetValueStealth((double)frac.Vibrancy);
			Color bgc = Color.FromArgb(
				Util.ClampByte((int)(frac.BackgroundColor.X * 255.0f)),
				Util.ClampByte((int)(frac.BackgroundColor.Y * 255.0f)),
				Util.ClampByte((int)(frac.BackgroundColor.Z * 255.0f)));
			backgroundColorPanel.BackColor = bgc;
		}

		private void updateBranchControls()
		{
			Branch branch = FractalManager.SelectedBranch;

			removeBranchToolStripButton.Enabled = (branch != null);
			removeBranchToolStripMenuItem.Enabled = (branch != null);
			duplicateBranchToolStripButton.Enabled = (branch != null);
			duplicateBranchToolStripMenuItem.Enabled = (branch != null);
			invertBranchToolStripButton.Enabled = (branch != null);
			invertBranchToolStripMenuItem.Enabled = (branch != null);

			weightLabel.Visible = (branch != null);
			weightSpinner.Visible = (branch != null);
			colorWeightLabel.Visible = (branch != null);
			colorWeightSpinner.Visible = (branch != null);
			localizedCheckbox.Visible = (branch != null);

			foreach(ComboBox db in variDropBoxes)
				db.Visible = (branch != null);
			foreach(DragSpin ds in variSpinners)
				ds.Visible = (branch != null);

			if(branch != null)
			{
				weightSpinner.SetValueStealth((double)branch.Weight);
				colorWeightSpinner.SetValueStealth((double)branch.ColorWeight);
				
				localizedCheckbox.CheckedChanged -= applyLocalized;
				localizedCheckbox.Checked = branch.Localized;
				localizedCheckbox.CheckedChanged += applyLocalized;

				foreach(ComboBox db in variDropBoxes){
					db.SelectedIndexChanged -= applyVariControls;
					db.SelectedIndex = 0;
				}
				foreach(DragSpin ds in variSpinners){
					ds.ValueChanged -= applyVariControls;
					ds.Value = 0;
				}				

				for(int i = 0; i < Math.Min(variControlCount, branch.Variations.Count); i++)
				{
					variDropBoxes[i].SelectedItem = Variation.Variations[branch.Variations[i].Index];
					variSpinners[i].Value = branch.Variations[i].Weight;
				}
				
				foreach(ComboBox db in variDropBoxes)
					db.SelectedIndexChanged += applyVariControls;
				foreach(DragSpin ds in variSpinners)
					ds.ValueChanged += applyVariControls;
			}
		}

		private void updateLibraryView()
		{
			libraryView.Items.Clear();
			foreach(Fractal fractal in FractalManager.Fractals)
			{
				ListViewItem item = new ListViewItem(fractal.Name ?? "unnamed");
				item.Tag = fractal;
				libraryView.Items.Add(item);
			}
		}

		private void backgroundColorPanel_Click(object sender, EventArgs e)
		{
			Fractal frac = FractalManager.Fractal;
			ColorDialog dlg = new ColorDialog();
			dlg.Color = Color.FromArgb(
				Util.ClampByte((int)(frac.BackgroundColor.X * 255.0f)),
				Util.ClampByte((int)(frac.BackgroundColor.Y * 255.0f)),
				Util.ClampByte((int)(frac.BackgroundColor.Z * 255.0f)));

			DialogResult result = dlg.ShowDialog();

			if(result == DialogResult.OK)
			{
				Color bgc = dlg.Color;
				Vec4 bgColor = new Vec4((float)bgc.R/255.0f, (float)bgc.G/255.0f, (float)bgc.B/255.0f, (float)bgc.A/255.0f);
				frac.BackgroundColor = bgColor;
				backgroundColorPanel.BackColor = dlg.Color;
				FractalManager.NotifyToneMapChanged();
			}
		}

		private void applyFractalName(object sender, EventArgs e)
		{
			FractalManager.Fractal.Name = nameTextBox.Text;
		}

		private void applyLocalized(object sender, EventArgs e)
		{
			Branch branch = FractalManager.SelectedBranch;
			if(branch == null) return;

			if(localizedCheckbox.Checked)
				branch.Localize();
			else
				branch.Delocalize();

			FractalManager.NotifyGeometryChanged();
		}

		private void applyVariControls(object sender, EventArgs ea)
		{
			Branch branch = FractalManager.SelectedBranch;
			if(branch == null) return;

			branch.Variations.Clear();
			for(int i = 0; i < variControlCount; i++)
			{
				if(variDropBoxes[i].SelectedItem is Variation)
				{
					branch.Variations.Add(new Branch.VariEntry(
						(variDropBoxes[i].SelectedItem as Variation).Index,
						(float)variSpinners[i].Value));
				}
			}

			FractalManager.NotifyGeometryChanged();
		}
		#endregion

		#region Startup and Shutdown events
		private void renderer_HandleCreated(object sender, EventArgs e)
		{
			readyToRender = true;
			layoutRenderer();
		}
		private void MainForm_Shown(object sender, EventArgs e)
		{
#if DEBUG
			if(renderer.Context != null)
				renderer.Context.ErrorChecking = true;
#endif
		}


		private void MainForm_Load(object sender, EventArgs e)
		{
		}

		protected override void OnClosed(EventArgs e)
		{
			FractronConfig.Save(config);
			Shutdown();
			base.OnClosed(e);
		}

		public void Shutdown()
		{
			readyToRender = false;
			try{
				engineMgr.Shutdown();
			}
			catch(Exception){}
			try{
				if(renderer != null) renderer.Dispose();
			}
			catch(Exception){}
		}
		#endregion

		#region Iteration
		public void HandleIdle(object sender, EventArgs ea)
		{
			if(engineMgr == null)
				return;

			chooseDesiredEngineState();
			engineMgr.UpdateEngineState();
			updateEngineStatusPanel();

			if(engineMgr.EngineState != FractalEngineState.Online)
				return;

			if(engineMgr.DoneIterating)
			{
				engineMgr.DoCycle();
			}
			else
			{
				while(appIsIdle()) //keep cycling as long as there are no messages in the queue
				{
					engineMgr.DoCycle();
					System.Threading.Thread.Sleep(0); //yield to other threads
				}
			}
		}

		public void NotifyProgress(object sender, EventArgs ea)
		{
			if(renderer != null)
				renderer.Render(engineMgr.Engine.GetOutputTextureId());
			updatePerformanceMessage();
		}

		private void updatePerformanceMessage()
		{
			FractalEngine.Stats stats = engineMgr.Stats;
			double t_s = engineMgr.SecondsSinceReset;
			double t_iter = engineMgr.SecondsSpentCycling;
			double t_tone = engineMgr.SecondsSpentTonemapping;

			double fps = (double)engineMgr.CycleCount / t_s;
			double dps = (double)stats.TotalDotCount / t_s;
			double ips = (double)stats.TotalIterCount / t_s;

			dotCountStatusLabel.Text = "Dots: "       + Util.HumanReadableNumber((double)stats.TotalDotCount);
			dotRateStatusLabel.Text =  "Dots/Sec: "   + Util.HumanReadableNumber(dps);
			qualityStatusLabel.Text =  "Quality: "    + Util.HumanReadableNumber((double)stats.meanDotsPerSubpixel);
			iterStatusLabel.Text =     "Iterations: " + Util.HumanReadableNumber((double)stats.TotalIterCount);

			double pct_iter = t_iter / t_s * 100.0;
			double pct_tone = t_tone / t_s * 100.0;

			//timeStatusLabel.Text = string.Format("Time: {0:f2}, {1:f1}% iterating, {2:f1}% tonemapping", t_s, pct_iter, pct_tone);
			timeStatusLabel.Text = string.Format("t = {0:f2}", FractalManager.Time);
		}

		#endregion

		#region Resizing and Layout
		protected override void OnResizeBegin(EventArgs e)
		{
			resizing = true;
			base.OnResizeBegin(e);
		}

		private void renderContainer_Resize(object sender, EventArgs e)
		{
			layoutRenderer();
			chooseDesiredEngineState();
			if(resizing)
			{
				engineMgr.UpdateEngineState();
				updateEngineStatusPanel();
			}
		}

		protected override void OnResizeEnd(EventArgs e)
		{
			base.OnResizeEnd(e);
			resizing = false;
			layoutRenderer();
			chooseDesiredEngineState();
		}

		private void layoutRenderer()
		{
			int spx = renderContainer.ClientSize.Width/2 - engineStatusPanel.Size.Width/2;
			int spy = renderContainer.ClientSize.Height/2 - engineStatusPanel.Size.Height/2;

			engineStatusPanel.Location = new Point(spx,spy);

			if(Config.AutoSizeRenderer)
			{
				renderer.ClientSize = renderContainer.ClientSize;
				renderer.Location = new Point(0,0);
			}
			else
			{
				int xs = 0;
				int ys = 0;

				int cx = renderContainer.ClientSize.Width;
				int cy = renderContainer.ClientSize.Height;
				
				//see if the renderer has a wider aspect ratio 
				if(Config.CustomRes.Width * cy > cx * Config.CustomRes.Height)
				{
					xs = cx;
					ys = xs * Config.CustomRes.Height / Config.CustomRes.Width;
				}
				//the renderer must have a taller aspect ratio
				else
				{
					ys = cy;
					xs = ys * Config.CustomRes.Width / Config.CustomRes.Height;
				}
				
				renderer.ClientSize = new Size(xs,ys);
				int xPad = (renderContainer.ClientSize.Width  - renderer.Size.Width) /2;
				int yPad = (renderContainer.ClientSize.Height - renderer.Size.Height)/2;
				renderer.Location = new Point(xPad,yPad);
			}
		}
		#endregion

		#region Misc Helpers
		//chooses an appropriate state for the rendering engine
		private void chooseDesiredEngineState()
		{
			FractalEngineState state = FractalEngineState.Offline;
			if(!this.ReadyToRender || !renderer.HasGraphics)
			{
				state = FractalEngineState.Offline;
			}
			else if(
				this.ClientSize.IsEmpty || this.RendererSize.IsEmpty ||
				this.Resizing || this.DlgActive || FractalManager.Fractal == null)
			{
				state = FractalEngineState.Suspended;
			}
			else
			{
				state = FractalEngineState.Online;
			}
			engineMgr.DesiredEngineState = state;

			if(config.AutoSizeRenderer)
			{
				engineMgr.DesiredOutputSize = renderer.ClientSize;
			}
			else
			{
				engineMgr.DesiredOutputSize = config.CustomRes;
			}
			updateEngineStatusPanel();

			//if an engine error hasn't been shown before, do the engine error dialog.
			if(engineMgr.EngineState == FractalEngineState.Error && !engineErrorShown)
			{
				engineErrorShown = true;
				ErrorForm.Show(engineMgr.CurrentException);
			}
		}

		private void updateEngineStatusPanel()
		{
			if(engineMgr == null) return;

			FractalEngineState cs = engineMgr.EngineState;
			FractalEngineState ds = engineMgr.DesiredEngineState;

			string str = "";

			switch(cs)
			{
				case FractalEngineState.Online:
					switch(ds){
						case FractalEngineState.Online: str = "is online."; break;
						case FractalEngineState.Suspended: str = "is suspending..."; break;
						case FractalEngineState.Offline: str = "is shutting Down..."; break;
					}
					engineStatusPanel.Visible = false;
					renderer.Visible = true;
					break;
				case FractalEngineState.Suspended:
					switch(ds){
						case FractalEngineState.Online: str = "is resuming..."; break;
						case FractalEngineState.Suspended: str = "is suspended."; break;
						case FractalEngineState.Offline: str = "is shutting Down..."; break;
					}
					renderer.Visible = false;
					viewEngineErrorButton.Visible = false;
					restartEngineButton.Visible = false;
					engineStatusPanel.Visible = true;
					break;
				case FractalEngineState.Offline:
					switch(ds){
						case FractalEngineState.Online: str = "is starting..."; break;
						case FractalEngineState.Suspended: str = "is starting..."; break;
						case FractalEngineState.Offline: str = "is offline."; break;
					}
					renderer.Visible = false;
					viewEngineErrorButton.Visible = false;
					restartEngineButton.Visible = false;
					engineStatusPanel.Visible = true;
					break;
				case FractalEngineState.Error:
				default:
					str = "has crashed.";
					renderer.Visible = false;
					viewEngineErrorButton.Visible = true;
					restartEngineButton.Visible = true;
					engineStatusPanel.Visible = true;
					break;
			}
			engineStatusLabel.Text = string.Format("Fractal engine {0}", str);
		}


		private void trySaveLibrary()
		{
			try{
				FlameFileIO.WriteFlameFile(config.CurrentLibraryFile, FractalManager.Fractals);
			}catch(Exception ex){
#if DEBUG
				throw ex;
#else
				string msg = string.Format(Narratives.Error_FlameSaveFailed, config.CurrentLibraryFile);
				ErrorForm.Show(msg, ex);
#endif
			}
		}

		private void beginDlg()
		{
			dlgActive = true;
			chooseDesiredEngineState();
			engineMgr.UpdateEngineState();
			updateEngineStatusPanel();
			Application.DoEvents();
		}

		private void endDlg()
		{
			dlgActive = false;
			chooseDesiredEngineState();
		}
		#endregion

		#region Misc Handlers
		private void viewEngineErrorButton_Click(object sender, EventArgs e)
		{
			if(engineMgr == null) return;

			if(engineMgr.CurrentException == null)
				ErrorForm.Show("No Error::No error", null);
			else
				ErrorForm.Show(engineMgr.CurrentException);
		}

		private void restartEngineButton_Click(object sender, EventArgs e)
		{
			if(engineMgr == null) return;

			engineMgr.ClearError();
			engineErrorShown = false;
			chooseDesiredEngineState();
		}
		#endregion

		#region File Menu Handlers
		private void newToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.LoadNewFractal();
		}

		private void openLibraryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			beginDlg();

			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Fractal Flame files (*.flame)|*.flame|All Files (*.*)|*.*";
			dlg.DefaultExt = "flame";
			dlg.CheckFileExists = true;
			dlg.InitialDirectory = Config.FractalDir;
			DialogResult dlgResult = dlg.ShowDialog();

			if(dlgResult == DialogResult.OK)
			{
				bool loadOk = false;
				try{
					FractalManager.ReadFromFlameFile(dlg.FileName, config);
					libraryNameLabel.Text = Path.GetFileName(dlg.FileName);
					loadOk = true;
				}
				catch(FileNotFoundException ex)
				{
					MessageBox.Show(ex.Message, "File not found", MessageBoxButtons.OK, MessageBoxIcon.Error);			
				}
				catch(Exception ex)
				{
					ErrorForm.Show(Narratives.Error_FractalOpenFailed, ex);
				}

				if(loadOk)
				{
					if(FractalManager.Fractals.ContainsNonFractronFractals())
					{
						DialogResult bakResult = MessageBox.Show(Narratives.Notice_NonFractronFile, "Compatibility warning", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);

						if(bakResult == DialogResult.Yes)
						{
							string bakFileName = FlameFileIO.BackupFlameFile(dlg.FileName);
							if(bakFileName != null)
								MessageBox.Show(string.Format(Narratives.Notice_BackupCreated, bakFileName), "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
							else
								MessageBox.Show(string.Format(Narratives.Warning_BackupFailed, bakFileName), "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
						}
					}
					FractalManager.Fractals.ConvertNonFractronFlames();
					if(FractalManager.Fractals.Count > 0)
						FractalManager.SetCurrentCopy(FractalManager.Fractals[0]);
					config.CurrentLibraryFile = dlg.FileName;
					FractronConfig.Save(config);
					updateLibraryView();
				}
			}

			endDlg();
		}

		private void saveToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Fractal fractal = FractalManager.Fractal;
			fractal.Name = nameTextBox.Text;

			if(fractal.Name == null || fractal.Name == "")
			{
				MessageBox.Show("You must name a fractal before saving it", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}

			DialogResult overwriteOk = DialogResult.OK;

			int idx = FractalManager.Fractals.GetIndexByName(fractal.Name);
			if(idx >= 0)
			{
				string message = string.Format(
					"The library already contains a fractal named \"{0}\" which will be overwritten. Proceed anyway?", fractal.Name);
				overwriteOk = MessageBox.Show(
					message, "Name Exists", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
			}

			if(overwriteOk == DialogResult.OK)
			{
				FractalManager.Fractals.AddByName(new Fractal(fractal));
				updateLibraryView();

				trySaveLibrary();
			}
		}

		private void saveLibraryToolStripMenuItem_Click(object sender, EventArgs e)
		{
			beginDlg();

			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = "Fractal Flame files (*.flame)|*.flame|All Files (*.*)|*.*";
			dlg.DefaultExt = "flame";
			dlg.InitialDirectory = Config.FractalDir;
			dlg.OverwritePrompt = true;

			if(dlg.ShowDialog() == DialogResult.OK)
			{
				try{
					FlameFileIO.WriteFlameFile(dlg.FileName, FractalManager.Fractals);
					config.CurrentLibraryFile = dlg.FileName;
					libraryNameLabel.Text = Path.GetFileName(dlg.FileName);
				}
				catch(Exception ex)
				{
					string msg = string.Format(Narratives.Error_FlameSaveFailed, dlg.FileName);
					ErrorForm.Show(msg, ex);
				}
			}
			endDlg();
		}

		private void saveAsImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(engineMgr.Engine == null)
				return;
			
			Color[,] output = engineMgr.Engine.GetOutputPixels(); //grab the image pixel data before shutting down the engine

			beginDlg(); //tell the engine that we are now in a dialog

			ExportToImageDlg imgDlg = new ExportToImageDlg();

			string fileName = String.Format("{0}_{1}x{2}.png", (FractalManager.Fractal.Name ?? "unnamed"), output.GetLength(1), output.GetLength(0));
			imgDlg.FileName = Path.Combine(config.ImageDir, fileName);
			imgDlg.Format = ExportToImageDlg.FormatOption.Png;
			imgDlg.Transparency = ExportToImageDlg.TransparencyOption.None;

			if(imgDlg.ShowDialog() == DialogResult.OK)
			{
				try{
					imgDlg.SaveOutput(output);
					config.ImageDir = Path.GetDirectoryName(imgDlg.FileName);
				}
				catch(Exception ex)
				{
					string msg = string.Format(Narratives.Error_ImageSaveFailed, imgDlg.FileName);
					ErrorForm.Show(msg, ex);
				}
			}
			endDlg(); //bring the engine back online
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}
		#endregion

		#region Edit Menu Handlers
		private void addBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.AddBranch();
		}

		private void removeBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.RemoveSelectedBranch();
		}
		
		private void duplicateBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.DuplicateSelectedBranch();
		}
		private void invertBranchToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.InvertSelectedBranch();
		}

		private void configurationToolStripMenuItem_Click(object sender, EventArgs e)
		{
			readyToRender = false;
			chooseDesiredEngineState();
			engineMgr.UpdateEngineState();
			updateEngineStatusPanel();
			Application.DoEvents();

			FractronConfig prevConfig = new FractronConfig(config);
			
			ConfigForm dlg = new ConfigForm(Config, engineMgr);
			DialogResult result = dlg.ShowDialog();

			readyToRender = true;

			if(result == DialogResult.OK)
				config = new FractronConfig(dlg.Config);
			else
				config = new FractronConfig(prevConfig);

			config.Valid = false;
			
			layoutRenderer();
			chooseDesiredEngineState();

			FractronConfig.Save(config);
		}
		#endregion

		#region View Menu Handlers
		private void zoomInToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.ZoomIn();
		}

		private void zoomOutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.ZoomOut();
		}
				
		private void flipVerticalToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.FlipVertical();
		}

		private void nextToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.NextFractal();
		}

		private void prevToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.PrevFractal();
		}

		private void toggleEditorsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			renderer.EditMode = !renderer.EditMode;
			outerSplitContainer.Panel1Collapsed = !renderer.EditMode;
			innerSplitContainer.Panel1Collapsed = !renderer.EditMode;
		}

		private void viewXmlToolStripMenuItem_Click(object sender, EventArgs e)
		{
			beginDlg();

			StringWriter sw = new StringWriter();
			XmlWriterSettings settings = new XmlWriterSettings();
			settings.OmitXmlDeclaration = true;
			settings.ConformanceLevel = ConformanceLevel.Fragment;
			settings.Indent = true;
			XmlWriter xw = XmlWriter.Create(sw, settings);
			FlameFileIO.WriteFlame(xw, FractalManager.Fractal);
			xw.Flush();
			
			TextDisplayForm form = new TextDisplayForm();

			form.Text = "Fractal XML";
			form.Content = sw.ToString();
			form.ContentTextBox.WordWrap = false;
			form.ShowDialog();
			
			endDlg();
		}

		private void viewErrorToolStripMenuItem_Click(object sender, EventArgs e)
		{

		}

		private void resetViewToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.Fractal.CameraTransform = new Affine2D(2.0f, 0.0f, 0.0f, 2.0f, 0.0f, 0.0f);
			FractalManager.NotifyGeometryChanged();
		}
		#endregion

		#region Animate Menu Handlers
		private void playToolStripMenuItem_Click(object sender, EventArgs e)
		{
			engineMgr.StartPlayback();
		}
		private void stopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			engineMgr.StopPlayback();
		}
		private void seekBackMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.StepAnim(-0.05);
		}
		private void seekForwardMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.StepAnim(0.05);
		}
		#endregion

		#region Help Menu Handlers
		private void manualToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.Diagnostics.Process.Start(FractronConfig.ManualFileName);
		}
		
		private void hardwareInfoToolStripMenuItem_Click(object sender, EventArgs e)
		{
			System.IO.StringWriter text = new System.IO.StringWriter();

			if(engineMgr.FoundDevices == null) return;

			try{
				CPUDeviceEntry.CheckSupport();
			}catch(Exception ex){
				text.WriteLine("No CPU/OpenGL Support: "+ex.Message);
			}
			try{
				OpenCLDeviceEntry.CheckSupport();
			}catch(Exception ex){
				text.WriteLine("No OpenCL Support: "+ex.Message);
			}
			try{
				CudaDeviceEntry.CheckSupport();
			}catch(Exception ex){
				text.WriteLine("No CUDA Support: "+ex.Message);
			}
			text.WriteLine();
			foreach(DeviceEntry dev in engineMgr.FoundDevices)
				text.WriteLine(dev.GetReport());


			beginDlg();
			TextDisplayForm dlg = new TextDisplayForm();
			dlg.Text = "Hardware Info";
			dlg.Content = text.ToString();
			dlg.ShowDialog();
			endDlg();
		}

		private void runDiagnosticsToolStripMenuItem_Click(object sender, EventArgs e)
		{
			readyToRender = false;
			chooseDesiredEngineState();
			engineMgr.UpdateEngineState();
			updateEngineStatusPanel();
			Application.DoEvents();

			TestRunner runner = new TestRunner();
			runner.RunTests(new OpenCL.OpenCLTest());
			string report = runner.Report;

			TextDisplayForm dlg = new TextDisplayForm();
			dlg.Text = "Diagnostic Tests";
			dlg.Content = report;
			dlg.ShowDialog();

			readyToRender = true;
			layoutRenderer();
			chooseDesiredEngineState();
		}

		private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
		{
			beginDlg();
			FractronAboutBox dlg = new FractronAboutBox();
			dlg.ShowDialog();
			endDlg();
		}
		#endregion

		#region Palette Menu Handlers
		private void loadImageToolStripMenuItem_Click(object sender, EventArgs e)
		{
			beginDlg();
			OpenFileDialog dlg = new OpenFileDialog();
			dlg.Filter = "Supported Image Files|*.bmp;*.gif;*.jpg;*.jpeg;*.jfif;*.png;*.tiff";
			dlg.CheckFileExists = true;
			dlg.CheckPathExists = true;
			dlg.InitialDirectory = Config.PaletteDir;

			if(dlg.ShowDialog() == DialogResult.OK)
			{
				try{
					Palette pal = new Palette(dlg.FileName);
					FractalManager.SetPalette(pal);
				}
				catch(Exception ex){
					ErrorForm.Show("Palette Load Failed.", string.Format("Could not open palette file \"{0}\".",dlg.FileName), ex);
				}
			}
			endDlg();
		}
		
		private void load1DToolStripMenuItem_Click(object sender, EventArgs e)
		{
			beginDlg();
			if(paletteSelect1DForm == null)
				paletteSelect1DForm = new PaletteSelect1DForm();

			DialogResult result = paletteSelect1DForm.ShowDialog();
			if(result == DialogResult.OK && paletteSelect1DForm.Palette != null)
			{
				FractalManager.SetPalette(paletteSelect1DForm.Palette);
			}
			endDlg();
		}

		private void loadDefaultToolStripMenuItem_Click(object sender, EventArgs e)
		{
			FractalManager.SetPalette(Palette.DefaultPalette);
		}
		#endregion

		#region Library Menu Handlers
		private void libraryItemMenu_Opening(object sender, CancelEventArgs e)
		{
			if(libraryView.SelectedItems.Count <= 0)
				e.Cancel = true;
			else
				e.Cancel = false;
		}
		private void displayToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedItems.Count != 1) return;
			ListViewItem item = libraryView.SelectedItems[0];
			int itemIdx = libraryView.SelectedIndices[0];

			Fractal selectedFractal = item.Tag as Fractal;
			if(selectedFractal == null) return;

			FractalManager.Time = (double)itemIdx;
			FractalManager.SetCurrentCopy(selectedFractal);
		}

		private void renameToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedIndices.Count != 1) return;
			ListViewItem item = libraryView.SelectedItems[0];

			item.BeginEdit();
		}

		private void libraryView_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			Fractal selectedFractal = libraryView.Items[e.Item].Tag as Fractal;
			if(selectedFractal == null) return;

			if(e.Label == selectedFractal.Name) return; //no change, so just return

			int conflictIdx = FractalManager.Fractals.GetIndexByName(e.Label);
			
			if(e.Label == "") //name empty
			{
				MessageBox.Show("The name must have at least one character.", "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				e.CancelEdit = true;
			}
			else if(conflictIdx >= 0) //name found
			{
				MessageBox.Show("Another fractal already has that name.", "Rename Failed", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
				e.CancelEdit = true;
			}
			else //no conflict
			{
				selectedFractal.Name = e.Label;
				e.CancelEdit = false;
				trySaveLibrary();
			}
		}

		private void deleteToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedItems.Count != 1) return;
			ListViewItem item = libraryView.SelectedItems[0];
			Fractal selectedFractal = item.Tag as Fractal;
			if(selectedFractal == null) return;
			
			string msg = string.Format(@"Are you sure you want to delete ""{0}""?", selectedFractal.Name);
			DialogResult yn = MessageBox.Show(msg, "Delete Confirmation", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

			if(yn == DialogResult.Yes)
			{
				FractalManager.Fractals.Remove(selectedFractal);
				libraryView.Items.Remove(item);
				trySaveLibrary();
			}
		}

		private void moveToTopToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedIndices.Count != 1) return;
			int fromIdx = libraryView.SelectedIndices[0];
			moveLibraryItem(fromIdx, 0);
		}

		private void moveUpToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedIndices.Count != 1) return;
			int fromIdx = libraryView.SelectedIndices[0];
			moveLibraryItem(fromIdx, fromIdx-1);
		}

		private void moveDownToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedIndices.Count != 1) return;
			int fromIdx = libraryView.SelectedIndices[0];
			moveLibraryItem(fromIdx, fromIdx+1);
		}

		private void moveToBottomToolStripMenuItem_Click(object sender, EventArgs e)
		{
			if(libraryView.SelectedIndices.Count != 1) return;
			int fromIdx = libraryView.SelectedIndices[0];
			moveLibraryItem(fromIdx, libraryView.Items.Count - 1);
		}

		private void moveLibraryItem(int fromIdx, int toIdx)
		{
			if(fromIdx < 0 || fromIdx > libraryView.Items.Count-1 || toIdx < 0 || toIdx > libraryView.Items.Count-1)
				return;

			ListViewItem item = libraryView.Items[fromIdx];
			libraryView.Items.RemoveAt(fromIdx);
			libraryView.Items.Insert(toIdx, item);

			syncFractalsWithLibraryView();
			trySaveLibrary();
		}

		private void syncFractalsWithLibraryView()
		{
			FractalManager.Fractals.Clear();

			for(int i = 0; i < libraryView.Items.Count; i++)
			{
				if(libraryView.Items[i].Tag is Fractal)
					FractalManager.Fractals.Add(libraryView.Items[i].Tag as Fractal);
			}
		}

		#endregion

		#region Interop Stuff
		[StructLayout(LayoutKind.Sequential)]
		private struct peek_message{
			public IntPtr hWnd;
			public Message msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public System.Drawing.Point p;
		}

		[return: MarshalAs(UnmanagedType.Bool)]
		[System.Security.SuppressUnmanagedCodeSecurity] 
		[DllImport("User32.dll", CharSet=CharSet.Auto)]
		private static extern bool PeekMessage(
			out peek_message msg,
			IntPtr hWnd,
			uint messageFilterMin,
			uint messageFilterMax,
			uint flags
		);

		
		private const uint QS_KEY            = 0x0001;
		private const uint QS_MOUSEMOVE      = 0x0002;
		private const uint QS_MOUSEBUTTON    = 0x0004;
		private const uint QS_POSTMESSAGE    = 0x0008;
		private const uint QS_TIMER          = 0x0010;
		private const uint QS_PAINT          = 0x0020;
		private const uint QS_SENDMESSAGE    = 0x0040;
		private const uint QS_HOTKEY         = 0x0080;
		private const uint QS_ALLPOSTMESSAGE = 0x0100;
		private const uint QS_RAWINPUT       = 0x0400;
		private const uint QS_MOUSE          = QS_MOUSEMOVE | QS_MOUSEBUTTON;
		private const uint QS_INPUT          = QS_MOUSE | QS_KEY | QS_RAWINPUT;
		private const uint QS_ALLEVENTS      = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY;
		private const uint QS_ALLINPUT       = QS_INPUT | QS_POSTMESSAGE | QS_TIMER | QS_PAINT | QS_HOTKEY | QS_SENDMESSAGE;

		[System.Security.SuppressUnmanagedCodeSecurity] 
		[DllImport("User32.dll")]
		private static extern uint GetQueueStatus(uint flags);


		//if there are any windows messages at all in the queue to be processed, this should return false
		private bool appIsIdle()
		{
			uint status = GetQueueStatus(QS_ALLINPUT);
			uint current = status >> 16;
			uint recent = status & 0x0000FFFF;
			return current == 0 && recent == 0;
		}
		#endregion

		private void stepAnimToolStripButton_Click(object sender, EventArgs e)
		{
			FractalManager.StepAnim(0.05);
		}
	}
}
