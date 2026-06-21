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
using System.Drawing;
using System.IO;

using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using MTUtil;


namespace Fractron9000
{
	public static class GLUtil
	{
		public static void GLLoadAffineMatrix(Affine2D a)
		{
			GLLoadAffineMatrix(a, 1.0f);
		}

		public static void GLLoadAffineMatrix(Affine2D a, float z)
		{
			float[] vals = new float[]{ a.XAxis.X, a.XAxis.Y, 0.0f, 0.0f,
                                     a.YAxis.X, a.YAxis.Y, 0.0f, 0.0f,
                                     0.0f,  0.0f,  z,    0.0f,
                                     a.Translation.X, a.Translation.Y, 0.0f, 1.0f  };
			GL.LoadMatrix(vals);
		}

		public static void GLMultAffineMatrix(Affine2D a)
		{
			float[] vals = new float[]{ a.XAxis.X, a.XAxis.Y, 0.0f, 0.0f,
                                     a.YAxis.X, a.YAxis.Y, 0.0f, 0.0f,
                                     0.0f,  0.0f,  1.0f, 0.0f,
                                     a.Translation.X, a.Translation.Y, 0.0f, 1.0f  };
			GL.MultMatrix(vals);
		}

		public static int MakeShader(string name, string src, ShaderType type)
		{
			int shader = GL.CreateShader(type);
			GL.ShaderSource(shader,src);
			GL.CompileShader(shader);
			int status;
			GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
			if(status == 0)
			{
				string info;
				GL.GetShaderInfoLog(shader, out info);
				var ex = new FractronException("Shader compilation failed.");
				ex.Data["name"] = name;
				ex.Data["info"] = info;
				throw ex;
			}
			return shader;
		}

		public static int MakeProgram(string name, int vertShader, int fragShader)
		{
			int program = GL.CreateProgram();
			if(vertShader != 0)
				GL.AttachShader(program, vertShader);
			if(fragShader != 0)
				GL.AttachShader(program, fragShader);
			GL.LinkProgram(program);
			int status;
			GL.GetProgram(program, ProgramParameter.LinkStatus, out status);
			if(status == 0)
			{
				string info;
				GL.GetProgramInfoLog(program, out info);
				var ex = new FractronException("Program link failed.");
				ex.Data["name"] = name;
				ex.Data["info"] = info;
				throw ex;
			}
			return program;
		}
	}
}