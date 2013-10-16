SETLOCAL EnableExtensions

for /F "usebackq tokens=1,2 delims==" %%i in (`wmic os get LocalDateTime /VALUE 2^>NUL`) do if '.%%i.'=='.LocalDateTime.' set ldt=%%j
set ldt=%ldt:~0,4%-%ldt:~4,2%-%ldt:~6,2% %ldt:~8,2%:%ldt:~10,2%:%ldt:~12,6%

ECHO %ldt% : Begin installing the New Relic .net Agent >> "%RoleRoot%\nr.log" 2>&1

:: Update with your license key
SET LICENSE_KEY=REPLACE_WITH_LICENSE_KEY
:: Current version of the installer
SET NR_INSTALLER_NAME=NewRelicAgent_x64_2.14.53.0.msi
:: Path used for custom configuration and worker role environment varibles
SET NR_HOME=%ALLUSERSPROFILE%\New Relic\.NET Agent\

ECHO Installing the New Relic .net Agent. >> "%RoleRoot%\nr.log" 2>&1

IF "%IsWorkerRole%" EQU "true" (
    msiexec.exe /i %NR_INSTALLER_NAME% /norestart /quiet NR_LICENSE_KEY=%LICENSE_KEY% INSTALLLEVEL=50 /lv* %RoleRoot%\nr_install.log
) ELSE (
    msiexec.exe /i %NR_INSTALLER_NAME% /norestart /quiet NR_LICENSE_KEY=%LICENSE_KEY% /lv* %RoleRoot%\nr_install.log
)

:: CUSTOM newrelic.xml : Uncomment the line below if you want to copy a custom newrelic.xml file into your instance
REM copy /Y newrelic.xml %NR_HOME% >> %RoleRoot%\nr.log

:: CUSTOM INSTRUMENTATION : Uncomment the line below to copy custom instrumentation into the agent directory.
REM copy /y CustomInstrumentation.xml %NR_HOME%\extensions >> %RoleRoot%\nr.log

:: If we are in a Worker Role then there is no need to restart W3SVC
if "%IsWorkerRole%" EQU "true" goto :FINALIZE

:: If we are emulating locally then do not restart W3SVC
if "%EMULATED%" EQU "true" goto :FINALIZE

:: WEB ROLES : Restart the service to pick up the new environment variables
ECHO Restarting IIS and W3SVC to pick up the new environment variables >> "%RoleRoot%\nr.log" 2>&1
IISRESET
NET START W3SVC

:FINALIZE
IF %ERRORLEVEL% EQU 0 (
  REM  The New Relic .net Agent installed ok and does not need to be installed again.
  ECHO New Relic .net Agent was installed successfully. >> "%RoleRoot%\nr.log" 2>&1

) ELSE (
  REM   An error occurred. Log the error to a separate log and exit with the error code.
  ECHO  An error occurred installing the New Relic .net Agent 1. Errorlevel = %ERRORLEVEL%. >> "%RoleRoot%\nr_error.log" 2>&1

  EXIT %ERRORLEVEL%
)

:EXIT
EXIT /B 0
