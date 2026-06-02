-- =============================================================================
-- Parche idempotente: tipos de transacción para pagos de servicios en ventanilla
-- =============================================================================
-- Aplicable sobre la base de datos ya desplegada (Azure) sin re-correr el wipe.
-- Agrega los dos tipos que necesita el flujo "POST /api/Pagos/ventanilla":
--
--   8 -> PAGO_VENTANILLA_INGRESO_EFECTIVO
--           +monto en la cuenta interna de comisiones (rol de caja).
--           Es la transacción ORIGEN del RegistroPagoServicio en ventanilla.
--
--   9 -> PAGO_VENTANILLA_TRANSFERENCIA_PRESTADORA
--           -95% desde la cuenta interna de comisiones (rol de caja) hacia la
--           cuenta de la empresa prestadora. Compensa la acreditación del 95%.
--
-- Modelo contable resultante por cada pago de Qx en ventanilla:
--   cuenta_comisiones    : +x  (ingreso efectivo)   tipo 8
--   cuenta_comisiones    : -0.95x (egreso a prestadora) tipo 9
--   cuenta_prestadora    : +0.95x (acreditación)    tipo 4
--   cuenta_comisiones    : +0.05x (comisión banco)  tipo 5
--   Saldo neto de cuenta_comisiones: +0.05x  ✓ (igual que el flujo del cliente)
--   Saldo neto de cuenta_prestadora: +0.95x  ✓
-- =============================================================================

INSERT IGNORE INTO `tipo_transaccion` (`id_tipo_transaccion`, `descripcion`) VALUES
  (8, 'PAGO_VENTANILLA_INGRESO_EFECTIVO'),
  (9, 'PAGO_VENTANILLA_TRANSFERENCIA_PRESTADORA');

-- Verificación opcional:
-- SELECT * FROM tipo_transaccion ORDER BY id_tipo_transaccion;
