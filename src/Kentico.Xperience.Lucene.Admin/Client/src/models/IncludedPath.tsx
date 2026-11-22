import type { LuceneIndexContentType } from './LuceneIndexContentType';

export interface IncludedPath {
  [key: string]: string | LuceneIndexContentType[] | null;
  aliasPath: string | null;
  contentTypes: LuceneIndexContentType[];
  identifier: string | null;
}
