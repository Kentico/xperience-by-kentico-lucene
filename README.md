# Xperience by Kentico Lucene

[![CI: Build and Test](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml) [![NuGet Package](https://img.shields.io/nuget/v/Kentico.Xperience.Lucene.svg)](https://www.nuget.org/packages/Kentico.Xperience.Lucene)

## Description

Xperience by Kentico search integration with the latest 4.8 beta version of [Lucene.NET](https://github.com/apache/lucenenet),
enabling auto-indexing of content in Xperience based on application-local, code-defined search indexes and search results retrieval.

## Screenshots

<a href="https://raw.githubusercontent.com/Kentico/xperience-by-kentico-lucene/main/images/dancing-goat-search-results.jpg">
  <img src="https://raw.githubusercontent.com/Kentico/xperience-by-kentico-lucene/main/images/dancing-goat-search-results.jpg" width="600" alt="Example search results">
</a>

<a href="https://raw.githubusercontent.com/Kentico/xperience-by-kentico-lucene/main/images/dancing-goat-lucene-index-admin.jpg">
  <img src="https://raw.githubusercontent.com/Kentico/xperience-by-kentico-lucene/main/images/dancing-goat-lucene-index-admin.jpg" width="600" alt="Example Xperience admin index view">
</a>

## Dependencies

- [ASP.NET Core 6.0](https://dotnet.microsoft.com/en-us/download)
- [Xperience by Kentico](https://docs.xperience.io/xp/changelog)

### Library Support

| Xperience Version   | Library Version   |
| ------------------- | ----------------- |
| >= 26.2.0, < 27.0.0 | 2.x               |
| >= 27.0.0           | Not yet supported |

## Package Installation

Add the package to your application using the .NET CLI

```powershell
dotnet add package Kentico.Xperience.Lucene
```

## Quick Start

1. Define a custom (or multiple) `LuceneSearchModel` implementation to represent the content you want index.
1. Define a custom `DefaultLuceneIndexingStrategy` implementation to customize how page content/fields are processed for the index.
1. Add this library to the application services, registering your custom `LuceneSearchModel`, using `builder.Services.AddLucene()`

   ```csharp
   // Program.cs

   var builder = WebApplication.CreateBuilder(args);

   // ...

   builder.Services.AddLucene(new[]
   {
       new LuceneIndex(
           typeof(MySearchModel),
           new StandardAnalyzer(
              Lucene.Net.Util.LuceneVersion.LUCENE_48),
           MySearchModel.IndexName,
           indexPath: null,
           new MyCustomIndexingStrategy()),
   });
   ```

1. Rebuild the index in Xperience's Administration within the Lucene application added by this library.
1. Use the `ILuceneIndexService` (via DI) to retrieve the index populated by your custom `LuceneSearchModel`.
1. Execute a search with a customized Lucene `Query` (like the `MatchAllDocsQuery`) using the `ILuceneIndexService`.
1. Display the results on your site with a Razor View 👍.

## Full Instructions

View the [Usage Guide](./docs/Usage-Guide.md) for more detailed instructions.

## Contributing

To see the guidelines for Contributing to Kentico open source software, please see [`CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [`CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

Instructions and technical details for contributing to **this** product can be found in [Contributing Setup](./docs/Contributing-Setup.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

This contribution has **Full support by 7-day bug-fix policy**.

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

## Security

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
