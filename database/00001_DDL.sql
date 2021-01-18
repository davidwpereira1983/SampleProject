DROP TABLE IF EXISTS dbo.Product
GO

CREATE TABLE dbo.Product
(
	Id				uniqueidentifier    NOT NULL
,	Name			nvarchar(256)	    NOT NULL
,	CreatedOn		datetime		    NOT NULL DEFAULT GETUTCDATE()
,	CONSTRAINT PK_Product PRIMARY KEY(Id)
)
GO