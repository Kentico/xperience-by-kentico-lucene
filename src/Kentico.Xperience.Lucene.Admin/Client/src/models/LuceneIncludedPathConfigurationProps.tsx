import type { IncludedPath } from "./IncludedPath";
import type { LuceneIndexContentType } from "./LuceneIndexContentType";

export interface LuceneIncludedPathConfigurationProps {
  value: IncludedPath[];
  possibleContentTypeItems: LuceneIndexContentType[] | null;

  onChange?: (value: IncludedPath[]) => void;
}
