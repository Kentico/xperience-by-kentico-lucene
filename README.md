# Xperience by Kentico Lucene

[![CI: Build and Test](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml)

[![NuGet Package](https://img.shields.io/nuget/v/Kentico.Xperience.Lucene.svg)](https://www.nuget.org/packages/Kentico.Xperience.Lucene)

## About The Project

Xperience by Kentico search integration with the latest 4.8 beta version of [Lucene.NET](https://github.com/apache/lucenenet),
enabling auto-indexing of content in Xperience based on application-local, code-defined search indexes and search results retrieval.

![Example search results](https://raw.githubusercontent.com/Kentico/xperience-by-kentico-lucene/main/images/dancing-goat-search-results.jpg)
![Example Xperience admin index viwe](https://raw.githubusercontent.com/Kentico/xperience-by-kentico-lucene/main/images/dancing-goat-lucene-index-admin.jpg)

## Getting Started

### Prerequisites

- Xperience by Kentico >= 26.2.0

### Installation

Add the package to your application using the .NET CLI

```powershell
dotnet add package Kentico.Xperience.Lucene
```

### Setup

1. Define a custom (or multiple) `LuceneSearchModel` implementation to represent the content you want index.
1. Define a custom `DefaultLuceneIndexingStrategy` implementation to customize how page content/fields are processed for the index.
1. Add this library to the application services, registering your custom `LuceneSearchModel`.

   ```csharp
   builder.Services.AddKentico();
   // ... other registrations
   builder.Services.AddLucene(new[]
   {
       new LuceneIndex(
           typeof(MySearchModel),
           new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
           MySearchModel.IndexName,
           indexPath: null,
           new MyCustomIndexingStrategy()),
   });
   ```

1. Rebuild the index in Xperience's Administration within the Lucene application added by this library.
1. Use the `ILuceneIndexService` (via DI) to retrieve the index populated by your custom `LuceneSearchModel`.
1. Execute a search with a customized Lucene `Query` (like the `MatchAllDocsQuery`) using the ILuceneIndexService.
1. Return or display the results on your site 👍.

## Usage

- Review the "Search" functionality in the `src\Kentico.Xperience.Lucene.Sample` Dancing Goat project to see how to implement search.
- Read the Lucene.NET [introduction](https://lucenenet.apache.org/) or [full documentation](https://lucenenet.apache.org/docs/4.8.0-beta00016/) to explore the core library's APIs and functionality.
- Explore the [Lucene.NET source on GitHub](https://github.com/apache/lucenenet)

## Sample features

### Trigger rebuild of index via webhook

Rebuild of index could be triggered by calling `POST` on webhook `/search/rebuild` with body

```json
{
  "indexName": "...",
  "secret": "..."
}
```

This could be used to trigger regular reindexing of content via CRON, Windows Task Scheduler or any other external scheduler.

## Contributing

For Contributing please see [`CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [`CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

### Requirements

- .NET SDK >= 7.0.109

  - <https://dotnet.microsoft.com/en-us/download/dotnet/7.0>

- Node.js >= 18.12

  - <https://nodejs.org/en/download>
  - <https://github.com/coreybutler/nvm-windows>

### Sample Project

To run the Sample app Admin customization in development mode, add the following to your [User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-7.0&tabs=windows#secret-manager) for the application.

```json
"CMSAdminClientModuleSettings": {
  "kentico-xperience-integrations-lucene": {
    "Mode": "Proxy",
    "Port": 3009
  }
}
```

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

This contribution has **Full support by 7-day bug-fix policy**.

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
