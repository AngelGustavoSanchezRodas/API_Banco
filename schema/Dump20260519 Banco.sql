-- MySQL dump 10.13  Distrib 8.0.45, for Win64 (x86_64)
--
-- Host: servicio-de-pago.mysql.database.azure.com    Database: api_banco
-- ------------------------------------------------------
-- Server version	8.0.44-azure

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;

--
-- Table structure for table `bitacora_transacciones`
--

DROP TABLE IF EXISTS `bitacora_transacciones`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `bitacora_transacciones` (
  `id_transaccion` int NOT NULL AUTO_INCREMENT,
  `id_cuenta` int NOT NULL,
  `monto` decimal(15,2) NOT NULL,
  `descripcion` varchar(255) DEFAULT NULL,
  `fecha_transaccion` datetime DEFAULT CURRENT_TIMESTAMP,
  `id_tipo_transaccion` int NOT NULL,
  `referencia_vinculante` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id_transaccion`),
  KEY `idx_bitacora_cuenta` (`id_cuenta`),
  KEY `idx_bitacora_tipo` (`id_tipo_transaccion`),
  CONSTRAINT `fk_bitacora_cuenta` FOREIGN KEY (`id_cuenta`) REFERENCES `cuenta_bancaria` (`id_cuenta`),
  CONSTRAINT `fk_bitacora_tipo` FOREIGN KEY (`id_tipo_transaccion`) REFERENCES `tipo_transaccion` (`id_tipo_transaccion`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cliente`
--

DROP TABLE IF EXISTS `cliente`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cliente` (
  `id_cliente` int NOT NULL AUTO_INCREMENT,
  `dpi` varchar(20) NOT NULL,
  `nit` varchar(20) DEFAULT NULL,
  `nombre` varchar(100) NOT NULL,
  `apellido` varchar(100) NOT NULL,
  `telefono` varchar(20) DEFAULT NULL,
  `email` varchar(100) DEFAULT NULL,
  PRIMARY KEY (`id_cliente`),
  UNIQUE KEY `idx_cliente_dpi` (`dpi`),
  UNIQUE KEY `idx_cliente_email` (`email`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cuenta_bancaria`
--

DROP TABLE IF EXISTS `cuenta_bancaria`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cuenta_bancaria` (
  `id_cuenta` int NOT NULL AUTO_INCREMENT,
  `no_cuenta` varchar(20) NOT NULL,
  `id_cliente` int NOT NULL,
  `id_tipo_cuenta` int NOT NULL,
  `saldo_actual` decimal(15,2) NOT NULL DEFAULT '0.00',
  `id_estado` int DEFAULT '3',
  PRIMARY KEY (`id_cuenta`),
  UNIQUE KEY `idx_cuenta_no_cuenta` (`no_cuenta`),
  KEY `idx_cuenta_cliente` (`id_cliente`),
  KEY `idx_cuenta_estado` (`id_estado`),
  KEY `idx_cuenta_tipo` (`id_tipo_cuenta`),
  CONSTRAINT `fk_cuenta_cliente` FOREIGN KEY (`id_cliente`) REFERENCES `cliente` (`id_cliente`),
  CONSTRAINT `fk_cuenta_estado` FOREIGN KEY (`id_estado`) REFERENCES `estado` (`id_estado`),
  CONSTRAINT `fk_cuenta_tipo` FOREIGN KEY (`id_tipo_cuenta`) REFERENCES `tipo_cuenta` (`id_tipo_cuenta`)
) ENGINE=InnoDB AUTO_INCREMENT=7 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `cuenta_comision_banco`
--

DROP TABLE IF EXISTS `cuenta_comision_banco`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cuenta_comision_banco` (
  `id_comision_cuenta` int NOT NULL AUTO_INCREMENT,
  `nombre_cuenta` varchar(50) DEFAULT 'COMISIONES_PAGOS_SERVICIOS',
  `saldo_acumulado` decimal(15,2) DEFAULT '0.00',
  PRIMARY KEY (`id_comision_cuenta`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `estado`
--

DROP TABLE IF EXISTS `estado`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `estado` (
  `id_estado` int NOT NULL AUTO_INCREMENT,
  `descripcion` varchar(50) NOT NULL,
  PRIMARY KEY (`id_estado`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `registro_pagos_servicios`
--

DROP TABLE IF EXISTS `registro_pagos_servicios`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `registro_pagos_servicios` (
  `id_registro_pago` int NOT NULL AUTO_INCREMENT,
  `id_transaccion_origen` int NOT NULL,
  `entidad_servicio` enum('LUZ','TELEFONO','UNIVERSIDAD') NOT NULL,
  `identificador_servicio` varchar(50) NOT NULL,
  `monto_total_pagado` decimal(15,2) NOT NULL,
  `monto_empresa_95` decimal(15,2) NOT NULL,
  `comision_banco_5` decimal(15,2) NOT NULL,
  PRIMARY KEY (`id_registro_pago`),
  KEY `idx_registro_transaccion` (`id_transaccion_origen`),
  CONSTRAINT `fk_registro_transaccion` FOREIGN KEY (`id_transaccion_origen`) REFERENCES `bitacora_transacciones` (`id_transaccion`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tarjeta_credito`
--

DROP TABLE IF EXISTS `tarjeta_credito`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tarjeta_credito` (
  `id_tarjeta_credito` int NOT NULL AUTO_INCREMENT,
  `id_cliente` int NOT NULL,
  `no_tarjeta` varchar(16) NOT NULL,
  `pin_hash` varchar(255) NOT NULL,
  `limite_credito` decimal(15,2) NOT NULL,
  `saldo_consumido` decimal(15,2) NOT NULL DEFAULT '0.00',
  `fecha_vencimiento` date NOT NULL,
  `id_estado` int DEFAULT '1',
  PRIMARY KEY (`id_tarjeta_credito`),
  UNIQUE KEY `idx_tarjetac_no` (`no_tarjeta`),
  KEY `idx_tarjetac_cliente` (`id_cliente`),
  KEY `idx_tarjetac_estado` (`id_estado`),
  CONSTRAINT `fk_tarjetac_cliente` FOREIGN KEY (`id_cliente`) REFERENCES `cliente` (`id_cliente`),
  CONSTRAINT `fk_tarjetac_estado` FOREIGN KEY (`id_estado`) REFERENCES `estado` (`id_estado`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tarjeta_debito`
--

DROP TABLE IF EXISTS `tarjeta_debito`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tarjeta_debito` (
  `id_tarjeta` int NOT NULL AUTO_INCREMENT,
  `id_cuenta` int NOT NULL,
  `no_tarjeta` varchar(16) NOT NULL,
  `pin_hash` varchar(255) NOT NULL,
  `fecha_vencimiento` date NOT NULL,
  `id_estado` int DEFAULT '1',
  PRIMARY KEY (`id_tarjeta`),
  UNIQUE KEY `idx_tarjeta_no` (`no_tarjeta`),
  KEY `idx_tarjeta_cuenta` (`id_cuenta`),
  KEY `idx_tarjeta_estado` (`id_estado`),
  CONSTRAINT `fk_tarjeta_cuenta` FOREIGN KEY (`id_cuenta`) REFERENCES `cuenta_bancaria` (`id_cuenta`),
  CONSTRAINT `fk_tarjeta_estado` FOREIGN KEY (`id_estado`) REFERENCES `estado` (`id_estado`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tipo_cuenta`
--

DROP TABLE IF EXISTS `tipo_cuenta`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tipo_cuenta` (
  `id_tipo_cuenta` int NOT NULL AUTO_INCREMENT,
  `descripcion` varchar(50) NOT NULL,
  PRIMARY KEY (`id_tipo_cuenta`)
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `tipo_transaccion`
--

DROP TABLE IF EXISTS `tipo_transaccion`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `tipo_transaccion` (
  `id_tipo_transaccion` int NOT NULL AUTO_INCREMENT,
  `descripcion` varchar(100) NOT NULL,
  PRIMARY KEY (`id_tipo_transaccion`)
) ENGINE=InnoDB AUTO_INCREMENT=8 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Table structure for table `usuario_acceso`
--

DROP TABLE IF EXISTS `usuario_acceso`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuario_acceso` (
  `id_usuario` int NOT NULL AUTO_INCREMENT,
  `id_cliente` int DEFAULT NULL,
  `nombre_usuario` varchar(50) DEFAULT NULL,
  `correo_electronico` varchar(150) NOT NULL,
  `password_hash` varchar(255) NOT NULL,
  `rol` enum('ADMIN','CLIENTE') NOT NULL DEFAULT 'CLIENTE',
  `fecha_creacion` datetime DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (`id_usuario`),
  UNIQUE KEY `correo_electronico` (`correo_electronico`),
  UNIQUE KEY `idx_usuario_unico` (`nombre_usuario`),
  KEY `id_cliente` (`id_cliente`),
  CONSTRAINT `usuario_acceso_ibfk_1` FOREIGN KEY (`id_cliente`) REFERENCES `cliente` (`id_cliente`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;
