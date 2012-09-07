$dir=Get-Location
$dir=''+$dir
$p=$dir+'\src\cooper.web'
$env:Path=$env:Path+';C:\PROGRA~2\IIS Express'
start iexplore "http://localhost:9000"
iisexpress.exe /path:$p  /port:9000 /clr:V4.0
