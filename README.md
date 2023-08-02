# Xperience by Kentico Lucene

[![CI: Build and Test](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml)

## About The Project

Helps with indexing and searching data with Lucene .NET.

> Current version is PRE RELEASE !!!

![Example search results](./images/dancing-goat-search-results.jpg)
![Example Xperience admin index viwe](./images/dancing-goat-lucene-index-admin.jpg)

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

## Contributing

- .NET SDK >= 7.0.109

  - <https://dotnet.microsoft.com/en-us/download/dotnet/7.0>

- Node.js >= 18.12

  - <https://nodejs.org/en/download>
  - <https://github.com/coreybutler/nvm-windows>

For Contributing please see [`CONTRIBUTING.md`](https://github.com/Kentico/.github/blob/main/CONTRIBUTING.md) for more information and follow the [`CODE_OF_CONDUCT`](https://github.com/Kentico/.github/blob/main/CODE_OF_CONDUCT.md).

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

## Support

This contribution has **Full support by 7-day bug-fix policy**.

See [`SUPPORT.md`](https://github.com/Kentico/.github/blob/main/SUPPORT.md#full-support) for more information.

For any security issues see [`SECURITY.md`](https://github.com/Kentico/.github/blob/main/SECURITY.md).
