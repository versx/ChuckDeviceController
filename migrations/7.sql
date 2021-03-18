CREATE TABLE `session` (
   `Id` varchar(449) NOT NULL,
   `AbsoluteExpiration` datetime(6) DEFAULT NULL,
   `ExpiresAtTime` datetime(6) NOT NULL,
   `SlidingExpirationInSeconds` bigint(20) DEFAULT NULL,
   `Value` longblob NOT NULL,
   PRIMARY KEY (`Id`),
   KEY `Index_ExpiresAtTime` (`ExpiresAtTime`)
);
