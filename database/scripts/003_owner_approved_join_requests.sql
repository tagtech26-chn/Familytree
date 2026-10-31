/* Phase 1, version 2: owner-approved family join requests.
   Run against FamilyConnectDB after migration 002. Existing rows are preserved. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.FamilyGroupMemberships','U') IS NULL
    THROW 50001, 'FamilyGroupMemberships table does not exist. Apply migration 002 first.', 1;

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE parent_object_id = OBJECT_ID('dbo.FamilyGroupMemberships')
             AND name = 'CK_Membership_Status')
    ALTER TABLE dbo.FamilyGroupMemberships DROP CONSTRAINT CK_Membership_Status;

ALTER TABLE dbo.FamilyGroupMemberships
    ADD CONSTRAINT CK_Membership_Status
    CHECK (Status IN ('Pending','Invited','Active','Suspended','Left'));

IF NOT EXISTS (SELECT 1 FROM dbo.SchemaVersions WHERE VersionNumber = 2)
    INSERT dbo.SchemaVersions(VersionNumber, Description)
    VALUES (2, N'Owner-approved family join requests and Pending membership status');

COMMIT TRANSACTION;
GO
