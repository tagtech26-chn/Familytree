/* SQL Server Express. Run after selecting FamilyConnectDB. */
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO
IF OBJECT_ID('dbo.UserAccounts','U') IS NULL
CREATE TABLE dbo.UserAccounts(
 UserId uniqueidentifier NOT NULL CONSTRAINT PK_UserAccounts PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
 MobileNumber nvarchar(20) NULL, NormalizedMobileNumber nvarchar(20) NULL,
 Email nvarchar(254) NULL, NormalizedEmail nvarchar(254) NULL,
 PasswordHash nvarchar(500) NOT NULL,
 PreferredLanguage char(2) NOT NULL CONSTRAINT DF_User_Language DEFAULT 'en',
 IsMobileVerified bit NOT NULL CONSTRAINT DF_User_MobileVerified DEFAULT 0,
 IsEmailVerified bit NOT NULL CONSTRAINT DF_User_EmailVerified DEFAULT 0,
 Status nvarchar(20) NOT NULL CONSTRAINT DF_User_Status DEFAULT 'Pending',
 CreatedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_User_Created DEFAULT SYSUTCDATETIME(),
 UpdatedAtUtc datetime2(0) NULL,
 CONSTRAINT CK_User_Language CHECK(PreferredLanguage IN ('en','ta')),
 CONSTRAINT CK_User_Status CHECK(Status IN ('Pending','Active','Suspended','Closed')),
 CONSTRAINT CK_User_Contact CHECK(MobileNumber IS NOT NULL OR Email IS NOT NULL)
);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_User_Mobile' AND object_id=OBJECT_ID('dbo.UserAccounts'))
 CREATE UNIQUE INDEX UX_User_Mobile ON dbo.UserAccounts(NormalizedMobileNumber) WHERE NormalizedMobileNumber IS NOT NULL;
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_User_Email' AND object_id=OBJECT_ID('dbo.UserAccounts'))
 CREATE UNIQUE INDEX UX_User_Email ON dbo.UserAccounts(NormalizedEmail) WHERE NormalizedEmail IS NOT NULL;
GO
IF OBJECT_ID('dbo.OtpChallenges','U') IS NULL
CREATE TABLE dbo.OtpChallenges(
 OtpChallengeId uniqueidentifier NOT NULL CONSTRAINT PK_OtpChallenges PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
 UserId uniqueidentifier NOT NULL, Destination nvarchar(254) NOT NULL, Purpose nvarchar(30) NOT NULL,
 CodeHash varbinary(48) NOT NULL, ExpiresAtUtc datetime2(0) NOT NULL, ConsumedAtUtc datetime2(0) NULL,
 FailedAttempts int NOT NULL CONSTRAINT DF_Otp_Failed DEFAULT 0,
 CreatedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Otp_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_Otp_User FOREIGN KEY(UserId) REFERENCES dbo.UserAccounts(UserId)
);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Otp_User_Destination' AND object_id=OBJECT_ID('dbo.OtpChallenges'))
 CREATE INDEX IX_Otp_User_Destination ON dbo.OtpChallenges(UserId,Destination,Purpose,CreatedAtUtc DESC);
GO
IF OBJECT_ID('dbo.RefreshTokens','U') IS NULL
CREATE TABLE dbo.RefreshTokens(
 RefreshTokenId uniqueidentifier NOT NULL CONSTRAINT PK_RefreshTokens PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
 UserId uniqueidentifier NOT NULL, TokenHash varbinary(32) NOT NULL,
 ExpiresAtUtc datetime2(0) NOT NULL,
 CreatedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Refresh_Created DEFAULT SYSUTCDATETIME(),
 RevokedAtUtc datetime2(0) NULL, ReplacedByTokenHash varbinary(32) NULL,
 CONSTRAINT FK_Refresh_User FOREIGN KEY(UserId) REFERENCES dbo.UserAccounts(UserId)
);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='UX_Refresh_TokenHash' AND object_id=OBJECT_ID('dbo.RefreshTokens'))
 CREATE UNIQUE INDEX UX_Refresh_TokenHash ON dbo.RefreshTokens(TokenHash);
GO
IF OBJECT_ID('dbo.FamilyGroups','U') IS NULL
CREATE TABLE dbo.FamilyGroups(
 FamilyGroupId uniqueidentifier NOT NULL CONSTRAINT PK_FamilyGroups PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
 GroupCode nvarchar(20) NOT NULL, GroupName nvarchar(150) NOT NULL, Description nvarchar(1000) NULL,
 CreatedByUserId uniqueidentifier NOT NULL,
 DefaultLanguage char(2) NOT NULL CONSTRAINT DF_Family_Language DEFAULT 'en',
 TimeZoneId nvarchar(100) NOT NULL CONSTRAINT DF_Family_TimeZone DEFAULT 'Asia/Kolkata',
 Status nvarchar(20) NOT NULL CONSTRAINT DF_Family_Status DEFAULT 'Active',
 CreatedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Family_Created DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_Family_Creator FOREIGN KEY(CreatedByUserId) REFERENCES dbo.UserAccounts(UserId),
 CONSTRAINT UQ_Family_GroupCode UNIQUE(GroupCode),
 CONSTRAINT CK_Family_Language CHECK(DefaultLanguage IN ('en','ta')),
 CONSTRAINT CK_Family_Status CHECK(Status IN ('Active','Suspended','Archived'))
);
GO
IF OBJECT_ID('dbo.FamilyGroupMemberships','U') IS NULL
CREATE TABLE dbo.FamilyGroupMemberships(
 MembershipId uniqueidentifier NOT NULL CONSTRAINT PK_FamilyGroupMemberships PRIMARY KEY DEFAULT NEWSEQUENTIALID(),
 FamilyGroupId uniqueidentifier NOT NULL, UserId uniqueidentifier NOT NULL,
 RoleCode nvarchar(30) NOT NULL, Status nvarchar(20) NOT NULL CONSTRAINT DF_Membership_Status DEFAULT 'Active',
 JoinedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_Membership_Joined DEFAULT SYSUTCDATETIME(),
 CONSTRAINT FK_Membership_Family FOREIGN KEY(FamilyGroupId) REFERENCES dbo.FamilyGroups(FamilyGroupId),
 CONSTRAINT FK_Membership_User FOREIGN KEY(UserId) REFERENCES dbo.UserAccounts(UserId),
 CONSTRAINT UQ_Membership_Family_User UNIQUE(FamilyGroupId,UserId),
 CONSTRAINT CK_Membership_Role CHECK(RoleCode IN ('Owner','Admin','Editor','Member')),
 CONSTRAINT CK_Membership_Status CHECK(Status IN ('Invited','Active','Suspended','Left'))
);
GO
IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE name='IX_Membership_User_Status' AND object_id=OBJECT_ID('dbo.FamilyGroupMemberships'))
 CREATE INDEX IX_Membership_User_Status ON dbo.FamilyGroupMemberships(UserId,Status) INCLUDE(FamilyGroupId,RoleCode);
GO
IF OBJECT_ID('dbo.SchemaVersions','U') IS NULL
CREATE TABLE dbo.SchemaVersions(
 VersionNumber int NOT NULL CONSTRAINT PK_SchemaVersions PRIMARY KEY,
 Description nvarchar(250) NOT NULL,
 AppliedAtUtc datetime2(0) NOT NULL CONSTRAINT DF_SchemaVersions_Applied DEFAULT SYSUTCDATETIME()
);
GO
IF NOT EXISTS(SELECT 1 FROM dbo.SchemaVersions WHERE VersionNumber=1)
 INSERT dbo.SchemaVersions(VersionNumber,Description) VALUES(1,N'Phase 1 identity, OTP, refresh tokens and family groups');
GO
