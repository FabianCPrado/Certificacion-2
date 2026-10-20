# AuthenticationSystem

Servicio interno de autenticación entre servicios. Emite JWT de corta duración para `pedidos-service`; no tiene login de usuarios ni persiste access tokens.

## Base de datos

La API aplica las migraciones EF Core al iniciar en Development. La cadena por defecto crea `AuthenticationDB` en LocalDB.

## Secretos locales

Configure la misma clave de firma en Authentication.Api y Catalogo.Api, y el mismo secreto de cliente en Authentication.Api y Pedidos.Api. No guarde estos valores en `appsettings.json` ni en Git.

```powershell
$bytes = New-Object byte[] 64
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$signingKey = [Convert]::ToBase64String($bytes)

$bytes = New-Object byte[] 48
[Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
$pedidosSecret = [Convert]::ToBase64String($bytes)

dotnet user-secrets set "Jwt:SigningKey" $signingKey --project src/Authentication.Api
dotnet user-secrets set "ClientCredentials:PedidosSecret" $pedidosSecret --project src/Authentication.Api
dotnet user-secrets set "Jwt:SigningKey" $signingKey --project ../CatalogoSystem/src/Catalogo.Api
dotnet user-secrets set "ClientCredentials:PedidosSecret" $pedidosSecret --project ../PedidosSystem/src/Pedidos.Api
```

## Ejecución local

Desde esta carpeta, iniciar `Authentication.Api` en `https://localhost:7401`, después `Catalogo.Api` en `https://localhost:7301` y finalmente `Pedidos.Api` en `https://localhost:7101`. Pedidos obtiene y cachea el token al llamar a Catalogo; AuthenticationDB conserva el identificador y el hash del secreto de Pedidos.