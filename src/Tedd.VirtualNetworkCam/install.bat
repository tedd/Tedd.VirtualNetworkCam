@echo off
c:
cd c:\Tedd.VirtualNetworkCam\

call uninstall.bat
del *.tlb

set FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v4.0.30319

%FRAMEWORKDIR%\ngen.exe install System.Buffers.dll
%FRAMEWORKDIR%\ngen.exe install System.IO.Pipelines.dll
%FRAMEWORKDIR%\ngen.exe install System.Memory.dll
%FRAMEWORKDIR%\ngen.exe install System.Numerics.Vectors.dll
%FRAMEWORKDIR%\ngen.exe install System.Runtime.CompilerServices.Unsafe.dll
%FRAMEWORKDIR%\ngen.exe install System.Threading.Tasks.Extensions.dll

%FRAMEWORKDIR%\RegAsm.exe Tedd.VirtualNetworkCam.dll /nologo /codebase /tlb: Tedd.VirtualNetworkCam.tlb
