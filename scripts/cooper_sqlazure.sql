--sql on sqlazure
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_Account](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NULL,
	[Password] [nvarchar](255) NULL,
	[CreateTime] [datetime] NULL
 CONSTRAINT [PK_Cooper_Account] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_Lock](
	[ID] [nvarchar](50) NOT NULL,
 CONSTRAINT [PK_Cooper_Lock] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_AccountProfile](
	[AccountId] [int] NOT NULL,
	[Profile] [nvarchar](max) NULL,
 CONSTRAINT [PK_Cooper_AccountProfile] PRIMARY KEY CLUSTERED 
(
	[AccountId] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_Task](
	[ID] [bigint] IDENTITY(1,1) NOT NULL,
	[Subject] [nvarchar](500) NULL,
	[Body] [nvarchar](max) NULL,
	[Priority] [tinyint] NULL,
	[DueTime] [datetime] NULL,
	[IsCompleted] [bit] NULL,
	[CreateTime] [datetime] NULL,
	[LastUpdateTime] [datetime] NULL,
	[CreatorAccountId] [int] NULL,
	[AssignedContacterId] [int] NULL,
 CONSTRAINT [PK_Cooper_Task] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_AccountConnection](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](255) NULL,
	[AccountId] [int] NULL,
	[CreateTime] [datetime] NULL,
	[ConnectionType] [nvarchar](10) NULL,
	[Token] [nvarchar](1000) NULL,
 CONSTRAINT [PK_Cooper_AccountConnection] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_SyncInfo](
	[AccountId] [int] NOT NULL,
	[LocalDataId] [nvarchar](10) NOT NULL,
	[SyncDataId] [nvarchar](300) NOT NULL,
	[SyncDataType] [int] NOT NULL,
 CONSTRAINT [PK_Cooper_SyncInfo] PRIMARY KEY CLUSTERED 
(
	[AccountId] ASC,
	[LocalDataId] ASC,
	[SyncDataId] ASC,
	[SyncDataType] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

-------------------------------------------------------------------------------
--initialize
--locks
insert into Cooper_Lock (id) values('Account')
insert into Cooper_Lock (id) values('AccountConnection')
insert into Cooper_Lock (id) values('ArkConnection')
insert into Cooper_Lock (id) values('GoogleConnection')
insert into Cooper_Lock (id) values('GitHubConnection')

-------------------------------------------------------------------------------
--changes
-------------------------------------------------------------------------------
--20120723 add tasklist
CREATE TABLE [dbo].[Cooper_Tasklist](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](50) NULL,
	[CreateTime] [datetime] NULL,
	[Extensions] [nvarchar](max) NULL,
	[ListType] [nvarchar](10) NULL,
	[OwnerAccountId] [int] NULL,
 CONSTRAINT [PK_Cooper_Tasklist] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

ALTER TABLE dbo.Cooper_Task ADD TasklistId int NULL
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--20120816
--1. Add new tables, including:
--       Cooper_AddressBook, Cooper_Contact, Cooper_ContactGroup
--       Cooper_Team, Cooper_TeamMember, Cooper_Project, Cooper_TaskProjectRelationship,
--2. Alter Cooper_Task table.

CREATE TABLE [dbo].[Cooper_AddressBook](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NOT NULL,
	[ParentId] [int] NULL,
	[OwnerAccountId] [int] NULL,
	[AddressBookType] [nvarchar](10) NOT NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Cooper_AddressBook_1] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE TABLE [dbo].[Cooper_Contact](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[AccountId] [int] NULL,
	[AddressBookId] [int] NOT NULL,
	[GroupId] [int] NULL,
	[FullName] [nvarchar](200) NOT NULL,
	[Email] [nvarchar](100) NOT NULL,
	[Phone] [nvarchar](100) NULL,
	[CreateTime] [datetime] NOT NULL,
	[LastUpdateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Cooper_Contact_1] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE TABLE [dbo].[Cooper_ContactGroup](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NOT NULL,
	[AddressBookId] [int] NOT NULL,
	[CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Cooper_ContactGroup_1] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE TABLE [dbo].[Cooper_Team](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](200) NULL,
	[CreateTime] [datetime] NULL,
 CONSTRAINT [PK_Cooper_Team] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE TABLE [dbo].[Cooper_TeamMember](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NULL,
	[Email] [nvarchar](100) NULL,
	[CreateTime] [datetime] NULL,
	[TeamId] [int] NULL,
 CONSTRAINT [PK_Cooper_TeamMember] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE TABLE [dbo].[Cooper_Project](
	[ID] [int] IDENTITY(1,1) NOT NULL,
	[Name] [nvarchar](100) NULL,
	[IsPublic] [bit] NULL,
	[TeamId] [int] NULL,
	[CreateTime] [datetime] NULL,
	[Extensions] [nvarchar](max) NULL,
 CONSTRAINT [PK_Cooper_Project] PRIMARY KEY CLUSTERED 
(
	[ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
CREATE TABLE [dbo].[Cooper_TaskProjectRelationship](
	[TaskId] [int] NOT NULL,
	[ProjectId] [int] NOT NULL,
 CONSTRAINT [PK_Cooper_TaskProjectRelationship] PRIMARY KEY CLUSTERED 
(
	[TaskId] ASC,
	[ProjectId] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

ALTER TABLE dbo.Cooper_Task ADD TaskType nvarchar(10) NULL
ALTER TABLE dbo.Cooper_Task ADD TeamId int NULL
ALTER TABLE dbo.Cooper_Task ADD AssigneeId int NULL
ALTER TABLE dbo.Cooper_Task DROP COLUMN AssignedContacterId
-------------------------------------------------------------------------------