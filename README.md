# Xperience by Kentico Lucene

[![CI: Build and Test](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/Kentico/xperience-by-kentico-lucene/actions/workflows/ci.yml)

## About The Project

Helps with indexing and searching data with Lucene .NET.

> Current version is PRE RELEASE !!!

## Getting Started

### Prerequisites

- Xperience by Kentico >= 26.2.0

### Installation

Add the package to your application using the .NET CLI

```powershell
dotnet add package Kentico.Xperience.Lucene
```

### Add to your application dependencies

```csharp
builder.Services.AddKentico();
// ... other registrations
builder.Services.AddLucene(builder.Configuration, new[]
{
     // use your own index definition
     new LuceneIndex(
        typeof(KBankNewsSearchModel),
        new StandardAnalyzer(Lucene.Net.Util.LuceneVersion.LUCENE_48),
        KBankNewsSearchModel.IndexName,
        luceneIndexingStrategy: new KBankNewsLuceneIndexingStrategy()
     )
});
```

## Usage

## Contributing

- .NET SDK >= 7.0.109

  - <https://dotnet.microsoft.com/en-us/download/dotnet/7.0>

- Node.js >= 18.12

  - <https://nodejs.org/en/download>
  - <https://github.com/coreybutler/nvm-windows>

For Contributing please see [`CONTRIBUTING.md`](./CONTRIBUTING.md) for more information.

## License

Distributed under the MIT License. See [`LICENSE.md`](./LICENSE.md) for more information.

For any security issues see [`SECURITY.md`](./SECURITY.md).
