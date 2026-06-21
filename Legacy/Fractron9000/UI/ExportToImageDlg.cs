using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

using MTUtil;

namespace Fractron9000.UI
{
	public partial class ExportToImageDlg : Form
	{
		public enum FormatOption{ Png, Jpeg };
		public enum TransparencyOption{ None, Transparent };

		private string fileName;
		public string FileName{
			get{ return fileName; }
			set{ fileName = value; }
		}

		private FormatOption format;
		public FormatOption Format{
			get{ return format; }
			set{ format = value; }
		}

		private TransparencyOption transparency;
		public TransparencyOption Transparency{
			get{ return transparency; }
			set{ transparency = value; }
		}

		private FractronConfig config;
		public FractronConfig Config{
			get{ return config; }
			set{ config = value; }
		}

		public ExportToImageDlg()
		{
			fileName = "";
			Format = FormatOption.Png;
			Transparency = TransparencyOption.None;
			config = null;
			InitializeComponent();
		}

		protected override void OnShown(EventArgs e)
		{
			base.OnShown(e);
			fileNameBox.Text = fileName;
			pngRadioButton.Checked = (format == FormatOption.Png);
			colorRadioButton.Checked = (transparency == TransparencyOption.None);
		}

		private void saveButton_Click(object sender, EventArgs e)
		{
			bool ok = true;

			fileName = fileNameBox.Text;
			if(jpegRadioButton.Checked)
				format = FormatOption.Jpeg;
			else
				format = FormatOption.Png;

			if(transparentRadioButton.Checked)
				transparency = TransparencyOption.Transparent;
			else
				transparency = TransparencyOption.None;

			//if the file exists, only proceed if the user confirms an overwrite
			if(File.Exists(fileName))
			{
				string msg = string.Format("Warning: The file {0} already exists. Overwrite?", Path.GetFileName(fileName));				
				ok = (DialogResult.Yes == MessageBox.Show(msg, "File Exists", MessageBoxButtons.YesNo, MessageBoxIcon.Warning));
			}

			if(ok){
				DialogResult = DialogResult.OK;
				Close();
			}
		}

		private void cancelButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void fileNameBox_Leave(object sender, EventArgs e)
		{
			fileName = fileNameBox.Text;
		}

		private void browseButton_Click(object sender, EventArgs e)
		{
			string dir = Path.GetDirectoryName(fileName);
			if(dir == null || dir == "")
			{
				if(config != null)
					dir = config.ImageDir;
				else
					dir = "";
			}

			SaveFileDialog dlg = new SaveFileDialog();
			dlg.Filter = "Portable Network Graphic (*.png)|*.png|JPEG Image (*.jpg)|*.jpg|All Files (*.*)|*.*";
			dlg.RestoreDirectory = true;
			dlg.FileName = Path.GetFileName(fileName);
			
			if(Format == FormatOption.Jpeg)
			{
				dlg.DefaultExt = ".jpg";
				dlg.FilterIndex = 2;
			}
			else
			{
				dlg.DefaultExt = ".png";
				dlg.FilterIndex = 1;
			}

			dlg.InitialDirectory = dir;
			dlg.OverwritePrompt = false;

			if(dlg.ShowDialog() == DialogResult.OK)
			{
				fileName = dlg.FileName;
				fileNameBox.Text = fileName;

				string ext = Path.GetExtension(fileName).ToLower();

				if(ext == ".png")
				{
					Format = FormatOption.Png;
					pngRadioButton.Checked = true;
				}
				if(ext == ".jpg")
				{
					Format = FormatOption.Jpeg;
					jpegRadioButton.Checked = true;
				}
			}
		}

		private void pngRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if(pngRadioButton.Checked)
			{
				format = FormatOption.Png;

				string ext = Path.GetExtension(fileName).ToLower();
				if(ext != ".png"){
					string dir = Path.GetDirectoryName(fileName);
					string fn = Path.GetFileNameWithoutExtension(fileName);
					fileName = Path.Combine(dir, fn + ".png");
					fileNameBox.Text = fileName;
				}

				transparencyGroupBox.Enabled = true;
			}
		}

		private void jpegRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if(jpegRadioButton.Checked)
			{
				format = FormatOption.Jpeg;
				string ext = Path.GetExtension(fileName).ToLower();
				if(ext != ".jpg"){
					string dir = Path.GetDirectoryName(fileName);
					string fn = Path.GetFileNameWithoutExtension(fileName);
					fileName = Path.Combine(dir, fn + ".jpg");
					fileNameBox.Text = fileName;
				}

				transparencyGroupBox.Enabled = false;
			}
		}

		private void colorRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if(colorRadioButton.Checked)
				Transparency = TransparencyOption.None;
		}

		private void transparentRadioButton_CheckedChanged(object sender, EventArgs e)
		{
			if(transparentRadioButton.Checked)
				Transparency = TransparencyOption.Transparent;
		}

		private static Bitmap getFlatBitmapFromPixels(Color[,] pixels)
		{
			Bitmap bmp = new Bitmap(pixels.GetLength(1), pixels.GetLength(0), PixelFormat.Format24bppRgb);
			
			Color bg = Util.ColorFromVec4(FractalManager.Fractal.BackgroundColor); 

			Color pix;
			int r,g,b,a;
			for(int y = 0; y < bmp.Height; y++)
			{
				for(int x = 0; x < bmp.Width; x++)
				{
					pix = pixels[y,x];
					a = (int)pix.A;
					r = (int)bg.R*(255-a)/255 + (int)pix.R*a/255;
					g = (int)bg.G*(255-a)/255 + (int)pix.G*a/255;
					b = (int)bg.B*(255-a)/255 + (int)pix.B*a/255;
					bmp.SetPixel(x, y, Color.FromArgb(r,g,b));
				}
			}
			return bmp;
		}

		private static Bitmap getBitmapFromPixels(Color[,] pixels)
		{
			Bitmap bmp = new Bitmap(pixels.GetLength(1), pixels.GetLength(0), PixelFormat.Format32bppArgb);
			for(int y = 0; y < bmp.Height; y++)
				for(int x = 0; x < bmp.Width; x++)
					bmp.SetPixel(x, y, pixels[y,x]);
			return bmp;
		}

		public void SaveOutput(Color[,] pixels)
		{
			//Force no transparency if the user chose jpeg encoding
			if(format == FormatOption.Jpeg){
				Transparency = TransparencyOption.None;
			}

			Bitmap bmp = null;

			if(Transparency == TransparencyOption.Transparent)
				bmp = getBitmapFromPixels(pixels);
			else
				bmp = getFlatBitmapFromPixels(pixels);

			ImageCodecInfo codec = null;
			EncoderParameters codecParams = null;

			if(Format == FormatOption.Png)
			{
				codec = GetCodec(ImageFormat.Png);
				if(codec == null)
					throw new FractronException("Could not find PNG encoder.");
				codecParams = new EncoderParameters(1);
				if(Transparency == TransparencyOption.Transparent)
					codecParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 32L);
				else
					codecParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
			}
			else if(Format == FormatOption.Jpeg)
			{
				codec = GetCodec(ImageFormat.Jpeg);
				if(codec == null)
					throw new FractronException("Could not find JPEG encoder.");
				codecParams = new EncoderParameters(2);
				codecParams.Param[0] = new EncoderParameter(Encoder.ColorDepth, 24L);
				codecParams.Param[1] = new EncoderParameter(Encoder.Quality, 95L);
			}

			bmp.Save(fileName, codec, codecParams);
		}

		private ImageCodecInfo GetCodec(ImageFormat format)
		{
			foreach(var codec in ImageCodecInfo.GetImageEncoders())
				if(codec.FormatID == format.Guid)
					return codec;

			return null;
		}
	}
}
