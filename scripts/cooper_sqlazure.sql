
--Scripts to drop all tables
-------------------------------------------------------------------------------
DROP TABLE [dbo].[Cooper_AccountProfile]
DROP TABLE [dbo].[Cooper_AccountConnection]
DROP TABLE [dbo].[Cooper_Account]
DROP TABLE [dbo].[Cooper_Contact]
DROP TABLE [dbo].[Cooper_ContactGroup]
DROP TABLE [dbo].[Cooper_AddressBook]
DROP TABLE [dbo].[Cooper_Lock]
DROP TABLE [dbo].[Cooper_SyncInfo]
DROP TABLE [dbo].[Cooper_TaskProjectRelationship]
DROP TABLE [dbo].[Cooper_TaskComment]
DROP TABLE [dbo].[Cooper_Project]
DROP TABLE [dbo].[Cooper_Task]
DROP TABLE [dbo].[Cooper_Tasklist]
DROP TABLE [dbo].[Cooper_TeamMember]
DROP TABLE [dbo].[Cooper_Team]
-------------------------------------------------------------------------------

--Scripts to create all tables
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
	[Tags] [nvarchar](1000) NULL,
	[CreateTime] [datetime] NULL,
	[LastUpdateTime] [datetime] NULL,
	[CreatorAccountId] [int] NULL,
	[CreatorMemberId] [int] NULL,
	[TasklistId] [int] NULL,
	[TaskType] [nvarchar](255) NULL,
	[TeamId] [int] NULL,
	[AssigneeId] [int] NULL,
	[IsTrashed] [bit] NULL,
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
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--20120822 add new tables:
--       Cooper_AddressBook, Cooper_Contact, Cooper_ContactGroup
--       Cooper_Team, Cooper_TeamMember, Cooper_Project, Cooper_TaskProjectRelationship
CREATE TABLE [dbo].[Cooper_AddressBook](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NOT NULL,
    [ParentId] [int] NULL,
    [OwnerAccountId] [int] NULL,
    [AddressBookType] [nvarchar](10) NOT NULL,
    [CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Cooper_AddressBook_1] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_Contact](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [AccountId] [int] NULL,
    [AddressBookId] [int] NOT NULL,
    [GroupId] [int] NULL,
    [FullName] [nvarchar](255) NOT NULL,
    [Email] [nvarchar](255) NOT NULL,
    [Phone] [nvarchar](100) NULL,
    [CreateTime] [datetime] NOT NULL,
    [LastUpdateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Cooper_Contact_1] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_ContactGroup](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NOT NULL,
    [AddressBookId] [int] NOT NULL,
    [CreateTime] [datetime] NOT NULL,
 CONSTRAINT [PK_Cooper_ContactGroup_1] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_Team](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NULL,
    [Extensions] [nvarchar](max) NULL,
    [CreateTime] [datetime] NULL,
 CONSTRAINT [PK_Cooper_Team] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_TeamMember](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NULL,
    [Email] [nvarchar](255) NULL,
    [CreateTime] [datetime] NULL,
    [TeamId] [int] NULL,
    [AssociatedAccountId] [int] NULL,
    [MemberType] [nvarchar](255) NULL,
 CONSTRAINT [PK_Cooper_TeamMember] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_Project](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NULL,
    [IsPublic] [bit] NULL,
    [TeamId] [int] NULL,
    [CreateTime] [datetime] NULL,
    [Extensions] [nvarchar](max) NULL,
 CONSTRAINT [PK_Cooper_Project] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------
CREATE TABLE [dbo].[Cooper_TaskProjectRelationship](
    [TaskId] [bigint] NOT NULL,
    [ProjectId] [int] NOT NULL,
 CONSTRAINT [PK_Cooper_TaskProjectRelationship] PRIMARY KEY CLUSTERED 
(
    [TaskId] ASC,
    [ProjectId] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
-------------------------------------------------------------------------------

-------------------------------------------------------------------------------
--20120824 add new table Cooper_TaskComment
CREATE TABLE [dbo].[Cooper_TaskComment](
    [ID] [bigint] IDENTITY(1,1) NOT NULL,
    [Body] [nvarchar](max) NOT NULL,
    [CreateTime] [datetime] NOT NULL,
    [TaskId] [bigint] NULL,
    [CreatorId] [int] NULL,
 CONSTRAINT [PK_Cooper_TaskComment] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)
--------------------------------------------------------------------------------

--initialize table data
-------------------------------------------------------------------------------
--locks
insert into Cooper_Lock (id) values('Account')
insert into Cooper_Lock (id) values('AccountConnection')
insert into Cooper_Lock (id) values('ArkConnection')
insert into Cooper_Lock (id) values('GoogleConnection')
insert into Cooper_Lock (id) values('GitHubConnection')
insert into Cooper_Lock (id) values('Member')
-------------------------------------------------------------------------------