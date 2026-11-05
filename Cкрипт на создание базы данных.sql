USE [master];
GO

USE [FleetDB];
GO

IF OBJECT_ID(N'dbo.Vehicles', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Vehicles
    (
        Id                     INT IDENTITY(1,1) NOT NULL,
        LicensePlate           NVARCHAR(12) NOT NULL,
        Model                  NVARCHAR(50) NOT NULL,
        Manufacturer           NVARCHAR(50) NOT NULL,
        YearOfManufacture      INT NOT NULL,
        Mileage                INT NOT NULL,
        LastMaintenanceMileage INT NOT NULL
            CONSTRAINT DF_Vehicles_LastMaintenanceMileage DEFAULT (0),
        LastMaintenanceDate    DATE NULL,
        Status                 NVARCHAR(20) NOT NULL
            CONSTRAINT DF_Vehicles_Status DEFAULT (N'Доступен'),

        CONSTRAINT PK_Vehicles PRIMARY KEY (Id),
        CONSTRAINT UQ_Vehicles_LicensePlate UNIQUE (LicensePlate),
        CONSTRAINT CK_Vehicles_Year CHECK (YearOfManufacture BETWEEN 1980 AND 2100),
        CONSTRAINT CK_Vehicles_Mileage CHECK (Mileage >= 0),
        CONSTRAINT CK_Vehicles_LastMaintenanceMileage
            CHECK (LastMaintenanceMileage >= 0 AND LastMaintenanceMileage <= Mileage),
        CONSTRAINT CK_Vehicles_Status
            CHECK (Status IN (N'Доступен', N'В рейсе', N'В ремонте'))
    );
END;
GO

IF COL_LENGTH(N'dbo.Vehicles', N'LastMaintenanceMileage') IS NULL
BEGIN
    ALTER TABLE dbo.Vehicles
    ADD LastMaintenanceMileage INT NOT NULL
        CONSTRAINT DF_Vehicles_LastMaintenanceMileage DEFAULT (0) WITH VALUES;
END;
GO

IF COL_LENGTH(N'dbo.Vehicles', N'LastMaintenanceDate') IS NULL
BEGIN
    ALTER TABLE dbo.Vehicles
    ADD LastMaintenanceDate DATE NULL;
    PRINT N'Добавлен столбец Vehicles.LastMaintenanceDate.';
END;
GO

IF OBJECT_ID(N'dbo.CK_Vehicles_LastMaintenanceMileage', N'C') IS NULL
BEGIN
    ALTER TABLE dbo.Vehicles
    ADD CONSTRAINT CK_Vehicles_LastMaintenanceMileage
        CHECK (LastMaintenanceMileage >= 0 AND LastMaintenanceMileage <= Mileage);
END;
GO


IF OBJECT_ID(N'dbo.Drivers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Drivers
    (
        Id            INT IDENTITY(1,1) NOT NULL,
        FullName      NVARCHAR(100) NOT NULL,
        LicenseNumber NVARCHAR(20) NOT NULL,
        PhoneNumber   NVARCHAR(20) NOT NULL,
        Experience    INT NOT NULL,
        Email         NVARCHAR(100) NOT NULL,
        Password      NVARCHAR(100) NOT NULL,

        CONSTRAINT PK_Drivers PRIMARY KEY (Id),
        CONSTRAINT UQ_Drivers_LicenseNumber UNIQUE (LicenseNumber),
        CONSTRAINT UQ_Drivers_Email UNIQUE (Email),
        CONSTRAINT CK_Drivers_Experience CHECK (Experience >= 0)
    );
END;
GO

IF OBJECT_ID(N'dbo.Administrators', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Administrators
    (
        Id       INT IDENTITY(1,1) NOT NULL,
        FullName NVARCHAR(100) NOT NULL,
        Email    NVARCHAR(100) NOT NULL,
        Password NVARCHAR(100) NOT NULL,

        CONSTRAINT PK_Administrators PRIMARY KEY (Id),
        CONSTRAINT UQ_Administrators_Email UNIQUE (Email)
    );
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.Administrators
    WHERE Email = N'Admin@manager.ru'
)
BEGIN
    INSERT INTO dbo.Administrators (FullName, Email, Password)
    VALUES
    (
        N'Администратор',
        N'Admin@manager.ru',
        N'weHdtX+UAoVWoUk1/hJEOLt6vX7GyAvxcqDGKjnwXko='
    );
END
ELSE
BEGIN
END;
GO

IF OBJECT_ID(N'dbo.Routes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Routes
    (
        Id            INT IDENTITY(1,1) NOT NULL,
        VehicleId     INT NOT NULL,
        DriverId      INT NOT NULL,
        StartDate     DATE NOT NULL,
        EndDate       DATE NOT NULL,
        StartLocation NVARCHAR(100) NOT NULL,
        EndLocation   NVARCHAR(100) NOT NULL,
        Distance      INT NOT NULL,

        CONSTRAINT PK_Routes PRIMARY KEY (Id),
        CONSTRAINT FK_Routes_Vehicles FOREIGN KEY (VehicleId)
            REFERENCES dbo.Vehicles (Id),
        CONSTRAINT FK_Routes_Drivers FOREIGN KEY (DriverId)
            REFERENCES dbo.Drivers (Id),
        CONSTRAINT CK_Routes_Dates CHECK (EndDate >= StartDate),
        CONSTRAINT CK_Routes_Distance CHECK (Distance > 0)
    );
END;
GO

SELECT N'Vehicles' AS TableName, COUNT(*) AS RowsCount FROM dbo.Vehicles
UNION ALL
SELECT N'Drivers', COUNT(*) FROM dbo.Drivers
UNION ALL
SELECT N'Administrators', COUNT(*) FROM dbo.Administrators
UNION ALL
SELECT N'Routes', COUNT(*) FROM dbo.Routes;
GO
