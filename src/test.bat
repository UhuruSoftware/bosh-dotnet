echo off

set testexe=mstest.exe
set firstdir="%programfiles%"
set seconddir="%programfiles% (x86)"
set mstest=mstest.exe

for /F "tokens=*" %%F in ('dir %firstdir%\%testexe% /s /b') do (
	@echo %%F
	if exist "%%F" (
		@echo Using mstest found here: %%F
		set mstest=%%F
		goto runtest
	)
)

for /F "tokens=*" %%F in ('dir %seconddir%\%testexe% /s /b') do (
	@echo %%F
	if exist "%%F" (
		@echo Using mstest found here: %%F
		set mstest=%%F
		goto runtest
	)
)

echo MSTEST exe not found. Aborting run.
goto exit

:runtest
@echo Running tests...

call "%mstest%" /testcontainer:..\bin\Uhuru.BOSH.Test.dll /category:"Unit|Integration" /resultsfile:testrun.trx


:exit
