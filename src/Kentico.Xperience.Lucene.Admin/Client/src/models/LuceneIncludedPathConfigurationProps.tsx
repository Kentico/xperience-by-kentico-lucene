import { IncludedPath } from './IncludedPath';
import { LuceneIndexContentType } from './LuceneIndexContentType';

export interface LuceneIncludedPathConfigurationProps
{
    value: IncludedPath[];
    possibleContentTypeItems: LuceneIndexContentType[] | null;
    /* eslint-disable @typescript-eslint/naming-convention */
    onChange?: (value: IncludedPath[]) => void;
    /* eslint-enable @typescript-eslint/naming-convention */
}