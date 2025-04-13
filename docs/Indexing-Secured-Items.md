# Including Secured Items in Lucene Indexing

## Overview

By default, the `DefaultLuceneClient` excludes secured items when rebuilding an index. However, secured items are included when edited, which creates inconsistent behavior. To address this, a new configuration option has been introduced to allow secured items to be included during index rebuilding.

## Configuration

To enable the inclusion of secured items during index rebuilding, add the following configuration to your `appsettings.json` file:

```json
"CMSLuceneSearch": {
  "IncludeSecuredItems": false
}
```

- **`IncludeSecuredItems`**: A boolean value that determines whether secured items should be included during index rebuilding. The default value is `false`.

When registering Lucene services in your application, pass the configuration to the `AddKenticoLucene` method:

```csharp
// Program.cs

builder.Services.AddKenticoLucene(builder.Configuration);
```

## Behavior

The following algorithm determines how secured items are treated during indexing:

- If the item is not secured, it is included in the index.
- If the item is secured and `IncludeSecuredItems` is set to `true`, it is included in the index.
- If the item is secured and `IncludeSecuredItems` is set to `false`, it is removed from the index.

With this setup, you can ensure consistent behavior for secured items during both index rebuilding and editing.