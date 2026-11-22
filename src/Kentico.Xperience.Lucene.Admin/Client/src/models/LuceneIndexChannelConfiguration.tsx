import type { IncludedPath } from "./IncludedPath";

export interface LuceneIndexChannelConfiguration {
  [key: string]: string | IncludedPath[] | null;
  websiteChannelName: string | null;
  channelDisplayName: string | null;
  includedPaths: IncludedPath[];
}
