REM dont remove this line
"%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" /nologo bosh-dotnet.sln /t:Clean
"%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe" /nologo bosh-dotnet.sln /p:Configuration=Debug /p:Platform="Any CPU"
