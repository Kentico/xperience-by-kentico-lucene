import type { FormComponentProps } from '@kentico/xperience-admin-base';
import type { LuceneIndexContentType } from './LuceneIndexContentType';
import type { LuceneIndexChannelConfiguration } from './LuceneIndexChannelConfiguration';
import type { LuceneIndexChannel } from './LuceneIndexChannel';

export interface LuceneIndexConfigurationFormComponentProps extends FormComponentProps {
  value: LuceneIndexChannelConfiguration[];
  possibleContentTypeItems: LuceneIndexContentType[] | null;
  possibleChannels: LuceneIndexChannel[] | null;
}
