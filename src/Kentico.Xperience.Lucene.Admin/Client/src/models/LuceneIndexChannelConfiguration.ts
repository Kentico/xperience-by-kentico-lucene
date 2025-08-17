import { IncludedPath } from "./IncludedPath";

export interface LuceneIndexChannelConfiguration {
    channelName: string | null;
    channelDisplayName: string | null;
    identifier: string | null;
    includedPaths: IncludedPath[];
}