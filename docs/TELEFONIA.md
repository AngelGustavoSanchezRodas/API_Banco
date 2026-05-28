# Integración bancaria — Telefonía (`tipoServicio = 2`)

El identificador es el **número telefónico** (8–15 dígitos). Se aceptan separadores (`-`, espacios, paréntesis); el banco normaliza a solo dígitos.

| Endpoint | Uso |
|----------|-----|
| `POST /api/Pagos/validar` | Valida formato del número |
| `GET /api/Pagos/consultar-deuda/2/{telefono}` | Deuda postpago (prepago → `0`) |
| `POST /api/Pagos/ejecutar` | Cobro con tarjeta + PIN |

## Reglas de cobro

- **Postpago:** si hay deuda en `Integraciones:TelefoniaDemoPostpago` (o en la API de telefonía cuando esté configurada), el `monto` debe coincidir con la deuda consultada.
- **Prepago (recarga):** deuda `0`; cualquier `monto` > 0 válido según saldo de la tarjeta.
- Misma lógica de tarjeta/PIN y distribución 95/5 que Universidad y Energía.

## Datos de prueba (seed del banco)

| Campo | Valor |
|-------|--------|
| Tarjeta | `4123123456781234` |
| PIN | `1234` |
| Postpago demo | `82542114` — deuda **Q 125.50** |
| Postpago demo | `55551234` — deuda **Q 89.00** |
| Prepago | cualquier otro número válido (8–15 dígitos), ej. `87654321` |

## Ejemplos curl

Base: `https://bancocentroamericano.azurewebsites.net` (o `http://localhost:5195` en local).

### Validar (válido)

```bash
curl -s -X POST "%BASE%/api/Pagos/validar" ^
  -H "Content-Type: application/json" ^
  -d "{\"tipoServicio\":2,\"identificador\":\"82542114\"}"
```

### Validar (inválido)

```bash
curl -s -X POST "%BASE%/api/Pagos/validar" ^
  -H "Content-Type: application/json" ^
  -d "{\"tipoServicio\":2,\"identificador\":\"12\"}"
```

### Consultar deuda (postpago)

```bash
curl -s "%BASE%/api/Pagos/consultar-deuda/2/82542114"
```

Respuesta exitosa: número decimal (ej. `125.50`).

### Ejecutar — pago de factura postpago

```bash
curl -s -X POST "%BASE%/api/Pagos/ejecutar" ^
  -H "Content-Type: application/json" ^
  -d "{\"numeroTarjeta\":\"4123123456781234\",\"pin\":\"1234\",\"tipoServicio\":2,\"identificador\":\"82542114\",\"monto\":125.50,\"referenciaCliente\":\"REC-82542114-20260526120000\"}"
```

### Ejecutar — recarga prepago

```bash
curl -s -X POST "%BASE%/api/Pagos/ejecutar" ^
  -H "Content-Type: application/json" ^
  -d "{\"numeroTarjeta\":\"4123123456781234\",\"pin\":\"1234\",\"tipoServicio\":2,\"identificador\":\"87654321\",\"monto\":25.00,\"referenciaCliente\":\"REC-87654321-20260526120000\"}"
```

## Callback a la API de Telefonía (opcional)

Cuando `Integraciones:TelefoniaApiUrl` apunta a la API publicada (no contiene `REEMPLAZAR`), tras un cobro exitoso el banco intenta `POST` a `TelefoniaNotificacionRutaRelativa` (por defecto `api/IntegracionBancaria/pago`) con header `X-Api-Key` si está configurado. Si la URL no está activa, el cobro igual se confirma en el banco (solo se registra en logs).
