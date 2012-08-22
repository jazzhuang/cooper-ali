-------------------------------------------------------------------------------
--20120822
--1. Add new tables, including:
--       Cooper_AddressBook, Cooper_Contact, Cooper_ContactGroup
--       Cooper_Team, Cooper_TeamMember, Cooper_Project, Cooper_TaskProjectRelationship,
--2. Alter Cooper_Task table.

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

CREATE TABLE [dbo].[Cooper_Team](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NULL,
    [CreateTime] [datetime] NULL,
 CONSTRAINT [PK_Cooper_Team] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

CREATE TABLE [dbo].[Cooper_TeamMember](
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](255) NULL,
    [Email] [nvarchar](255) NULL,
    [CreateTime] [datetime] NULL,
    [TeamId] [int] NULL,
    [AssociatedAccountId] [int] NULL,
 CONSTRAINT [PK_Cooper_TeamMember] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX  = OFF, ALLOW_ROW_LOCKS  = ON, ALLOW_PAGE_LOCKS  = ON)
)

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