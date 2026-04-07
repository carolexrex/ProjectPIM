SET ANSI_NULLS ON;
GO
SET QUOTED_IDENTIFIER ON;
GO

IF COL_LENGTH('dbo.CustomFieldDefinition', 'AiCapability') IS NULL
BEGIN
    ALTER TABLE dbo.CustomFieldDefinition
    ADD AiCapability nvarchar(32) NOT NULL
        CONSTRAINT DF_CustomFieldDefinition_AiCapability DEFAULT (N'None');
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = 'CK_CustomFieldDefinition_AiCapability'
)
BEGIN
    ALTER TABLE dbo.CustomFieldDefinition
    ADD CONSTRAINT CK_CustomFieldDefinition_AiCapability
    CHECK (AiCapability IN (N'None', N'Generate', N'Translate', N'Rewrite', N'Summarize'));
END;
GO

IF OBJECT_ID(N'dbo.AiPromptTemplate', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiPromptTemplate (
        Id uniqueidentifier NOT NULL,
        TenantId uniqueidentifier NULL,
        Code nvarchar(64) NOT NULL,
        Name nvarchar(128) NOT NULL,
        EntityType nvarchar(64) NOT NULL,
        FieldKey nvarchar(128) NOT NULL,
        Capability nvarchar(32) NOT NULL,
        CultureCode nvarchar(16) NULL,
        MarketId uniqueidentifier NULL,
        PromptText nvarchar(max) NOT NULL,
        SystemInstruction nvarchar(max) NULL,
        OutputFormat nvarchar(32) NOT NULL,
        ModelName nvarchar(128) NULL,
        Temperature decimal(4,3) NULL,
        MaxTokens int NULL,
        IsActive bit NOT NULL CONSTRAINT DF_AiPromptTemplate_IsActive DEFAULT (1),
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AiPromptTemplate PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT UQ_AiPromptTemplate_Tenant_Code UNIQUE (TenantId, Code),
        CONSTRAINT CK_AiPromptTemplate_Capability CHECK (Capability IN (N'Generate', N'Translate', N'Rewrite', N'Summarize')),
        CONSTRAINT FK_AiPromptTemplate_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AiGenerationJob', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiGenerationJob (
        Id uniqueidentifier NOT NULL,
        TenantId uniqueidentifier NULL,
        PromptTemplateId uniqueidentifier NULL,
        Type nvarchar(32) NOT NULL,
        Status nvarchar(32) NOT NULL,
        RequestedBy nvarchar(128) NOT NULL,
        Provider nvarchar(64) NULL,
        ModelName nvarchar(128) NULL,
        SourceLanguage nvarchar(16) NULL,
        TargetLanguage nvarchar(16) NULL,
        StartedAtUtc datetime2 NULL,
        CompletedAtUtc datetime2 NULL,
        ErrorMessage nvarchar(max) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AiGenerationJob PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AiGenerationJob_Type CHECK (Type IN (N'Generate', N'Translate', N'Rewrite', N'Summarize', N'BulkGenerate', N'BulkTranslate')),
        CONSTRAINT CK_AiGenerationJob_Status CHECK (Status IN (N'Pending', N'Running', N'Completed', N'Failed', N'Cancelled')),
        CONSTRAINT FK_AiGenerationJob_PromptTemplate FOREIGN KEY (PromptTemplateId) REFERENCES dbo.AiPromptTemplate(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AiGenerationJobItem', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiGenerationJobItem (
        Id uniqueidentifier NOT NULL,
        AiGenerationJobId uniqueidentifier NOT NULL,
        EntityType nvarchar(64) NOT NULL,
        EntityId uniqueidentifier NOT NULL,
        FieldKey nvarchar(128) NOT NULL,
        CustomFieldDefinitionId uniqueidentifier NULL,
        Capability nvarchar(32) NOT NULL,
        SourceCultureCode nvarchar(16) NULL,
        TargetCultureCode nvarchar(16) NULL,
        MarketId uniqueidentifier NULL,
        InputPayload nvarchar(max) NULL,
        InputHash nvarchar(128) NULL,
        Status nvarchar(32) NOT NULL,
        ErrorMessage nvarchar(max) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_AiGenerationJobItem PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AiGenerationJobItem_Capability CHECK (Capability IN (N'Generate', N'Translate', N'Rewrite', N'Summarize')),
        CONSTRAINT CK_AiGenerationJobItem_Status CHECK (Status IN (N'Pending', N'Running', N'Completed', N'Failed', N'Skipped')),
        CONSTRAINT FK_AiGenerationJobItem_AiGenerationJob FOREIGN KEY (AiGenerationJobId) REFERENCES dbo.AiGenerationJob(Id),
        CONSTRAINT FK_AiGenerationJobItem_CustomFieldDefinition FOREIGN KEY (CustomFieldDefinitionId) REFERENCES dbo.CustomFieldDefinition(Id),
        CONSTRAINT FK_AiGenerationJobItem_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AiContentSuggestion', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiContentSuggestion (
        Id uniqueidentifier NOT NULL,
        TenantId uniqueidentifier NULL,
        AiGenerationJobItemId uniqueidentifier NOT NULL,
        EntityType nvarchar(64) NOT NULL,
        EntityId uniqueidentifier NOT NULL,
        FieldKey nvarchar(128) NOT NULL,
        CustomFieldDefinitionId uniqueidentifier NULL,
        Capability nvarchar(32) NOT NULL,
        CultureCode nvarchar(16) NULL,
        MarketId uniqueidentifier NULL,
        SourceValue nvarchar(max) NULL,
        SuggestedValue nvarchar(max) NOT NULL,
        SuggestedJson nvarchar(max) NULL,
        ConfidenceScore decimal(5,4) NULL,
        Status nvarchar(32) NOT NULL,
        AcceptedAtUtc datetime2 NULL,
        AcceptedBy nvarchar(128) NULL,
        RejectedAtUtc datetime2 NULL,
        RejectedBy nvarchar(128) NULL,
        RejectionReason nvarchar(512) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        UpdatedAtUtc datetime2 NOT NULL,
        RowVersion rowversion NOT NULL,
        CONSTRAINT PK_AiContentSuggestion PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AiContentSuggestion_Capability CHECK (Capability IN (N'Generate', N'Translate', N'Rewrite', N'Summarize')),
        CONSTRAINT CK_AiContentSuggestion_Status CHECK (Status IN (N'Draft', N'Accepted', N'Rejected', N'Expired')),
        CONSTRAINT FK_AiContentSuggestion_AiGenerationJobItem FOREIGN KEY (AiGenerationJobItemId) REFERENCES dbo.AiGenerationJobItem(Id),
        CONSTRAINT FK_AiContentSuggestion_CustomFieldDefinition FOREIGN KEY (CustomFieldDefinitionId) REFERENCES dbo.CustomFieldDefinition(Id),
        CONSTRAINT FK_AiContentSuggestion_Market FOREIGN KEY (MarketId) REFERENCES dbo.Market(Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.AiSuggestionReview', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AiSuggestionReview (
        Id uniqueidentifier NOT NULL,
        AiContentSuggestionId uniqueidentifier NOT NULL,
        Action nvarchar(32) NOT NULL,
        ReviewedBy nvarchar(128) NOT NULL,
        Comment nvarchar(512) NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT PK_AiSuggestionReview PRIMARY KEY CLUSTERED (Id),
        CONSTRAINT CK_AiSuggestionReview_Action CHECK (Action IN (N'Accepted', N'Rejected', N'Edited', N'Published')),
        CONSTRAINT FK_AiSuggestionReview_AiContentSuggestion FOREIGN KEY (AiContentSuggestionId) REFERENCES dbo.AiContentSuggestion(Id)
    );
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiPromptTemplate_EntityType_FieldKey_Capability' AND object_id = OBJECT_ID(N'dbo.AiPromptTemplate'))
BEGIN
    CREATE INDEX IX_AiPromptTemplate_EntityType_FieldKey_Capability
    ON dbo.AiPromptTemplate (EntityType, FieldKey, Capability);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiPromptTemplate_MarketId_CultureCode' AND object_id = OBJECT_ID(N'dbo.AiPromptTemplate'))
BEGIN
    CREATE INDEX IX_AiPromptTemplate_MarketId_CultureCode
    ON dbo.AiPromptTemplate (MarketId, CultureCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiGenerationJob_Status_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.AiGenerationJob'))
BEGIN
    CREATE INDEX IX_AiGenerationJob_Status_CreatedAtUtc
    ON dbo.AiGenerationJob (Status, CreatedAtUtc);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiGenerationJob_PromptTemplateId' AND object_id = OBJECT_ID(N'dbo.AiGenerationJob'))
BEGIN
    CREATE INDEX IX_AiGenerationJob_PromptTemplateId
    ON dbo.AiGenerationJob (PromptTemplateId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiGenerationJobItem_Job_Status' AND object_id = OBJECT_ID(N'dbo.AiGenerationJobItem'))
BEGIN
    CREATE INDEX IX_AiGenerationJobItem_Job_Status
    ON dbo.AiGenerationJobItem (AiGenerationJobId, Status);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiGenerationJobItem_Entity_Field_Culture' AND object_id = OBJECT_ID(N'dbo.AiGenerationJobItem'))
BEGIN
    CREATE INDEX IX_AiGenerationJobItem_Entity_Field_Culture
    ON dbo.AiGenerationJobItem (EntityType, EntityId, FieldKey, TargetCultureCode);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiContentSuggestion_Entity_Field_Status' AND object_id = OBJECT_ID(N'dbo.AiContentSuggestion'))
BEGIN
    CREATE INDEX IX_AiContentSuggestion_Entity_Field_Status
    ON dbo.AiContentSuggestion (EntityType, EntityId, FieldKey, Status);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiContentSuggestion_JobItem' AND object_id = OBJECT_ID(N'dbo.AiContentSuggestion'))
BEGIN
    CREATE INDEX IX_AiContentSuggestion_JobItem
    ON dbo.AiContentSuggestion (AiGenerationJobItemId);
END;
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_AiSuggestionReview_Suggestion_CreatedAtUtc' AND object_id = OBJECT_ID(N'dbo.AiSuggestionReview'))
BEGIN
    CREATE INDEX IX_AiSuggestionReview_Suggestion_CreatedAtUtc
    ON dbo.AiSuggestionReview (AiContentSuggestionId, CreatedAtUtc);
END;
GO
