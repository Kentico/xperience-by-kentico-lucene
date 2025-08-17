import { LuceneIndexContentType } from "./LuceneIndexContentType";

export interface IncludedPath {
    aliasPath: string | null;
    contentTypes: LuceneIndexContentType[];
    identifier: string | null;
}