# Esquema y datos — API Banco (`api_banco`)

## Archivos

| Archivo | Propósito |
|---------|-----------|
| `Dump20260519 Banco.sql` | Estructura (CREATE TABLE) |
| `seed_banco.sql` | Datos mínimos sobre BD existente (no borra nada) |
| `cleanup_banco.sql` | **Limpieza total** + datos de prueba mínimos |

## Orden de ejecución en MySQL

### Instalación nueva
1. **Estructura:** `Dump20260519 Banco.sql`
2. **Datos semilla:** `seed_banco.sql`

### Resetear datos de prueba (BD ya existente)
1. **Backup** (Workbench → Data Export)
2. Ejecutar `cleanup_banco.sql`

### Azure Data Studio / MySQL Workbench

Conéctate a `api_banco` y ejecuta cada archivo en orden.

### Línea de comandos (ejemplo)

```bash
mysql -h servicio-de-pago.mysql.database.azure.com -u USUARIO -p --ssl-mode=REQUIRED api_banco < "Dump20260519 Banco.sql"
mysql -h servicio-de-pago.mysql.database.azure.com -u USUARIO -p --ssl-mode=REQUIRED api_banco < "seed_banco.sql"
```

## Universidad (`api_universidad`)

Los scripts de la API Universidad están en el repo **ApiUniversidadUMG**:

| Archivo | Base de datos |
|---------|----------------|
| `ApiUniversidadUMG/schema/Dump20260519 Universidad.sql` | Estructura |
| `ApiUniversidadUMG/schema/seed_universidad.sql` | Datos de prueba |

Ejecutar **Universidad antes de probar pagos** (el banco consulta la deuda por HTTP).

## Datos de prueba tras `cleanup_banco.sql`

| Concepto | Valor |
|----------|--------|
| Login | `juan.perez@correo.test` / `Temp1012!` |
| Tarjeta débito | `4123123456781234` |
| PIN | `1234` |
| Cuenta cliente (id) | `1` — `1000000001` — Q 10,000.00 |
| Cuenta comisiones (id) | `100` — `9900000100` |
| Cuenta prestadora Universidad | `101` — `9900000101` |
| Cuenta prestadora Telefonía | `102` — `9900000102` |
| Cuenta prestadora Energía | `103` — `9900000103` |

Las cuentas internas 100-103 están enlazadas a `appsettings.Development.json → Pagos:*`.

## Prueba de pago de servicios (Scalar)

1. Levantar **ApiUniversidadUMG** (ej. `http://localhost:5212`).
2. En `API_Banco/appsettings.Development.json` (o User Secrets):

   ```json
   "Integraciones": {
     "UniversidadApiUrl": "http://localhost:5212"
   }
   ```

3. Levantar **API_Banco** (ej. `http://localhost:5195`).
4. Carnet universidad: `2024001001` — deuda **Q 1,500.00**.
5. `POST api/Pagos/validar` → `tipoServicio: 1`, `identificador: "2024001001"`.
6. `POST api/Pagos/ejecutar` → misma tarjeta/PIN, `monto: 1500.00`.

## Notas

- Los dumps solo traen **estructura**; sin `seed_banco.sql` faltan estados, tipos de transacción y cuentas 1/2.
- Si ya existen filas con los mismos `id_*`, el seed actualiza descripciones/saldos sin borrar datos históricos.
- Ajusta emails/DPI si chocan con registros ya creados en Azure.
