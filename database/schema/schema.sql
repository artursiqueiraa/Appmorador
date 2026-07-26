
/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `autorizacoes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `autorizacoes` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `MoradorResponsavelId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UnidadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VisitanteId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DataInicial` datetime(6) NOT NULL,
  `DataFinal` datetime(6) NOT NULL,
  `HorarioInicial` time(6) DEFAULT NULL,
  `HorarioFinal` time(6) DEFAULT NULL,
  `StatusManual` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Autorizacoes_MoradorResponsavelId` (`MoradorResponsavelId`),
  KEY `IX_Autorizacoes_UnidadeId` (`UnidadeId`),
  KEY `IX_Autorizacoes_VisitanteId` (`VisitanteId`),
  CONSTRAINT `FK_Autorizacoes_Moradores_MoradorResponsavelId` FOREIGN KEY (`MoradorResponsavelId`) REFERENCES `moradores` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Autorizacoes_Unidades_UnidadeId` FOREIGN KEY (`UnidadeId`) REFERENCES `unidades` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Autorizacoes_Visitantes_VisitanteId` FOREIGN KEY (`VisitanteId`) REFERENCES `visitantes` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `cameras`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `cameras` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `GravadorId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Canal` int NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Cameras_GravadorId` (`GravadorId`),
  KEY `IX_Cameras_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Cameras_Gravadores_GravadorId` FOREIGN KEY (`GravadorId`) REFERENCES `gravadores` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Cameras_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `centrais`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `centrais` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `NumeroSerie` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Centrais_NumeroSerie` (`NumeroSerie`),
  KEY `IX_Centrais_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Centrais_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `credenciais`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `credenciais` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `MoradorId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Credenciais_MoradorId` (`MoradorId`),
  CONSTRAINT `FK_Credenciais_Moradores_MoradorId` FOREIGN KEY (`MoradorId`) REFERENCES `moradores` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `entregas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `entregas` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `MoradorDestinatarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UnidadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `RecebidoPor` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `DataRecebimentoUtc` datetime(6) DEFAULT NULL,
  `DataRetiradaUtc` datetime(6) DEFAULT NULL,
  `Observacoes` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Status` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Entregas_MoradorDestinatarioId` (`MoradorDestinatarioId`),
  KEY `IX_Entregas_UnidadeId` (`UnidadeId`),
  CONSTRAINT `FK_Entregas_Moradores_MoradorDestinatarioId` FOREIGN KEY (`MoradorDestinatarioId`) REFERENCES `moradores` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_Entregas_Unidades_UnidadeId` FOREIGN KEY (`UnidadeId`) REFERENCES `unidades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `equipamentos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `equipamentos` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Modelo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Fabricante` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Ip` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Porta` int DEFAULT NULL,
  `Usuario` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `SenhaCriptografada` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Identificador` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Status` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UltimaSincronizacaoUtc` datetime(6) DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Equipamentos_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Equipamentos_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `eventosequipamento`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `eventosequipamento` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `EquipamentoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CodigoEventoOriginal` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `OcorridoEmUtc` datetime(6) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_EventosEquipamento_EquipamentoId` (`EquipamentoId`),
  CONSTRAINT `FK_EventosEquipamento_Equipamentos_EquipamentoId` FOREIGN KEY (`EquipamentoId`) REFERENCES `equipamentos` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `gravadores`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `gravadores` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Fabricante` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Ip` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Porta` int NOT NULL,
  `NomeAcesso` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Senha` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Gravadores_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Gravadores_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `historicocredenciais`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `historicocredenciais` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CredencialId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `TipoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HistoricoCredenciais_CredencialId` (`CredencialId`),
  CONSTRAINT `FK_HistoricoCredenciais_Credenciais_CredencialId` FOREIGN KEY (`CredencialId`) REFERENCES `credenciais` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `historicoentregas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `historicoentregas` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `EntregaId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `TipoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HistoricoEntregas_EntregaId` (`EntregaId`),
  CONSTRAINT `FK_HistoricoEntregas_Entregas_EntregaId` FOREIGN KEY (`EntregaId`) REFERENCES `entregas` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `historicovagas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `historicovagas` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VagaId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `TipoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HistoricoVagas_VagaId` (`VagaId`),
  CONSTRAINT `FK_HistoricoVagas_Vagas_VagaId` FOREIGN KEY (`VagaId`) REFERENCES `vagas` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `historicoveiculos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `historicoveiculos` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VeiculoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `TipoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HistoricoVeiculos_VeiculoId` (`VeiculoId`),
  CONSTRAINT `FK_HistoricoVeiculos_Veiculos_VeiculoId` FOREIGN KEY (`VeiculoId`) REFERENCES `veiculos` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `historicovisitantes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `historicovisitantes` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VisitanteId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `AutorizacaoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `TipoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Descricao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `UsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_HistoricoVisitantes_AutorizacaoId` (`AutorizacaoId`),
  KEY `IX_HistoricoVisitantes_VisitanteId` (`VisitanteId`),
  CONSTRAINT `FK_HistoricoVisitantes_Autorizacoes_AutorizacaoId` FOREIGN KEY (`AutorizacaoId`) REFERENCES `autorizacoes` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_HistoricoVisitantes_Visitantes_VisitanteId` FOREIGN KEY (`VisitanteId`) REFERENCES `visitantes` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `moradores`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `moradores` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UnidadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FotoPath` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Telefone` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Email` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Documento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Status` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Observacoes` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Moradores_UnidadeId` (`UnidadeId`),
  CONSTRAINT `FK_Moradores_Unidades_UnidadeId` FOREIGN KEY (`UnidadeId`) REFERENCES `unidades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `ocorrencias`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `ocorrencias` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `NumeroSeriePainel` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CodigoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ZonaOuUsuario` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Particao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `CentralId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `ZonaId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `StatusResolucao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ImagePath` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  KEY `IX_Ocorrencias_CentralId` (`CentralId`),
  KEY `IX_Ocorrencias_CreatedAtUtc` (`CreatedAtUtc`),
  KEY `IX_Ocorrencias_ZonaId_CreatedAtUtc` (`ZonaId`,`CreatedAtUtc`),
  KEY `IX_Ocorrencias_PropriedadeId_CreatedAtUtc` (`PropriedadeId`,`CreatedAtUtc`),
  CONSTRAINT `FK_Ocorrencias_Centrais_CentralId` FOREIGN KEY (`CentralId`) REFERENCES `centrais` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Ocorrencias_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Ocorrencias_Zonas_ZonaId` FOREIGN KEY (`ZonaId`) REFERENCES `zonas` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `permissoesacesso`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissoesacesso` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CredencialId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PontoAcessoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `DiasPermitidos` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `HorarioInicial` time(6) DEFAULT NULL,
  `HorarioFinal` time(6) DEFAULT NULL,
  `DataInicial` datetime(6) DEFAULT NULL,
  `DataFinal` datetime(6) DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PermissoesAcesso_CredencialId` (`CredencialId`),
  KEY `IX_PermissoesAcesso_PontoAcessoId` (`PontoAcessoId`),
  CONSTRAINT `FK_PermissoesAcesso_Credenciais_CredencialId` FOREIGN KEY (`CredencialId`) REFERENCES `credenciais` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_PermissoesAcesso_PontosAcesso_PontoAcessoId` FOREIGN KEY (`PontoAcessoId`) REFERENCES `pontosacesso` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `permissoesveiculares`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `permissoesveiculares` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VeiculoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PontoAcessoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PermissoesVeiculares_PontoAcessoId` (`PontoAcessoId`),
  KEY `IX_PermissoesVeiculares_VeiculoId` (`VeiculoId`),
  CONSTRAINT `FK_PermissoesVeiculares_PontosAcesso_PontoAcessoId` FOREIGN KEY (`PontoAcessoId`) REFERENCES `pontosacesso` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_PermissoesVeiculares_Veiculos_VeiculoId` FOREIGN KEY (`VeiculoId`) REFERENCES `veiculos` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `pontosacesso`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pontosacesso` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT (_utf8mb4'Geral'),
  PRIMARY KEY (`Id`),
  KEY `IX_PontosAcesso_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_PontosAcesso_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `propriedades`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `propriedades` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Endereco` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `ProprietarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT (_utf8mb4'Outro'),
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `Excluido` tinyint(1) NOT NULL DEFAULT '0',
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Propriedades_ProprietarioId` (`ProprietarioId`),
  CONSTRAINT `FK_Propriedades_Usuarios_ProprietarioId` FOREIGN KEY (`ProprietarioId`) REFERENCES `usuarios` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `refreshtokens`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `refreshtokens` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `UsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `TokenHash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ExpiresAtUtc` datetime(6) NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `RevokedAtUtc` datetime(6) DEFAULT NULL,
  `ReplacedByTokenHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_RefreshTokens_TokenHash` (`TokenHash`),
  KEY `IX_RefreshTokens_UsuarioId` (`UsuarioId`),
  CONSTRAINT `FK_RefreshTokens_Usuarios_UsuarioId` FOREIGN KEY (`UsuarioId`) REFERENCES `usuarios` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `registroseventoalarme`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `registroseventoalarme` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Payload` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NumeroSerie` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CodigoEvento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Zona` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Timestamp` datetime(6) NOT NULL,
  `ResultadoProcessamento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `snapshotsoperacionais`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `snapshotsoperacionais` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `GeradoEmUtc` datetime(6) NOT NULL,
  `Saude` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `QuantidadeEquipamentosOnline` int NOT NULL,
  `QuantidadeEquipamentosOffline` int NOT NULL,
  `UltimaComunicacaoUtc` datetime(6) DEFAULT NULL,
  `QuantidadeEventosHoje` int NOT NULL,
  `QuantidadeAlarmesAtivos` int NOT NULL,
  `QuantidadeFalhasDetectadas` int NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SnapshotsOperacionais_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_SnapshotsOperacionais_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `statuscentraisjfl`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `statuscentraisjfl` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `EquipamentoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CapturadoEmUtc` datetime(6) NOT NULL,
  `QuantidadeParticoesArmadas` int NOT NULL,
  `QuantidadeParticoesDesarmadas` int NOT NULL,
  `TemProblemaAtivo` tinyint(1) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_StatusCentraisJfl_EquipamentoId` (`EquipamentoId`),
  CONSTRAINT `FK_StatusCentraisJfl_Equipamentos_EquipamentoId` FOREIGN KEY (`EquipamentoId`) REFERENCES `equipamentos` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `unidades`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `unidades` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Identificacao` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Unidades_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Unidades_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `usuarios`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usuarios` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SenhaHash` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `TentativasFalhas` int NOT NULL,
  `BloqueadoAteUtc` datetime(6) DEFAULT NULL,
  `SecurityStamp` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Usuarios_Email` (`Email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `vagas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vagas` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Numero` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Bloco` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Andar` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Coberta` tinyint(1) NOT NULL,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `StatusManual` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Observacoes` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Vagas_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Vagas_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `veiculos`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `veiculos` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `MoradorId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Placa` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Marca` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Modelo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Cor` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Ano` int DEFAULT NULL,
  `Observacoes` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Tipo` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Veiculos_MoradorId` (`MoradorId`),
  CONSTRAINT `FK_Veiculos_Moradores_MoradorId` FOREIGN KEY (`MoradorId`) REFERENCES `moradores` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `vinculosveiculovaga`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vinculosveiculovaga` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VeiculoId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `VagaId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `DataInicioUtc` datetime(6) NOT NULL,
  `DataFimUtc` datetime(6) DEFAULT NULL,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_VinculosVeiculoVaga_VagaId` (`VagaId`),
  KEY `IX_VinculosVeiculoVaga_VeiculoId` (`VeiculoId`),
  CONSTRAINT `FK_VinculosVeiculoVaga_Vagas_VagaId` FOREIGN KEY (`VagaId`) REFERENCES `vagas` (`Id`) ON DELETE RESTRICT,
  CONSTRAINT `FK_VinculosVeiculoVaga_Veiculos_VeiculoId` FOREIGN KEY (`VeiculoId`) REFERENCES `veiculos` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `vinculoszonacamera`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `vinculoszonacamera` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `ZonaId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CameraId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_VinculosZonaCamera_CameraId` (`CameraId`),
  KEY `IX_VinculosZonaCamera_ZonaId` (`ZonaId`),
  CONSTRAINT `FK_VinculosZonaCamera_Cameras_CameraId` FOREIGN KEY (`CameraId`) REFERENCES `cameras` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_VinculosZonaCamera_Zonas_ZonaId` FOREIGN KEY (`ZonaId`) REFERENCES `zonas` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `visitantes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `visitantes` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `PropriedadeId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Documento` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Telefone` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `FotoPath` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `Observacoes` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAtUtc` datetime(6) NOT NULL,
  `Excluido` tinyint(1) NOT NULL,
  `DataExclusaoUtc` datetime(6) DEFAULT NULL,
  `ExcluidoPorUsuarioId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Visitantes_PropriedadeId` (`PropriedadeId`),
  CONSTRAINT `FK_Visitantes_Propriedades_PropriedadeId` FOREIGN KEY (`PropriedadeId`) REFERENCES `propriedades` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
DROP TABLE IF EXISTS `zonas`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `zonas` (
  `Id` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `CentralId` char(36) CHARACTER SET ascii COLLATE ascii_general_ci NOT NULL,
  `Numero` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Nome` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Zonas_CentralId_Numero` (`CentralId`,`Numero`),
  CONSTRAINT `FK_Zonas_Centrais_CentralId` FOREIGN KEY (`CentralId`) REFERENCES `centrais` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

