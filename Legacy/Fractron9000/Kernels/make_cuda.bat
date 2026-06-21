echo off
echo "Making CUDA"
call "D:\Microsoft Visual Studio 9.0\Common7\Tools\vsvars32.bat"
SET vsbin="D:\Microsoft Visual Studio 9.0\VC\bin"
SET cu_input_dir=d:\projects\Fractron9000\Fractron9000\Kernels
SET cu_output_dir=d:\projects\Fractron9000\Fractron9000\Kernels
SET cu_include_dir=d:\projects\Fractron9000\Fractron9000\Kernels\include

REM preprocess the kernel sources into a single monolithic file

cl /X /I"%cu_include_dir%" /FI"interop_cuda.h" /EP "%cu_input_dir%\kernels.c" > kernels.cu

%CUDA_BIN_PATH%\nvcc -m 32 -ccbin %vsbin% -ptx --ptxas-options=-v -arch compute_10 -code compute_10 -DWIN32 -D_MBCS -o %cu_output_dir%\kernels.ptx %cu_input_dir%\kernels.cu -Xcompiler /EHsc,/W3,/nologo,/Od,/Zi,/RTC1,/MDd
REM %CUDA_BIN_PATH%\nvcc -m 32 -ccbin %vsbin% -cubin --ptxas-options=-v -o %cu_output_dir%\kernels.cubin %cu_output_dir%\kernels.ptx

echo done