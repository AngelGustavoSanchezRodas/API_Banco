# Esquema y datos — API Banco (`api_banco`)

## Archivos

| Archivo | Propósito |
|---------|-----------|
| `Dump20260529.sql` | Snapshot histórico (`mysqldump`) anterior a la última limpieza. Solo referencia. |
| `wipe_and_admin.sql` | **Script único de operación**: limpia la BD, elimina campos no usados y deja un solo admin. |

## Qué hace `wipe_and_admin.sql`

1. **Elimina objetos no usados por el código C#**:
   - Tabla `__efmigrationshistory` (artefacto sin uso, el proyecto no usa migraciones EF).
   - Tabla `cuenta_comision_banco` (diseño viejo; el sistema acumula comisiones en `cuenta_bancaria.id_cuenta = 100`).
   - Columna `bitacora_transacciones.descripcion` (nunca mapeada en `TransaccionBanco`).
   - Columna `usuario_acceso.fecha_creacion` (nunca mapeada en `UsuarioAcceso`).
2. **`TRUNCATE`** de todas las tablas transaccionales.
3. **Re-siembra de catálogos** `estado`, `tipo_cuenta`, `tipo_transaccion` con los IDs que el código resuelve por nombre.
4. **Re-siembra del cliente sistema (id=100)** y sus 4 cuentas internas (100=Comisiones, 101=Universidad, 102=Telefonía, 103=Energía). Indispensables porque están cableadas en `appsettings.json → Pagos:*`.
5. **Crea un único usuario `ADMIN`**.

> ⚠️ **Antes de ejecutar, hacer backup**. Es destructivo.

## Único acceso resultante

| Concepto | Valor |
|----------|-------|
| Correo | `admin@banco.local` |
| Password | `Admin2026!` (texto plano la primera vez) |
| Rol | `ADMIN` |

La password queda en texto plano en la fila para arrancar; al **primer login exitoso** el banco la rehashea automáticamente con BCrypt (work factor 11) gracias al fallback transparente de `AuthController`. Para producción real, cambiarla por una nueva apenas se entre.

## Cuentas internas obligatorias

| `id_cuenta` | `no_cuenta` | Función | Llave en `appsettings.json` |
|-------------|--------------|---------|------------------------------|
| 100 | 9900000100 | Comisiones banco (5 %) | `Pagos:IdCuentaComisiones` |
| 101 | 9900000101 | Prestadora Universidad UMG (95 %) | `Pagos:CuentasPrestadoras:Universidad` |
| 102 | 9900000102 | Prestadora Telefonía (95 %) | `Pagos:CuentasPrestadoras:Telefonia` |
| 103 | 9900000103 | Prestadora Energía Eléctrica (95 %) | `Pagos:CuentasPrestadoras:EnergiaElectrica` |

## Orden de ejecución en MySQL

Conéctate a `api_banco` desde Workbench / Azure Data Studio / línea de comandos y ejecuta:

```bash
mysql -h servicio-de-pago.mysql.database.azure.com -u USUARIO -p --ssl-mode=REQUIRED api_banco < wipe_and_admin.sql
```

## Universidad (`api_universidad`)

Para probar pagos universitarios, levantar **ApiUniversidadUMG** antes (el banco consulta la deuda por HTTP). Sus scripts están en el repo `ApiUniversidadUMG/schema/`.

## Notas

- Los IDs literales en `wipe_and_admin.sql` (1=ACTIVO, 6=TRANSFERENCIA_ORIGEN, etc.) son los que ya estaban en el dump. Si la fila ya existe, `INSERT IGNORE` evita el conflicto.
- El código C# resuelve cada estado/tipo de transacción **por descripción** (vía `CodigosEstado` / `CodigosTipoTransaccion`), así que los IDs pueden cambiar sin romper la app, pero conviene mantenerlos fijos por consistencia con la bitácora histórica.
