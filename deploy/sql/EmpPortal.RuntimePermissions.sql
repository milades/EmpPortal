:setvar ApplicationLogin "CORP\\EmpPortalGmsa$"

USE [EmpPortal];
GO

IF NOT EXISTS (SELECT 1 FROM sys.database_principals WHERE name = N'$(ApplicationLogin)')
BEGIN
    CREATE USER [$(ApplicationLogin)] FOR LOGIN [$(ApplicationLogin)];
END;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[identity] TO [$(ApplicationLogin)];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[security] TO [$(ApplicationLogin)];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[portal] TO [$(ApplicationLogin)];
GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::[forms] TO [$(ApplicationLogin)];
GRANT INSERT ON SCHEMA::[audit] TO [$(ApplicationLogin)];
GO
