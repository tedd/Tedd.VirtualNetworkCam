@echo off
c:
cd c:\Tedd.VirtualNetworkCam\

set FRAMEWORKDIR=%windir%\Microsoft.NET\Framework\v4.0.30319

REM %windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo System.Buffers.dll
REM %windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo System.IO.Pipelines.dll
REM %windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo System.Memory.dll
REM %windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo System.Numerics.Vectors.dll
REM %windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo System.Runtime.CompilerServices.Unsafe.dll
REM %windir%\Microsoft.NET\Framework\v4.0.30319\RegAsm.exe /unregister /nologo System.Threading.Tasks.Extensions.dll
%FRAMEWORKDIR%\RegAsm.exe /unregister /nologo Tedd.VirtualNetworkCam.dll

%FRAMEWORKDIR%\ngen.exe uninstall System.Buffers.dll
%FRAMEWORKDIR%\ngen.exe uninstall System.IO.Pipelines.dll
%FRAMEWORKDIR%\ngen.exe uninstall System.Memory.dll
%FRAMEWORKDIR%\ngen.exe uninstall System.Numerics.Vectors.dll
%FRAMEWORKDIR%\ngen.exe uninstall System.Runtime.CompilerServices.Unsafe.dll
%FRAMEWORKDIR%\ngen.exe uninstall System.Threading.Tasks.Extensions.dll
