# SocketProxier
Listens on a specified local port and forwards traffic to a local or remote port

## SocketProxier.exe usage
Syntax: `SocketProxier.exe listenPort destinationHost destinationPort`

#### Example 1:
`SocketProxier.exe 12345 localhost 443`
Opens a local listening port on 12345 which redirects traffic to localhost:443
#### Example 2:
`SocketProxier.exe 12345 www.website.com 443`
Opens a local listening port on 12345 which redirects traffic to www.website.com:443
#### Example 3
`SocketProxier.exe 12345 192.168.0.1 443`
Opens a local listening port on 12345 which redirects traffic to 192.168.0.1:443

## SocketProxierLib.dll usage (PowerShell)
#### Start Proxy
```
Add-Type -Path .\SocketProxierLib.dll
$Controller = [SocketProxierLib.Controller]::new(12345,"localhost",1671)
$Controller.Start()
```
#### Get Proxy Status
`$Controller.Status()`
#### Get Open Connections
`$Controller.OpenConnections()`
