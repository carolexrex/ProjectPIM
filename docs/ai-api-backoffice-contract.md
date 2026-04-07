# AI API And Backoffice Contract v1

## Purpose

This document defines the first admin/backoffice contract for AI-assisted content workflows.

It covers:

- admin API endpoints
- request/response shapes
- backoffice actions
- state transitions
- how accepted suggestions become live content

This document is for the admin side of the platform, not storefront consumers.

## Scope

The AI workflow in v1 supports:

- generate content suggestions
- translate content suggestions
- rewrite content suggestions
- summarize content suggestions
- review suggestions in backoffice
- accept or reject suggestions

The AI workflow in v1 does not support:

- automatic publishing without review
- direct storefront use of model output
- agent-style autonomous catalog editing

## Main Objects

The API works primarily with:

- `AiPromptTemplate`
- `AiGenerationJob`
- `AiGenerationJobItem`
- `AiContentSuggestion`
- `AiSuggestionReview`

## Field Identity

Built-in fields should use stable field keys such as:

- `ProductTranslation.Name`
- `ProductTranslation.ShortDescription`
- `ProductTranslation.LongDescription`
- `ProductTranslation.SeoTitle`
- `ProductTranslation.SeoDescription`

Custom fields should use:

- `CustomField:{entityType}:{key}`

Examples:

- `CustomField:Product:marketingBadge`
- `CustomField:Variant:salesNote`

## API Surface

Base path:

- `/api/admin/ai`

Authentication:

- authenticated admin user or scoped integration client

Authorization:

- `PlatformAdmin`
- `CatalogManager`
- future fine-grained `AiContentManager`

## Prompt Templates

## List Templates

`GET /api/admin/ai/prompt-templates`

Query parameters:

- `entityType`
- `fieldKey`
- `capability`
- `cultureCode`
- `marketId`
- `isActive`

Response:

```json
{
  "items": [
    {
      "id": "40000000-0000-0000-0000-000000000001",
      "code": "PRODUCT_LONGDESC_GENERATE_V1",
      "name": "Product Long Description Generate",
      "entityType": "ProductTranslation",
      "fieldKey": "LongDescription",
      "capability": "Generate",
      "cultureCode": null,
      "marketId": null,
      "outputFormat": "Text",
      "modelName": null,
      "temperature": 0.4,
      "maxTokens": 900,
      "isActive": true
    }
  ],
  "total": 1
}
```

## Get Template

`GET /api/admin/ai/prompt-templates/{id}`

Returns the full template including prompt text and system instruction.

## Create Template

`POST /api/admin/ai/prompt-templates`

Request:

```json
{
  "code": "PRODUCT_LONGDESC_GENERATE_V2",
  "name": "Product Long Description Generate v2",
  "entityType": "ProductTranslation",
  "fieldKey": "LongDescription",
  "capability": "Generate",
  "cultureCode": null,
  "marketId": null,
  "promptText": "Write a clear product description based on the supplied product data.",
  "systemInstruction": "Be factual and concise.",
  "outputFormat": "Text",
  "modelName": "gpt-5",
  "temperature": 0.4,
  "maxTokens": 900,
  "isActive": true
}
```

## Update Template

`PUT /api/admin/ai/prompt-templates/{id}`

Same shape as create.

## Generate Jobs

## Create Single Generate Job

`POST /api/admin/ai/jobs`

This creates one AI job with one or more items.

Request:

```json
{
  "promptTemplateId": "40000000-0000-0000-0000-000000000001",
  "type": "Generate",
  "provider": "OpenAI",
  "modelName": "gpt-5",
  "requestedBy": "admin@example.local",
  "items": [
    {
      "entityType": "ProductTranslation",
      "entityId": "50000000-0000-0000-0000-000000000001",
      "fieldKey": "LongDescription",
      "capability": "Generate",
      "sourceCultureCode": null,
      "targetCultureCode": "sv-SE",
      "marketId": "20000000-0000-0000-0000-000000000001",
      "inputPayload": {
        "productNumber": "SKU-EXAMPLE-1",
        "name": "Example Drill",
        "brand": "Acme",
        "attributes": {
          "power": "18V"
        }
      }
    }
  ]
}
```

Response:

```json
{
  "jobId": "50000000-0000-0000-0000-000000000101",
  "status": "Pending",
  "itemCount": 1
}
```

## Create Bulk Generate Job

`POST /api/admin/ai/jobs/bulk`

Use for:

- many products
- many cultures
- mass description generation
- mass translation generation

Request:

```json
{
  "promptTemplateId": "40000000-0000-0000-0000-000000000006",
  "type": "BulkTranslate",
  "provider": "OpenAI",
  "modelName": "gpt-5",
  "requestedBy": "admin@example.local",
  "selection": {
    "entityType": "ProductTranslation",
    "entityIds": [
      "50000000-0000-0000-0000-000000000001",
      "50000000-0000-0000-0000-000000000002"
    ],
    "fieldKey": "LongDescription",
    "sourceCultureCode": "en-GB",
    "targetCultureCode": "sv-SE",
    "marketId": "20000000-0000-0000-0000-000000000001"
  }
}
```

## Get Job

`GET /api/admin/ai/jobs/{id}`

Response:

```json
{
  "id": "50000000-0000-0000-0000-000000000101",
  "type": "Generate",
  "status": "Completed",
  "requestedBy": "admin@example.local",
  "provider": "OpenAI",
  "modelName": "gpt-5",
  "createdAtUtc": "2026-03-11T12:00:00Z",
  "startedAtUtc": "2026-03-11T12:00:05Z",
  "completedAtUtc": "2026-03-11T12:00:15Z",
  "items": [
    {
      "id": "50000000-0000-0000-0000-000000000111",
      "entityType": "ProductTranslation",
      "entityId": "50000000-0000-0000-0000-000000000001",
      "fieldKey": "LongDescription",
      "capability": "Generate",
      "status": "Completed",
      "targetCultureCode": "sv-SE"
    }
  ]
}
```

## List Jobs

`GET /api/admin/ai/jobs`

Query parameters:

- `status`
- `type`
- `requestedBy`
- `createdFromUtc`
- `createdToUtc`

## Suggestions

## List Suggestions

`GET /api/admin/ai/suggestions`

Query parameters:

- `entityType`
- `entityId`
- `fieldKey`
- `status`
- `capability`
- `cultureCode`
- `marketId`
- `jobId`

Response:

```json
{
  "items": [
    {
      "id": "50000000-0000-0000-0000-000000000121",
      "entityType": "ProductTranslation",
      "entityId": "50000000-0000-0000-0000-000000000001",
      "fieldKey": "LongDescription",
      "capability": "Generate",
      "cultureCode": "sv-SE",
      "status": "Accepted",
      "confidenceScore": 0.923,
      "createdAtUtc": "2026-03-11T12:00:10Z"
    }
  ],
  "total": 1
}
```

## Get Suggestion

`GET /api/admin/ai/suggestions/{id}`

Response includes:

- source value
- suggested value
- job metadata
- template metadata
- review history

## Accept Suggestion

`POST /api/admin/ai/suggestions/{id}/accept`

Request:

```json
{
  "acceptedBy": "admin@example.local",
  "publishMode": "ApplyToLiveField",
  "comment": "Accepted after light review."
}
```

Rules:

- this copies suggestion content into the live target field
- this writes a review record
- this marks suggestion status as `Accepted`
- this must be idempotent

## Reject Suggestion

`POST /api/admin/ai/suggestions/{id}/reject`

Request:

```json
{
  "rejectedBy": "editor@example.local",
  "reason": "Too generic and not aligned with product facts."
}
```

Rules:

- suggestion status becomes `Rejected`
- live field is unchanged
- review record is created

## Edit Suggestion Before Accept

`POST /api/admin/ai/suggestions/{id}/edit`

Request:

```json
{
  "editedBy": "editor@example.local",
  "editedValue": "Updated text after manual review.",
  "comment": "Trimmed repetition and corrected terminology."
}
```

Rules:

- keeps trace of original suggestion
- updates editable working value
- review record action should be `Edited`

## Preview Apply

`POST /api/admin/ai/suggestions/{id}/preview-apply`

Purpose:

- show what field will change
- show old value vs new value
- validate field rules before actual accept

## Field Capability Metadata

## Get AI-Capable Fields

`GET /api/admin/ai/fields`

Response:

```json
{
  "items": [
    {
      "entityType": "ProductTranslation",
      "fieldKey": "LongDescription",
      "fieldType": "Text",
      "capabilities": ["Generate", "Rewrite", "Translate"],
      "isBuiltIn": true
    },
    {
      "entityType": "Product",
      "fieldKey": "CustomField:Product:marketingBadge",
      "fieldType": "Text",
      "capabilities": ["Generate", "Rewrite"],
      "isBuiltIn": false
    }
  ]
}
```

## Backoffice Contract

## Main Screens

V1 backoffice should have these AI-related screens:

1. template list
2. template editor
3. generation job list
4. generation job details
5. suggestion inbox
6. suggestion review panel
7. entity-level AI panel inside product edit views

## Product Edit AI Panel

For a product translation screen, the editor should be able to:

- choose field
- see allowed AI capabilities for that field
- choose template
- choose culture and market where relevant
- generate suggestion
- compare current value vs suggested value
- accept, reject, or edit

## Suggestion Inbox

Columns:

- created at
- entity type
- entity id
- field key
- capability
- culture
- market
- status
- confidence score
- requested by

Primary actions:

- open
- accept
- reject
- filter by field/culture/status

## Job Details Screen

Shows:

- template used
- provider/model
- who requested it
- status
- per-item results
- failure messages
- links to resulting suggestions

## State Model

## Job States

- `Pending`
- `Running`
- `Completed`
- `Failed`
- `Cancelled`

## Job Item States

- `Pending`
- `Running`
- `Completed`
- `Failed`
- `Skipped`

## Suggestion States

- `Draft`
- `Accepted`
- `Rejected`
- `Expired`

## Review Actions

- `Accepted`
- `Rejected`
- `Edited`
- `Published`

## Accept Flow

Recommended accept flow:

1. fetch suggestion
2. validate target field and permissions
3. copy suggested content into live target field
4. update suggestion status to `Accepted`
5. set `AcceptedAtUtc` and `AcceptedBy`
6. create `AiSuggestionReview`
7. emit domain/integration event

## Domain Events

Useful events:

- `AiGenerationJobCreated`
- `AiGenerationJobCompleted`
- `AiSuggestionCreated`
- `AiSuggestionAccepted`
- `AiSuggestionRejected`
- `AiSuggestionEdited`

## Validation Rules

1. `Capability` must be allowed for the target field.
2. Target field must belong to the specified entity type.
3. Translate operations should require source and target cultures unless template semantics define otherwise.
4. Accept must fail if the target entity no longer exists.
5. Accept should use optimistic concurrency where live content may have changed since suggestion creation.

## Security Rules

1. Only authorized admins may create jobs.
2. Only authorized admins may accept/reject suggestions.
3. Prompt templates should be editable only by privileged roles.
4. All AI operations should be audit logged.

## Recommended Next Step

After this contract, the next useful artifact is:

1. application service contracts in `.NET`
2. EF Core mappings for AI tables
3. product edit/backoffice UI wireframes for AI review
