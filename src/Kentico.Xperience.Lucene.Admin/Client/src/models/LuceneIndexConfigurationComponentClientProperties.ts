import { FormComponentProps } from "@kentico/xperience-admin-base";
import { IncludedPath } from "./IncludedPath";
import { LuceneIndexContentType } from "./LuceneIndexContentType";

export interface LuceneIndexConfigurationComponentClientProperties
    extends FormComponentProps {
    value: IncludedPath[];
    possibleContentTypeItems: LuceneIndexContentType[] | null;
}