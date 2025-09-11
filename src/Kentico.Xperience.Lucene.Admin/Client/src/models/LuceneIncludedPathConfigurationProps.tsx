import { IncludedPath } from './IncludedPath';
import { LuceneIndexContentType } from './LuceneIndexContentType';

export interface LuceneIncludedPathConfigurationProps
{
    value: IncludedPath[];
    possibleContentTypeItems: LuceneIndexContentType[] | null;
    onChange?: (value: IncludedPath[]) => void;
}