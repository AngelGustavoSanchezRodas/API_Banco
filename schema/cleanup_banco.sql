-- =============================================================================
-- cleanup_banco.sql — Limpieza completa de api_banco
-- -----------------------------------------------------------------------------
-- BORRA todos los datos transaccionales y de prueba, y deja únicamente:
--   * Catálogos (estado, tipo_cuenta, tipo_transaccion) intactos
--   * Cliente del sistema (id 100) + cuentas internas (id 100, 101, 102, 103)
--   * 1 cliente de prueba (Juan Perez) con cuenta, tarjeta y usuario
-- -----------------------------------------------------------------------------
-- !!! HACER BACKUP ANTES DE EJECUTAR !!!
-- =============================================================================

USE `api_banco`;
SET NAMES utf8mb4;
SET FOREIGN_KEY_CHECKS = 0;

-- -----------------------------------------------------------------------------
-- 1. Vaciar tablas transaccionales y de datos (conservar catálogos)
-- -----------------------------------------------------------------------------
TRUNCATE TABLE `registro_pagos_servicios`;
TRUNCATE TABLE `bitacora_transacciones`;
TRUNCATE TABLE `usuario_acceso`;
TRUNCATE TABLE `tarjeta_credito`;
TRUNCATE TABLE `tarjeta_debito`;
TRUNCATE TABLE `cuenta_bancaria`;
TRUNCATE TABLE `cuenta_comision_banco`;
TRUNCATE TABLE `cliente`;

-- -----------------------------------------------------------------------------
-- 2. Asegurar catálogos mínimos
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
  (7, 'TRANSFERENCIA_DESTINO');

-- -----------------------------------------------------------------------------
-- 3. Cliente del sistema (dueño de cuentas internas)
-- -----------------------------------------------------------------------------
INSERT INTO `cliente` (`id_cliente`, `dpi`, `nit`, `nombre`, `apellido`, `telefono`, `email`) VALUES
  (100, '0000000000100', 'CF-SISTEMA', 'Cuentas', 'Internas Banco', NULL, 'interno.sistema@banco.local');

INSERT INTO `cuenta_bancaria` (`id_cuenta`, `no_cuenta`, `id_cliente`, `id_tipo_cuenta`, `saldo_actual`, `id_estado`) VALUES
  (100, '9900000100', 100, 3, 0.00, 1),
  (101, '9900000101', 100, 3, 0.00, 1),
  (102, '9900000102', 100, 3, 0.00, 1),
  (103, '9900000103', 100, 3, 0.00, 1);

INSERT INTO `cuenta_comision_banco` (`id_comision_cuenta`, `nombre_cuenta`, `saldo_acumulado`) VALUES
  (1, 'COMISIONES_PAGOS_SERVICIOS', 0.00);

-- -----------------------------------------------------------------------------
-- 4. Cliente de prueba para Scalar
-- -----------------------------------------------------------------------------
INSERT INTO `cliente` (`id_cliente`, `dpi`, `nit`, `nombre`, `apellido`, `telefono`, `email`) VALUES
  (1, '2345678901012', '1234567-8', 'Juan', 'Perez Prueba', '50255551234', 'juan.perez@correo.test');

INSERT INTO `cuenta_bancaria` (`id_cuenta`, `no_cuenta`, `id_cliente`, `id_tipo_cuenta`, `saldo_actual`, `id_estado`) VALUES
  (1, '1000000001', 1, 1, 10000.00, 1);

INSERT INTO `tarjeta_debito` (`id_tarjeta`, `id_cuenta`, `no_tarjeta`, `pin_hash`, `fecha_vencimiento`, `id_estado`) VALUES
  (1, 1, '4123123456781234', '1234', DATE_ADD(CURDATE(), INTERVAL 3 YEAR), 1);

INSERT INTO `usuario_acceso` (`id_usuario`, `id_cliente`, `nombre_usuario`, `correo_electronico`, `password_hash`, `rol`) VALUES
  (1, 1, 'juan.perez', 'juan.perez@correo.test', 'Temp1012!', 'CLIENTE');

SET FOREIGN_KEY_CHECKS = 1;

-- =============================================================================
-- Estado final
-- -----------------------------------------------------------------------------
-- Cliente prueba:   Juan Perez (id_cliente = 1)
-- Cuenta:           id_cuenta = 1 | no_cuenta = 1000000001 | saldo = Q10,000.00
-- Tarjeta:          4123123456781234   PIN: 1234
-- Login:            juan.perez@correo.test / Temp1012!
--
-- Cuentas internas (NO TOCAR — configuradas en appsettings):
--   100 = COMISIONES
--   101 = UNIVERSIDAD
--   102 = TELEFONIA
--   103 = ENERGIA
-- =============================================================================

-- Verificación rápida (ejecutar para confirmar):
-- SELECT 'cliente' tabla, COUNT(*) total FROM cliente
-- UNION ALL SELECT 'cuenta_bancaria', COUNT(*) FROM cuenta_bancaria
-- UNION ALL SELECT 'tarjeta_debito', COUNT(*) FROM tarjeta_debito
-- UNION ALL SELECT 'usuario_acceso', COUNT(*) FROM usuario_acceso
-- UNION ALL SELECT 'bitacora_transacciones', COUNT(*) FROM bitacora_transacciones
-- UNION ALL SELECT 'registro_pagos_servicios', COUNT(*) FROM registro_pagos_servicios;
