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
using System.IO;
using System.Windows.Forms;
using OpenCL;

namespace Fractron9000
{
	internal sealed class Program
	{
		private static UI.MainForm mainForm = null;

		[STAThread]
		private static void Main(string[] args)
		{
#if !DEBUG
			try
			{
#endif
				Application.EnableVisualStyles();
				Application.SetCompatibleTextRenderingDefault(false);

				mainForm = new UI.MainForm();
				Application.Idle += mainForm.HandleIdle;
				Application.Run(mainForm);
				Application.Idle -= mainForm.HandleIdle;
#if !DEBUG
			}
			catch(Exception ex)
			{
				HandleFatalError(ex);			
				throw ex;
			}
#endif
			
		}

		public static void HandleFatalError(Exception ex)
		{
			if(mainForm != null)
				Application.Idle -= mainForm.HandleIdle;

			UI.ErrorForm.Show(UI.Narratives.Error_Unknown, ex);
			try
			{
				if(mainForm != null)
					mainForm.Close();
			}catch(Exception){}
		}
	}
}
