SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.FamilyMembers', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyMembers
    (
        FamilyMemberId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FamilyMembers PRIMARY KEY,
        FamilyGroupId UNIQUEIDENTIFIER NOT NULL,
        LinkedUserId UNIQUEIDENTIFIER NULL,
        FullName NVARCHAR(200) NOT NULL,
        TamilName NVARCHAR(200) NULL,
        Gender NVARCHAR(20) NULL,
        DateOfBirth DATE NULL,
        PlaceOfBirth NVARCHAR(150) NULL,
        DateOfDeath DATE NULL,
        ContactNumber NVARCHAR(30) NULL,
        Email NVARCHAR(254) NULL,
        Status NVARCHAR(20) NOT NULL CONSTRAINT DF_FamilyMembers_Status DEFAULT ('Active'),
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_FamilyMembers_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_FamilyMembers_UpdatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CONSTRAINT FK_FamilyMembers_FamilyGroups FOREIGN KEY (FamilyGroupId)
            REFERENCES dbo.FamilyGroups(FamilyGroupId),
        CONSTRAINT FK_FamilyMembers_UserAccounts FOREIGN KEY (LinkedUserId)
            REFERENCES dbo.UserAccounts(UserId),
        CONSTRAINT CK_FamilyMembers_Gender CHECK (Gender IS NULL OR Gender IN ('Male','Female','Other','PreferNotToSay')),
        CONSTRAINT CK_FamilyMembers_Status CHECK (Status IN ('Active','Deceased','Archived')),
        CONSTRAINT CK_FamilyMembers_Dates CHECK (DateOfBirth IS NULL OR DateOfDeath IS NULL OR DateOfDeath >= DateOfBirth)
    );

    CREATE INDEX IX_FamilyMembers_FamilyGroupId_Status_FullName
        ON dbo.FamilyMembers(FamilyGroupId, Status, FullName);
    CREATE UNIQUE INDEX UX_FamilyMembers_FamilyGroupId_LinkedUserId
        ON dbo.FamilyMembers(FamilyGroupId, LinkedUserId)
        WHERE LinkedUserId IS NOT NULL AND Status <> 'Archived';
END;

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionNumber = 6)
BEGIN
    INSERT INTO dbo.SchemaVersions (VersionNumber, Description, AppliedAtUtc)
    VALUES (6, N'Family member profiles and role-based profile permissions', SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
GO