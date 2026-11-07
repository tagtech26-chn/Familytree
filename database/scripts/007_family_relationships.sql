SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.FamilyRelationships', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FamilyRelationships
    (
        FamilyRelationshipId UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_FamilyRelationships PRIMARY KEY,
        FamilyGroupId UNIQUEIDENTIFIER NOT NULL,
        FromFamilyMemberId UNIQUEIDENTIFIER NOT NULL,
        ToFamilyMemberId UNIQUEIDENTIFIER NOT NULL,
        RelationshipType NVARCHAR(20) NOT NULL,
        CreatedAtUtc DATETIME2(3) NOT NULL CONSTRAINT DF_FamilyRelationships_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
        CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
        CONSTRAINT FK_FamilyRelationships_FamilyGroups FOREIGN KEY (FamilyGroupId)
            REFERENCES dbo.FamilyGroups(FamilyGroupId),
        CONSTRAINT FK_FamilyRelationships_FromMember FOREIGN KEY (FromFamilyMemberId)
            REFERENCES dbo.FamilyMembers(FamilyMemberId),
        CONSTRAINT FK_FamilyRelationships_ToMember FOREIGN KEY (ToFamilyMemberId)
            REFERENCES dbo.FamilyMembers(FamilyMemberId),
        CONSTRAINT FK_FamilyRelationships_CreatedBy FOREIGN KEY (CreatedByUserId)
            REFERENCES dbo.UserAccounts(UserId),
        CONSTRAINT CK_FamilyRelationships_Type CHECK (RelationshipType IN ('ParentChild','Spouse')),
        CONSTRAINT CK_FamilyRelationships_NoSelf CHECK (FromFamilyMemberId <> ToFamilyMemberId)
    );

    CREATE UNIQUE INDEX UX_FamilyRelationships_Endpoints_Type
        ON dbo.FamilyRelationships(FamilyGroupId, FromFamilyMemberId, ToFamilyMemberId, RelationshipType);
    CREATE INDEX IX_FamilyRelationships_FamilyGroupId
        ON dbo.FamilyRelationships(FamilyGroupId);
END;

IF OBJECT_ID(N'dbo.SchemaVersions', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionNumber = 7)
BEGIN
    INSERT INTO dbo.SchemaVersions (VersionNumber, Description, AppliedAtUtc)
    VALUES (7, N'Family member relationships for parent-child and spouse links', SYSUTCDATETIME());
END;

COMMIT TRANSACTION;
GO
