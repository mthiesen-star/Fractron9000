namespace Fractron9000.UI
{
	partial class ExportToImageDlg
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
			this.fileNameBox = new System.Windows.Forms.TextBox();
			this.fileNameLabel = new System.Windows.Forms.Label();
			this.browseButton = new System.Windows.Forms.Button();
			this.saveButton = new System.Windows.Forms.Button();
			this.cancelButton = new System.Windows.Forms.Button();
			this.transparencyGroupBox = new System.Windows.Forms.GroupBox();
			this.colorRadioButton = new System.Windows.Forms.RadioButton();
			this.transparentRadioButton = new System.Windows.Forms.RadioButton();
			this.formatGroupBox = new System.Windows.Forms.GroupBox();
			this.pngRadioButton = new System.Windows.Forms.RadioButton();
			this.jpegRadioButton = new System.Windows.Forms.RadioButton();
			this.transparencyGroupBox.SuspendLayout();
			this.formatGroupBox.SuspendLayout();
			this.SuspendLayout();
			// 
			// fileNameBox
			// 
			this.fileNameBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.fileNameBox.Location = new System.Drawing.Point(88, 12);
			this.fileNameBox.Name = "fileNameBox";
			this.fileNameBox.Size = new System.Drawing.Size(339, 20);
			this.fileNameBox.TabIndex = 0;
			this.fileNameBox.Leave += new System.EventHandler(this.fileNameBox_Leave);
			// 
			// fileNameLabel
			// 
			this.fileNameLabel.Location = new System.Drawing.Point(12, 12);
			this.fileNameLabel.Name = "fileNameLabel";
			this.fileNameLabel.Size = new System.Drawing.Size(70, 20);
			this.fileNameLabel.TabIndex = 1;
			this.fileNameLabel.Text = "File Name";
			this.fileNameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
			// 
			// browseButton
			// 
			this.browseButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.browseButton.Location = new System.Drawing.Point(433, 12);
			this.browseButton.Name = "browseButton";
			this.browseButton.Size = new System.Drawing.Size(70, 20);
			this.browseButton.TabIndex = 2;
			this.browseButton.Text = "&Browse...";
			this.browseButton.UseVisualStyleBackColor = true;
			this.browseButton.Click += new System.EventHandler(this.browseButton_Click);
			// 
			// saveButton
			// 
			this.saveButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.saveButton.Location = new System.Drawing.Point(433, 152);
			this.saveButton.Name = "saveButton";
			this.saveButton.Size = new System.Drawing.Size(70, 23);
			this.saveButton.TabIndex = 3;
			this.saveButton.Text = "&Save";
			this.saveButton.UseVisualStyleBackColor = true;
			this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cancelButton.DialogResult = System.Windows.Forms.DialogResult.Cancel;
			this.cancelButton.Location = new System.Drawing.Point(12, 152);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(70, 23);
			this.cancelButton.TabIndex = 3;
			this.cancelButton.Text = "&Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// transparencyGroupBox
			// 
			this.transparencyGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.transparencyGroupBox.Controls.Add(this.transparentRadioButton);
			this.transparencyGroupBox.Controls.Add(this.colorRadioButton);
			this.transparencyGroupBox.Location = new System.Drawing.Point(12, 89);
			this.transparencyGroupBox.Name = "transparencyGroupBox";
			this.transparencyGroupBox.Size = new System.Drawing.Size(491, 51);
			this.transparencyGroupBox.TabIndex = 4;
			this.transparencyGroupBox.TabStop = false;
			this.transparencyGroupBox.Text = "Background Transparency";
			// 
			// colorRadioButton
			// 
			this.colorRadioButton.AutoSize = true;
			this.colorRadioButton.Location = new System.Drawing.Point(6, 19);
			this.colorRadioButton.Name = "colorRadioButton";
			this.colorRadioButton.Size = new System.Drawing.Size(163, 17);
			this.colorRadioButton.TabIndex = 0;
			this.colorRadioButton.TabStop = true;
			this.colorRadioButton.Text = "None (use background color)";
			this.colorRadioButton.UseVisualStyleBackColor = true;
			this.colorRadioButton.CheckedChanged += new System.EventHandler(this.colorRadioButton_CheckedChanged);
			// 
			// transparentRadioButton
			// 
			this.transparentRadioButton.AutoSize = true;
			this.transparentRadioButton.Location = new System.Drawing.Point(258, 19);
			this.transparentRadioButton.Name = "transparentRadioButton";
			this.transparentRadioButton.Size = new System.Drawing.Size(82, 17);
			this.transparentRadioButton.TabIndex = 0;
			this.transparentRadioButton.TabStop = true;
			this.transparentRadioButton.Text = "Transparent";
			this.transparentRadioButton.UseVisualStyleBackColor = true;
			this.transparentRadioButton.CheckedChanged += new System.EventHandler(this.transparentRadioButton_CheckedChanged);
			// 
			// formatGroupBox
			// 
			this.formatGroupBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.formatGroupBox.Controls.Add(this.jpegRadioButton);
			this.formatGroupBox.Controls.Add(this.pngRadioButton);
			this.formatGroupBox.Location = new System.Drawing.Point(12, 38);
			this.formatGroupBox.Name = "formatGroupBox";
			this.formatGroupBox.Size = new System.Drawing.Size(491, 45);
			this.formatGroupBox.TabIndex = 5;
			this.formatGroupBox.TabStop = false;
			this.formatGroupBox.Text = "File Format";
			// 
			// pngRadioButton
			// 
			this.pngRadioButton.AutoSize = true;
			this.pngRadioButton.Location = new System.Drawing.Point(6, 19);
			this.pngRadioButton.Name = "pngRadioButton";
			this.pngRadioButton.Size = new System.Drawing.Size(48, 17);
			this.pngRadioButton.TabIndex = 0;
			this.pngRadioButton.TabStop = true;
			this.pngRadioButton.Text = "PNG";
			this.pngRadioButton.UseVisualStyleBackColor = true;
			this.pngRadioButton.CheckedChanged += new System.EventHandler(this.pngRadioButton_CheckedChanged);
			// 
			// jpegRadioButton
			// 
			this.jpegRadioButton.AutoSize = true;
			this.jpegRadioButton.Location = new System.Drawing.Point(258, 19);
			this.jpegRadioButton.Name = "jpegRadioButton";
			this.jpegRadioButton.Size = new System.Drawing.Size(52, 17);
			this.jpegRadioButton.TabIndex = 0;
			this.jpegRadioButton.TabStop = true;
			this.jpegRadioButton.Text = "JPEG";
			this.jpegRadioButton.UseVisualStyleBackColor = true;
			this.jpegRadioButton.CheckedChanged += new System.EventHandler(this.jpegRadioButton_CheckedChanged);
			// 
			// ExportToImageDlg
			// 
			this.AcceptButton = this.saveButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.CancelButton = this.cancelButton;
			this.ClientSize = new System.Drawing.Size(515, 187);
			this.Controls.Add(this.formatGroupBox);
			this.Controls.Add(this.transparencyGroupBox);
			this.Controls.Add(this.cancelButton);
			this.Controls.Add(this.saveButton);
			this.Controls.Add(this.browseButton);
			this.Controls.Add(this.fileNameLabel);
			this.Controls.Add(this.fileNameBox);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ExportToImageDlg";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Save Fractal as Image";
			this.transparencyGroupBox.ResumeLayout(false);
			this.transparencyGroupBox.PerformLayout();
			this.formatGroupBox.ResumeLayout(false);
			this.formatGroupBox.PerformLayout();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.TextBox fileNameBox;
		private System.Windows.Forms.Label fileNameLabel;
		private System.Windows.Forms.Button browseButton;
		private System.Windows.Forms.Button saveButton;
		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.GroupBox transparencyGroupBox;
		private System.Windows.Forms.RadioButton colorRadioButton;
		private System.Windows.Forms.RadioButton transparentRadioButton;
		private System.Windows.Forms.GroupBox formatGroupBox;
		private System.Windows.Forms.RadioButton jpegRadioButton;
		private System.Windows.Forms.RadioButton pngRadioButton;
	}
}