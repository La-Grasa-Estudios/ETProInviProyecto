CREATE DATABASE IF NOT EXISTS GestionBienesETProDB;
USE GestionBienesETProDB;

CREATE TABLE IF NOT EXISTS Users (
    ID VARCHAR(450) PRIMARY KEY,
    UserName VARCHAR(100) NOT NULL UNIQUE,
    FullName VARCHAR(150) NULL,
    PasswordHash TEXT NOT NULL,
    DepartmentID INT NULL
);

CREATE TABLE IF NOT EXISTS Permissions (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL UNIQUE,
    Description VARCHAR(255),
    Category VARCHAR(50)
);

CREATE TABLE IF NOT EXISTS TemplatePermissions (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    Description VARCHAR(255),
    Editable TINYINT(1) NOT NULL DEFAULT 1
);

CREATE TABLE IF NOT EXISTS TemplatePermissionDetails (
    TemplateID INT NOT NULL,
    PermissionID INT NOT NULL,
    PRIMARY KEY (TemplateID, PermissionID),
    FOREIGN KEY (TemplateID) REFERENCES TemplatePermissions(Id) ON DELETE CASCADE,
    FOREIGN KEY (PermissionID) REFERENCES Permissions(ID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS UserPermission (
    UserID VARCHAR(450) NOT NULL,
    PermissionID INT NOT NULL,
    PRIMARY KEY (UserID, PermissionID),
    FOREIGN KEY (UserID) REFERENCES Users(ID) ON DELETE CASCADE,
    FOREIGN KEY (PermissionID) REFERENCES Permissions(ID) ON DELETE CASCADE
);

CREATE TABLE IF NOT EXISTS BienesMuebles (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    NumeroIdentificacion VARCHAR(50) NOT NULL,
    Nombre VARCHAR(100) NOT NULL,
    Marca VARCHAR(50) NULL,
    Modelo VARCHAR(50) NULL,
    `Serial` VARCHAR(100) NULL,        
    Color VARCHAR(50) NULL,
    Material VARCHAR(50) NULL,
    ObservacionesAdicionales VARCHAR(150) NULL,
    Grupo INT NOT NULL DEFAULT 2,
    DependenciaID INT NOT NULL,
    ValorUnitario DECIMAL(18,2) NOT NULL,
    Activo TINYINT(1) NOT NULL DEFAULT 1,
    Aprobado TINYINT(1) NOT NULL DEFAULT 0   
);

CREATE TABLE IF NOT EXISTS Departments (
    ID INT AUTO_INCREMENT PRIMARY KEY,
    Name VARCHAR(100) NOT NULL,
    ManagerId VARCHAR(450) NULL,
    CustodianId VARCHAR(450) NULL,
    CONSTRAINT FK_Departments_Manager FOREIGN KEY (ManagerId) REFERENCES Users(ID)
        ON DELETE SET NULL,
    CONSTRAINT FK_Departments_Custodian FOREIGN KEY (CustodianId) REFERENCES Users(ID)
        ON DELETE SET NULL
);

CREATE TABLE IF NOT EXISTS Movements (
    Id INT AUTO_INCREMENT PRIMARY KEY,
    BienId INT NOT NULL,
    Type INT NOT NULL, 
	OriginDepartmentId INT NULL,
    DestinationDepartmentId INT NULL,
    Motivo TEXT NOT NULL,
    FechaSolicitud DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
    Estado VARCHAR(50) NOT NULL DEFAULT 'Pendiente',
    UsuarioSolicitanteId VARCHAR(450) NOT NULL,
    UsuarioAprobadorId VARCHAR(450) NULL,
    FechaAprobacion DATETIME NULL,

    CONSTRAINT FK_Movements_Bien FOREIGN KEY (BienId) REFERENCES BienesMuebles(ID) ON DELETE RESTRICT,
    CONSTRAINT FK_Movements_OriginDept FOREIGN KEY (OriginDepartmentId) REFERENCES Departments(ID) ON DELETE RESTRICT,
    CONSTRAINT FK_Movements_DestDept FOREIGN KEY (DestinationDepartmentId) REFERENCES Departments(ID) ON DELETE RESTRICT,
    CONSTRAINT FK_Movements_Solicitante FOREIGN KEY (UsuarioSolicitanteId) REFERENCES Users(ID) ON DELETE RESTRICT,
    CONSTRAINT FK_Movements_Aprobador FOREIGN KEY (UsuarioAprobadorId) REFERENCES Users(ID) ON DELETE RESTRICT
);