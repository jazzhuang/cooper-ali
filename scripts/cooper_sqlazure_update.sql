------------------------------------------------------------------------------
--20120911 Alter Table Cooper_Task
ALTER TABLE dbo.Cooper_Task ADD Tags nvarchar(1000) NULL
ALTER TABLE dbo.Cooper_Task ADD IsTrashed [bit] NULL
ALTER TABLE dbo.Cooper_Team ADD Extensions nvarchar(max) NULL
-------------------------------------------------------------------------------