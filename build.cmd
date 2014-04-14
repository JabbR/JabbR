@echo Off
set config=%1
if "%config%" == "" (
   set config=debug
)

.nuget\NuGet.exe restore JabbR.sln -configFile "%~dp0.nuget\NuGet.config" -nocache
msbuild "%~dp0Build\Build.proj" /p:Configuration="%config%" /v:M /fl /flp:LogFile=msbuild.log;Verbosity=Normal /nr:false