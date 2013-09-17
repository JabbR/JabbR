@echo Off
set config=%1
if "%config%" == "" (
   set config=debug
)

.nuget\NuGet.exe restore JabbR.sln
msbuild %~dp0\Build\Build.proj /p:Configuration="%config%" /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false /p:VisualStudioVersion=11.0