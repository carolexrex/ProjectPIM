SET NOCOUNT ON;
GO

DECLARE @Now datetime2 = SYSUTCDATETIME();

DECLARE @MarketSe uniqueidentifier = '20000000-0000-0000-0000-000000000001';
DECLARE @ExampleEntityId uniqueidentifier = '50000000-0000-0000-0000-000000000001';

DECLARE @TemplateLongDesc uniqueidentifier;
DECLARE @TemplateNameTranslate uniqueidentifier;
DECLARE @TemplateShortRewrite uniqueidentifier;

SELECT @TemplateLongDesc = Id
FROM dbo.AiPromptTemplate
WHERE TenantId IS NULL
  AND Code = N'PRODUCT_LONGDESC_GENERATE_V1';

SELECT @TemplateNameTranslate = Id
FROM dbo.AiPromptTemplate
WHERE TenantId IS NULL
  AND Code = N'PRODUCT_NAME_TRANSLATE_V1';

SELECT @TemplateShortRewrite = Id
FROM dbo.AiPromptTemplate
WHERE TenantId IS NULL
  AND Code = N'PRODUCT_SHORTDESC_REWRITE_V1';

IF @TemplateLongDesc IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.AiGenerationJob WHERE Id = '50000000-0000-0000-0000-000000000101')
BEGIN
    INSERT INTO dbo.AiGenerationJob (
        Id, TenantId, PromptTemplateId, Type, Status, RequestedBy, Provider, ModelName,
        SourceLanguage, TargetLanguage, StartedAtUtc, CompletedAtUtc, ErrorMessage,
        CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000101', NULL, @TemplateLongDesc, N'Generate', N'Completed',
        N'seed@example.local', N'OpenAI', N'gpt-5', NULL, N'sv-SE',
        DATEADD(second, -15, @Now), DATEADD(second, -5, @Now), NULL, DATEADD(second, -20, @Now), @Now
    );

    INSERT INTO dbo.AiGenerationJobItem (
        Id, AiGenerationJobId, EntityType, EntityId, FieldKey, CustomFieldDefinitionId, Capability,
        SourceCultureCode, TargetCultureCode, MarketId, InputPayload, InputHash, Status, ErrorMessage,
        CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000111', '50000000-0000-0000-0000-000000000101',
        N'ProductTranslation', @ExampleEntityId, N'LongDescription', NULL, N'Generate',
        NULL, N'sv-SE', @MarketSe,
        N'{"productNumber":"SKU-EXAMPLE-1","name":"Example Drill","brand":"Acme","attributes":{"power":"18V","use":"professional"}}',
        N'6F3F7FCE3FE0A8B2B58C6C3E4A611001', N'Completed', NULL, DATEADD(second, -20, @Now), @Now
    );

    INSERT INTO dbo.AiContentSuggestion (
        Id, TenantId, AiGenerationJobItemId, EntityType, EntityId, FieldKey, CustomFieldDefinitionId,
        Capability, CultureCode, MarketId, SourceValue, SuggestedValue, SuggestedJson, ConfidenceScore,
        Status, AcceptedAtUtc, AcceptedBy, RejectedAtUtc, RejectedBy, RejectionReason, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000121', NULL, '50000000-0000-0000-0000-000000000111',
        N'ProductTranslation', @ExampleEntityId, N'LongDescription', NULL, N'Generate', N'sv-SE', @MarketSe,
        NULL,
        N'Example Drill is a powerful 18V tool designed for demanding work. It combines reliable performance, practical handling, and a construction suited to repeated professional use.',
        NULL, CAST(0.9230 AS decimal(5,4)), N'Accepted',
        DATEADD(second, -2, @Now), N'admin@example.local', NULL, NULL, NULL, DATEADD(second, -10, @Now), @Now
    );

    INSERT INTO dbo.AiSuggestionReview (
        Id, AiContentSuggestionId, Action, ReviewedBy, Comment, CreatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000131',
        '50000000-0000-0000-0000-000000000121',
        N'Accepted',
        N'admin@example.local',
        N'Accepted as a good starting draft.',
        DATEADD(second, -2, @Now)
    );
END;

IF @TemplateNameTranslate IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.AiGenerationJob WHERE Id = '50000000-0000-0000-0000-000000000201')
BEGIN
    INSERT INTO dbo.AiGenerationJob (
        Id, TenantId, PromptTemplateId, Type, Status, RequestedBy, Provider, ModelName,
        SourceLanguage, TargetLanguage, StartedAtUtc, CompletedAtUtc, ErrorMessage,
        CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000201', NULL, @TemplateNameTranslate, N'Translate', N'Completed',
        N'seed@example.local', N'OpenAI', N'gpt-5', N'en-GB', N'sv-SE',
        DATEADD(second, -40, @Now), DATEADD(second, -32, @Now), NULL, DATEADD(second, -45, @Now), @Now
    );

    INSERT INTO dbo.AiGenerationJobItem (
        Id, AiGenerationJobId, EntityType, EntityId, FieldKey, CustomFieldDefinitionId, Capability,
        SourceCultureCode, TargetCultureCode, MarketId, InputPayload, InputHash, Status, ErrorMessage,
        CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000211', '50000000-0000-0000-0000-000000000201',
        N'ProductTranslation', @ExampleEntityId, N'Name', NULL, N'Translate',
        N'en-GB', N'sv-SE', @MarketSe,
        N'{"sourceValue":"Cordless Impact Driver"}',
        N'8A42BA1C93BCA7A57B2E4D8AABAF2002', N'Completed', NULL, DATEADD(second, -45, @Now), @Now
    );

    INSERT INTO dbo.AiContentSuggestion (
        Id, TenantId, AiGenerationJobItemId, EntityType, EntityId, FieldKey, CustomFieldDefinitionId,
        Capability, CultureCode, MarketId, SourceValue, SuggestedValue, SuggestedJson, ConfidenceScore,
        Status, AcceptedAtUtc, AcceptedBy, RejectedAtUtc, RejectedBy, RejectionReason, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000221', NULL, '50000000-0000-0000-0000-000000000211',
        N'ProductTranslation', @ExampleEntityId, N'Name', NULL, N'Translate', N'sv-SE', @MarketSe,
        N'Cordless Impact Driver',
        N'Sladdlos slagskruvdragare',
        NULL, CAST(0.8875 AS decimal(5,4)), N'Draft',
        NULL, NULL, NULL, NULL, NULL, DATEADD(second, -35, @Now), @Now
    );
END;

IF @TemplateShortRewrite IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM dbo.AiGenerationJob WHERE Id = '50000000-0000-0000-0000-000000000301')
BEGIN
    INSERT INTO dbo.AiGenerationJob (
        Id, TenantId, PromptTemplateId, Type, Status, RequestedBy, Provider, ModelName,
        SourceLanguage, TargetLanguage, StartedAtUtc, CompletedAtUtc, ErrorMessage,
        CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000301', NULL, @TemplateShortRewrite, N'Rewrite', N'Completed',
        N'seed@example.local', N'OpenAI', N'gpt-5', N'sv-SE', N'sv-SE',
        DATEADD(second, -70, @Now), DATEADD(second, -60, @Now), NULL, DATEADD(second, -75, @Now), @Now
    );

    INSERT INTO dbo.AiGenerationJobItem (
        Id, AiGenerationJobId, EntityType, EntityId, FieldKey, CustomFieldDefinitionId, Capability,
        SourceCultureCode, TargetCultureCode, MarketId, InputPayload, InputHash, Status, ErrorMessage,
        CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000311', '50000000-0000-0000-0000-000000000301',
        N'ProductTranslation', @ExampleEntityId, N'ShortDescription', NULL, N'Rewrite',
        N'sv-SE', N'sv-SE', @MarketSe,
        N'{"sourceValue":"Mycket bra maskin som ar valdigt kraftfull och bra for manga olika jobb."}',
        N'0D21FA8B320B6A45A55E4F112CD83003', N'Completed', NULL, DATEADD(second, -75, @Now), @Now
    );

    INSERT INTO dbo.AiContentSuggestion (
        Id, TenantId, AiGenerationJobItemId, EntityType, EntityId, FieldKey, CustomFieldDefinitionId,
        Capability, CultureCode, MarketId, SourceValue, SuggestedValue, SuggestedJson, ConfidenceScore,
        Status, AcceptedAtUtc, AcceptedBy, RejectedAtUtc, RejectedBy, RejectionReason, CreatedAtUtc, UpdatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000321', NULL, '50000000-0000-0000-0000-000000000311',
        N'ProductTranslation', @ExampleEntityId, N'ShortDescription', NULL, N'Rewrite', N'sv-SE', @MarketSe,
        N'Mycket bra maskin som ar valdigt kraftfull och bra for manga olika jobb.',
        N'En kraftfull maskin som passar for flera olika arbetsmoment.',
        NULL, CAST(0.7610 AS decimal(5,4)), N'Rejected',
        NULL, NULL, DATEADD(second, -55, @Now), N'editor@example.local', N'Too generic and removed too much meaning.',
        DATEADD(second, -65, @Now), @Now
    );

    INSERT INTO dbo.AiSuggestionReview (
        Id, AiContentSuggestionId, Action, ReviewedBy, Comment, CreatedAtUtc
    )
    VALUES (
        '50000000-0000-0000-0000-000000000331',
        '50000000-0000-0000-0000-000000000321',
        N'Rejected',
        N'editor@example.local',
        N'Rejected because the rewrite became too vague.',
        DATEADD(second, -55, @Now)
    );
END;
GO
