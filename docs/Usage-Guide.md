# Usage Guide

This library supports using Lucene.NET to index both unstructured and high structured, interrelated content in an Xperience by Kentico solution. This indexed content can then be programmatically queried and displayed in a website channel.

Below are the steps to integrate the library into your solution.

## Create a custom Indexing Strategy

See [Custom index strategy](Custom-index-strategy.md)

## Continuous Integration

When starting your application for the first time after adding this library to your solution, a custom module and custom module classes will automatically be created
to support managing search index configuration within the administration UI.

If you do not see new items added to your [CI repository](https://docs.xperience.io/x/FAKQC) for the new auto-generated Lucene search data types, stop your application and perform a [CI store](https://docs.xperience.io/xp/developers-and-admins/ci-cd/continuous-integration#ContinuousIntegration-Storeobjectdatatotherepository) to add the library's custom module configuration to the CI repository.

You should now be able to run a [CI restore](https://docs.xperience.io/xp/developers-and-admins/ci-cd/continuous-integration#ContinuousIntegration-Restorerepositoryfilestothedatabase).
Attempting to run a CI restore without the CI files in the CI repository will result in a SQL error during the restore.

When team members are merging changes that include the addition of this library, they _must_ first run a CI restore to ensure they have the same object metadata for the search custom module as your database.

Future updates to indexes will be tracked in the CI repository [unless they are excluded](https://docs.xperience.io/x/ygAcCQ).

## Managing search indexes

See [Managing search indexes](Managing-Indexes.md)

## Search index querying

See [Search index querying](Search-index-querying.md)

## Implementing document decay

You can score indexed items by "freshness" or "recency" using several techniques, each with different tradeoffs.

1. Boost relevant fields by setting field boost (preferable method, but requires more work).
2. Boost one field with constant value, that is always present in search query (shown in the example project, less desirable method.

   The Downside of this method is that all documents get matched, usable only for scenarios where total number of result is not required).

3. Use a sort expression. Implementation details can be found in Lucene.NET unit tests, Lucene.NET implementations

> Small differences in boosts will be ignored by Lucene.

## Uninstalling the integration

This integration programmatically inserts custom module classes and their configuration into the Xperience solution on startup (see `LuceneModuleInstaller.cs`).

To remove this configuration and the added database tables perform one of the following sets of changes to your solution:

### Using Continuous Integration (CI)

1. If you are using Xperience's Continuous Integration (CI), delete the files with the paths from your CI repository folder:

   - `\App_Data\CIRepository\@global\cms.class\lucene.*\**`
   - `\App_Data\CIRepository\@global\cms.class\kentico.xperience.lucene\**`
   - `\App_Data\CIRepository\@global\lucene.*\**`

1. Remove the `Kentico.Xperience.Lucene` NuGet package from the solution
1. Remove any code references to the package and recompile your solution
1. Run a CI restore, which will clean up the database tables and `CMS_Class` records.

### No Continuous Integration

If you are not using CI run the following SQL _after_ removing the NuGet package from the solution:

```sql
drop table lucene_lucenecontenttypeitem
drop table lucene_luceneincludedpathitem
drop table lucene_luceneindexlanguageitem
drop table lucene_luceneindexitem

delete
FROM [dbo].[CMS_Class] where ClassName like 'lucene%'

delete
from [CMS_Resource] where ResourceName = 'Kentico.Xperience.Lucene'
```

> Note: there is currently no way to migrate index configuration in the database between versions of this integration in the case that the database schema includes breaking changes. This feature could be added in a future update.
