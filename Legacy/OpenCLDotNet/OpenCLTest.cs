#region License
/*
    OpenCLDotNet - Compatability layer between OpenCL and the .NET framework
    Copyright (C) 2010 Michael J. Thiesen
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
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;

namespace OpenCL
{
	[TestFixture]
	public class OpenCLTest
	{
		[StructLayout(LayoutKind.Sequential)]
		private struct Float4{
			public float X;
			public float Y;
			public float Z;
			public float W;
		}
		[StructLayout(LayoutKind.Sequential)]
		private struct Float2{
			public float X;
			public float Y;
		}

		private Random rand = new Random(51413);
		private Platform platform = null;
		private Device device = null;
		private Context context = null;
		private Program program = null;

		private static string sampleProgram = @"

__constant float scaleFactor = 1.0f;

__kernel void vec_add(__global const float* a, __global const float* b, __global float* c)
{
	int gid = get_global_id(0);	
	c[gid] = a[gid] + b[gid];
}
";

		public OpenCLTest()
		{
		}

		[TestFixtureSetUp]
		public void Init()
		{
			Platform[] platforms = Platform.GetPlatforms();
			if(platforms.Length >= 1)
			{
				platform = platforms[0];
			
				Device[] devices = platform.GetDevices(DeviceTypeFlags.Default);
				if(devices.Length >= 1)
				{
					device = devices[0];

					context = Context.Create(new Device[]{device}, new ContextParam(ContextProperties.Platform, platform));
					Assert.IsNotNull(context);

					string[] sources = new string[]{sampleProgram};

					program = Program.CreateFromSource(context, sources);
					program.Build(context.Devices, "");

					Program.UnloadCompiler();
				}
			}
		}

		[TestFixtureTearDown]
		public void TearDown()
		{
			context.Dispose();
			Assert.AreEqual(IntPtr.Zero, context.Handle);
			program.Dispose();
			Assert.AreEqual(IntPtr.Zero, program.Handle);
		}

		private void checkSetup()
		{
			if(platform.Handle == IntPtr.Zero)
				throw new InconclusiveException("No OpenCL Platforms Found.");
			if(device.Handle == IntPtr.Zero)
				throw new InconclusiveException("No OpenCL Devices Found.");
			if(program.Handle == IntPtr.Zero)
				throw new InconclusiveException("OpenCL Program not created.");
		}


		[Test]
		public void TestPlatformProperties()
		{
			checkSetup();
			Assert.IsTrue(platform.Handle != IntPtr.Zero, "handle is null");
			Assert.IsNotEmpty(platform.Profile);
			Assert.IsNotEmpty(platform.Version);
			Assert.IsNotEmpty(platform.Name);
			Assert.IsNotEmpty(platform.Vendor);
			Assert.IsNotNull(platform.Extensions);
		}

		[Test]
		public void TestDeviceProperties()
		{
			checkSetup();
			checkProperties(device);
		}

		[Test]
		public void TestContextProperties()
		{
			checkSetup();
			checkProperties(context);

			ContextParam[] cp = context.CreateParams;

			Assert.AreEqual(1, cp.Length);
			Assert.AreEqual(ContextProperties.Platform, cp[0].Name);
			Assert.AreEqual(platform.Handle, cp[0].Value);

			Device[] devices = context.Devices;

			Assert.AreEqual(1, devices.Length);
			Assert.AreEqual(device.Name, devices[0].Name);
			Assert.AreEqual(device.Handle, devices[0].Handle);
		}

		[Test]
		public void TestCommandQueue()
		{
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);

			checkProperties(queue);

			Assert.AreEqual(CommandQueueFlags.ProfilingEnable, queue.Flags);
			queue.SetProperty(CommandQueueFlags.OutOfOrderExecModeEnable, true);
			Assert.AreEqual(CommandQueueFlags.ProfilingEnable | CommandQueueFlags.OutOfOrderExecModeEnable, queue.Flags);

			queue.Dispose();
			Assert.AreEqual(IntPtr.Zero, queue.Handle);
		}

		[Test]
		public void TestBufferCreate()
		{		
			checkSetup();
			Buffer buffer = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)16);
			Assert.True(buffer.Handle != IntPtr.Zero, "Buffer handle was null.");

			buffer.Dispose();
			Assert.AreEqual(buffer.Handle, IntPtr.Zero);
		}

		[Test]
		public void TestBufferProperties()
		{		
			checkSetup();
			Buffer buffer = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)16);
			Assert.True(buffer.Handle != IntPtr.Zero, "Buffer handle was null.");

			Assert.AreEqual(MemObjectType.Buffer, buffer.MemObjectType);
			Assert.AreEqual(MemFlags.ReadWrite, buffer.Flags);
			Assert.AreEqual((IntPtr)16, buffer.Size);
			uint mc = buffer.MapCount;
			uint rc = buffer.ReferenceCount;

			Assume.That(context.ReferenceCount == 1);
			Context bufCtx = buffer.Context;

			Assert.AreEqual(context.Handle, bufCtx.Handle);
			Assert.AreEqual(2, context.ReferenceCount);
			bufCtx.Dispose();
			Assert.AreEqual(1, context.ReferenceCount);

			buffer.Dispose();
			Assert.AreEqual(buffer.Handle, IntPtr.Zero);
		}

		[Test]
		public void TestBufferMap()
		{		
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			Buffer buffer = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)16);

			IntPtr writePtr = buffer.Map(queue, MapFlags.Write, (IntPtr)0, (IntPtr)16);
			try{
				unsafe{
					float* arr = (float*)writePtr;
					arr[0] = 2.0f;
					arr[1] = 4.0f;
					arr[2] = 6.0f;
					arr[3] = 8.0f;
				}
			}
			finally{
				buffer.Unmap(queue, ref writePtr);
			}
			Assert.AreEqual(IntPtr.Zero, writePtr);

			float sum = 0.0f;
			IntPtr readPtr = buffer.Map(queue, MapFlags.Read, (IntPtr)0, (IntPtr)16);
			try{
				unsafe{
					float* arr = (float*)readPtr;
					sum = arr[0] + arr[1] + arr[2] + arr[3];
				}
			}
			finally{
				buffer.Unmap(queue, ref readPtr);
			}
			Assert.AreEqual(IntPtr.Zero, readPtr);
			Assert.AreEqual(20.0f, sum);
		}

		[Test]
		public void TestBufferReadWriteRaw()
		{		
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			Buffer buffer = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)16);

			Float4 vec = new Float4();
			vec.X = 1.0f; vec.Y = 2.0f; vec.Z = 3.0f; vec.W = 4.0f;

			IntPtr vecBuff = Marshal.AllocHGlobal(16);
			try{
				Marshal.StructureToPtr(vec, vecBuff, false);

				unsafe{
					Event writeEvent;
					buffer.EnqueueWriteRaw(queue, (IntPtr)0, (IntPtr)16, vecBuff, out writeEvent);
					writeEvent.Wait();

					float v0, v1, v2, v3;
					buffer.ReadRaw(queue, (IntPtr)0,  (IntPtr)4, (IntPtr)(&v0));
					buffer.ReadRaw(queue, (IntPtr)4,  (IntPtr)4, (IntPtr)(&v1));
					buffer.ReadRaw(queue, (IntPtr)8,  (IntPtr)4, (IntPtr)(&v2));
					buffer.ReadRaw(queue, (IntPtr)12, (IntPtr)4, (IntPtr)(&v3));

					Assert.AreEqual(1.0f, v0);
					Assert.AreEqual(2.0f, v1);
					Assert.AreEqual(3.0f, v2);
					Assert.AreEqual(4.0f, v3);

					float k = 9.0f;
					buffer.WriteRaw(queue, (IntPtr)4, (IntPtr)4, (IntPtr)(&k));
					buffer.ReadRaw(queue, (IntPtr)4,  (IntPtr)4, (IntPtr)(&v1));
					Assert.AreEqual(9.0f, v1);
				}
			}
			finally{
				Marshal.FreeHGlobal(vecBuff);
				buffer.Dispose();
				queue.Dispose();
			}
		}

		[Test]
		public void TestBufferReadWrite()
		{		
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			Buffer buffer = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)32);

			float[] vals = new float[]{1.0f, 2.0f, 3.0f, 4.0f, 5.0f, 6.0f, 7.0f, 8.0f};
			Float4[] vecs = new Float4[2];

			buffer.Write(queue, vals);

			buffer.Read(queue, vecs);

			Assert.AreEqual(1.0f, vecs[0].X);
			Assert.AreEqual(8.0f, vecs[1].W);

			float[] tooSmall = new float[6];
			bool caught = false;
			try{
				buffer.Read(queue, tooSmall);
			}
			catch(OpenCLBufferSizeException){
				caught = true;
			}
			Assert.True(caught);
			buffer.Dispose();
			queue.Dispose();
		}
		
		[Test]
		public void TestImageFormat()
		{		
			checkSetup();

			ImageFormat[] supported = ImageFormat.GetSupportedImageFormats(context, (MemFlags)0, MemObjectType.Image2d);
			Assert.That(supported.Length != 0);

			ImageFormat fmt1 = new ImageFormat(ChannelOrder.Rgba, ChannelType.UnsignedInt8);
			ImageFormat fmt2 = new ImageFormat(ChannelOrder.Bgra, ChannelType.Float);

			Assert.That(fmt1.IsSupported(context, MemFlags.None, MemObjectType.Image2d));
			Assert.That(!fmt2.IsSupported(context, MemFlags.None, MemObjectType.Image2d));
		}

		[Test]
		public void TestImage2DProperties()
		{		
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			
			ImageFormat fmt = new ImageFormat(ChannelOrder.Ra, ChannelType.Float);
			Image2D img = Image2D.Create(context, MemFlags.ReadOnly, fmt, 2, 2);

			Assert.AreEqual(2, img.Width);
			Assert.AreEqual(2, img.Height);
			Assert.AreEqual(fmt, img.ImageFormat);
			Assert.AreEqual((IntPtr)8, img.ElementSize);

			img.Dispose();
			queue.Dispose();
		}

		[Test]
		public void TestImage2DReadWrite()
		{		
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			
			ImageFormat fmt = new ImageFormat(ChannelOrder.R, ChannelType.Float);
			Image2D img = Image2D.Create(context, MemFlags.ReadOnly, fmt, 4, 2);

			Assert.AreEqual((IntPtr)4, img.ElementSize);

			float[,] pix = new float[,]{{1.0f,0.0f,1.0f,0.0f},{2.0f,0.0f,2.0f,0.0f}};
			float[,] ret = new float[2,4];
			img.Write(queue, pix);

			img.Read(queue,ret);

			for(int r = 0; r < pix.GetLength(0); r++)
				for(int c = 0; c < pix.GetLength(1); c++)
					Assert.AreEqual(pix[r,c], ret[r,c]);

			img.Dispose();
			queue.Dispose();
		}

		[Test]
		public void TestEvents()
		{
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			Buffer buffer = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)16);

			Float4 vec = new Float4();
			vec.X = 1.0f; vec.Y = 2.0f; vec.Z = 3.0f; vec.W = 4.0f;

			IntPtr vecBuff = Marshal.AllocHGlobal(16);
			try{
				Marshal.StructureToPtr(vec, vecBuff, false);

				unsafe{
					Event writeEvent;
					buffer.EnqueueWriteRaw(queue, (IntPtr)0, (IntPtr)16, vecBuff, out writeEvent);
					Assert.True(writeEvent.Handle != IntPtr.Zero, "writeEvent handle was null.");

					writeEvent.Wait();
					Assert.AreEqual(CommandType.WriteBuffer, writeEvent.CommandType);
					Assert.AreEqual(CommandExecutionStatus.Complete, writeEvent.ExecutionStatus);
					Trace.WriteLine("writeEvent.ReferenceCount: "+writeEvent.ReferenceCount.ToString(),"notice");			
				}
			}
			finally{
				Marshal.FreeHGlobal(vecBuff);
				buffer.Dispose();
				queue.Dispose();
			}
		}

		[Test]
		public void TestProgram()
		{
			checkSetup();
			Assert.True(program.Handle != IntPtr.Zero, "Program handle should not be null.");
			Assert.AreEqual(1, program.Devices.Length);
			Assert.AreEqual(device.Handle, program.Devices[0].Handle);
			
			Assert.AreEqual(BuildStatus.Success, program.GetBuildStatus(device));
			string log = program.GetBuildLog(device);
			Trace.WriteLine("Buld Log:\n"+log, "notice");
		}

		[Test]
		public void TestKernelProperties()
		{
			checkSetup();
			Kernel kernel = Kernel.Create(program, "vec_add");

			Assert.AreEqual("vec_add", kernel.FunctionName);
			Assert.AreEqual(3, kernel.NumArgs);

			kernel.Dispose();
		}

		[Test]
		public void TestKernelExecution()
		{
			int elemCount = 16;
			int localSize = 8;
			checkSetup();
			CommandQueue queue = CommandQueue.Create(context, device, CommandQueueFlags.ProfilingEnable);
			Kernel kernel = Kernel.Create(program, "vec_add");
			Buffer a = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)(elemCount * sizeof(float)));
			Buffer b = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)(elemCount * sizeof(float)));
			Buffer c = Buffer.Create(context, MemFlags.ReadWrite, (IntPtr)(elemCount * sizeof(float)));
			
			float[] aLoc = new float[elemCount];
			float[] bLoc = new float[elemCount];
			float[] cLoc = new float[elemCount];

			for(int i = 0; i < elemCount; i++)
			{
				aLoc[i] = (float)rand.NextDouble();
				bLoc[i] = (float)rand.NextDouble();
			}

			a.Write(queue, aLoc);
			b.Write(queue, bLoc);

			kernel.SetArgs(a, b, c);
			Event launchEvt;
			kernel.EnqueueLaunch(queue, elemCount, localSize, null, out launchEvt);
			launchEvt.Wait();
			launchEvt.Dispose();

			c.Read(queue, cLoc);

			for(int i = 0; i < elemCount; i++)
			{
				Assert.That(Math.Abs(aLoc[i] + bLoc[i] - cLoc[i]) < 0.05f);
			}

			c.Dispose();
			b.Dispose();
			a.Dispose();
			kernel.Dispose();
			queue.Dispose();
		}

		private static void checkProperties(object srcObj)
		{
			foreach(var prop in srcObj.GetType().GetProperties())
			{
				try{
					if(prop.CanRead && prop.GetIndexParameters().Length == 0 && !prop.GetGetMethod().IsStatic)
					{
						object foo = null;
						object val = prop.GetValue(srcObj, null);
						if(val is Array)
						{
							foreach(var elem in (val as Array))
								foo = elem;
						}
						else
						{
							foo = val;
						}
					}
				}
				catch
				{
					Trace.WriteLine("Failed reading "+prop.Name);
					throw;
				}
			}
		}
	}
}