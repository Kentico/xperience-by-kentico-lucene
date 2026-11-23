export interface LuceneGenerationInfo {
  readonly generation: number;
  readonly isPublished: boolean;
}

export interface LuceneRebuildRecord {
  readonly requestedAtUtc: string;
  readonly queuedItemCount: number;
}

export interface LuceneIndexStatus {
  readonly hasPublishedGeneration: boolean;
  readonly publishedGeneration: number | null;
  readonly generations: LuceneGenerationInfo[];
  readonly documentCount: number;
  readonly deletedDocumentCount: number;
  readonly lastUpdatedUtc: string | null;
  readonly lastRebuildRequest: LuceneRebuildRecord | null;
  readonly readError: string | null;
}

export interface IndexPathSummary {
  readonly aliasPath: string;
  readonly contentTypes: string[];
}

export interface IndexChannelSummary {
  readonly channelName: string;
  readonly paths: IndexPathSummary[];
}

export interface IndexSummaryResult {
  readonly indexName: string;
  readonly displayName: string;
  readonly strategyName: string;
  readonly analyzerName: string;
  readonly languages: string[];
  readonly reusableContentTypes: string[];
  readonly channels: IndexChannelSummary[];
  readonly status: LuceneIndexStatus;
  /** Content type code name to Xperience display name. Missing entries fall back to the code name. */
  readonly contentTypeDisplayNames?: Readonly<Record<string, string>> | null;
}

export interface LuceneStoredField {
  readonly name: string;
  readonly value: string;
}

export interface LuceneStoredDocument {
  readonly documentId: number;
  readonly fields: LuceneStoredField[];
}

export interface LuceneStoredDocumentsPage {
  readonly total: number;
  readonly documents: LuceneStoredDocument[];
  readonly readError: string | null;
}

export interface LuceneQueryHit {
  readonly document: LuceneStoredDocument;
  readonly score: number;
  readonly explanation: string;
}

export interface LuceneQueryTestResult {
  readonly parsedQuery: string | null;
  readonly totalHits: number;
  readonly hits: LuceneQueryHit[];
  readonly error: string | null;
}

export interface LuceneDiagnosticItem {
  readonly itemGuid: string;
  readonly languageName: string;
  readonly contentTypeName: string;
  readonly name: string;
  readonly reason: string;
}

export interface LuceneIndexDiagnostics {
  readonly selectedItemCount: number;
  readonly indexableCount: number;
  readonly skippedCount: number;
  readonly missingCount: number;
  readonly orphanCount: number;
  readonly skipped: LuceneDiagnosticItem[];
  readonly missing: LuceneDiagnosticItem[];
  readonly orphans: LuceneDiagnosticItem[];
  readonly error: string | null;
}

export interface LuceneDiagnosticsRun {
  /** Null when the inspector cannot page the results. */
  readonly runId: string | null;
  /** Counts, with the first page of each category. */
  readonly diagnostics: LuceneIndexDiagnostics;
}

export type LuceneDiagnosticCategory = "skipped" | "missing" | "orphans";

export interface LuceneDiagnosticsPage {
  readonly total: number;
  readonly items: LuceneDiagnosticItem[];
  readonly error: string | null;
}

export interface DiagnosticsRunArguments {
  readonly take: number;
}

export interface DiagnosticsPageArguments {
  readonly runId: string;
  readonly category: LuceneDiagnosticCategory;
  readonly skip: number;
  readonly take: number;
}

/** `requested` does not mean the rebuild finished. */
export interface IndexRebuildResult {
  readonly requested: boolean;
  readonly error: string | null;
}

export interface IndexRebuildAccessResult {
  readonly canRebuild: boolean;
}

export interface DocumentsArguments {
  readonly skip: number;
  readonly take: number;
}

export interface TestQueryArguments {
  readonly query: string;
  readonly field: string;
  readonly skip: number;
  readonly take: number;
}

export type SearchFieldKind = "text" | "exact" | "boolean" | "untyped";

export interface SearchFieldResult {
  readonly name: string;
  readonly displayName: string;
  readonly kind: SearchFieldKind;
}

export interface SearchFieldsResult {
  readonly fields: SearchFieldResult[];
  readonly error: string | null;
}

export interface SearchFilterArgument {
  readonly field: string;
  readonly value: string;
}

export interface FilterQueryArguments {
  readonly filters: SearchFilterArgument[];
  readonly skip: number;
  readonly take: number;
}
