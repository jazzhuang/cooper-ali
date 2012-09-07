.\build web Debug

$dir=Get-Location
$dir=''+$dir

$p=$dir+'\build\debug\cooper_web'
echo 'Run on IISExpress'
$env:Path=$env:Path+';C:\PROGRA~2\IIS Express'
start iexplore "http://localhost:8889"
iisexpress.exe /path:$p  /port:8889 /clr:V4.0

