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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace Fractron9000.UI
{
	public partial class ConfigForm : Form
	{
		private FractronConfig config;
		public FractronConfig Config{
			get{ return config; }
		}

		private EngineManager engineMgr;
		public EngineManager EngineManager{
			get{ return engineMgr; }
		}

		public ConfigForm(FractronConfig config, EngineManager engineMgr)
		{
			InitializeComponent();
			this.config = config;
			this.engineMgr = engineMgr;

			initializeDevices();
		}

		protected override void OnShown(EventArgs e)
		{
			updateRes();
			updateControls();
			base.OnShown(e);
		}

		private void initializeDevices()
		{
			deviceComboBox.Items.Clear();
			foreach(DeviceEntry dev in engineMgr.FoundDevices)
				deviceComboBox.Items.Add(dev);

			autoDeviceRadioButton.Checked = (config.EngineType == EngineType.Auto);
			customDeviceRadioButton.Checked = !(config.EngineType == EngineType.Auto);
			if(config.EngineType != EngineType.Auto)
			{
        float bestScore = -1.0f;
        DeviceEntry bestDev = null;
				foreach(DeviceEntry dev in engineMgr.FoundDevices)
        {
          float score = dev.EvalMatch(config);

					if(score > bestScore)
          {
						bestDev = dev;
            bestScore = score;
          }
        }
        deviceComboBox.SelectedItem = bestDev;
			}
		}

		private void autoDeviceRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if(autoDeviceRadioButton.Checked)
			{
				DeviceEntry autoDev = engineMgr.GetBestDevice();
				if(autoDev != null)
					deviceComboBox.SelectedItem = autoDev;

				deviceComboBox.Enabled = false;
			}
		}

		private void customDeviceRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if(customDeviceRadioButton.Checked)
			{
				deviceComboBox.Enabled = true;
			}
		}

		private void resolution_CheckedChanged(object sender, EventArgs e)
		{
			updateRes();
		}

		private void updateRes()
		{
			widthSpinner.SetValueStealth((double)config.CustomRes.Width);
			heightSpinner.SetValueStealth((double)config.CustomRes.Height);
			widthSpinner.Enabled = customRadioButton.Checked;
			heightSpinner.Enabled = customRadioButton.Checked;
		}

		private void updateControls()
		{
			autoSizeRadioButton.Checked = config.AutoSizeRenderer;
			customRadioButton.Checked = !config.AutoSizeRenderer;

			redrawQualitySpinner.Value = (double)config.RedrawQuality;
			targetQualityDragSpin.Value = (double)config.TargetQuality;

			paletteDirTextBox.Text = config.PaletteDir;
			imageDirTextBox.Text = config.ImageDir;
		}

		private void deviceComboBox_SelectedIndexChanged(object sender, EventArgs e)
		{
			DeviceEntry selDevice = deviceComboBox.SelectedItem as DeviceEntry;
			if(selDevice != null)
			{
				hardwareTypeValueLabel.Text = selDevice.Api;
				performanceValueLabel.Text = selDevice.PerformanceRating.ToString("F1");
			}
			else
			{
				hardwareTypeValueLabel.Text = "";
				performanceValueLabel.Text = "";
			}
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			int width = 0;
			int height = 0;

			DeviceEntry selectedDevice = deviceComboBox.SelectedItem as DeviceEntry;
			if(autoDeviceRadioButton.Checked || selectedDevice == null)
			{
				config.EngineType = EngineType.Auto;
				config.DeviceName = "";
        config.DeviceIdentifier = "";
        config.DeviceIndex = 0;
			}
			else
			{
				config.EngineType = selectedDevice.EngineType;
        config.DeviceName = selectedDevice.Name;
        config.DeviceIdentifier = selectedDevice.Identifier;
				config.DeviceIndex = selectedDevice.Index;
			}

			width =  (int)widthSpinner.Value;
			height = (int)heightSpinner.Value;

			if(!Directory.Exists(paletteDirTextBox.Text))
			{
				MessageBox.Show(string.Format("\"{0}\" is not a valid directory.", paletteDirTextBox.Text),
					"Notice", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			if(!Directory.Exists(imageDirTextBox.Text))
			{
				MessageBox.Show(string.Format("\"{0}\" is not a valid directory.", imageDirTextBox.Text),
					"Notice", MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
				return;
			}

			config.CustomRes = new Size(width,height);
			config.AutoSizeRenderer = autoSizeRadioButton.Checked;
			config.RedrawQuality = (int)redrawQualitySpinner.Value;
			config.TargetQuality = (float)targetQualityDragSpin.Value;
			
			config.ImageDir = imageDirTextBox.Text;
			config.PaletteDir = paletteDirTextBox.Text;

			DialogResult = DialogResult.OK;
			Close();
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void paletteDirButton_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.SelectedPath = config.PaletteDir;
			if(dlg.ShowDialog() == DialogResult.OK)
				paletteDirTextBox.Text = dlg.SelectedPath;
		}

		private void imageDirButton_Click(object sender, EventArgs e)
		{
			FolderBrowserDialog dlg = new FolderBrowserDialog();
			dlg.SelectedPath = config.ImageDir;
			if(dlg.ShowDialog() == DialogResult.OK)
				imageDirTextBox.Text = dlg.SelectedPath;
		}
	}
}
