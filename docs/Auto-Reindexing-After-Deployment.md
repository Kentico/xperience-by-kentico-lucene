# Automatic Lucene Reindexing after Deployment

## Overview

Lucene integration **requires persistent file system access** to store and manage indexes. In environments like **Kentico Xperience SaaS**, which do not provide persistent file system storage for indexes, the index data is lost during deployments or restarts. As a result, indexes must be rebuilt after each deployment to ensure correct search functionality.

To address this issue, a new configuration option has been introduced to enable automatic reindexing. The solution adds a hosted service that periodically checks the application’s assembly version, compares it with the stored assembly version for each index, and updates it after a rebuild. If the versions differ, the index is automatically rebuilt.

## Configuration

To enable the automatic assembly check and index rebuilding, add the following configuration to your `appsettings.json` file:

```json
"CMSLuceneSearch": {
    // Other settings ...
    "PostStartupReindexingOptions": {
        "Enabled": true,
        "IndexesExcludedFromAutomaticReindexing": [ "ExampleExcludedIndex" ], // Indexes to exclude from automatic rebuilding and version checks.
        "CheckIntervalMinutes": 2 // Frequency of assembly version checks (in minutes). Default is 2 minutes.
    }
}
