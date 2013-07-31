@echo Off
set config=%1
if "%config%" == "" (
   set config=debug
)
%WINDIR%\Microsoft.NET\Framework\v4.0.30319\msbuild %~dp0\Build\Build.proj /p:Configuration="%config%",VisualStudioVersion=11.0 /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false