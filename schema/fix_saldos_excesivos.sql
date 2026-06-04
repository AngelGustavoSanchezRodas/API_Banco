-- =====================================================================
-- fix_saldos_excesivos.sql
--
-- Parche QUIRÚRGICO para corregir cuentas que fueron infladas con
-- depósitos / activaciones por encima del nuevo tope operativo del banco
-- (Q 50,000.00 por operación, ver
--  API_Banco.Application.Services.Internos.ValidadoresEntrada).
--
-- Qué hace, en orden:
--   1. Te muestra las transacciones "anómalas" (las que exceden el tope)
--      para que las revises ANTES de borrar nada.
--   2. Revierte el efecto de cada transacción anómala sobre el saldo de
--      su cuenta (resta lo que sumaron y devuelve lo que restaron).
--   3. Borra los registros de pagos de servicios asociados (FK).
--   4. Borra las transacciones anómalas de la bitácora. Esto baja el
--      "Volumen mensual" del dashboard admin a sus valores reales.
--   5. Te muestra el saldo final de las cuentas tocadas para verificar.
--
-- Diseño:
--   * Idempotente: si vuelves a correrlo y ya no hay transacciones
--     anómalas, no toca nada.
--   * Envuelto en START TRANSACTION + SELECTs de verificación. Si algo
--     no te gusta, lanza ROLLBACK; si todo está bien, lanza COMMIT.
--   * NO toca cuentas internas del banco (id_estado / tipo) más allá del
--     saldo; los tipos de transacción y estados quedan intactos.
--
-- USO (MySQL Workbench / Azure MySQL):
--   1. Corre el script completo.
--   2. Revisa los SELECTs intermedios.
--   3. Si todo cuadra, ejecuta:        COMMIT;
--      Si algo no cuadra, ejecuta:     ROLLBACK;
-- =====================================================================

SET @monto_max := 50000.00;

-- ---------------------------------------------------------------------
-- (1) Inspección previa: ¿qué transacciones se consideran anómalas?
-- ---------------------------------------------------------------------
SELECT
    t.id_transaccion,
    t.id_cuenta,
    c.no_cuenta,
    cli.nombre,
    cli.apellido,
    tt.descripcion AS tipo_transaccion,
    t.monto,
    t.fecha_transaccion,
    t.referencia_vinculante
FROM bitacora_transacciones t
JOIN cuenta_bancaria c   ON c.id_cuenta = t.id_cuenta
LEFT JOIN cliente cli    ON cli.id_cliente = c.id_cliente
JOIN tipo_transaccion tt ON tt.id_tipo_transaccion = t.id_tipo_transaccion
WHERE t.monto > @monto_max
ORDER BY t.fecha_transaccion;

-- ---------------------------------------------------------------------
-- Snapshot del saldo ANTES del ajuste, solo para las cuentas afectadas.
-- ---------------------------------------------------------------------
SELECT
    c.id_cuenta,
    c.no_cuenta,
    cli.nombre,
    cli.apellido,
    c.saldo_actual AS saldo_antes
FROM cuenta_bancaria c
LEFT JOIN cliente cli ON cli.id_cliente = c.id_cliente
WHERE c.id_cuenta IN (
    SELECT DISTINCT id_cuenta
    FROM bitacora_transacciones
    WHERE monto > @monto_max
);

START TRANSACTION;

-- ---------------------------------------------------------------------
-- (2.a) Para los tipos que SUMAN al saldo del titular, restamos el
--       monto anómalo: depósitos, transferencias recibidas, ingresos
--       de pago por ventanilla en efectivo (el efectivo entra a la
--       cuenta del banco, no a la del cliente, pero por simetría con
--       cómo se asentó originalmente, restamos también).
-- ---------------------------------------------------------------------
UPDATE cuenta_bancaria c
JOIN (
    SELECT t.id_cuenta, SUM(t.monto) AS total
    FROM bitacora_transacciones t
    JOIN tipo_transaccion tt ON tt.id_tipo_transaccion = t.id_tipo_transaccion
    WHERE t.monto > @monto_max
      AND tt.descripcion IN (
          'DEPOSITO',
          'TRANSFERENCIA_DESTINO',
          'PAGO_VENTANILLA_INGRESO_EFECTIVO'
      )
    GROUP BY t.id_cuenta
) anom_pos ON anom_pos.id_cuenta = c.id_cuenta
SET c.saldo_actual = c.saldo_actual - anom_pos.total;

-- ---------------------------------------------------------------------
-- (2.b) Para los tipos que RESTAN del saldo del titular, sumamos el
--       monto anómalo de vuelta: retiros, transferencias enviadas,
--       pagos de servicios y transferencias hacia prestadoras.
-- ---------------------------------------------------------------------
UPDATE cuenta_bancaria c
JOIN (
    SELECT t.id_cuenta, SUM(t.monto) AS total
    FROM bitacora_transacciones t
    JOIN tipo_transaccion tt ON tt.id_tipo_transaccion = t.id_tipo_transaccion
    WHERE t.monto > @monto_max
      AND tt.descripcion IN (
          'RETIRO',
          'TRANSFERENCIA_ORIGEN',
          'PAGO_SERVICIO',
          'PAGO_VENTANILLA_TRANSFERENCIA_PRESTADORA'
      )
    GROUP BY t.id_cuenta
) anom_neg ON anom_neg.id_cuenta = c.id_cuenta
SET c.saldo_actual = c.saldo_actual + anom_neg.total;

-- ---------------------------------------------------------------------
-- (3) Borra los registros de pagos de servicios que apuntan a alguna
--     transacción anómala. Hay FK física (fk_registro_transaccion),
--     así que esto DEBE correr ANTES del DELETE de la bitácora.
-- ---------------------------------------------------------------------
DELETE rp
FROM registro_pagos_servicios rp
JOIN bitacora_transacciones t
     ON t.id_transaccion = rp.id_transaccion_origen
WHERE t.monto > @monto_max;

-- ---------------------------------------------------------------------
-- (4) Borra las transacciones anómalas de la bitácora.
--     Esto es lo que hace que el "Volumen mensual" del dashboard
--     vuelva a la normalidad sin tener que hacer wipe completo.
-- ---------------------------------------------------------------------
DELETE FROM bitacora_transacciones
WHERE monto > @monto_max;

-- ---------------------------------------------------------------------
-- (5) Verificación POSTERIOR. Si algún saldo se ve raro (negativo,
--     o aún muy alto), lanza ROLLBACK en vez de COMMIT.
-- ---------------------------------------------------------------------
SELECT
    c.id_cuenta,
    c.no_cuenta,
    cli.nombre,
    cli.apellido,
    c.saldo_actual AS saldo_despues
FROM cuenta_bancaria c
LEFT JOIN cliente cli ON cli.id_cliente = c.id_cliente
ORDER BY c.id_cuenta;

-- ---------------------------------------------------------------------
-- ¿Todo bien?                            ¿Algo se ve mal?
--   ↓                                       ↓
--   COMMIT;                                 ROLLBACK;
-- ---------------------------------------------------------------------
