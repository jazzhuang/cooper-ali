del packages /F /S /Q
echo off
@forfiles /s /m packages.config /c "cmd /c %1\nuget install @file -o %2 -source http://10.13.23.84/feeds/nuget;http://cs-nuget.apphb.com/nuget;http://nuget.org/api/v2/"
echo on