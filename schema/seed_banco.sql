-- =============================================================================
-- seed_banco.sql — Datos mínimos para API_Banco (api_banco)
-- Ejecutar DESPUÉS de: Dump20260519 Banco.sql
-- Idempotente: puede ejecutarse varias veces (ON DUPLICATE KEY UPDATE)
-- =============================================================================

USE `api_banco`;

SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- -----------------------------------------------------------------------------
-- Catálogos
-- -----------------------------------------------------------------------------
INSERT INTO `estado` (`id_estado`, `descripcion`) VALUES
  (1, 'ACTIVO'),
  (2, 'INACTIVO'),
  (3, 'PENDIENTE_ACTIVACION') AS nuevo
ON DUPLICATE KEY UPDATE `descripcion` = nuevo.`descripcion`;

INSERT INTO `tipo_cuenta` (`id_tipo_cuenta`, `descripcion`) VALUES
  (1, 'AHORRO'),
  (2, 'CORRIENTE'),
  (3, 'CUENTA_INTERNA_BANCO') AS nuevo
ON DUPLICATE KEY UPDATE `descripcion` = nuevo.`descripcion`;

-- Orden fijo: ids 6 y 7 usados por transferencias en OperacionesFinancierasServicio
INSERT INTO `tipo_transaccion` (`id_tipo_transaccion`, `descripcion`) VALUES
  (1, 'DEPOSITO'),
  (2, 'RETIRO'),
  (3, 'PAGO_SERVICIO_DEBITO_CUENTAHABIENTE'),
  (4, 'PAGO_SERVICIO_ACREDITACION_PRESTADORA'),
  (5, 'PAGO_SERVICIO_COMISION_BANCO'),
  (6, 'TRANSFERENCIA_ORIGEN'),
  (7, 'TRANSFERENCIA_DESTINO') AS nuevo
ON DUPLICATE KEY UPDATE `descripcion` = nuevo.`descripcion`;

-- -----------------------------------------------------------------------------
-- Cliente del sistema (dueño de cuentas internas: comisiones y prestadoras)
-- Usa id_cliente = 100 para no chocar con clientes reales
-- -----------------------------------------------------------------------------
INSERT INTO `cliente` (`id_cliente`, `dpi`, `nit`, `nombre`, `apellido`, `telefono`, `email`) VALUES
  (100, '0000000000100', 'CF-SISTEMA', 'Cuentas', 'Internas Banco', NULL, 'interno.sistema@banco.local') AS nuevo
ON DUPLICATE KEY UPDATE
  `nombre` = nuevo.`nombre`,
  `apellido` = nuevo.`apellido`;

-- -----------------------------------------------------------------------------
-- Cuentas internas (configuradas en appsettings → Pagos:*)
--   100 = Comisiones banco (5 %)
--   101 = Prestadora Universidad UMG (95 %)
--   102 = Prestadora Telefonía
--   103 = Prestadora Energía Eléctrica
-- -----------------------------------------------------------------------------
INSERT INTO `cuenta_bancaria` (`id_cuenta`, `no_cuenta`, `id_cliente`, `id_tipo_cuenta`, `saldo_actual`, `id_estado`) VALUES
  (100, '9900000100', 100, 3, 0.00, 1),
  (101, '9900000101', 100, 3, 0.00, 1),
  (102, '9900000102', 100, 3, 0.00, 1),
  (103, '9900000103', 100, 3, 0.00, 1) AS nuevo
ON DUPLICATE KEY UPDATE
  `id_estado` = nuevo.`id_estado`;

INSERT INTO `cuenta_comision_banco` (`id_comision_cuenta`, `nombre_cuenta`, `saldo_acumulado`) VALUES
  (1, 'COMISIONES_PAGOS_SERVICIOS', 0.00) AS nuevo
ON DUPLICATE KEY UPDATE
  `nombre_cuenta` = nuevo.`nombre_cuenta`;

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================================
-- Referencia rápida (Scalar / pruebas locales)
-- -----------------------------------------------------------------------------
-- Cuentas internas creadas (no tocar):
--   id_cuenta 100 → COMISIONES (5 %)
--   id_cuenta 101 → UNIVERSIDAD (95 %)
--   id_cuenta 102 → TELEFONIA   (95 %)
--   id_cuenta 103 → ENERGIA     (95 %)
--
-- Para pagar usa cualquier tarjeta de débito EXISTENTE en tu BD
-- ligada a una cuenta != 100..103. Por ejemplo: tu cuenta id_cuenta = 3.
--
-- Para ver saldo de una tarjeta:
--   SELECT t.no_tarjeta, t.pin_hash, c.id_cuenta, c.saldo_actual
--   FROM tarjeta_debito t JOIN cuenta_bancaria c ON t.id_cuenta = c.id_cuenta
--   WHERE c.id_estado = 1;
-- =============================================================================
