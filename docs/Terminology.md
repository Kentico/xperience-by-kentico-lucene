# Terminology Guide

This guide explains key terms and concepts used in the Xperience by Kentico Lucene integration to help you better understand how to configure and use Lucene search.

## Lucene.NET Concepts

### Index

A **search index** is a structured collection of documents that have been analyzed and optimized for fast searching. Think of it as a specialized database designed specifically for search queries. In Lucene, indexes are stored on the file system.

### Document

A **document** is the basic unit of indexing and searching in Lucene. Each document represents a piece of content (like a web page or content item) and contains one or more fields. When you index a web page, you create a Lucene document that represents that page.

### Field

A **field** is a named piece of data within a document. For example, a document might have fields like "Title", "Content", "URL", etc. Each field has a name, a value, and configuration that determines how it's stored and searched.

#### Field Types

- **TextField** - Used for full-text searching. The content is analyzed and tokenized.
- **StringField** - Stored as-is without analysis. Useful for exact matching (like IDs or URLs).
- **StoredField** - Only stored in the index, not searchable. Used to retrieve data without searching it.
- **FacetField** - Used for faceted search, allowing users to filter results by categories.

### Analyzer

An **analyzer** processes text before indexing and searching. It breaks text into tokens, removes stop words, applies stemming, and more. The default is the `StandardAnalyzer`, but you can use language-specific analyzers for better results in different languages.

### Query

A **query** defines what you're searching for. Lucene supports various query types:

- **TermQuery** - Searches for an exact term
- **PhraseQuery** - Searches for a phrase (sequence of terms)
- **BooleanQuery** - Combines multiple queries with AND, OR, NOT logic
- **WildcardQuery** - Uses wildcards (* and ?) for pattern matching
- **FuzzyQuery** - Finds terms similar to the search term (handles typos)

### Facets

**Facets** enable filtering search results by categories. For example, you might facet by content type, allowing users to filter results to only show "Articles" or "Products".

### Searcher

A **searcher** (specifically `IndexSearcher`) executes queries against an index and returns matching documents. It's the component you use to retrieve search results.

## Xperience by Kentico Integration Concepts

### Indexing Strategy

An **indexing strategy** is a custom class that defines how content from Xperience is transformed into Lucene documents. It determines:

- Which content types to index
- What data to include in each document
- How to handle relationships between content items
- Which fields to create and how to configure them

Each index in Xperience is associated with one indexing strategy.

### Index Event Item Model

These models (`IIndexEventItemModel`, `IndexEventWebPageItemModel`, `IndexEventReusableItemModel`) represent content items being processed for indexing. Your indexing strategy receives these models and transforms them into Lucene documents.

### Web Page Items vs Reusable Content Items

- **Web Page Items** - Content that exists as pages in your website channel (like articles, landing pages, product pages)
- **Reusable Content Items** - Content that can be referenced by multiple pages (like authors, categories, product specifications)

Both can be indexed, and changes to reusable items can trigger reindexing of related web pages.

### Tree Path

The **Tree Path** is the hierarchical path of a web page in the content tree structure (e.g., `/products/coffee/espresso`). This is used when configuring which pages to include in an index. It differs from the URL path which may be customized.

### Included Path

An **Included Path** is a configuration setting that determines which pages in a website channel should trigger indexing. You can specify exact paths (e.g., `/products/coffee`) or wildcard paths (e.g., `/products/%`) to include entire sections of your site.

### Website Channel

A **website channel** represents a distinct website in Xperience. Each index can be configured to include content from specific channels. When configuring an index, you specify which channels' content should be indexed.

### Indexed Languages

Languages selected for an index determine which language variants of content are indexed. Only content in the selected languages will be included in the index.

### Rebuild Hook

A **rebuild hook** is a security token used to validate requests to rebuild an index from external sources (like an API call or webhook). This prevents unauthorized index rebuilds.

## Search Operation Concepts

### Indexing

**Indexing** is the process of adding, updating, or removing documents in a search index. In this integration, indexing happens automatically when content is created, updated, or deleted in Xperience.

### Reindexing

**Reindexing** means rebuilding the entire index from scratch, processing all content that matches the index configuration. This is typically done:

- After creating a new index
- After changing the indexing strategy
- After deployment in environments without persistent storage
- When the index becomes corrupted

### Querying

**Querying** is the process of searching an index for documents that match specific criteria. You build a query, execute it against the index using `ILuceneSearchService`, and receive matching documents ranked by relevance.

### Scoring and Relevance

Lucene assigns each search result a **score** based on how well it matches the query. Higher scores appear first in results. Scoring considers factors like:

- Term frequency (how often search terms appear)
- Document frequency (how rare/common terms are)
- Field boosts (increasing importance of specific fields)
- Query boosts (emphasizing certain parts of the query)

### Boosting

**Boosting** increases the importance of certain fields or documents in search results. For example, you might boost the title field so matches in titles rank higher than matches in body content.

### Pagination

**Pagination** divides search results into pages. You specify the page size (results per page) and page number when querying, allowing users to navigate through large result sets.

## Advanced Concepts

### Web Crawling/Scraping

**Web crawling** is the process of programmatically visiting web pages to extract their rendered HTML content for indexing. This is useful when you want to index the final rendered content (including content from view components or layouts) rather than just raw field values.

### Auto-Scaling

In **auto-scaling** environments (like Azure App Service with multiple instances), index files must be synchronized across instances. The integration provides strategies for handling this scenario.

### Auto-Reindexing After Deployment

In environments like Kentico Xperience SaaS where the file system is not persistent, indexes are lost during deployment. **Auto-reindexing** automatically rebuilds indexes after the application starts to ensure search functionality is restored.

### Content Item Linking

When indexed content references other content items (like an article linking to an author), changes to the referenced item should trigger reindexing of the referring item. This is handled by implementing the `FindItemsToReindex` method in your indexing strategy.

## Common Workflow Terms

### Searchable vs Stored

- **Searchable** fields are analyzed and can be queried but don't need to be retrieved
- **Stored** fields are saved in the index and can be retrieved in search results
- Fields can be both searchable and stored, searchable only, or stored only

### Field Store Options

When adding fields to a document, you specify whether to store the field value:

- `Field.Store.YES` - Store the value so it can be retrieved
- `Field.Store.NO` - Don't store the value (only make it searchable)

### Facet Configuration

**Facet configuration** defines how facets behave in your index:

- **Multi-valued** facets allow multiple values per document (like multiple categories)
- **Single-valued** facets allow only one value per document
- **Hierarchical** facets support parent-child relationships (like category/subcategory)
