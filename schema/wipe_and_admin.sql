-- =============================================================================
-- wipe_and_admin.sql — Limpieza total del esquema api_banco + un solo admin
-- -----------------------------------------------------------------------------
-- Qué hace:
--   1. DROP de tablas / columnas que el código C# NO usa
--      (cuenta_comision_banco, __efmigrationshistory,
--       bitacora_transacciones.descripcion, usuario_acceso.fecha_creacion).
--   2. TRUNCATE de tablas transaccionales y de datos.
--   3. Re-siembra de catálogos (estado, tipo_cuenta, tipo_transaccion).
--   4. Re-siembra del cliente del sistema (id=100) con sus 4 cuentas internas
--      (100=Comisiones, 101=Universidad, 102=Telefonía, 103=Energía).
--   5. Crea UN solo usuario: admin@banco.local / Admin2026!
--      (queda en texto plano; al primer login el banco lo rehashea con BCrypt
--      vía el migrador transparente de AuthController).
--
-- !!! HACER BACKUP ANTES DE EJECUTAR !!!
-- =============================================================================

USE `api_banco`;
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- -----------------------------------------------------------------------------
-- 1. Eliminar objetos no usados por el código
-- -----------------------------------------------------------------------------

-- Tabla artefacto de un intento previo de migraciones EF Core.
DROP TABLE IF EXISTS `__efmigrationshistory`;

-- Tabla del diseño viejo de comisiones; el sistema actual acumula
-- comisiones en cuenta_bancaria.id_cuenta = 100.
DROP TABLE IF EXISTS `cuenta_comision_banco`;

-- Columna nunca leída por la entidad TransaccionBanco.
-- MySQL 8.0 NO soporta `DROP COLUMN IF EXISTS`; lo emulamos con information_schema.
SET @sql := (
    SELECT IF(
        COUNT(*) > 0,
        'ALTER TABLE `bitacora_transacciones` DROP COLUMN `descripcion`',
        'SELECT 1'
    )
    FROM `information_schema`.`COLUMNS`
    WHERE `TABLE_SCHEMA` = DATABASE()
      AND `TABLE_NAME`   = 'bitacora_transacciones'
      AND `COLUMN_NAME`  = 'descripcion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- Columna nunca leída por la entidad UsuarioAcceso.
SET @sql := (
    SELECT IF(
        COUNT(*) > 0,
        'ALTER TABLE `usuario_acceso` DROP COLUMN `fecha_creacion`',
        'SELECT 1'
    )
    FROM `information_schema`.`COLUMNS`
    WHERE `TABLE_SCHEMA` = DATABASE()
      AND `TABLE_NAME`   = 'usuario_acceso'
      AND `COLUMN_NAME`  = 'fecha_creacion'
);
PREPARE stmt FROM @sql; EXECUTE stmt; DEALLOCATE PREPARE stmt;

-- -----------------------------------------------------------------------------
-- 2. Vaciar tablas transaccionales y de datos (catálogos quedan tras paso 3)
-- -----------------------------------------------------------------------------
TRUNCATE TABLE `registro_pagos_servicios`;
TRUNCATE TABLE `bitacora_transacciones`;
TRUNCATE TABLE `usuario_acceso`;
TRUNCATE TABLE `tarjeta_debito`;
TRUNCATE TABLE `cuenta_bancaria`;
TRUNCATE TABLE `cliente`;

-- -----------------------------------------------------------------------------
-- 3. Catálogos mínimos
--    Los IDs son los mismos que el código resuelve por DESCRIPCIÓN
--    (CodigosEstado / CodigosTipoTransaccion), pero conviene fijar los valores
--    para mantener consistencia en reportes y kardex.
-- -----------------------------------------------------------------------------
INSERT IGNORE INTO `estado` (`id_estado`, `descripcion`) VALUES
  (1, 'ACTIVO'),
  (2, 'INACTIVO'),
  (3, 'PENDIENTE_ACTIVACION');

INSERT IGNORE INTO `tipo_cuenta` (`id_tipo_cuenta`, `descripcion`) VALUES
  (1, 'AHORRO'),
  (2, 'CORRIENTE'),
  (3, 'CUENTA_INTERNA_BANCO');

INSERT IGNORE INTO `tipo_transaccion` (`id_tipo_transaccion`, `descripcion`) VALUES
  (1, 'DEPOSITO'),
  (2, 'RETIRO'),
  (3, 'PAGO_SERVICIO_DEBITO_CUENTAHABIENTE'),
  (4, 'PAGO_SERVICIO_ACREDITACION_PRESTADORA'),
  (5, 'PAGO_SERVICIO_COMISION_BANCO'),
  (6, 'TRANSFERENCIA_ORIGEN'),
  (7, 'TRANSFERENCIA_DESTINO'),
  -- Pagos en ventanilla (efectivo): la cuenta de comisiones (id_cuenta = 100)
  -- juega el rol de "caja" recibiendo el ingreso de efectivo y compensando el
  -- 95% que se transfiere a la cuenta de la prestadora. La acreditación del 5%
  -- sigue usando PAGO_SERVICIO_COMISION_BANCO para reutilizar reportes existentes.
  (8, 'PAGO_VENTANILLA_INGRESO_EFECTIVO'),
  (9, 'PAGO_VENTANILLA_TRANSFERENCIA_PRESTADORA');

-- -----------------------------------------------------------------------------
-- 4. Cliente del sistema + cuentas internas (indispensables para pagos)
--    Configuradas en appsettings.json → sección "Pagos":
--      IdCuentaComisiones            = 100
--      CuentasPrestadoras.Universidad = 101
--      CuentasPrestadoras.Telefonia  = 102
--      CuentasPrestadoras.EnergiaElectrica = 103
-- -----------------------------------------------------------------------------
INSERT INTO `cliente` (`id_cliente`, `dpi`, `nit`, `nombre`, `apellido`, `telefono`, `email`) VALUES
  (100, '0000000000100', 'CF-SISTEMA', 'Cuentas', 'Internas Banco', NULL, 'interno.sistema@banco.local');

INSERT INTO `cuenta_bancaria` (`id_cuenta`, `no_cuenta`, `id_cliente`, `id_tipo_cuenta`, `saldo_actual`, `id_estado`) VALUES
  (100, '9900000100', 100, 3, 0.00, 1),
  (101, '9900000101', 100, 3, 0.00, 1),
  (102, '9900000102', 100, 3, 0.00, 1),
  (103, '9900000103', 100, 3, 0.00, 1);

-- -----------------------------------------------------------------------------
-- 5. Único usuario administrador
--    La password queda en texto plano; al primer login exitoso, el banco la
--    rehashea con BCrypt (work factor 11) gracias al fallback transparente.
--
--    id_cliente queda en NULL porque un ADMIN no está atado a ningún
--    cuentahabiente. La entidad UsuarioAcceso.IdCliente es `int?` en C#,
--    así que EF Core lo materializa sin problema.
-- -----------------------------------------------------------------------------
INSERT INTO `usuario_acceso` (`id_cliente`, `nombre_usuario`, `correo_electronico`, `password_hash`, `rol`) VALUES
  (NULL, 'admin', 'admin@banco.local', 'Admin2026!', 'ADMIN');

-- -----------------------------------------------------------------------------
-- 6. Reset de AUTO_INCREMENT en tablas que quedaron vacías o con muy pocos rows
-- -----------------------------------------------------------------------------
ALTER TABLE `cliente`                  AUTO_INCREMENT = 101;
ALTER TABLE `cuenta_bancaria`          AUTO_INCREMENT = 104;
ALTER TABLE `tarjeta_debito`           AUTO_INCREMENT = 1;
ALTER TABLE `bitacora_transacciones`   AUTO_INCREMENT = 1;
ALTER TABLE `registro_pagos_servicios` AUTO_INCREMENT = 1;
ALTER TABLE `usuario_acceso`           AUTO_INCREMENT = 2;

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================================
-- Estado final
-- -----------------------------------------------------------------------------
-- Único usuario:
--   correo  : admin@banco.local
--   password: Admin2026!        (cámbiela en el primer login real)
--   rol     : ADMIN
--
-- Cliente sistema (id=100): dueño de las 4 cuentas internas:
--   100 → Comisiones banco (5 %)
--   101 → Prestadora Universidad UMG (95 %)
--   102 → Prestadora Telefonía       (95 %)
--   103 → Prestadora Energía         (95 %)
-- =============================================================================

-- Verificación rápida:
-- SELECT 'cliente' tabla, COUNT(*) FROM cliente
-- UNION ALL SELECT 'cuenta_bancaria',          COUNT(*) FROM cuenta_bancaria
-- UNION ALL SELECT 'tarjeta_debito',           COUNT(*) FROM tarjeta_debito
-- UNION ALL SELECT 'usuario_acceso',           COUNT(*) FROM usuario_acceso
-- UNION ALL SELECT 'bitacora_transacciones',   COUNT(*) FROM bitacora_transacciones
-- UNION ALL SELECT 'registro_pagos_servicios', COUNT(*) FROM registro_pagos_servicios;
