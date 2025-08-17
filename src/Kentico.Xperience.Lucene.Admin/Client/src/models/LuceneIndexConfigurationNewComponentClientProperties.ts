import { FormComponentProps } from "@kentico/xperience-admin-base";
import { LuceneIndexContentType } from "./LuceneIndexContentType";
import { LuceneIndexChannelConfiguration } from "./LuceneIndexChannelConfiguration";
import { LuceneIndexChannel } from "./LuceneIndexChannel";

export interface LuceneIndexConfigurationNewComponentClientProperties
    extends FormComponentProps {
    value: LuceneIndexChannelConfiguration[];
    possibleContentTypeItems: LuceneIndexContentType[] | null;
    possibleChannels: LuceneIndexChannel[] | null;
}