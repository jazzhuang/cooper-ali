echo off
@forfiles /s /m packages.config /c "cmd /c %1\nuget install @file -o %2 -source http://nuget.icodesharp.com/nuget;http://taobao-ops-base/feeds/nuget"
echo on