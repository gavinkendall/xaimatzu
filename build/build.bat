@echo off
set build_dir=%cd%
del /s /q xaimatzu.exe
del /s /q XaimatzuSetup.msi
del /s /q ..\bin\Release\*.*
devenv ..\xaimatzu.sln /Project ..\xaimatzu.csproj /Build Release
signtool sign /f ..\xaimatzu.pfx /p Sonic2020! /fd SHA256 ..\bin\Release\xaimatzu.exe

if exist "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\VSI\DisableOutOfProcBuild\DisableOutOfProcBuild.exe" (
    cd "C:\Program Files (x86)\Microsoft Visual Studio\2019\Enterprise\Common7\IDE\CommonExtensions\Microsoft\VSI\DisableOutOfProcBuild\"
    DisableOutOfProcBuild.exe
    cd %build_dir%
)

devenv ..\xaimatzu.sln /Project ..\XaimatzuSetup\XaimatzuSetup.vdproj /Build Release
move ..\bin\Release\xaimatzu.exe .
move ..\XaimatzuSetup\Release\XaimatzuSetup.msi .
del ..\XaimatzuSetup\Release\setup.exe
signtool sign /f ..\xaimatzu.pfx /p Sonic2020! /fd SHA256 xaimatzu.exe
signtool sign /f ..\xaimatzu.pfx /p Sonic2020! /fd SHA256 XaimatzuSetup.msi