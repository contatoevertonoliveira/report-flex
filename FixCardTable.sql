USE CMS;
IF NOT EXISTS(SELECT 1 FROM sys.columns WHERE Name = N'StateID' AND Object_ID = Object_ID(N'dbo.Card'))
BEGIN
    ALTER TABLE dbo.Card ADD StateID int DEFAULT 0;
END
GO
UPDATE dbo.Card SET StateID = 0 WHERE StateID IS NULL;
GO
