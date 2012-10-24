echo off
echo Starting nats server and simple blobstore server
set PATH=c:\ruby\bin;%PATH%
start cmd /C "nats-server"
cd c:\ruby\simple_blobstore_server
start cmd /C "ruby bin\simple_blobstore_server"
cd c:\build\private-bosh-dotnet\src
@echo Running build.bat
cmd /c build.bat > e:\bochsresults\build.log
@echo Running test.bat
cmd /c test.bat > e:\bochsresults\test.log
@echo Copying results and logs
IF EXIST TestRun.trx copy /Y TestRun.trx e:\bochsresults\TestRun.trx
@echo Shutting down
IF EXIST c:\build\bochsboot.bat shutdown -s -f -t 00
