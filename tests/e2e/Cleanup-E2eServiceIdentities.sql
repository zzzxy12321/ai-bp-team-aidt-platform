SET NOCOUNT ON;
SET XACT_ABORT ON;

DECLARE @Suffix nvarchar(40) = N'$(E2eSuffix)';
DECLARE @UserPrefixLow nvarchar(120) = CONCAT(N'e2e.lowrisk.', @Suffix);
DECLARE @UserPrefixHigh nvarchar(120) = CONCAT(N'e2e.highrisk.', @Suffix);
DECLARE @Now datetime2(3) = SYSUTCDATETIME();

BEGIN TRANSACTION;

UPDATE profile
SET LifecycleStatus = N'Disabled',
    UpdatedAt = @Now
FROM [Agent].[AgentProfile] profile
JOIN [Identity].[User] targetUser ON targetUser.Id = profile.ServiceUserId
WHERE targetUser.UserName IN (@UserPrefixLow, @UserPrefixHigh);

UPDATE [Identity].[User]
SET IsActive = 0
WHERE UserName IN (@UserPrefixLow, @UserPrefixHigh);

COMMIT TRANSACTION;
