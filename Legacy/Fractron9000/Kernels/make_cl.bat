echo off
echo "Making OpenCL"
call "D:\Microsoft Visual Studio 9.0\Common7\Tools\vsvars32.bat"
SET code_dir=d:\projects\Fractron9000\Fractron9000\Kernels
SET include_dir=d:\projects\Fractron9000\Fractron9000\Kernels\include

REM preprocess the kernel sources into two monolithic files
REM one is a low performance version, the other is a standard version.

cl /X /I"%include_dir%" /FI"interop_cl.h" /EP "%code_dir%\kernels.c" > kernels.cl
cl /X /I"%include_dir%" /FI"interop_cl.h" /D_LOW_PROFILE_ /EP "%code_dir%\kernels.c" > kernels_low.cl
echo "done"
