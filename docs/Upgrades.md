# Upgrades

## 3.0.0 -> 4.0.0

The core custom module tables have been renamed to align with Kentico's naming conventions.

- `Lucene_LuceneContentTypeItem` -> `KenticoLucene_LuceneContentTypeItem`
- `Lucene_LuceneIncludedPathItem` -> `KenticoLucene_LuceneIncludedPathItem`
- `Lucene_LuceneIndexLanguageItem` -> `KenticoLucene_LuceneIndexLanguageItem`
- `Lucene_LuceneIndexItem` -> `KenticoLucene_LuceneIndexItem`

Installing the v4.0.0 NuGet package will automatically install the new custom module classes and create their tables,
but it will not migrate your index definitions, which will need to be re-created.

After re-creating your indexes, you can clean up the old data by running the following SQL:

```sql
drop table Lucene_LuceneContentTypeItem
drop table Lucene_LuceneIncludedPathItem
drop table Lucene_LuceneIndexLanguageItem
drop table Lucene_LuceneIndexItem

delete
FROM [dbo].[CMS_Class] where ClassName like 'lucene%'
```

If you are using Xperience's CI feature, you will want to [run a CI store](https://docs.xperience.io/xp/developers-and-admins/ci-cd/continuous-integration#ContinuousIntegration-Storeobjectdatatotherepository) to automatically remove the old CI repository files.

## Uninstall

This integration programmatically inserts custom module classes and their configuration into the Xperience solution on startup (see `LuceneModuleInstaller.cs`).

To remove this configuration and the added database tables perform one of the following sets of changes to your solution:

### Using Continuous Integration (CI)

1. Remove the `Kentico.Xperience.Lucene` NuGet package from the solution
1. Remove any code references to the package and recompile your solution
1. If you are using Xperience's Continuous Integration (CI), delete the files with the paths from your CI repository folder:

   - `\App_Data\CIRepository\@global\cms.class\kenticolucene.*\**`
   - `\App_Data\CIRepository\@global\cms.class\kentico.xperience.lucene\**`
   - `\App_Data\CIRepository\@global\kenticolucene.*\**`

1. Run a CI restore, which will clean up the database tables and `CMS_Class` records.

### No Continuous Integration

If you are not using CI run the following SQL _after_ removing the NuGet package from the solution:

```sql
drop table KenticoLucene_LuceneContentTypeItem
drop table KenticoLucene_LuceneIncludedPathItem
drop table KenticoLucene_LuceneIndexLanguageItem
drop table KenticoLucene_LuceneIndexItem

delete
FROM [dbo].[CMS_Class] where ClassName like 'kenticolucene%'

delete
from [CMS_Resource] where ResourceName = 'Kentico.Xperience.Lucene'
```

> Note: there is currently no way to migrate index configuration in the database between versions of this integration in the case that the database schema includes breaking changes. This feature could be added in a future update.
