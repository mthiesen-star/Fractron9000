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
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using MTUtil;


namespace Fractron9000
{
	public class TestRunner
	{
		private StringWriter report = new StringWriter();

		public string Report{
			get{ return report.ToString(); }
		}

		public void RunTests(object fixture)
		{
			bool startOK = true;
			int testCount = 0;
			int successCount = 0;

			report.WriteLine("Running Test Fixture: {0}", fixture.GetType().Name);
			foreach(var method in fixture.GetType().GetMethods())
			{
				object[] attrs = method.GetCustomAttributes(typeof(TestFixtureSetUpAttribute),true);
				var parms = method.GetParameters();
				if((method.Attributes & MethodAttributes.Static) == 0
				&&  method.ReturnType == typeof(void)
				&&  attrs.Length > 0 && parms.Length == 0)
				{
					startOK = startOK && runTest(fixture, method);
				}
			}
			if(startOK)
			{
				foreach(var method in fixture.GetType().GetMethods())
				{
					object[] attrs = method.GetCustomAttributes(typeof(TestAttribute),true);
					var parms = method.GetParameters();
					if((method.Attributes & MethodAttributes.Static) == 0
					&&  method.ReturnType == typeof(void)
					&&  attrs.Length > 0 && parms.Length == 0)
					{
						testCount++;
						if(runTest(fixture, method))
							successCount++;
					}
				}
				foreach(var method in fixture.GetType().GetMethods())
				{
					object[] attrs = method.GetCustomAttributes(typeof(TestFixtureTearDownAttribute),true);
					var parms = method.GetParameters();
					if((method.Attributes & MethodAttributes.Static) == 0
					&&  method.ReturnType == typeof(void)
					&&  attrs.Length > 0 && parms.Length == 0)
					{
						runTest(fixture, method);
					}
				}
			}

			report.WriteLine("Done. {0} out of {1} tests were successful.", successCount, testCount);
		}

		private bool runTest(object fixture, MethodInfo testMethod)
		{
			bool ok = true;
			report.Write("Running {0} ... ", testMethod.Name);

			object[] attrs = testMethod.GetCustomAttributes(typeof(IgnoreAttribute),false);
			if(attrs.Length > 0)
			{
				Console.WriteLine("Ignored.");
				return true;
			}
			try{
				testMethod.Invoke(fixture, new object[]{});
			}
			catch(InconclusiveException ex)
			{
				report.WriteLine("Inconclusive: {0}", ex.Message);
				ok = false;
			}
			catch(Exception ex)
			{
				report.WriteLine("Failed: {0}", ex.Message);
				if(ex.InnerException != null)
					report.WriteLine(ex.InnerException.Message);
				ok = false;
			}

			if(ok)
				report.WriteLine("OK.");

			return ok;
		}

	}
}