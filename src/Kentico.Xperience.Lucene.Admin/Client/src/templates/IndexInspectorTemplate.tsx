import {
  Box,
  Button,
  ButtonColor,
  ButtonSize,
  ButtonType,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Card,
  CellType,
  Colors,
  Column,
  ColumnContentType,
  Cols,
  Headline,
  HeadlineSize,
  Inline,
  Input,
  LayoutAlignment,
  MenuItem,
  Pagination,
  RadioButton,
  RadioGroup,
  Row,
  RowWrap,
  Select,
  SidePanel,
  SidePanelSize,
  Spacing,
  Spinner,
  Stack,
  Table,
  TextWithLabel,
  type ActionCell,
  type ComponentCell,
  type StringCell,
  type TableColumn,
  type TableRow,
} from "@kentico/xperience-admin-components";
import * as React from "react";
import { IndexOverviewTab } from "./IndexOverviewTab";
import { RemoveButton } from "../components/RemoveButton";
import { useCommandResult } from "../hooks/useCommandResult";
import type {
  DocumentsArguments,
  FilterQueryArguments,
  IndexSummaryResult,
  LuceneQueryTestResult,
  LuceneStoredDocument,
  LuceneStoredDocumentsPage,
  SearchFieldKind,
  SearchFieldsResult,
  SearchFilterArgument,
  TestQueryArguments,
} from "../models/IndexInspectorModels";

interface IndexInspectorProps {
  readonly tab: string;
}

// eslint-disable-next-line @typescript-eslint/no-unnecessary-type-parameters
const useInspectorCommand = <TResult, TData = void>(name: string) => {
  const command = useCommandResult<TResult, TData>(name);
  const [result, setResult] = React.useState<TResult | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [busy, setBusy] = React.useState(false);
  const mounted = React.useRef(true);

  React.useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  const run = React.useCallback(
    async (data?: TData): Promise<void> => {
      setBusy(true);
      setError(null);
      setResult(null);
      try {
        const value = await command(data);
        if (mounted.current) {
          setResult(value);
        }
      } catch (cause: unknown) {
        if (mounted.current) {
          setError(
            cause instanceof Error
              ? cause.message
              : "The request failed. Please try again.",
          );
        }
      } finally {
        if (mounted.current) {
          setBusy(false);
        }
      }
    },
    [command],
  );

  return { result, error, busy, run };
};

const Error_ = ({
  message,
  retry,
}: {
  readonly message: string;
  readonly retry?: () => void;
}): React.JSX.Element => (
  <div role="alert" data-testid="inspector-error">
    <Callout
      type={CalloutType.FriendlyWarning}
      placement={CalloutPlacementType.OnPaper}
      headline="Unable to complete the request"
      actionButton={
        retry && (
          <Button type={ButtonType.Button} label="Retry" onClick={retry} />
        )
      }
    >
      {message}
    </Callout>
  </div>
);

const Loading = (): React.JSX.Element => (
  <div
    role="status"
    aria-label="Loading inspector"
    data-testid="inspector-loading"
  >
    <Inline spacing={Spacing.S}>
      <Spinner />
      <span>Loading…</span>
    </Inline>
  </div>
);

const SEARCH_PAGE_SIZE = 20;
const SEARCH_DEFAULT_FIELD = "Name";

interface SearchRow {
  readonly position: number;
  readonly document: LuceneStoredDocument;
  readonly score: number | null;
  readonly explanation: string | null;
}

interface SearchRequest {
  readonly kind: "browse" | "query" | "filter";
  readonly query: string;
  readonly field: string;
  readonly filters: readonly SearchFilterArgument[];
  readonly page: number;
  readonly run: number;
}

type SearchMode = "filters" | "query";

interface SearchFilterDraft {
  readonly id: number;
  readonly field: string;
  readonly value: string;
}

const SEARCH_MAX_FILTERS = 20;
const SEARCH_BOOLEAN_ANY = "any";
const SEARCH_BROWSE = {
  kind: "browse",
  query: "",
  field: "",
  filters: [],
} as const;

const SEARCH_KIND_LABELS: Readonly<Record<SearchFieldKind, string>> = {
  text: "Analyzed text",
  exact: "Exact value",
  boolean: "Yes or no",
  untyped: "Untyped",
};

const SEARCH_VALUE_PLACEHOLDERS: Readonly<Record<"text" | "exact", string>> = {
  text: "Words, for example coffee or brew*",
  exact: "Exact value, for example en or *arabica*",
};

const searchFieldValue = (
  document: LuceneStoredDocument,
  name: string,
): string | undefined => {
  const value = document.fields.find((field) => field.name === name)?.value;
  return value === "" ? undefined : value;
};

const searchTitle = (document: LuceneStoredDocument): string =>
  searchFieldValue(document, "Title") ??
  searchFieldValue(document, "Name") ??
  searchFieldValue(document, "ItemGuid") ??
  `Document ${String(document.documentId)}`;

const searchCount = (count: number, noun: string): string =>
  `${String(count)} ${noun}${count === 1 ? "" : "s"}`;

const searchColumn = (
  name: string,
  caption: string,
  minWidth: number,
  maxWidth: number,
): TableColumn => ({
  name,
  caption,
  visible: true,
  minWidth,
  maxWidth,
  contentType: ColumnContentType.Text,
  sortable: false,
  searchable: false,
});

const searchCell = (columnName: string, value: string): StringCell => ({
  type: CellType.String,
  columnName,
  value,
});

type ContentTypeNames = Readonly<Record<string, string>> | null | undefined;

const contentTypeDisplayName = (
  codeName: string,
  displayNames: ContentTypeNames,
): string | undefined => {
  const key = Object.keys(displayNames ?? {}).find(
    (name) => name.toLowerCase() === codeName.toLowerCase(),
  );
  const displayName = key ? displayNames?.[key] : undefined;
  return displayName && displayName !== codeName ? displayName : undefined;
};

const singleLine: React.CSSProperties = {
  display: "block",
  overflow: "hidden",
  textOverflow: "ellipsis",
  whiteSpace: "nowrap",
};

// Matches the native MenuItem secondary label: 12px low-emphasis text.
const CodeNameText = ({
  children,
}: {
  readonly children: string;
}): React.JSX.Element => (
  <span
    data-testid="code-name"
    style={{
      ...singleLine,
      fontSize: "12px",
      lineHeight: 1.1,
      color: Colors.TextLowEmphasis,
    }}
  >
    {children}
  </span>
);

const ContentTypeText = ({
  codeName,
  displayName,
}: {
  readonly codeName: string;
  readonly displayName: string | undefined;
}): React.JSX.Element => (
  <span
    title={displayName ? `${displayName} (${codeName})` : codeName}
    style={{
      ...singleLine,
      width: "100%",
      minWidth: 0,
      textAlign: "left",
      lineHeight: "18px",
      color: Colors.TextDefaultOnLight,
    }}
  >
    {displayName ? (
      <>
        <span style={singleLine}>{displayName}</span>
        <CodeNameText>{codeName}</CodeNameText>
      </>
    ) : (
      codeName
    )}
  </span>
);

// Native component cells render a component type without props, so keep one per content type.
const contentTypeCells = new Map<string, React.FC>();

const contentTypeCell = (
  codeName: string | undefined,
  displayNames: ContentTypeNames,
): ComponentCell | StringCell => {
  if (!codeName) {
    return searchCell("type", "");
  }
  const displayName = contentTypeDisplayName(codeName, displayNames);
  const key = `${displayName ?? ""}\u0000${codeName}`;
  let component = contentTypeCells.get(key);
  if (!component) {
    const ContentTypeCellText = (): React.JSX.Element => (
      <ContentTypeText codeName={codeName} displayName={displayName} />
    );
    component = ContentTypeCellText;
    contentTypeCells.set(key, component);
  }
  return { type: CellType.Component, columnName: "type", component };
};

const SearchSectionHeading = ({
  children,
}: {
  readonly children: React.ReactNode;
}): React.JSX.Element => (
  <div role="heading" aria-level={3}>
    <Headline size={HeadlineSize.S}>{children}</Headline>
  </div>
);

const SearchDetails = ({
  row,
  contentTypeNames,
  onClose,
}: {
  readonly row: SearchRow | null;
  readonly contentTypeNames: ContentTypeNames;
  readonly onClose: () => void;
}): React.JSX.Element => {
  const document = row?.document;
  const typeCode = document
    ? searchFieldValue(document, "ContentTypeName")
    : undefined;
  const metadata: [string, string][] = row
    ? [
        ["Position", String(row.position)],
        ...(row.score === null
          ? []
          : ([["Score", row.score.toFixed(4)]] as [string, string][])),
        ["Language", searchFieldValue(row.document, "LanguageName") ?? "None"],
        ["Content type", ""],
        ["URL", searchFieldValue(row.document, "Url") ?? "None"],
        ["Item GUID", searchFieldValue(row.document, "ItemGuid") ?? "None"],
        ["Lucene document", String(row.document.documentId)],
      ]
    : [];

  return (
    <SidePanel
      headline={
        <div
          role="heading"
          aria-level={2}
          data-testid="search-panel-title"
          style={{ color: Colors.TextDefaultOnLight }}
        >
          <Headline size={HeadlineSize.L}>
            {document ? searchTitle(document) : ""}
          </Headline>
        </div>
      }
      size={SidePanelSize.Stackable}
      isVisible={row !== null}
      onClose={onClose}
      tooltips={{ close: "Close" }}
      footer={
        <Button type={ButtonType.Button} label="Close" onClick={onClose} />
      }
    >
      {row && (
        <div
          role="region"
          aria-label={`Stored record of ${searchTitle(row.document)}`}
          data-testid="search-panel"
          style={{ color: Colors.TextDefaultOnLight }}
        >
          <Stack spacing={Spacing.XL}>
            <section aria-label="Metadata" data-testid="search-panel-metadata">
              <Stack spacing={Spacing.M}>
                <SearchSectionHeading>Metadata</SearchSectionHeading>
                {metadata.map(([label, value]) =>
                  label === "Content type" && typeCode ? (
                    <div key={label}>
                      <TextWithLabel
                        label={label}
                        value={
                          contentTypeDisplayName(typeCode, contentTypeNames) ??
                          typeCode
                        }
                      />
                      {contentTypeDisplayName(typeCode, contentTypeNames) ? (
                        <CodeNameText>{typeCode}</CodeNameText>
                      ) : null}
                    </div>
                  ) : (
                    <TextWithLabel
                      key={label}
                      label={label}
                      value={label === "Content type" ? "None" : value}
                    />
                  ),
                )}
              </Stack>
            </section>
            <section
              aria-label="Stored fields"
              data-testid="search-panel-fields"
            >
              <Stack spacing={Spacing.M}>
                <SearchSectionHeading>
                  {`Stored fields (${String(row.document.fields.length)})`}
                </SearchSectionHeading>
                {row.document.fields.map((field, index) => (
                  <TextWithLabel
                    key={`${field.name}-${String(index)}`}
                    label={field.name}
                    value={field.value}
                  />
                ))}
              </Stack>
            </section>
            {row.explanation !== null && (
              <section
                aria-label="Score explanation"
                data-testid="search-panel-explanation"
              >
                <Stack spacing={Spacing.M}>
                  <SearchSectionHeading>Score explanation</SearchSectionHeading>
                  <span>
                    Lucene&apos;s breakdown of how this document&apos;s score
                    was calculated for the query.
                  </span>
                  <pre
                    data-testid="search-explain"
                    style={{
                      margin: 0,
                      whiteSpace: "pre-wrap",
                      overflowWrap: "anywhere",
                    }}
                  >
                    {row.explanation}
                  </pre>
                </Stack>
              </section>
            )}
          </Stack>
        </div>
      )}
    </SidePanel>
  );
};

const SearchTab = (): React.JSX.Element => {
  const browse = useInspectorCommand<
    LuceneStoredDocumentsPage,
    DocumentsArguments
  >("LoadDocuments");
  const search = useInspectorCommand<LuceneQueryTestResult, TestQueryArguments>(
    "RunQuery",
  );
  const filterSearch = useInspectorCommand<
    LuceneQueryTestResult,
    FilterQueryArguments
  >("RunFilterQuery");
  const fieldsCommand =
    useInspectorCommand<SearchFieldsResult>("LoadSearchFields");
  const { result: summary, run: loadSummary } =
    useInspectorCommand<IndexSummaryResult>("LoadSummary");
  const [mode, setMode] = React.useState<SearchMode>("filters");
  const [filters, setFilters] = React.useState<readonly SearchFilterDraft[]>(
    [],
  );
  const nextFilterId = React.useRef(1);
  const [query, setQuery] = React.useState("");
  const [field, setField] = React.useState("");
  const [request, setRequest] = React.useState<SearchRequest>({
    ...SEARCH_BROWSE,
    page: 1,
    run: 0,
  });
  const [knownFields, setKnownFields] = React.useState<readonly string[]>([]);
  const [selected, setSelected] = React.useState<SearchRow | null>(null);
  const resultsRef = React.useRef<HTMLDivElement>(null);
  const openerIndex = React.useRef<number | null>(null);
  const isQuery = request.kind !== "browse";
  const isRawQuery = request.kind === "query";
  const matching = request.kind === "filter" ? filterSearch : search;
  const runBrowse = browse.run;
  const runSearch = search.run;
  const runFilterSearch = filterSearch.run;
  const loadFields = fieldsCommand.run;

  React.useEffect(() => {
    void loadSummary();
    void loadFields();
  }, [loadSummary, loadFields]);

  const execute = React.useCallback(() => {
    const skip = (request.page - 1) * SEARCH_PAGE_SIZE;
    if (request.kind === "query") {
      void runSearch({
        query: request.query,
        field: request.field,
        skip,
        take: SEARCH_PAGE_SIZE,
      });
    } else if (request.kind === "filter") {
      void runFilterSearch({
        filters: [...request.filters],
        skip,
        take: SEARCH_PAGE_SIZE,
      });
    } else {
      void runBrowse({ skip, take: SEARCH_PAGE_SIZE });
    }
  }, [request, runBrowse, runSearch, runFilterSearch]);

  React.useEffect(execute, [execute]);

  const busy = isQuery ? matching.busy : browse.busy;
  const failure = isQuery
    ? (matching.error ?? matching.result?.error)
    : (browse.error ?? browse.result?.readError);
  const total = isQuery
    ? (matching.result?.totalHits ?? null)
    : (browse.result?.total ?? null);
  const firstPosition = (request.page - 1) * SEARCH_PAGE_SIZE + 1;
  const rows: SearchRow[] | null = failure
    ? null
    : isQuery
      ? (matching.result?.hits.map((hit, index) => ({
          position: firstPosition + index,
          document: hit.document,
          score: hit.score,
          explanation: hit.explanation,
        })) ?? null)
      : (browse.result?.documents.map((document, index) => ({
          position: firstPosition + index,
          document,
          score: null,
          explanation: null,
        })) ?? null);

  const loadedFields = [
    ...new Set(
      (rows ?? []).flatMap((row) => row.document.fields.map((f) => f.name)),
    ),
  ];
  const newFields = loadedFields.filter((name) => !knownFields.includes(name));
  if (newFields.length > 0) {
    setKnownFields([...knownFields, ...newFields]);
  }

  const pageCount = Math.max(1, Math.ceil((total ?? 0) / SEARCH_PAGE_SIZE));
  if (rows?.length === 0 && request.page > 1 && total !== null && !busy) {
    setRequest({ ...request, page: pageCount, run: request.run + 1 });
  }

  const defaultField = request.field || SEARCH_DEFAULT_FIELD;
  const typedField = field.trim() || SEARCH_DEFAULT_FIELD;
  const suggestedField =
    ["Title", "Content", "Name"].find((name) => knownFields.includes(name)) ??
    knownFields[0];
  const fieldMissing = (name: string): boolean =>
    knownFields.length > 0 && !knownFields.includes(name);
  const unpublished = summary?.indexName
    ? !summary.status.hasPublishedGeneration
    : false;

  const submit = (next: Omit<SearchRequest, "page" | "run">): void => {
    if (busy) {
      return;
    }
    setSelected(null);
    setRequest((current) => ({
      ...next,
      page: 1,
      run: current.run + 1,
    }));
  };

  const searchFields = fieldsCommand.result?.fields ?? [];
  const fieldsFailure = fieldsCommand.error ?? fieldsCommand.result?.error;
  const typedFields = searchFields.filter((f) => f.kind !== "untyped");
  const fieldOf = (name: string) => searchFields.find((f) => f.name === name);
  const activeFilters: SearchFilterArgument[] = filters.flatMap((filter) => {
    const kind = fieldOf(filter.field)?.kind;
    const value = filter.value.trim();
    return !kind || kind === "untyped" || !value || value === SEARCH_BOOLEAN_ANY
      ? []
      : [{ field: filter.field, value }];
  });
  const updateFilter = (id: number, change: Partial<SearchFilterDraft>) => {
    setFilters((current) =>
      current.map((f) => (f.id === id ? { ...f, ...change } : f)),
    );
  };
  const submitForm = (): void => {
    if (mode === "query") {
      const nextQuery = query.trim();
      submit(
        nextQuery
          ? {
              kind: "query",
              query: nextQuery,
              field: field.trim(),
              filters: [],
            }
          : SEARCH_BROWSE,
      );
      return;
    }
    submit(
      activeFilters.length > 0
        ? { kind: "filter", query: "", field: "", filters: activeFilters }
        : SEARCH_BROWSE,
    );
  };

  const open = (row: SearchRow, index: number): void => {
    openerIndex.current = index;
    setSelected(row);
  };
  const viewButtons = (): HTMLElement[] =>
    Array.from(
      resultsRef.current?.querySelectorAll<HTMLElement>(
        '[data-testid="button-view"]',
      ) ?? [],
    );
  // Row areas are not focusable, so focus returns to the row's View action.
  const close = (): void => {
    setSelected(null);
    const index = openerIndex.current;
    openerIndex.current = null;
    requestAnimationFrame(() => {
      if (index !== null) {
        viewButtons()[index]?.focus();
      }
    });
  };
  // The native action cell does not apply the action label to its button.
  React.useEffect(() => {
    viewButtons().forEach((button, index) => {
      const row = rows?.[index];
      if (row) {
        button.setAttribute(
          "aria-label",
          `View stored record of ${searchTitle(row.document)}`,
        );
      }
    });
  });

  const columns: TableColumn[] = [
    searchColumn("position", "#", 4, 6),
    searchColumn("title", "Title", 24, 80),
    searchColumn("type", "Content type", 16, 40),
    searchColumn("language", "Language", 8, 14),
    ...(isQuery ? [searchColumn("score", "Score", 8, 12)] : []),
    searchColumn("url", "URL", 16, 60),
    {
      ...searchColumn("actions", "Actions", 6, 8),
      contentType: ColumnContentType.Action,
    },
  ];
  const tableRows: TableRow[] = (rows ?? []).map((row, index) => ({
    identifier: row.document.documentId,
    disabled: false,
    cells: [
      searchCell("position", String(row.position)),
      searchCell("title", searchTitle(row.document)),
      contentTypeCell(
        searchFieldValue(row.document, "ContentTypeName"),
        summary?.contentTypeDisplayNames,
      ),
      searchCell(
        "language",
        searchFieldValue(row.document, "LanguageName") ?? "",
      ),
      ...(row.score === null
        ? []
        : [searchCell("score", row.score.toFixed(4))]),
      searchCell("url", searchFieldValue(row.document, "Url") ?? ""),
      {
        type: CellType.Action,
        columnName: "actions",
        actions: [
          {
            identifier: "view",
            label: "View stored record",
            title: `View the stored record of ${searchTitle(row.document)}`,
            icon: "xp-eye",
            disabled: false,
            destructive: false,
          },
        ],
        onInvokeAction: () => {
          open(row, index);
          return Promise.resolve();
        },
      } satisfies ActionCell,
    ],
  }));

  const summaryText =
    total === null || rows === null || busy
      ? null
      : total === 0
        ? isQuery
          ? "0 matching documents."
          : "0 stored documents."
        : `Showing ${String(firstPosition)}–${String(firstPosition + rows.length - 1)} of ${searchCount(
            total,
            isQuery ? "matching document" : "stored document",
          )}${isQuery ? ", highest score first" : ""}.`;

  return (
    <>
      <Card
        headline={
          <span role="heading" aria-level={1}>
            Search
          </span>
        }
        description={
          <span data-testid="search-context">
            Lists the stored records in the published index. Search without
            filters to browse every document, or add filters that must all
            match. Text filters and Lucene queries use
            {summary?.analyzerName
              ? ` the ${summary.analyzerName} analyzer configured for this index`
              : " the analyzer configured for this index"}
            .
          </span>
        }
      >
        <Stack spacing={Spacing.L}>
          {unpublished && (
            <div data-testid="search-unpublished">
              <Callout
                type={CalloutType.FriendlyWarning}
                placement={CalloutPlacementType.OnPaper}
                headline="Nothing to search yet"
              >
                The index has no published generation. Rebuild the index and
                wait for the rebuild to finish, then search again.
              </Callout>
            </div>
          )}
          <form
            role="search"
            aria-label="Search stored documents"
            onSubmit={(event) => {
              event.preventDefault();
              submitForm();
            }}
          >
            <Stack spacing={Spacing.M}>
              <RadioGroup
                name="lucene-search-mode"
                label="Search with"
                inline
                value={mode}
                onChange={(value) => {
                  setMode(value === "query" ? "query" : "filters");
                }}
              >
                <RadioButton value="filters">Filters</RadioButton>
                <RadioButton value="query">Lucene query (advanced)</RadioButton>
              </RadioGroup>
              {mode === "filters" && (
                <div data-testid="search-filters">
                  <Stack spacing={Spacing.M}>
                    {fieldsCommand.busy && <Loading />}
                    {fieldsFailure && (
                      <Error_
                        message={fieldsFailure}
                        retry={() => void loadFields()}
                      />
                    )}
                    {fieldsCommand.result &&
                      !fieldsFailure &&
                      typedFields.length === 0 && (
                        <span
                          data-testid="search-no-typed-fields"
                          style={{ color: Colors.TextLowEmphasis }}
                        >
                          {searchFields.length === 0
                            ? "No indexed fields were found. Rebuild the index, then search again."
                            : "The indexing strategy declares no typed fields. Use a Lucene query to search this index."}
                        </span>
                      )}
                    {typedFields.length > 0 && (
                      <Stack spacing={Spacing.S}>
                        {filters.length > 0 && (
                          <span
                            data-testid="search-filter-context"
                            style={{ color: Colors.TextLowEmphasis }}
                          >
                            Documents must match all filters. In text and exact
                            values, * matches any characters and ? matches one
                            character.
                          </span>
                        )}
                        {filters.map((filter, index) => {
                          const descriptor = fieldOf(filter.field);
                          const kind = descriptor?.kind;
                          const showLabels = index === 0;
                          return (
                            <div
                              key={filter.id}
                              data-testid="search-filter"
                              role="group"
                              aria-label={`Filter ${String(index + 1)}${descriptor ? `: ${descriptor.displayName}` : ""}`}
                            >
                              <Row
                                spacingX={Spacing.M}
                                spacingY={Spacing.S}
                                alignY={LayoutAlignment.End}
                                wrap={RowWrap.Wrap}
                              >
                                <Column
                                  cols={Cols.Col12}
                                  colsMd={Cols.Col6}
                                  colsLg={Cols.Col5}
                                >
                                  <Select
                                    id={`lucene-filter-${String(filter.id)}-field`}
                                    label={showLabels ? "Field" : undefined}
                                    placeholder="Choose a field"
                                    value={filter.field || undefined}
                                    onChange={(value) => {
                                      updateFilter(filter.id, {
                                        field: value ?? "",
                                        value: "",
                                      });
                                    }}
                                  >
                                    {searchFields.map((f) => (
                                      <MenuItem
                                        key={f.name}
                                        value={f.name}
                                        primaryLabel={f.displayName}
                                        secondaryLabel={
                                          f.displayName === f.name
                                            ? SEARCH_KIND_LABELS[f.kind]
                                            : `${f.name} · ${SEARCH_KIND_LABELS[f.kind]}`
                                        }
                                        tooltipText={
                                          f.kind === "untyped"
                                            ? "The indexing strategy does not declare this field. Search it with a Lucene query."
                                            : undefined
                                        }
                                        disabled={f.kind === "untyped"}
                                      />
                                    ))}
                                  </Select>
                                </Column>
                                <Column
                                  cols={Cols.Col12}
                                  colsMd={Cols.Col5}
                                  colsLg={Cols.Col6}
                                >
                                  <div
                                    style={{
                                      display: "flex",
                                      alignItems: "flex-end",
                                      gap: "16px",
                                    }}
                                  >
                                    <div
                                      style={{ flex: "1 1 auto", minWidth: 0 }}
                                    >
                                      {kind === "boolean" ? (
                                        <Select
                                          id={`lucene-filter-${String(filter.id)}-value`}
                                          label={
                                            showLabels ? "Value" : undefined
                                          }
                                          value={
                                            filter.value || SEARCH_BOOLEAN_ANY
                                          }
                                          onChange={(value) => {
                                            updateFilter(filter.id, {
                                              value:
                                                value ?? SEARCH_BOOLEAN_ANY,
                                            });
                                          }}
                                        >
                                          <MenuItem
                                            value={SEARCH_BOOLEAN_ANY}
                                            primaryLabel="Any"
                                            secondaryLabel="Does not filter"
                                          />
                                          <MenuItem
                                            value="true"
                                            primaryLabel="Yes"
                                          />
                                          <MenuItem
                                            value="false"
                                            primaryLabel="No"
                                          />
                                        </Select>
                                      ) : (
                                        <Input
                                          id={`lucene-filter-${String(filter.id)}-value`}
                                          label={
                                            showLabels ? "Value" : undefined
                                          }
                                          value={filter.value}
                                          disabled={
                                            kind !== "text" && kind !== "exact"
                                          }
                                          placeholder={
                                            kind === "text" || kind === "exact"
                                              ? SEARCH_VALUE_PLACEHOLDERS[kind]
                                              : "Choose a field first"
                                          }
                                          onChange={(event) => {
                                            updateFilter(filter.id, {
                                              value: event.target.value,
                                            });
                                          }}
                                        />
                                      )}
                                    </div>
                                    <div style={{ flex: "none" }}>
                                      <RemoveButton
                                        title={`Remove filter ${String(index + 1)}${descriptor ? `: ${descriptor.displayName}` : ""}`}
                                        onClick={() => {
                                          setFilters((current) =>
                                            current.filter(
                                              (f) => f.id !== filter.id,
                                            ),
                                          );
                                        }}
                                      />
                                    </div>
                                  </div>
                                </Column>
                              </Row>
                            </div>
                          );
                        })}
                        <div>
                          <Button
                            type={ButtonType.Button}
                            size={ButtonSize.S}
                            color={ButtonColor.Secondary}
                            icon="xp-plus"
                            label="Add filter"
                            disabled={filters.length >= SEARCH_MAX_FILTERS}
                            onClick={() => {
                              const id = nextFilterId.current;
                              nextFilterId.current += 1;
                              setFilters((current) => [
                                ...current,
                                { id, field: "", value: "" },
                              ]);
                            }}
                          />
                        </div>
                      </Stack>
                    )}
                  </Stack>
                </div>
              )}
              {mode === "query" && (
                <>
                  <Input
                    id="lucene-search-query"
                    label="Query"
                    value={query}
                    placeholder="Leave blank to list all documents"
                    explanationText="Lucene query syntax. Prefix a term with a field name, for example Title:coffee. Leave blank to list every stored document."
                    onChange={(event) => {
                      setQuery(event.target.value);
                    }}
                  />
                  <Input
                    id="lucene-search-field"
                    label="Default field"
                    value={field}
                    placeholder={SEARCH_DEFAULT_FIELD}
                    explanationText={
                      fieldMissing(typedField)
                        ? `Terms without a field prefix search ${typedField}, which no stored document contains. Enter a field${suggestedField ? ` such as ${suggestedField}` : ""} or prefix your terms.`
                        : `Terms without a field prefix search this field. Leave empty to search ${SEARCH_DEFAULT_FIELD}.`
                    }
                    onChange={(event) => {
                      setField(event.target.value);
                    }}
                  />
                  {knownFields.length > 0 && (
                    <div data-testid="search-fields">
                      <TextWithLabel
                        label="Stored fields in this index"
                        value={knownFields.join(", ")}
                      />
                    </div>
                  )}
                </>
              )}
              <Inline spacing={Spacing.S}>
                <Button
                  type={ButtonType.Submit}
                  color={ButtonColor.Primary}
                  label="Search"
                  inProgress={busy}
                  disabled={busy}
                />
                <Button
                  type={ButtonType.Button}
                  label="Clear"
                  disabled={
                    busy ||
                    (!isQuery && !query && !field && filters.length === 0)
                  }
                  onClick={() => {
                    setQuery("");
                    setField("");
                    setFilters([]);
                    submit(SEARCH_BROWSE);
                  }}
                />
              </Inline>
            </Stack>
          </form>
        </Stack>
      </Card>
      <Card>
        <Stack spacing={Spacing.M}>
          <div
            role="heading"
            aria-level={2}
            data-testid="search-results-heading"
          >
            <Headline size={HeadlineSize.M}>
              {isQuery ? "Matching documents" : "Stored documents"}
            </Headline>
          </div>
          {busy && <Loading />}
          {failure && <Error_ message={failure} retry={execute} />}
          {isQuery && matching.result?.parsedQuery && !failure && (
            <span>
              Parsed query:{" "}
              <code data-testid="search-parsed">
                {matching.result.parsedQuery}
              </code>
            </span>
          )}
          {total === 0 && !failure && !busy && (
            <div data-testid="search-empty">
              <Callout
                type={CalloutType.QuickTip}
                placement={CalloutPlacementType.OnPaper}
                headline={
                  isQuery
                    ? "No documents matched"
                    : "The index has no stored documents"
                }
              >
                {!isQuery
                  ? "Rebuild the index from the index listing, then search again."
                  : !isRawQuery
                    ? "No document matches every filter. Remove a filter, use fewer words, or check that exact values match the indexed value, including case."
                    : fieldMissing(defaultField) && !request.query.includes(":")
                      ? `Terms without a field prefix searched ${defaultField}, which no stored document contains. Prefix your terms with a field${suggestedField ? `, for example ${suggestedField}:${request.query}` : ""}, or change the default field.`
                      : "Check the parsed query to see how the analyzer processed your terms. Try fewer or broader terms or another field, or clear the query to list all documents."}
              </Callout>
            </div>
          )}
          {rows && rows.length > 0 && (
            <>
              <div ref={resultsRef} data-testid="search-results">
                <Table
                  columns={columns}
                  rows={tableRows}
                  onRowClick={(identifier) => {
                    const index = rows.findIndex(
                      (r) => r.document.documentId === identifier,
                    );
                    if (index >= 0) {
                      open(rows[index], index);
                    }
                  }}
                />
              </div>
            </>
          )}
          {total !== null && pageCount > 1 && !failure && (
            <nav aria-label="Search result pages" data-testid="search-pages">
              <Pagination
                selectedPage={request.page}
                totalPages={pageCount}
                onPageChange={(page) => {
                  if (busy || page === request.page) {
                    return;
                  }
                  setSelected(null);
                  setRequest({ ...request, page, run: request.run + 1 });
                }}
              />
            </nav>
          )}
          {summaryText && (
            <span
              aria-live="polite"
              data-testid="search-total"
              style={{ color: Colors.TextLowEmphasis }}
            >
              {summaryText}
            </span>
          )}
        </Stack>
      </Card>
      <SearchDetails
        row={selected}
        contentTypeNames={summary?.contentTypeDisplayNames}
        onClose={close}
      />
    </>
  );
};

export const IndexInspectorTemplate = ({
  tab,
}: IndexInspectorProps): React.JSX.Element => (
  <div
    data-testid={`inspector-${tab}`}
    style={{ color: Colors.TextDefaultOnLight }}
  >
    <Box spacing={Spacing.XL}>
      <Stack spacing={Spacing.XL}>
        {tab === "overview" && <IndexOverviewTab />}
        {tab === "search" && <SearchTab />}
        {!["overview", "search"].includes(tab) && (
          <Error_ message="The inspector tab is not supported." />
        )}
      </Stack>
    </Box>
  </div>
);
