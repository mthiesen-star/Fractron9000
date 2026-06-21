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
using System.Collections.Generic;
using System.Windows.Forms;

namespace OpenCL
{
	public enum AddressingMode : int
	{
		None = ((int)0x1130),
		ClampToEdge = ((int)0x1131),
		Clamp = ((int)0x1132),
		Repeat = ((int)0x1133),
	}

	public enum All : int
	{
		DeviceTypeDefault = ((int)(1 << 0)),
		ExecKernel = ((int)(1 << 0)),
		FpDenorm = ((int)(1 << 0)),
		MapRead = ((int)(1 << 0)),
		MemReadWrite = ((int)(1 << 0)),
		QueueOutOfOrderExecModeEnable = ((int)(1 << 0)),
		DeviceTypeCpu = ((int)(1 << 1)),
		ExecNativeKernel = ((int)(1 << 1)),
		FpInfNan = ((int)(1 << 1)),
		MapWrite = ((int)(1 << 1)),
		MemWriteOnly = ((int)(1 << 1)),
		QueueProfilingEnable = ((int)(1 << 1)),
		DeviceTypeGpu = ((int)(1 << 2)),
		FpRoundToNearest = ((int)(1 << 2)),
		MemReadOnly = ((int)(1 << 2)),
		DeviceTypeAccelerator = ((int)(1 << 3)),
		FpRoundToZero = ((int)(1 << 3)),
		MemUseHostPtr = ((int)(1 << 3)),
		FpRoundToInf = ((int)(1 << 4)),
		MemAllocHostPtr = ((int)(1 << 4)),
		FpFma = ((int)(1 << 5)),
		MemCopyHostPtr = ((int)(1 << 5)),
		ScharMin = ((int)(-127 - 1)),
		IntMin = ((int)(-2147483647 - 1)),
		ShrtMin = ((int)(-32767 - 1)),
		BuildSuccess = ((int)0),
		False = ((int)0),
		Success = ((int)0),
		Complete = ((int)0x0),
		None = ((int)0x0),
		PlatformProfile = ((int)0x0900),
		PlatformVersion = ((int)0x0901),
		PlatformName = ((int)0x0902),
		PlatformVendor = ((int)0x0903),
		PlatformExtensions = ((int)0x0904),
		Local = ((int)0x1),
		ReadOnlyCache = ((int)0x1),
		Running = ((int)0x1),
		DeviceType = ((int)0x1000),
		DeviceVendorId = ((int)0x1001),
		DeviceMaxComputeUnits = ((int)0x1002),
		DeviceMaxWorkItemDimensions = ((int)0x1003),
		DeviceMaxWorkGroupSize = ((int)0x1004),
		DeviceMaxWorkItemSizes = ((int)0x1005),
		DevicePreferredVectorWidthChar = ((int)0x1006),
		DevicePreferredVectorWidthShort = ((int)0x1007),
		DevicePreferredVectorWidthInt = ((int)0x1008),
		DevicePreferredVectorWidthLong = ((int)0x1009),
		DevicePreferredVectorWidthFloat = ((int)0x100A),
		DevicePreferredVectorWidthDouble = ((int)0x100B),
		DeviceMaxClockFrequency = ((int)0x100C),
		DeviceAddressBits = ((int)0x100D),
		DeviceMaxReadImageArgs = ((int)0x100E),
		DeviceMaxWriteImageArgs = ((int)0x100F),
		DeviceMaxMemAllocSize = ((int)0x1010),
		DeviceImage2dMaxWidth = ((int)0x1011),
		DeviceImage2dMaxHeight = ((int)0x1012),
		DeviceImage3dMaxWidth = ((int)0x1013),
		DeviceImage3dMaxHeight = ((int)0x1014),
		DeviceImage3dMaxDepth = ((int)0x1015),
		DeviceImageSupport = ((int)0x1016),
		DeviceMaxParameterSize = ((int)0x1017),
		DeviceMaxSamplers = ((int)0x1018),
		DeviceMemBaseAddrAlign = ((int)0x1019),
		DeviceMinDataTypeAlignSize = ((int)0x101A),
		DeviceSingleFpConfig = ((int)0x101B),
		DeviceGlobalMemCacheType = ((int)0x101C),
		DeviceGlobalMemCachelineSize = ((int)0x101D),
		DeviceGlobalMemCacheSize = ((int)0x101E),
		DeviceGlobalMemSize = ((int)0x101F),
		DeviceMaxConstantBufferSize = ((int)0x1020),
		DeviceMaxConstantArgs = ((int)0x1021),
		DeviceLocalMemType = ((int)0x1022),
		DeviceLocalMemSize = ((int)0x1023),
		DeviceErrorCorrectionSupport = ((int)0x1024),
		DeviceProfilingTimerResolution = ((int)0x1025),
		DeviceEndianLittle = ((int)0x1026),
		DeviceAvailable = ((int)0x1027),
		DeviceCompilerAvailable = ((int)0x1028),
		DeviceExecutionCapabilities = ((int)0x1029),
		DeviceQueueProperties = ((int)0x102A),
		DeviceName = ((int)0x102B),
		DeviceVendor = ((int)0x102C),
		DriverVersion = ((int)0x102D),
		DeviceProfile = ((int)0x102E),
		DeviceVersion = ((int)0x102F),
		DeviceExtensions = ((int)0x1030),
		DevicePlatform = ((int)0x1031),
		ContextReferenceCount = ((int)0x1080),
		ContextDevices = ((int)0x1081),
		ContextProperties = ((int)0x1082),
		ContextPlatform = ((int)0x1084),
		QueueContext = ((int)0x1090),
		QueueDevice = ((int)0x1091),
		QueueReferenceCount = ((int)0x1092),
		QueueProperties = ((int)0x1093),
		R = ((int)0x10B0),
		A = ((int)0x10B1),
		Rg = ((int)0x10B2),
		Ra = ((int)0x10B3),
		Rgb = ((int)0x10B4),
		Rgba = ((int)0x10B5),
		Bgra = ((int)0x10B6),
		Argb = ((int)0x10B7),
		Intensity = ((int)0x10B8),
		Luminance = ((int)0x10B9),
		SnormInt8 = ((int)0x10D0),
		SnormInt16 = ((int)0x10D1),
		UnormInt8 = ((int)0x10D2),
		UnormInt16 = ((int)0x10D3),
		UnormShort565 = ((int)0x10D4),
		UnormShort555 = ((int)0x10D5),
		UnormInt101010 = ((int)0x10D6),
		SignedInt8 = ((int)0x10D7),
		SignedInt16 = ((int)0x10D8),
		SignedInt32 = ((int)0x10D9),
		UnsignedInt8 = ((int)0x10DA),
		UnsignedInt16 = ((int)0x10DB),
		UnsignedInt32 = ((int)0x10DC),
		HalfFloat = ((int)0x10DD),
		Float = ((int)0x10DE),
		MemObjectBuffer = ((int)0x10F0),
		MemObjectImage2d = ((int)0x10F1),
		MemObjectImage3d = ((int)0x10F2),
		MemType = ((int)0x1100),
		MemFlags = ((int)0x1101),
		MemSize = ((int)0x1102),
		MemHostPtr = ((int)0x1103),
		MemMapCount = ((int)0x1104),
		MemReferenceCount = ((int)0x1105),
		MemContext = ((int)0x1106),
		ImageFormat = ((int)0x1110),
		ImageElementSize = ((int)0x1111),
		ImageRowPitch = ((int)0x1112),
		ImageSlicePitch = ((int)0x1113),
		ImageWidth = ((int)0x1114),
		ImageHeight = ((int)0x1115),
		ImageDepth = ((int)0x1116),
		AddressNone = ((int)0x1130),
		AddressClampToEdge = ((int)0x1131),
		AddressClamp = ((int)0x1132),
		AddressRepeat = ((int)0x1133),
		FilterNearest = ((int)0x1140),
		FilterLinear = ((int)0x1141),
		SamplerReferenceCount = ((int)0x1150),
		SamplerContext = ((int)0x1151),
		SamplerNormalizedCoords = ((int)0x1152),
		SamplerAddressingMode = ((int)0x1153),
		SamplerFilterMode = ((int)0x1154),
		ProgramReferenceCount = ((int)0x1160),
		ProgramContext = ((int)0x1161),
		ProgramNumDevices = ((int)0x1162),
		ProgramDevices = ((int)0x1163),
		ProgramSource = ((int)0x1164),
		ProgramBinarySizes = ((int)0x1165),
		ProgramBinaries = ((int)0x1166),
		ProgramBuildStatus = ((int)0x1181),
		ProgramBuildOptions = ((int)0x1182),
		ProgramBuildLog = ((int)0x1183),
		KernelFunctionName = ((int)0x1190),
		KernelNumArgs = ((int)0x1191),
		KernelReferenceCount = ((int)0x1192),
		KernelContext = ((int)0x1193),
		KernelProgram = ((int)0x1194),
		KernelWorkGroupSize = ((int)0x11B0),
		KernelCompileWorkGroupSize = ((int)0x11B1),
		KernelLocalMemSize = ((int)0x11B2),
		EventCommandQueue = ((int)0x11D0),
		EventCommandType = ((int)0x11D1),
		EventReferenceCount = ((int)0x11D2),
		EventCommandExecutionStatus = ((int)0x11D3),
		CommandNdrangeKernel = ((int)0x11F0),
		CommandTask = ((int)0x11F1),
		CommandNativeKernel = ((int)0x11F2),
		CommandReadBuffer = ((int)0x11F3),
		CommandWriteBuffer = ((int)0x11F4),
		CommandCopyBuffer = ((int)0x11F5),
		CommandReadImage = ((int)0x11F6),
		CommandWriteImage = ((int)0x11F7),
		CommandCopyImage = ((int)0x11F8),
		CommandCopyImageToBuffer = ((int)0x11F9),
		CommandCopyBufferToImage = ((int)0x11FA),
		CommandMapBuffer = ((int)0x11FB),
		CommandMapImage = ((int)0x11FC),
		CommandUnmapMemObject = ((int)0x11FD),
		CommandMarker = ((int)0x11FE),
		CommandAcquireGlObjects = ((int)0x11FF),
		CommandReleaseGlObjects = ((int)0x1200),
		ProfilingCommandQueued = ((int)0x1280),
		ProfilingCommandSubmit = ((int)0x1281),
		ProfilingCommandStart = ((int)0x1282),
		ProfilingCommandEnd = ((int)0x1283),
		Global = ((int)0x2),
		ReadWriteCache = ((int)0x2),
		Submitted = ((int)0x2),
		Queued = ((int)0x3),
		UintMax = unchecked((int)0xffffffff),
		DeviceTypeAll = unchecked((int)0xFFFFFFFF),
		True = ((int)1),
		Version10 = ((int)1),
		BuildNone = ((int)-1),
		DeviceNotFound = ((int)-1),
		ImageFormatNotSupported = ((int)-10),
		DblMinExp = ((int)-1021),
		BuildProgramFailure = ((int)-11),
		MapFailure = ((int)-12),
		FltMinExp = ((int)-125),
		ScharMax = ((int)127),
		DblDig = ((int)15),
		DblRadix = ((int)2),
		FltRadix = ((int)2),
		BuildError = ((int)-2),
		DeviceNotAvailable = ((int)-2),
		IntMax = unchecked((int)2147483647),
		FltMantDig = ((int)24),
		UcharMax = ((int)255),
		BuildInProgress = ((int)-3),
		CompilerNotAvailable = ((int)-3),
		InvalidValue = ((int)-30),
		DblMin10Exp = ((int)-307),
		InvalidDeviceType = ((int)-31),
		InvalidPlatform = ((int)-32),
		ShrtMax = ((int)32767),
		InvalidDevice = ((int)-33),
		InvalidContext = ((int)-34),
		InvalidQueueProperties = ((int)-35),
		InvalidCommandQueue = ((int)-36),
		FltMin10Exp = ((int)-37),
		InvalidHostPtr = ((int)-37),
		InvalidMemObject = ((int)-38),
		InvalidImageFormatDescriptor = ((int)-39),
		MemObjectAllocationFailure = ((int)-4),
		InvalidImageSize = ((int)-40),
		InvalidSampler = ((int)-41),
		InvalidBinary = ((int)-42),
		InvalidBuildOptions = ((int)-43),
		InvalidProgram = ((int)-44),
		InvalidProgramExecutable = ((int)-45),
		InvalidKernelName = ((int)-46),
		InvalidKernelDefinition = ((int)-47),
		InvalidKernel = ((int)-48),
		InvalidArgIndex = ((int)-49),
		OutOfResources = ((int)-5),
		InvalidArgValue = ((int)-50),
		InvalidArgSize = ((int)-51),
		InvalidKernelArgs = ((int)-52),
		DblMantDig = ((int)53),
		InvalidWorkDimension = ((int)-53),
		InvalidWorkGroupSize = ((int)-54),
		InvalidWorkItemSize = ((int)-55),
		InvalidGlobalOffset = ((int)-56),
		InvalidEventWaitList = ((int)-57),
		InvalidEvent = ((int)-58),
		InvalidOperation = ((int)-59),
		FltDig = ((int)6),
		OutOfHostMemory = ((int)-6),
		InvalidGlObject = ((int)-60),
		InvalidBufferSize = ((int)-61),
		InvalidMipLevel = ((int)-62),
		UshrtMax = ((int)65535),
		ProfilingInfoNotAvailable = ((int)-7),
		CharBit = ((int)8),
		MemCopyOverlap = ((int)-8),
		ImageFormatMismatch = ((int)-9),
	}

	public enum Bool : int
	{
		False = ((int)0),
		True = ((int)1),
	}

	public enum BuildStatus : int
	{
		Success = ((int)0),
		None = ((int)-1),
		Error = ((int)-2),
		InProgress = ((int)-3),
	}

	public enum ChannelOrder : int
	{
		R = ((int)0x10B0),
		A = ((int)0x10B1),
		Rg = ((int)0x10B2),
		Ra = ((int)0x10B3),
		Rgb = ((int)0x10B4),
		Rgba = ((int)0x10B5),
		Bgra = ((int)0x10B6),
		Argb = ((int)0x10B7),
		Intensity = ((int)0x10B8),
		Luminance = ((int)0x10B9),
	}

	public enum ChannelType : int
	{
		SnormInt8 = ((int)0x10D0),
		SnormInt16 = ((int)0x10D1),
		UnormInt8 = ((int)0x10D2),
		UnormInt16 = ((int)0x10D3),
		UnormShort565 = ((int)0x10D4),
		UnormShort555 = ((int)0x10D5),
		UnormInt101010 = ((int)0x10D6),
		SignedInt8 = ((int)0x10D7),
		SignedInt16 = ((int)0x10D8),
		SignedInt32 = ((int)0x10D9),
		UnsignedInt8 = ((int)0x10DA),
		UnsignedInt16 = ((int)0x10DB),
		UnsignedInt32 = ((int)0x10DC),
		HalfFloat = ((int)0x10DD),
		Float = ((int)0x10DE),
	}

	public enum CommandExecutionStatus : int
	{
		Error = (int)-1,
		Complete = ((int)0x0),
		Running = ((int)0x1),
		Submitted = ((int)0x2),
		Queued = ((int)0x3),
	}

	public enum CommandQueueFlags : long
	{
		OutOfOrderExecModeEnable = ((int)(1 << 0)),
		ProfilingEnable = ((int)(1 << 1)),
	}

	public enum CommandQueueInfo : int
	{
		Context = ((int)0x1090),
		Device = ((int)0x1091),
		ReferenceCount = ((int)0x1092),
		Properties = ((int)0x1093),
	}

	public enum CommandType : int
	{
		NdrangeKernel = ((int)0x11F0),
		Task = ((int)0x11F1),
		NativeKernel = ((int)0x11F2),
		ReadBuffer = ((int)0x11F3),
		WriteBuffer = ((int)0x11F4),
		CopyBuffer = ((int)0x11F5),
		ReadImage = ((int)0x11F6),
		WriteImage = ((int)0x11F7),
		CopyImage = ((int)0x11F8),
		CopyImageToBuffer = ((int)0x11F9),
		CopyBufferToImage = ((int)0x11FA),
		MapBuffer = ((int)0x11FB),
		MapImage = ((int)0x11FC),
		UnmapMemObject = ((int)0x11FD),
		Marker = ((int)0x11FE),
		AcquireGlObjects = ((int)0x11FF),
		ReleaseGlObjects = ((int)0x1200),
	}

	public enum ContextInfo : int
	{
		ReferenceCount = ((int)0x1080),
		ContextDevices = ((int)0x1081),
		ContextProperties = ((int)0x1082),
	}

	public enum ContextProperties : int
	{
		Platform = ((int)0x1084),
		GlContext = ((int)0x2008),
		EglDisplay = ((int)0x2009),
		GlxDisplay = ((int)0x200A),
		WglHdc = ((int)0x200B),
		CglSharegroup = ((int)0x200C),
	}

	public enum DeviceExecCapabilitiesFlags : long
	{
		Kernel = ((int)(1 << 0)),
		NativeKernel = ((int)(1 << 1)),
	}

	public enum DeviceFpConfigFlags : long
	{
		Denorm = ((int)(1 << 0)),
		InfNan = ((int)(1 << 1)),
		RoundToNearest = ((int)(1 << 2)),
		RoundToZero = ((int)(1 << 3)),
		RoundToInf = ((int)(1 << 4)),
		Fma = ((int)(1 << 5)),
	}

	public enum DeviceInfo : int
	{
		Type = ((int)0x1000),
		VendorId = ((int)0x1001),
		MaxComputeUnits = ((int)0x1002),
		MaxWorkItemDimensions = ((int)0x1003),
		MaxWorkGroupSize = ((int)0x1004),
		MaxWorkItemSizes = ((int)0x1005),
		PreferredVectorWidthChar = ((int)0x1006),
		PreferredVectorWidthShort = ((int)0x1007),
		PreferredVectorWidthInt = ((int)0x1008),
		PreferredVectorWidthLong = ((int)0x1009),
		PreferredVectorWidthFloat = ((int)0x100A),
		PreferredVectorWidthDouble = ((int)0x100B),
		MaxClockFrequency = ((int)0x100C),
		AddressBits = ((int)0x100D),
		MaxReadImageArgs = ((int)0x100E),
		MaxWriteImageArgs = ((int)0x100F),
		MaxMemAllocSize = ((int)0x1010),
		Image2dMaxWidth = ((int)0x1011),
		Image2dMaxHeight = ((int)0x1012),
		Image3dMaxWidth = ((int)0x1013),
		Image3dMaxHeight = ((int)0x1014),
		Image3dMaxDepth = ((int)0x1015),
		ImageSupport = ((int)0x1016),
		MaxParameterSize = ((int)0x1017),
		MaxSamplers = ((int)0x1018),
		MemBaseAddrAlign = ((int)0x1019),
		MinDataTypeAlignSize = ((int)0x101A),
		SingleFpConfig = ((int)0x101B),
		GlobalMemCacheType = ((int)0x101C),
		GlobalMemCachelineSize = ((int)0x101D),
		GlobalMemCacheSize = ((int)0x101E),
		GlobalMemSize = ((int)0x101F),
		MaxConstantBufferSize = ((int)0x1020),
		MaxConstantArgs = ((int)0x1021),
		LocalMemType = ((int)0x1022),
		LocalMemSize = ((int)0x1023),
		ErrorCorrectionSupport = ((int)0x1024),
		ProfilingTimerResolution = ((int)0x1025),
		EndianLittle = ((int)0x1026),
		Available = ((int)0x1027),
		CompilerAvailable = ((int)0x1028),
		ExecutionCapabilities = ((int)0x1029),
		QueueProperties = ((int)0x102A),
		Name = ((int)0x102B),
		Vendor = ((int)0x102C),
		DriverVersion = ((int)0x102D),
		Profile = ((int)0x102E),
		Version = ((int)0x102F),
		Extensions = ((int)0x1030),
		Platform = ((int)0x1031),
	}

	public enum DeviceLocalMemType : int
	{
		Local = ((int)0x1),
		Global = ((int)0x2),
	}

	public enum DeviceMemCacheType : int
	{
		None = ((int)0x0),
		ReadOnlyCache = ((int)0x1),
		ReadWriteCache = ((int)0x2),
	}

	public enum DeviceTypeFlags : long
	{
		Default = ((int)(1 << 0)),
		Cpu = ((int)(1 << 1)),
		Gpu = ((int)(1 << 2)),
		Accelerator = ((int)(1 << 3)),
		All = unchecked((int)0xFFFFFFFF),
	}

	public enum ErrorCode : int
	{
		Success = ((int)0),
		DeviceNotFound = ((int)-1),
		ImageFormatNotSupported = ((int)-10),
		BuildProgramFailure = ((int)-11),
		MapFailure = ((int)-12),
		DeviceNotAvailable = ((int)-2),
		CompilerNotAvailable = ((int)-3),
		InvalidValue = ((int)-30),
		InvalidDeviceType = ((int)-31),
		InvalidPlatform = ((int)-32),
		InvalidDevice = ((int)-33),
		InvalidContext = ((int)-34),
		InvalidQueueProperties = ((int)-35),
		InvalidCommandQueue = ((int)-36),
		InvalidHostPtr = ((int)-37),
		InvalidMemObject = ((int)-38),
		InvalidImageFormatDescriptor = ((int)-39),
		MemObjectAllocationFailure = ((int)-4),
		InvalidImageSize = ((int)-40),
		InvalidSampler = ((int)-41),
		InvalidBinary = ((int)-42),
		InvalidBuildOptions = ((int)-43),
		InvalidProgram = ((int)-44),
		InvalidProgramExecutable = ((int)-45),
		InvalidKernelName = ((int)-46),
		InvalidKernelDefinition = ((int)-47),
		InvalidKernel = ((int)-48),
		InvalidArgIndex = ((int)-49),
		OutOfResources = ((int)-5),
		InvalidArgValue = ((int)-50),
		InvalidArgSize = ((int)-51),
		InvalidKernelArgs = ((int)-52),
		InvalidWorkDimension = ((int)-53),
		InvalidWorkGroupSize = ((int)-54),
		InvalidWorkItemSize = ((int)-55),
		InvalidGlobalOffset = ((int)-56),
		InvalidEventWaitList = ((int)-57),
		InvalidEvent = ((int)-58),
		InvalidOperation = ((int)-59),
		OutOfHostMemory = ((int)-6),
		InvalidGlObject = ((int)-60),
		InvalidBufferSize = ((int)-61),
		InvalidMipLevel = ((int)-62),
		ProfilingInfoNotAvailable = ((int)-7),
		MemCopyOverlap = ((int)-8),
		ImageFormatMismatch = ((int)-9),
	}

	public enum EventInfo : int
	{
		CommandQueue = ((int)0x11D0),
		CommandType = ((int)0x11D1),
		ReferenceCount = ((int)0x11D2),
		CommandExecutionStatus = ((int)0x11D3),
	}

	public enum FilterMode : int
	{
		Nearest = ((int)0x1140),
		Linear = ((int)0x1141),
	}

	public enum ImageInfo : int
	{
		Format = ((int)0x1110),
		ElementSize = ((int)0x1111),
		RowPitch = ((int)0x1112),
		SlicePitch = ((int)0x1113),
		Width = ((int)0x1114),
		Height = ((int)0x1115),
		Depth = ((int)0x1116),
	}

	public enum KernelInfo : int
	{
		FunctionName = ((int)0x1190),
		NumArgs = ((int)0x1191),
		ReferenceCount = ((int)0x1192),
		Context = ((int)0x1193),
		Program = ((int)0x1194),
	}

	public enum KernelWorkGroupInfo : int
	{
		WorkGroupSize = ((int)0x11B0),
		CompileWorkGroupSize = ((int)0x11B1),
		LocalMemSize = ((int)0x11B2),
	}

	public enum MapFlags : long
	{
		Read = ((int)(1 << 0)),
		Write = ((int)(1 << 1)),
	}

	public enum MemFlags : long
	{
		None = 0,
		ReadWrite = ((int)(1 << 0)),
		WriteOnly = ((int)(1 << 1)),
		ReadOnly = ((int)(1 << 2)),
		UseHostPtr = ((int)(1 << 3)),
		AllocHostPtr = ((int)(1 << 4)),
		CopyHostPtr = ((int)(1 << 5)),
	}

	public enum MemInfo : int
	{
		Type = ((int)0x1100),
		Flags = ((int)0x1101),
		Size = ((int)0x1102),
		HostPtr = ((int)0x1103),
		MapCount = ((int)0x1104),
		ReferenceCount = ((int)0x1105),
		Context = ((int)0x1106),
	}

	public enum MemObjectType : int
	{
		Buffer = ((int)0x10F0),
		Image2d = ((int)0x10F1),
		Image3d = ((int)0x10F2),
	}

	public enum PlatformInfo : int
	{
		Profile = ((int)0x0900),
		Version = ((int)0x0901),
		Name = ((int)0x0902),
		Vendor = ((int)0x0903),
		Extensions = ((int)0x0904),
	}

	public enum ProfilingInfo : int
	{
		CommandQueued = ((int)0x1280),
		CommandSubmit = ((int)0x1281),
		CommandStart = ((int)0x1282),
		CommandEnd = ((int)0x1283),
	}

	public enum ProgramBuildInfo : int
	{
		Status = ((int)0x1181),
		Options = ((int)0x1182),
		Log = ((int)0x1183),
	}

	public enum ProgramInfo : int
	{
		ReferenceCount = ((int)0x1160),
		Context = ((int)0x1161),
		NumDevices = ((int)0x1162),
		Devices = ((int)0x1163),
		Source = ((int)0x1164),
		BinarySizes = ((int)0x1165),
		Binaries = ((int)0x1166),
	}

	public enum SamplerInfo : int
	{
		ReferenceCount = ((int)0x1150),
		Context = ((int)0x1151),
		NormalizedCoords = ((int)0x1152),
		AddressingMode = ((int)0x1153),
		FilterMode = ((int)0x1154),
	}

	public enum Unknown : int
	{
		ScharMin = ((int)(-127 - 1)),
		IntMin = ((int)(-2147483647 - 1)),
		ShrtMin = ((int)(-32767 - 1)),
		UintMax = unchecked((int)0xffffffff),
		DblMinExp = ((int)-1021),
		FltMinExp = ((int)-125),
		ScharMax = ((int)127),
		DblDig = ((int)15),
		DblRadix = ((int)2),
		FltRadix = ((int)2),
		IntMax = unchecked((int)2147483647),
		FltMantDig = ((int)24),
		UcharMax = ((int)255),
		DblMin10Exp = ((int)-307),
		ShrtMax = ((int)32767),
		FltMin10Exp = ((int)-37),
		DblMantDig = ((int)53),
		FltDig = ((int)6),
		UshrtMax = ((int)65535),
		CharBit = ((int)8),
	}

	public enum Version : int
	{
		Version10 = ((int)1),
	}

	/* OpenGL Interop */
	public enum GLObjectType : uint
	{
		Buffer =           (uint)0x2000,
		Texture2D =        (uint)0x2001,
		Texture3D =        (uint)0x2002,
		RenderBuffer =     (uint)0x2003,
	}

	public enum GLTextureInfo : uint
	{
		TextureTarget =          (uint)0x2004,
		MipmapLevel =            (uint)0x2005,
	}

}