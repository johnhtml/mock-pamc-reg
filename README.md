# Mock PAMC Registraduría (`mock-pamc-reg`)

Azure Function **.NET 10 isolated** que mockea el consumo al endpoint de
Registraduría `REGISTRADURIA_API_URL` usado por
`SWPR251.COMPENSAR.AZFUNC.SRVORQ`.

- Host: `mock-pamc-reg-gsdtavcwaffxcedu.eastus2-01.azurewebsites.net`
- Runtime: `dotnet-isolated` / `10.0`
- Storage: `mockpamcreg`
- Application Insights: `mock-pamc-reg`

## Endpoints

| Método | Ruta | Descripción |
| --- | --- | --- |
| GET | `/apivalidaciones/v1.0.0/validaciones?numero_identificacion={n}&codigo_tipo_identificacion={t}` | Devuelve el JSON mock del documento (`{n}.json`). |
| GET | `/apivalidaciones/v1.0.0/health` | Health check → `200 {"status":"Healthy"}`. |
| POST | `/ServiciosCliente.svc` | Mock SOAP de `MarcacionRequisitosRegistraduria` (Gestión Clientes). |

Se usa `routePrefix` vacío (`host.json`) para replicar exactamente las rutas
del API real.

### Reglas de `validaciones`

- La función es **pública** (`AuthorizationLevel.Anonymous`); la autenticación
  de API Management (p. ej. `Ocp-Apim-Subscription-Key`, `Authorization`) se
  **acepta pero no se valida**.
- Lee el blob `{numero_identificacion}.json` del contenedor `mock-registraduria`
  y devuelve el JSON **tal cual** (sin re-serializar).
- El **código HTTP** de la respuesta es el valor del campo `codigo_estado` del
  JSON (p. ej. `200`, `209`). Si no viene, se asume `200`.
- Si falta `numero_identificacion` → `400`.
- Si no existe el blob → `404` con **placeholder** (`TODO`): aún no se conoce el
  body del escenario "documento no encontrado" de la API real.

## Estructura de blobs

Contenedor: `mock-registraduria`. Un JSON por documento:

```
mock-registraduria/
├── 1151957777.json   # codigo_estado 200
└── 52098280.json     # codigo_estado 209
```

Los JSON incluyen el bloque `estadoConsulta` y se devuelven sin modificar:

```json
"estadoConsulta": {
    "numeroControl": "5218067652",
    "codError": "0",
    "descripcionError": "OK",
    "fechaHoraConsulta": "2026-09-16 10:00:37"
}
```

Muestras versionadas de referencia en `data/muestras/`.

## Mock SOAP `MarcacionRequisitosRegistraduria`

`POST /ServiciosCliente.svc` reemplaza al servicio de Gestión Clientes
(`http://gclientespru.compensar.com/ServiciosCliente.svc`). Toma **todos** los
`vin:Requisito` recibidos y simula que todos fueron actualizados:

- `200` (`text/xml`) con `MarcacionRequisitosRegistraduriaResult` = `true` y un
  `mensaje` con una línea `Requisito '<SIGLA>' fue actualizado.` por requisito,
  en el mismo orden.
- `400` con SOAP Fault cuando el body es inválido: XML mal formado, sin la
  operación `MarcacionRequisitosRegistraduria` o sin requisitos.
- No valida `SOAPAction`/autenticación; es público.

```xml
<s:Envelope xmlns:s="http://schemas.xmlsoap.org/soap/envelope/">
  <s:Body>
    <MarcacionRequisitosRegistraduriaResponse xmlns="http://tempuri.org/">
      <MarcacionRequisitosRegistraduriaResult>true</MarcacionRequisitosRegistraduriaResult>
      <mensaje>Requisito 'IDEXPE' fue actualizado.
Requisito 'CORNEC' fue actualizado.
Requisito 'ESTREG' fue actualizado.
Requisito 'CEDULA' fue actualizado.</mensaje>
    </MarcacionRequisitosRegistraduriaResponse>
  </s:Body>
</s:Envelope>
```

No requiere blobs (la respuesta es determinística a partir del request).
Detalle en `docs/pamc/mock-gestion-clientes-marcacion.md` de SRVORQ.

## Configuración (`local.settings.json`)

| Variable | Descripción |
| --- | --- |
| `AzureWebJobsStorage` | Storage del host (Azurite en local). |
| `FUNCTIONS_WORKER_RUNTIME` | `dotnet-isolated`. |
| `Storage:AccountUri` | `https://mockpamcreg.blob.core.windows.net` (lectura por identidad). |
| `Storage:Container` | `mock-registraduria`. |
| `Storage:ConnectionString` | Opcional; fallback si no hay identidad. |
| `APPLICATIONINSIGHTS_CONNECTION_STRING` | Connection string de `mock-pamc-reg`. |

Si `Storage:AccountUri` no está definido, se usa
`Storage:ConnectionString` / `AzureWebJobsStorage` (Azurite local).

## Ejecución local

```bash
# 1. Iniciar Azurite (o autenticarse con az login para usar la cuenta real)
azurite --silent --location ./.azurite

# 2. Levantar la Function
dotnet run --project src/MockPamcReg.Functions
```

> Si usas la cuenta real (`mockpamcreg`), inicia sesión con `az login` y
> asegúrate de tener rol `Storage Blob Data Reader` sobre la cuenta.

Probar:

```bash
curl "http://localhost:7071/apivalidaciones/v1.0.0/validaciones?numero_identificacion=1151957777&codigo_tipo_identificacion=1"
curl "http://localhost:7071/apivalidaciones/v1.0.0/health"
```

## Pruebas

```bash
dotnet test MockPamcReg.slnx
```

Cubren: devolución del JSON tal cual, uso de `codigo_estado` como código HTTP,
códigos 200/209, `404` (documento inexistente), `400` (sin parámetro),
indiferencia ante headers de APIM y el health check.

## Postman

Colección de consumos y environment en `postman/`:

- `mock-pamc-reg.postman_collection.json` — consumos REST (`validaciones`
  200/209, 404 TODO, 400, health) y SOAP (`Gestion Clientes (SOAP)`:
  marcación 200 y 400), con tests de respuesta.
- `mock-pamc-reg.postman_environment.json` — variables locales (`base-url`,
  `apim-subscription-key`, `token`, `consumidor-id`).

Importar ambos en Postman. El environment apunta a `http://localhost:7071`;
para Azure, ajustar `base-url` a
`https://mock-pamc-reg-gsdtavcwaffxcedu.eastus2-01.azurewebsites.net`.

## Despliegue

```bash
dotnet publish src/MockPamcReg.Functions -c Release -o ./publish
cd ./publish && zip -r ../mock-pamc-reg.zip . && cd ..
az functionapp deployment source config-zip \
  -g <RESOURCE_GROUP> -n mock-pamc-reg-gsdtavcwaffxcedu --src mock-pamc-reg.zip
```

Configura en la Function App: `Storage:AccountUri`, `Storage:Container`,
`APPLICATIONINSIGHTS_CONNECTION_STRING` y la identidad administrada con rol
`Storage Blob Data Reader` sobre `mockpamcreg`.

## TODO / deuda técnica

- **Body de "documento no encontrado"**: pendiente contrato real con
  Registraduría. Implementado como `404` placeholder en
  `ValidacionesHttpFunction`.
- **Endpoint de token**: el consumidor actual obtiene token antes de llamar a
  `validaciones`. Al apuntar `REGISTRADURIA_API_URL` al mock, revisar
  `REGISTRADURIA_TOKEN_URL` (ver doc de integración en SRVORQ).
- **SOAP marcación — escenarios de fallo**: `MarcacionRequisitosRegistraduria`
  siempre responde `true`; no se modela `false`/SOAP Fault funcional.
