namespace Fractron9000.UI
{
	partial class ConfigForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.outputResolutionGroupBox = new System.Windows.Forms.GroupBox();
			this.heightLabel = new System.Windows.Forms.Label();
			this.widthLabel = new System.Windows.Forms.Label();
			this.customRadioButton = new System.Windows.Forms.RadioButton();
			this.autoSizeRadioButton = new System.Windows.Forms.RadioButton();
			this.heightSpinner = new MTUtil.UI.DragSpin();
			this.widthSpinner = new MTUtil.UI.DragSpin();
			this.okButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.qualityGroupBox = new System.Windows.Forms.GroupBox();
			this.label3 = new System.Windows.Forms.Label();
			this.label2 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.targetQualityDragSpin = new MTUtil.UI.DragSpin();
			this.redrawQualitySpinner = new MTUtil.UI.DragSpin();
			this.foldersGroupBox = new System.Windows.Forms.GroupBox();
			this.imageDirButton = new System.Windows.Forms.Button();
			this.paletteDirButton = new System.Windows.Forms.Button();
			this.imageDirTextBox = new System.Windows.Forms.TextBox();
			this.paletteDirTextBox = new System.Windows.Forms.TextBox();
			this.label6 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.deviceGroupBox = new System.Windows.Forms.GroupBox();
			this.performanceValueLabel = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.deviceComboBox = new System.Windows.Forms.ComboBox();
			this.customDeviceRadioButton = new System.Windows.Forms.RadioButton();
			this.autoDeviceRadioButton = new System.Windows.Forms.RadioButton();
			this.hardwareTypeValueLabel = new System.Windows.Forms.Label();
			this.outputResolutionGroupBox.SuspendLayout();
			this.qualityGroupBox.SuspendLayout();
			this.foldersGroupBox.SuspendLayout();
			this.deviceGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// outputResolutionGroupBox
			// 
			this.outputResolutionGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.outputResolutionGroupBox.Controls.Add(this.heightLabel);
			this.outputResolutionGroupBox.Controls.Add(this.widthLabel);
			this.outputResolutionGroupBox.Controls.Add(this.customRadioButton);
			this.outputResolutionGroupBox.Controls.Add(this.autoSizeRadioButton);
			this.outputResolutionGroupBox.Controls.Add(this.heightSpinner);
			this.outputResolutionGroupBox.Controls.Add(this.widthSpinner);
			this.outputResolutionGroupBox.Location = new System.Drawing.Point(12, 142);
			this.outputResolutionGroupBox.Name = "outputResolutionGroupBox";
			this.outputResolutionGroupBox.Size = new System.Drawing.Size(559, 102);
			this.outputResolutionGroupBox.TabIndex = 0;
			this.outputResolutionGroupBox.TabStop = false;
			this.outputResolutionGroupBox.Text = "Output Resolution";
			// 
			// heightLabel
			// 
			this.heightLabel.Location = new System.Drawing.Point(194, 68);
			this.heightLabel.Name = "heightLabel";
			this.heightLabel.Size = new System.Drawing.Size(46, 20);
			this.heightLabel.TabIndex = 2;
			this.heightLabel.Text = "height:";
			this.heightLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// widthLabel
			// 
			this.widthLabel.Location = new System.Drawing.Point(197, 42);
			this.widthLabel.Name = "widthLabel";
			this.widthLabel.Size = new System.Drawing.Size(43, 20);
			this.widthLabel.TabIndex = 2;
			this.widthLabel.Text = "width:";
			this.widthLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// customRadioButton
			// 
			this.customRadioButton.AutoSize = true;
			this.customRadioButton.Location = new System.Drawing.Point(208, 19);
			this.customRadioButton.Name = "customRadioButton";
			this.customRadioButton.Size = new System.Drawing.Size(60, 17);
			this.customRadioButton.TabIndex = 1;
			this.customRadioButton.TabStop = true;
			this.customRadioButton.Text = "Custom";
			this.customRadioButton.UseVisualStyleBackColor = true;
			this.customRadioButton.CheckedChanged += new System.EventHandler(this.resolution_CheckedChanged);
			// 
			// autoSizeRadioButton
			// 
			this.autoSizeRadioButton.AutoSize = true;
			this.autoSizeRadioButton.Location = new System.Drawing.Point(7, 20);
			this.autoSizeRadioButton.Name = "autoSizeRadioButton";
			this.autoSizeRadioButton.Size = new System.Drawing.Size(72, 17);
			this.autoSizeRadioButton.TabIndex = 0;
			this.autoSizeRadioButton.TabStop = true;
			this.autoSizeRadioButton.Text = "Automatic";
			this.autoSizeRadioButton.UseVisualStyleBackColor = true;
			this.autoSizeRadioButton.CheckedChanged += new System.EventHandler(this.resolution_CheckedChanged);
			// 
			// heightSpinner
			// 
			this.heightSpinner.FormatString = "0";
			this.heightSpinner.Location = new System.Drawing.Point(246, 68);
			this.heightSpinner.MajorTickStep = 100;
			this.heightSpinner.MaxVal = 12000;
			this.heightSpinner.MinorTicksPerMajorTick = 10;
			this.heightSpinner.MinVal = 8;
			this.heightSpinner.Name = "heightSpinner";
			this.heightSpinner.PixelsPerMinorTick = 10;
			this.heightSpinner.Size = new System.Drawing.Size(60, 20);
			this.heightSpinner.TabIndex = 0;
			this.heightSpinner.Value = 600;
			// 
			// widthSpinner
			// 
			this.widthSpinner.FormatString = "0";
			this.widthSpinner.Location = new System.Drawing.Point(246, 42);
			this.widthSpinner.MajorTickStep = 100;
			this.widthSpinner.MaxVal = 12000;
			this.widthSpinner.MinorTicksPerMajorTick = 10;
			this.widthSpinner.MinVal = 8;
			this.widthSpinner.Name = "widthSpinner";
			this.widthSpinner.PixelsPerMinorTick = 10;
			this.widthSpinner.Size = new System.Drawing.Size(60, 20);
			this.widthSpinner.TabIndex = 0;
			this.widthSpinner.Value = 800;
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(497, 445);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 1;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(12, 445);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 1;
			this.cancelButton.Text = "Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// qualityGroupBox
			// 
			this.qualityGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.qualityGroupBox.Controls.Add(this.label3);
			this.qualityGroupBox.Controls.Add(this.label2);
			this.qualityGroupBox.Controls.Add(this.label4);
			this.qualityGroupBox.Controls.Add(this.label1);
			this.qualityGroupBox.Controls.Add(this.targetQualityDragSpin);
			this.qualityGroupBox.Controls.Add(this.redrawQualitySpinner);
			this.qualityGroupBox.Location = new System.Drawing.Point(12, 250);
			this.qualityGroupBox.Name = "qualityGroupBox";
			this.qualityGroupBox.Size = new System.Drawing.Size(559, 105);
			this.qualityGroupBox.TabIndex = 2;
			this.qualityGroupBox.TabStop = false;
			this.qualityGroupBox.Text = "Quality";
			// 
			// label3
			// 
			this.label3.Location = new System.Drawing.Point(205, 46);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(156, 46);
			this.label3.TabIndex = 2;
			this.label3.Text = "Rendering will stop once this quality level is reached.";
			// 
			// label2
			// 
			this.label2.Location = new System.Drawing.Point(13, 46);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(153, 46);
			this.label2.TabIndex = 2;
			this.label2.Text = "Increase for higher quality rendering. Decrease for a more responsive user interf" +
				"ace.";
			// 
			// label4
			// 
			this.label4.Location = new System.Drawing.Point(205, 19);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(90, 20);
			this.label4.TabIndex = 1;
			this.label4.Text = "Target Quality";
			this.label4.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label1
			// 
			this.label1.Location = new System.Drawing.Point(10, 19);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(90, 20);
			this.label1.TabIndex = 1;
			this.label1.Text = "Frame Quality";
			this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// targetQualityDragSpin
			// 
			this.targetQualityDragSpin.FormatString = "0";
			this.targetQualityDragSpin.Location = new System.Drawing.Point(301, 19);
			this.targetQualityDragSpin.MajorTickStep = 64;
			this.targetQualityDragSpin.MaxVal = 65536;
			this.targetQualityDragSpin.MinorTicksPerMajorTick = 8;
			this.targetQualityDragSpin.MinVal = 32;
			this.targetQualityDragSpin.Name = "targetQualityDragSpin";
			this.targetQualityDragSpin.PixelsPerMinorTick = 8;
			this.targetQualityDragSpin.Size = new System.Drawing.Size(60, 20);
			this.targetQualityDragSpin.TabIndex = 0;
			this.targetQualityDragSpin.Value = 256;
			// 
			// redrawQualitySpinner
			// 
			this.redrawQualitySpinner.FormatString = "0";
			this.redrawQualitySpinner.Location = new System.Drawing.Point(106, 19);
			this.redrawQualitySpinner.MajorTickStep = 64;
			this.redrawQualitySpinner.MaxVal = 16384;
			this.redrawQualitySpinner.MinorTicksPerMajorTick = 8;
			this.redrawQualitySpinner.MinVal = 32;
			this.redrawQualitySpinner.Name = "redrawQualitySpinner";
			this.redrawQualitySpinner.PixelsPerMinorTick = 8;
			this.redrawQualitySpinner.Size = new System.Drawing.Size(60, 20);
			this.redrawQualitySpinner.TabIndex = 0;
			this.redrawQualitySpinner.Value = 256;
			// 
			// foldersGroupBox
			// 
			this.foldersGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.foldersGroupBox.Controls.Add(this.imageDirButton);
			this.foldersGroupBox.Controls.Add(this.paletteDirButton);
			this.foldersGroupBox.Controls.Add(this.imageDirTextBox);
			this.foldersGroupBox.Controls.Add(this.paletteDirTextBox);
			this.foldersGroupBox.Controls.Add(this.label6);
			this.foldersGroupBox.Controls.Add(this.label5);
			this.foldersGroupBox.Location = new System.Drawing.Point(12, 361);
			this.foldersGroupBox.Name = "foldersGroupBox";
			this.foldersGroupBox.Size = new System.Drawing.Size(558, 78);
			this.foldersGroupBox.TabIndex = 3;
			this.foldersGroupBox.TabStop = false;
			this.foldersGroupBox.Text = "Folders";
			// 
			// imageDirButton
			// 
			this.imageDirButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.imageDirButton.Location = new System.Drawing.Point(492, 48);
			this.imageDirButton.Name = "imageDirButton";
			this.imageDirButton.Size = new System.Drawing.Size(59, 20);
			this.imageDirButton.TabIndex = 2;
			this.imageDirButton.Text = "Browse...";
			this.imageDirButton.UseVisualStyleBackColor = true;
			this.imageDirButton.Click += new System.EventHandler(this.imageDirButton_Click);
			// 
			// paletteDirButton
			// 
			this.paletteDirButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.paletteDirButton.Location = new System.Drawing.Point(492, 22);
			this.paletteDirButton.Name = "paletteDirButton";
			this.paletteDirButton.Size = new System.Drawing.Size(59, 20);
			this.paletteDirButton.TabIndex = 2;
			this.paletteDirButton.Text = "Browse...";
			this.paletteDirButton.UseVisualStyleBackColor = true;
			this.paletteDirButton.Click += new System.EventHandler(this.paletteDirButton_Click);
			// 
			// imageDirTextBox
			// 
			this.imageDirTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.imageDirTextBox.Location = new System.Drawing.Point(103, 48);
			this.imageDirTextBox.Name = "imageDirTextBox";
			this.imageDirTextBox.Size = new System.Drawing.Size(383, 20);
			this.imageDirTextBox.TabIndex = 0;
			// 
			// paletteDirTextBox
			// 
			this.paletteDirTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.paletteDirTextBox.Location = new System.Drawing.Point(103, 22);
			this.paletteDirTextBox.Name = "paletteDirTextBox";
			this.paletteDirTextBox.Size = new System.Drawing.Size(383, 20);
			this.paletteDirTextBox.TabIndex = 0;
			// 
			// label6
			// 
			this.label6.Location = new System.Drawing.Point(6, 48);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(91, 20);
			this.label6.TabIndex = 1;
			this.label6.Text = "Saved Images:";
			this.label6.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// label5
			// 
			this.label5.Location = new System.Drawing.Point(6, 22);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(91, 20);
			this.label5.TabIndex = 1;
			this.label5.Text = "Palettes:";
			this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// deviceGroupBox
			// 
			this.deviceGroupBox.Controls.Add(this.hardwareTypeValueLabel);
			this.deviceGroupBox.Controls.Add(this.performanceValueLabel);
			this.deviceGroupBox.Controls.Add(this.label8);
			this.deviceGroupBox.Controls.Add(this.label7);
			this.deviceGroupBox.Controls.Add(this.deviceComboBox);
			this.deviceGroupBox.Controls.Add(this.customDeviceRadioButton);
			this.deviceGroupBox.Controls.Add(this.autoDeviceRadioButton);
			this.deviceGroupBox.Location = new System.Drawing.Point(12, 12);
			this.deviceGroupBox.Name = "deviceGroupBox";
			this.deviceGroupBox.Size = new System.Drawing.Size(558, 124);
			this.deviceGroupBox.TabIndex = 4;
			this.deviceGroupBox.TabStop = false;
			this.deviceGroupBox.Text = "Device";
			// 
			// performanceValueLabel
			// 
			this.performanceValueLabel.AutoSize = true;
			this.performanceValueLabel.Location = new System.Drawing.Point(259, 61);
			this.performanceValueLabel.Margin = new System.Windows.Forms.Padding(3);
			this.performanceValueLabel.Name = "performanceValueLabel";
			this.performanceValueLabel.Size = new System.Drawing.Size(13, 13);
			this.performanceValueLabel.TabIndex = 3;
			this.performanceValueLabel.Text = "0";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(149, 42);
			this.label8.Margin = new System.Windows.Forms.Padding(3);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(83, 13);
			this.label8.TabIndex = 2;
			this.label8.Text = "Hardware Type:";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(149, 61);
			this.label7.Margin = new System.Windows.Forms.Padding(3);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(104, 13);
			this.label7.TabIndex = 2;
			this.label7.Text = "Performance Rating:";
			// 
			// deviceComboBox
			// 
			this.deviceComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.deviceComboBox.Enabled = false;
			this.deviceComboBox.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.deviceComboBox.FormattingEnabled = true;
			this.deviceComboBox.Location = new System.Drawing.Point(152, 12);
			this.deviceComboBox.Name = "deviceComboBox";
			this.deviceComboBox.Size = new System.Drawing.Size(399, 24);
			this.deviceComboBox.TabIndex = 1;
			this.deviceComboBox.SelectedIndexChanged += new System.EventHandler(this.deviceComboBox_SelectedIndexChanged);
			// 
			// customDeviceRadioButton
			// 
			this.customDeviceRadioButton.AutoSize = true;
			this.customDeviceRadioButton.Location = new System.Drawing.Point(88, 19);
			this.customDeviceRadioButton.Name = "customDeviceRadioButton";
			this.customDeviceRadioButton.Size = new System.Drawing.Size(60, 17);
			this.customDeviceRadioButton.TabIndex = 0;
			this.customDeviceRadioButton.TabStop = true;
			this.customDeviceRadioButton.Text = "Custom";
			this.customDeviceRadioButton.UseVisualStyleBackColor = true;
			this.customDeviceRadioButton.CheckedChanged += new System.EventHandler(this.customDeviceRadioButton_CheckedChanged);
			// 
			// autoDeviceRadioButton
			// 
			this.autoDeviceRadioButton.AutoSize = true;
			this.autoDeviceRadioButton.Location = new System.Drawing.Point(10, 19);
			this.autoDeviceRadioButton.Name = "autoDeviceRadioButton";
			this.autoDeviceRadioButton.Size = new System.Drawing.Size(72, 17);
			this.autoDeviceRadioButton.TabIndex = 0;
			this.autoDeviceRadioButton.TabStop = true;
			this.autoDeviceRadioButton.Text = "Automatic";
			this.autoDeviceRadioButton.UseVisualStyleBackColor = true;
			this.autoDeviceRadioButton.CheckedChanged += new System.EventHandler(this.autoDeviceRadioButton_CheckedChanged);
			// 
			// hardwareTypeValueLabel
			// 
			this.hardwareTypeValueLabel.AutoSize = true;
			this.hardwareTypeValueLabel.Location = new System.Drawing.Point(259, 42);
			this.hardwareTypeValueLabel.Margin = new System.Windows.Forms.Padding(3);
			this.hardwareTypeValueLabel.Name = "hardwareTypeValueLabel";
			this.hardwareTypeValueLabel.Size = new System.Drawing.Size(13, 13);
			this.hardwareTypeValueLabel.TabIndex = 3;
			this.hardwareTypeValueLabel.Text = "0";
			// 
			// ConfigForm
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(584, 480);
			this.Controls.Add(this.deviceGroupBox);
			this.Controls.Add(this.foldersGroupBox);
			this.Controls.Add(this.qualityGroupBox);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.outputResolutionGroupBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ConfigForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Fractron 9000 Configuration";
			this.outputResolutionGroupBox.ResumeLayout(false);
			this.outputResolutionGroupBox.PerformLayout();
			this.qualityGroupBox.ResumeLayout(false);
			this.foldersGroupBox.ResumeLayout(false);
			this.foldersGroupBox.PerformLayout();
			this.deviceGroupBox.ResumeLayout(false);
			this.deviceGroupBox.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.GroupBox outputResolutionGroupBox;
		private System.Windows.Forms.RadioButton customRadioButton;
		private System.Windows.Forms.RadioButton autoSizeRadioButton;
		private System.Windows.Forms.Label heightLabel;
		private System.Windows.Forms.Label widthLabel;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.GroupBox qualityGroupBox;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private MTUtil.UI.DragSpin redrawQualitySpinner;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private MTUtil.UI.DragSpin targetQualityDragSpin;
		private System.Windows.Forms.GroupBox foldersGroupBox;
		private System.Windows.Forms.TextBox paletteDirTextBox;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Button imageDirButton;
		private System.Windows.Forms.Button paletteDirButton;
		private System.Windows.Forms.TextBox imageDirTextBox;
		private System.Windows.Forms.Label label6;
		private MTUtil.UI.DragSpin heightSpinner;
		private MTUtil.UI.DragSpin widthSpinner;
		private System.Windows.Forms.GroupBox deviceGroupBox;
		private System.Windows.Forms.RadioButton autoDeviceRadioButton;
		private System.Windows.Forms.ComboBox deviceComboBox;
		private System.Windows.Forms.RadioButton customDeviceRadioButton;
		private System.Windows.Forms.Label performanceValueLabel;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label hardwareTypeValueLabel;
	}
}