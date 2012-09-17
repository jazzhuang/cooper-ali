.\build web Debug

$dir=Get-Location
$dir=''+$dir

$p=$dir+'\build\debug\cooper_web'
echo 'Run on IISExpress'
$env:Path=$env:Path+';C:\PROGRA~2\IIS Express'
start iexplore "http://localhost:9000"
iisexpress.exe /path:$p  /port:9000 /clr:V4.0

