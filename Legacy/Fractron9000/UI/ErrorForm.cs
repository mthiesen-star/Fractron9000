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
using System.Windows.Forms;

namespace Fractron9000.UI
{
	public partial class ErrorForm : Form
	{
		private Exception ex = null;
		
		[Browsable(true)]
		[DefaultValue(null)]
		public Exception Exception{
			get{ return ex; }
			set{ ex = value; }
		}

		[Browsable(true)]
		[DefaultValue("An error has occured.")]
		public string Title{
			get{ return titleLabel.Text; }
			set{ titleLabel.Text = value; }
		}

		[Browsable(true)]
		[DefaultValue("")]
		public string Explanation{
			get{ return explanationBox.Text; }
			set{ explanationBox.Text = value; }
		}

		public static void Show(string title, string explanation, Exception ex)
		{
			var form = new ErrorForm();
			form.Title = title;
			form.Explanation = explanation;
			form.Exception = ex;
			form.ShowDialog();
		}

		public static void Show(string mergedTitleMessage, Exception ex)
		{
			var form = new ErrorForm();
			form.SetMergedTitleMessage(mergedTitleMessage);
			form.Exception = ex;
			form.ShowDialog();
		}

		public static void Show(Exception ex)
		{
			var form = new ErrorForm();
			form.Exception = ex;
			form.chooseTitleAndExplanation(ex);
			form.ShowDialog();
		}

		public ErrorForm()
		{
			InitializeComponent();

			Icon = SystemIcons.Error;
			collapse();
		}

		/// <summary>
		/// Sets the form's exception to ex and chooses a title and explanation based on the Exception.
		/// </summary>
		private void chooseTitleAndExplanation(Exception ex)
		{
			this.ex = ex;

			if(ex == null)
			{
				Title = "Unknown";
				Explanation = "Unknown";
			}
			else if(ex is NoDevicesFoundException)
			{
				if(ex.InnerException is CPUEngine.OpenGLVersionException)
				{
					CPUEngine.OpenGLVersionException glvEx = ex.InnerException as CPUEngine.OpenGLVersionException;
					string str = String.Format(Narratives.Error_OpenGLVersion, glvEx.ComponentName, glvEx.RequiredVersion, glvEx.FoundVersion);
					SetMergedTitleMessage(str);
				}
				else{
					SetMergedTitleMessage(Narratives.Error_NoDevicesFound);
				}
			}
			else if(ex is FractalEngineStateException)
			{
				FractalEngineStateException esx = ex as FractalEngineStateException;
				Exception ix = esx.InnerException;
				if(ix is DllNotFoundException)
					SetMergedTitleMessage(Narratives.Error_DllNotFound);
				else if(ix is Cuda.CudaVersionException)
					SetMergedTitleMessage(Narratives.Error_CudaVersionFail);
				else if(ix is Cuda.CudaException && (ix as Cuda.CudaException).Result == Cuda.CudaResult.OutOfMemory)
					SetMergedTitleMessage(Narratives.Error_OutOfMemory);
				else if(ix is OpenCL.OpenCLCallFailedException)
				{
					OpenCL.OpenCLCallFailedException cfx = ix as OpenCL.OpenCLCallFailedException;
					if(cfx.ErrorCode == OpenCL.ErrorCode.BuildProgramFailure)
					{
						string log = "";
						if(ix.Data.Contains("build_log"))
							log = ix.Data["build_log"].ToString();
						log = log.Replace("\n", "\r\n");
						string message = String.Format(Narratives.Error_BuildFailed, log);
						SetMergedTitleMessage(message);
					}
					else if(cfx.ErrorCode == OpenCL.ErrorCode.OutOfResources ||
						cfx.ErrorCode == OpenCL.ErrorCode.MemObjectAllocationFailure ||
						cfx.ErrorCode == OpenCL.ErrorCode.OutOfHostMemory)
					{
						SetMergedTitleMessage(Narratives.Error_OutOfMemory);
					}
					else
					{
						SetMergedTitleMessage(Narratives.Error_EngineStartupFail);
					}
				}
				else
				{
					SetMergedTitleMessage(Narratives.Error_EngineStartupFail);
				}
			}
			else if(ex is FractalEngineException)
			{
				FractalEngineException emx = (ex as FractalEngineException);
				Cuda.CudaException cx = emx.InnerException as Cuda.CudaException;
				OpenCL.OpenCLCallFailedException cfx = emx.InnerException as OpenCL.OpenCLCallFailedException;

				if(cx != null && cx.Result == Cuda.CudaResult.OutOfMemory)
				{
					SetMergedTitleMessage(Narratives.Error_OutOfMemory);
				}
				else if(cfx != null && (
						cfx.ErrorCode == OpenCL.ErrorCode.OutOfResources ||
						cfx.ErrorCode == OpenCL.ErrorCode.MemObjectAllocationFailure ||
						cfx.ErrorCode == OpenCL.ErrorCode.OutOfHostMemory
					))
				{
					SetMergedTitleMessage(Narratives.Error_OutOfMemory);
				}
				else
					SetMergedTitleMessage(Narratives.Error_EngineCycle);
			}
			else
			{
				Title = ex.GetType().Name;
				Explanation = ex.Message;
			}
		}

		/// <summary>
		/// Sets the Title and Message from a single string.
		/// </summary>
		/// <param name="mergedTitleMessage">A string containing the title and message seperated by "::"</param>
		public void SetMergedTitleMessage(string mergedTitleMessage)
		{
			string[] sp = mergedTitleMessage.Split(new string[]{"::"}, 2, StringSplitOptions.RemoveEmptyEntries);
			if(sp.Length == 0)
			{
				Title = "Unknown";
				Explanation = "Unknown";
			}
			else if(sp.Length == 1)
			{
				Title = "Error";
				Explanation = sp[0];
			}
			else
			{
				Title = sp[0];
				Explanation = sp[1];
			}
		}

		protected override void OnShown(EventArgs e)
		{
			if(ex != null)
			{			
				var str = new System.IO.StringWriter();
                writeException(this.Exception, str, "");
				detailsBox.Text = str.ToString();
			}

			base.OnShown(e);
		}

		private void writeException(Exception ex, System.IO.TextWriter tw, string indent)
		{
			if(ex == null)
				tw.WriteLine("<null>");
			else
			{
                tw.WriteLine("{0}{1}", indent, ex.GetType().Name);
				tw.WriteLine("{0}Message:{1}", indent, ex.Message);
				tw.WriteLine("{0}Source: {1}", indent, ex.Source);
				
				foreach(System.Collections.DictionaryEntry kv in ex.Data)
				{
					string kStr = kv.Key == null ? "null" : kv.Key.ToString();
					string vStr = kv.Value == null ? "null" : kv.Value.ToString();
						tw.WriteLine("{0}{1}: {2}",  indent, kStr, vStr);
				}
				tw.WriteLine("{0}Stack Trace:", indent);
				tw.WriteLine("{0}{1}", indent, ex.StackTrace);
				tw.Write("{0}Inner Exception: ", indent);
				writeException(ex.InnerException, tw, indent+"  ");
				tw.WriteLine();
			}
		}


		private void okButton_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void detailsButton_Click(object sender, EventArgs e)
		{
			if(detailsBox.Visible)
				collapse();
			else
				expand();
		}

		private void collapse()
		{
			detailsBox.Visible = false;
			detailsButton.Text = "Details...";
			this.ClientSize = new Size(ClientSize.Width, 269);
		}

		private void expand()
		{
			this.ClientSize = new Size(ClientSize.Width, 458);
			detailsBox.Visible = true;
			detailsButton.Text = "Hide Details";
		}
	}
}
