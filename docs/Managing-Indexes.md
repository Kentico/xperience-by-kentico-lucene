# Managing Indexes

This section is relevant for `Kentico.Xperience.Lucene.Admin` NuGet package.

To manage a search index in the Xperience administration, navigate to the Search application on the dashboard.

![Administration dashboard highlight the Search application](/images/xperience-administration-dashboard.jpg)

Create a new index or select and index to edit by clicking the index row or the pencil icon.

![Administration search index list](/images/xperience-administration-search-index-list.jpg)

Fill out the search index form, populating the fields with your custom values.

![Administration search index edit form](/images/xperience-administration-search-index-edit-form.jpg)

- Index Name - the name of the displayed index.
- Included Reusable Content Types - these are the reusable content types that will be processed by your custom indexing strategy.
If no option is selected, no items will be processed.
- Indexed Languages - the index will only include content in the selected languages.
- Channel Name - the index will only be triggered by web page item creation or modification in the selected website channel.
- Indexing Strategy - the indexing strategy specified in code during dependency registration of a custom indexing strategies.
  - If you want the default strategy to appear here, register it explicitly in `IServiceCollection.AddKenticoLucene()` method.
- Lucene Analyzer - the Lucene analyzer which indexes use to analyze text.
- Rebuild Hook - for validating a request rebuild of the search index from an external source (ex: API request).

Now, configure the web page paths and content types that the search index depends on by clicking the Add New Path button
or clicking an existing path in the table at the top of the index configuration form.

![Administration search index edit paths form](/images/xperience-administration-search-index-edit-form-paths-edit.jpg)

- Included Path - can be an exact relative path of a web page item, (ex: `/path/to/my/page`), or a wildcard path (ex: `/parent-path/%`)
  - To determine a web page path, select the web page in the website channel page tree, then view the "Current URL" in the Content tab of the web page. The path will be the relative path excluding the domain
- Included ContentType items - these are the web page content types that can be selected for the path. Each content type in the multi-select enables modification to web pages of that type to trigger an event that your custom indexing strategy can process. If no option is selected, no web pages will trigger these events.

## Indexing reusable content items

All reusable content item modifications will trigger an event to generate a `IndexEventReusableItemModel` for your custom index strategy class to process, as long as the content item has a language variant matching one of the languages selected for the index. You can use this to index reusable content items in addition to web page items but returning the reusable content item content as a `IIndexEventItemModel` from the strategy `FindItemsToReindex` method.

> Note: There is currently no UI to allow administrators to configure which types of reusable content items trigger indexing. This could be added in a future update.
