import type { IncludedPath } from './IncludedPath';

export interface LuceneIndexChannelConfiguration {
  websiteChannelName: string | null;
  channelDisplayName: string | null;
  includedPaths: IncludedPath[];
}
