$PSScriptRoot = ($MyInvocation.MyCommand.Path | Split-Path | Resolve-Path).ProviderPath
$serviceName = "Simcode.IdentityServer"

stop-service $serviceName
$maxRepeat = 100
$status = "Running" # change to Stopped if you want to wait for services to start
do 
{
    $count = (Get-Service $serviceName | ? {$_.status -eq $status}).count
    $maxRepeat--
    sleep -Milliseconds 600
} until ($count -eq 0 -or $maxRepeat -eq 0)
Start-Sleep -Seconds 5


Remove-Item "C:\Program Files\Simcode\IdentityServer\*" -Recurse -Force

Copy-Item -Path "$PSScriptRoot\Simcode.IdentityServer\bin\Release\net6.0\*" -Destination "C:\Program Files\Simcode\IdentityServer" -Recurse


start-service $serviceName

Read-Host -Prompt "Press Enter to exit"