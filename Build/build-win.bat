::
:: build.bat
::
:: The build script for building Stoffi on Windows.
::
:: Will build the Windows version of Stoffi and package it into
:: an upgrade package, an installer, and an installer bundled
:: with .NET.
::
:: It also updates project files and source files with proper
:: version numbers and the specified channel.
::
:: If the channel "test" is specified it will build two versions,
:: the second version is the special "2.0" version, to be used to
:: verify if Stoffi can be upgraded *from* this release.changes.
::
:::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::::
:: 
:: Copyright 2014 Simplare
:: 
:: This code is part of the Stoffi Music Player Project.
:: Visit our website at: stoffiplayer.com
::
:: This program is free software; you can redistribute it and/or
:: modify it under the terms of the GNU General Public License
:: as published by the Free Software Foundation; either version
:: 3 of the License, or (at your option) any later version.
:: 
:: See stoffiplayer.com/license for more information.
::
@setlocal enableextensions enabledelayedexpansion
@echo off
title Stoffi Build Script

if "%1" == "" goto USAGE
set test=0

if not "%1" == "stable" if not "%1" == "beta" if not "%1" == "alpha" if not "%1" == "test" (
	echo No such channel: %1%
	echo Possible values: stable, beta, alpha, test
	goto END
)
set channel=%1

set c=%channel%
if "%2" == "test" (
	set test=1
	set c=test
) else ( if not "%2" == "" (
	echo Invalid value: %2%
	echo Possible value: "test" or empty
	goto END
) )

call :GetUnixTime stamp
echo Version: %stamp%
echo Channel: %channel%
if %test% == 1 echo Running in test mode

if exist output ( rmdir output /s /q )
mkdir output

:: We need the current release to detect which files has
:: changed and need to be put inside the upgrade package.
::
:: Also, if we are producing a test build, we'll need
:: to produce a version of the current release which has
:: been patched to live inside the test channel.
echo Downloading current release...
call :DownloadCurrentRelease

echo Building current release...
cd temp\previous\Player
..\..\..\bin\msbuild "Player.csproj" /p:Configuration="Release" > nul
cd ..\..\..\

:: Set version, channel, etc.
echo Setting parameters...
call :SetParameters %stamp%

:: Thin installer
call :SetBundle 1
echo Building project...
call :Compile
echo Packaging installer...
call :PackageInstaller %stamp%

:: Bundled installer
call :SetBundle 2
echo Building project with bundle...
call :Compile
echo Packaging bundled installer...
call :PackageInstaller %stamp% Bundle

echo Calculating differences...
call :GetDiff ..\Player\bin\Release temp\previous\Player\bin\Release

:: Upgrade package
echo Packaging upgrade package...
call :PackageUpgrade %stamp%

if %test% == 1 (
	echo Creating 2.0 upgrade package...
	call :SetParameters 2000000000
	cd ..\Player\
	..\Build\bin\msbuild "Player.csproj" /p:Configuration="Release" > nul
	cd ..\Build\
	echo Stoffi.exe > temp\changes.txt
	call :PackageUpgrade 2000000000
	
	echo Creating patched version of current release...
	cd temp\previous\Player
	call :SetParametersSettingsTest
	:: Temporary force a manual change of channel (there is no // channel yet)
	rem notepad Properties\Settings.Designer.cs
	..\..\..\bin\msbuild "Player.csproj" /p:Configuration="Release" > nul
	cd ..\..\..\
	
	mkdir output\current
	mkdir output\new
	mkdir output\future
	copy /y temp\previous\Player\bin\Release\Stoffi.exe output\current > nul
	move output\2000000000.tar.bz2 output\future > nul
	move output\%stamp%.tar.bz2 output\new > nul
	move output\InstallStoffi.exe output\new > nul
	move output\InstallStoffiAndDotNet.exe output\new > nul
)

:: Clean up
echo Cleaning up...
if exist temp ( rmdir /s /q temp )

echo Finished
goto END

:USAGE
echo Usage: %0 channel [test]
echo.
echo This script builds and packages the Stoffi Music Player.
echo.
echo Parameters:
echo channel = The channel of the release (alpha/beta/stable)
echo test    = If test is specified, then the script will produce
echo           all packages needed to test upgrading to and from
echo           the new release inside the test channel.
echo.

:SetParameters
call :SetParametersPlayer
call :SetParametersAssembly
call :SetParametersSettings %~1
call :SetParametersInstaller
goto :eof

:SetParametersPlayer
set infile="..\Player\Player.csproj"
set outfile="..\Player\Player2.csproj"
type %infile% | find "" /V > %outfile%
move /y %outfile% %infile% > nul
set version=%stamp:~0,1%.%stamp:~1,2%.%stamp:~3,3%.%stamp:~6,4%
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
<"!InFile!" (
	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
		) else (
			if not "!line:ApplicationVersion=!x" == "!line!x" (
				echo     ^<ApplicationVersion^>%version%^</ApplicationVersion^>
			) else (
				if not "!line:ApplicationRevision=!x" == "!line!x" (
					echo     ^<ApplicationRevision^>%stamp:~6,4%^</ApplicationRevision^>
				) else ( echo.!line!)
			)
		)
	)
) >> "%outfile%"
endlocal
move /y %outfile% %infile% > nul
goto :eof

:SetParametersAssembly
set infile="..\Player\Properties\AssemblyInfo.cs"
set outfile="..\Player\Properties\AssemblyInfo2.cs"
type %infile% | find "" /V > %outfile%
move /y %outfile% %infile% > nul
set version=%stamp:~0,1%.%stamp:~1,2%.%stamp:~3,3%.%stamp:~6,4%
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
<"!InFile!" (
	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
		) else (
			if not "!line:AssemblyVersion=!x" == "!line!x" (
				if "!line://=!x" == "!line!x" (
					echo [assembly: AssemblyVersion^("%version%"^)]
				) else ( echo.!line!)
			) else (
				if not "!line:AssemblyFileVersion=!x" == "!line!x" (
					echo [assembly: AssemblyFileVersion^("%version%"^)]
				) else ( echo.!line!)
			)
		)
	)
) >> "%outfile%"
endlocal
move /y %outfile% %infile% > nul
goto :eof

:SetParametersMigrator
set infile="..\SettingsMigrator\SettingsMigrator.csproj"
set outfile="..\SettingsMigrator\SettingsMigrator2.csproj"
type %infile% | find "" /V > %outfile%
move /y %outfile% %infile% > nul
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
<!InFile! (
 	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
 		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
 		) else (
			if not "!line:AssemblyName=!x" == "!line!x" (
				echo     ^<AssemblyName^>Migrator.%stamp%^</AssemblyName^>
 			) else ( echo.!line!)
 		)
 	)
) >> %outfile%
endlocal
move /y "%outfile%" "%infile%" > nul
goto :eof

:SetParametersSettings
set infile="..\Player\Properties\Settings.Designer.cs"
set outfile="..\Player\Properties\Settings.Designer2.cs"
type %infile% | find "" /V > %outfile%
move /y %outfile% %infile% > nul
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
<"!InFile!" (
	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
		) else (
			if not "!line:// version=!x" == "!line!x" (
				echo 				return %~1; // version
			) else (
				if not "!line:// channel=!x" == "!line!x" (
					echo 				return "%c%"; // channel
				) else ( echo.!line!)
			)
		)
	)
) >> "%outfile%"
endlocal
move /y %outfile% %infile% > nul
goto :eof

:SetParametersSettingsTest
set infile="..\Player\Properties\Settings.Designer.cs"
set outfile="..\Player\Properties\Settings.Designer2.cs"
type %infile% | find "" /V > %outfile%
move /y "%outfile%" "%infile%" > nul
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
<"!InFile!" (
	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
		) else (
			if not "!line:// channel=!x" == "!line!x" (
				echo 				return "test"; // channel
			) else ( echo.!line!)
		)
	)
) >> "%outfile%"
endlocal
move /y %outfile% %infile% > nul
goto :eof

:SetParametersInstaller
set infile="..\Installer\Installer.vdproj"
set outfile="..\Installer\Installer2.vdproj"
type %infile% | find "" /V > %outfile%
move /y "%outfile%" "%infile%" > nul
set version=%stamp:~0,1%.%stamp:~1,2%.%stamp:~3,3%
for /f %%i in ('bin\uuidgen -c') do set productCode=%%i
for /f %%i in ('bin\uuidgen -c') do set packageCode=%%i
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
set i=0
<"!InFile!" (
	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
		) else (
			if not "!line:ProductVersion=!x" == "!line!x" (
				echo         "ProductVersion" = "8:%version%"
			) else (
				if not "!line:PackageCode=!x" == "!line!x" (
					echo         "PackageCode" = "8:{%PackageCode%}"
				) else (			
					if not "!line:ProductCode=!x" == "!line!x" (
						if !i! gtr 1200 (
							echo         "ProductCode" = "8:{%productCode%}"
						) else ( echo.!line!)
					) else ( echo.!line!)
				)
			)
		)
		set /a i=!i!+1
	)
) >> "%outfile%"
endlocal
move /y %outfile% %infile% > nul
goto :eof

:SetBundle
set infile="..\Installer\Installer.vdproj"
set outfile="..\Installer\Installer2.vdproj"
type %infile% | find "" /V > %outfile%
move /y "%outfile%" "%infile%" > nul
for /f %%a in ('type "%infile%"^|find /v /c ""') do set "till=%%a"
setlocal EnableDelayedExpansion
<"!InFile!" (
	for /l %%a in (1 1 0) do set /p "="
	for /l %%a in (1 1 %till%) do (
		set "line="
		set /p "line="
		if "!line!x" == "x" ( echo.
		) else (
			if not "!line:PrerequisitesLocation=!x" == "!line!x" (
				echo             "PrerequisitesLocation" = "2:%~1"
			) else ( echo.!line!)
		)
	)
) >> "%outfile%"
endlocal
move /y %outfile% %infile% > nul
goto :eof

:Compile
set devenv=D:\Apps\Visual Studio 2010\Common7\IDE\devenv.com
set build=Release
set solution=..\Stoffi.sln
set project=Installer\Installer.vdproj
set cmd="%devenv%" "%solution%" /build %build% /project "%project%" /projectconfig %build%
%cmd% > nul
goto :eof

:PackageInstaller
set name=InstallStoffi.exe
if "%~2" == "Bundle" ( set name=InstallStoffiAndDotNet.exe )
cd output
mkdir Installer
cd Installer
mkdir InstallStoffi
copy /y ..\..\..\Player\Stoffi.ico InstallStoffi\ > nul
xcopy /y /e ..\..\..\Installer\Release\* InstallStoffi\ > nul
..\..\bin\rar a -r -sfx -z"..\..\bin\xfs.conf" InstallStoffi.exe InstallStoffi > nul
..\..\bin\winrar s -ibck -z"..\..\bin\xfs.conf" -iicon"InstallStoffi\Stoffi.ico" InstallStoffi.exe
move /y InstallStoffi.exe ..\%name% > nul
cd ..
rmdir Installer /s /q
cd ..
goto:eof

:PackageUpgrade
cd output
mkdir Upgrade
cd Upgrade
for /f "delims=" %%i in (..\..\temp\changes.txt) do (
	echo f | xcopy /y ..\..\..\Player\bin\Release\%%i %%i > nul
)
if not "%~1" == "2000000000" (
	call :IncludeMigratorIfNeeded
)
..\..\bin\7z a -ttar %~1.tar * > nul
..\..\bin\7z a -tbzip2 %~1.tar.bz2 %~1.tar > nul
move /y %~1.tar.bz2 ..\ > nul
cd ..
rmdir Upgrade /s /q
cd ..
goto:eof

:GetUnixTime
setlocal enableextensions
for /f %%x in ('wmic path win32_utctime get /format:list ^| findstr "="') do (
    set %%x)
set /a z=(14-100%Month%%%100)/12, y=10000%Year%%%10000-z
set /a ut=y*365+y/4-y/100+y/400+(153*(100%Month%%%100+12*z-3)+2)/5+Day-719469
set /a ut=ut*86400+100%Hour%%%100*3600+100%Minute%%%100*60+100%Second%%%100
endlocal & set "%1=%ut%"
goto :eof

:DownloadCurrentRelease
set url=http://yet-another-music-application.googlecode.com/svn/tags/%channel%
setlocal enableextensions
for /f "tokens=3" %%a in ('bin\svn log -l 1 %url% ^| find "Tagged"') do (
	set prevStamp=%%a
)
if not exist temp ( mkdir temp )
for /f "delims=" %%a in ('bin\svn ls %url%/ ^| find "%prevStamp%"') do set prevTag=%%a
set url=%url%/%prevTag%
bin\svn co %url% temp\previous > nul
endlocal
goto :eof

:GetDiff
setlocal EnableDelayedExpansion
set pwd=%cd%
if exist %pwd%\temp\changes.txt ( del %pwd%\temp\changes.txt )
cd %1
for /L %%n in (1 1 500) do if "!__cd__:~%%n,1!" neq "" set /a "len=%%n+1"
for /R %%g in (*) do (
	set "absPath=%%g"
	setlocal EnableDelayedExpansion
	set "relPath=!absPath:~%len%!"
	for /f "tokens=1" %%a in ('%pwd%\bin\sha256deep -s %pwd%\%2\!relPath!') do set old=%%a
	for /f "tokens=1" %%a in ('%pwd%\bin\sha256deep -s %%g') do set new=%%a
	if not "!old!" == "!new!" (
		echo !relPath! >> %pwd%\temp\changes.txt
	)
	endlocal
)
cd %pwd%
endlocal
goto :eof

:IncludeMigratorIfNeeded
set oldF="..\..\temp\previous\Settings Migrator\Migrator.cs"
set newF="..\..\..\SettingsMigrator\Migrator.cs"
for /f "tokens=1" %%a in ('..\..\bin\sha256deep -s %oldF%') do set old=%%a
for /f "tokens=1" %%a in ('..\..\bin\sha256deep -s %newF%') do set new=%%a
if not "!old!" == "!new!" (
	echo Detected change in settings migrator
	echo Compiling settings migrator...
	cd "..\..\..\SettingsMigrator\"
	call :SetParametersMigrator
	echo Starting compilation...
	..\Build\bin\msbuild "SettingsMigrator.csproj" /p:Configuration="Release" > nul
	echo Adding settings migrator to upgrade package...
	cd ..\Build\output\Upgrade
	copy /y "..\..\..\SettingsMigrator\bin\Release\Migrator.%stamp%.dll" . > nul
)
goto :eof

:concat
set lst=%lst% %1;
goto :eof

:END