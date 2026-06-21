namespace Fractron9000.UI
{
	partial class PaletteSelect1DForm
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
			this.cancelButton = new System.Windows.Forms.Button();
			this.okButton = new System.Windows.Forms.Button();
			this.paletteList = new System.Windows.Forms.ListView();
			this.paletteHeader = new System.Windows.Forms.ColumnHeader();
			this.SuspendLayout();
			// 
			// cancelButton
			// 
			this.cancelButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.cancelButton.Location = new System.Drawing.Point(13, 500);
			this.cancelButton.Name = "cancelButton";
			this.cancelButton.Size = new System.Drawing.Size(75, 23);
			this.cancelButton.TabIndex = 0;
			this.cancelButton.Text = "&Cancel";
			this.cancelButton.UseVisualStyleBackColor = true;
			this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(391, 500);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 1;
			this.okButton.Text = "&OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.handleSelectionConfirmed);
			// 
			// paletteList
			// 
			this.paletteList.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.paletteList.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.paletteHeader});
			this.paletteList.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.paletteList.HideSelection = false;
			this.paletteList.Location = new System.Drawing.Point(12, 12);
			this.paletteList.MultiSelect = false;
			this.paletteList.Name = "paletteList";
			this.paletteList.Size = new System.Drawing.Size(454, 482);
			this.paletteList.TabIndex = 2;
			this.paletteList.TileSize = new System.Drawing.Size(384, 20);
			this.paletteList.UseCompatibleStateImageBehavior = false;
			this.paletteList.View = System.Windows.Forms.View.Details;
			this.paletteList.SelectedIndexChanged += new System.EventHandler(this.paletteList_SelectedIndexChanged);
			this.paletteList.DoubleClick += new System.EventHandler(this.handleSelectionConfirmed);
			// 
			// paletteHeader
			// 
			this.paletteHeader.Text = "Palette";
			this.paletteHeader.Width = 400;
			// 
			// PaletteSelect1DForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(478, 535);
			this.Controls.Add(this.paletteList);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.cancelButton);
			this.MinimizeBox = false;
			this.Name = "PaletteSelect1DForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Palette Select";
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Button cancelButton;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.ListView paletteList;
		private System.Windows.Forms.ColumnHeader paletteHeader;
	}
}