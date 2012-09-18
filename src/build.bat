REM dont remove this line

echo off

set buildexe=msbuild.exe
set firstdir="%windir%\microsoft.net\Framework\v4.0.30319"
set msbuild=msbuild.exe

for /F "tokens=*" %%F in ('dir %firstdir%\%buildexe% /s /b') do (
	@echo %%F
	if exist "%%F" (
		@echo Using msbuild found here: %%F
		set msbuild=%%F
		goto runbuild
	)
)

@echo MSBUILD exe not found. Aborting run.
goto exit

:runbuild
@echo Building...

"%msbuild%" /nologo bosh-dotnet.sln /t:Clean /p:Configuration=Debug /p:Platform="Any CPU"
"%msbuild%" /nologo bosh-dotnet.sln /p:Configuration=Debug /p:Platform="Any CPU"


:exit

