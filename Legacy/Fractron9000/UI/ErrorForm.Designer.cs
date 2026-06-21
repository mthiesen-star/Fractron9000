namespace Fractron9000.UI
{
	partial class ErrorForm
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
			this.titleLabel = new System.Windows.Forms.Label();
			this.okButton = new System.Windows.Forms.Button();
			this.detailsButton = new System.Windows.Forms.Button();
			this.detailsBox = new System.Windows.Forms.TextBox();
			this.explanationBox = new System.Windows.Forms.TextBox();
			this.SuspendLayout();
			// 
			// titleLabel
			// 
			this.titleLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.titleLabel.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.titleLabel.Location = new System.Drawing.Point(12, 9);
			this.titleLabel.Name = "titleLabel";
			this.titleLabel.Size = new System.Drawing.Size(640, 30);
			this.titleLabel.TabIndex = 0;
			this.titleLabel.Text = "Title";
			this.titleLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
			// 
			// okButton
			// 
			this.okButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.okButton.Location = new System.Drawing.Point(577, 240);
			this.okButton.Name = "okButton";
			this.okButton.Size = new System.Drawing.Size(75, 23);
			this.okButton.TabIndex = 2;
			this.okButton.Text = "OK";
			this.okButton.UseVisualStyleBackColor = true;
			this.okButton.Click += new System.EventHandler(this.okButton_Click);
			// 
			// detailsButton
			// 
			this.detailsButton.Location = new System.Drawing.Point(12, 240);
			this.detailsButton.Name = "detailsButton";
			this.detailsButton.Size = new System.Drawing.Size(75, 23);
			this.detailsButton.TabIndex = 3;
			this.detailsButton.Text = "Details";
			this.detailsButton.UseVisualStyleBackColor = true;
			this.detailsButton.Click += new System.EventHandler(this.detailsButton_Click);
			// 
			// detailsBox
			// 
			this.detailsBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.detailsBox.BackColor = System.Drawing.SystemColors.ControlLight;
			this.detailsBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.detailsBox.Location = new System.Drawing.Point(12, 269);
			this.detailsBox.Multiline = true;
			this.detailsBox.Name = "detailsBox";
			this.detailsBox.ReadOnly = true;
			this.detailsBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
			this.detailsBox.Size = new System.Drawing.Size(640, 163);
			this.detailsBox.TabIndex = 4;
			this.detailsBox.WordWrap = false;
			// 
			// messageBox
			// 
			this.explanationBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.explanationBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.explanationBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
			this.explanationBox.Location = new System.Drawing.Point(16, 42);
			this.explanationBox.Multiline = true;
			this.explanationBox.Name = "messageBox";
			this.explanationBox.ReadOnly = true;
			this.explanationBox.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
			this.explanationBox.Size = new System.Drawing.Size(636, 192);
			this.explanationBox.TabIndex = 5;
			// 
			// ErrorForm
			// 
			this.AcceptButton = this.okButton;
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.ClientSize = new System.Drawing.Size(664, 444);
			this.Controls.Add(this.explanationBox);
			this.Controls.Add(this.detailsBox);
			this.Controls.Add(this.detailsButton);
			this.Controls.Add(this.okButton);
			this.Controls.Add(this.titleLabel);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "ErrorForm";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.Text = "Fractron 9000 Error";
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion

		private System.Windows.Forms.Label titleLabel;
		private System.Windows.Forms.Button okButton;
		private System.Windows.Forms.Button detailsButton;
		private System.Windows.Forms.TextBox detailsBox;
		private System.Windows.Forms.TextBox explanationBox;
	}
}