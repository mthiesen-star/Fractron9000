using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace Fractron9000.UI
{
	public partial class TextDisplayForm : Form
	{
		public TextDisplayForm()
		{
			InitializeComponent();
		}

		public string Content{
			get{ return contentBox.Text; }
			set{ contentBox.Text = value; }
		}

		public TextBox ContentTextBox{
			get{ return contentBox; }
		}

		public static void Show(string title, string content)
		{
			TextDisplayForm form = new TextDisplayForm();
			form.Text = title;
			form.Content = content;
			form.ShowDialog();
		}

		private void okButton_Click(object sender, EventArgs e)
		{
			Close();
		}
	}
}
