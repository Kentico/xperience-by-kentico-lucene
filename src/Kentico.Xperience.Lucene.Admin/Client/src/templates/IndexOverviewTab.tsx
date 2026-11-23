import {
  BarItem,
  Button,
  ButtonColor,
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
  Dialog,
  Headline,
  Icon,
  HeadlineSize,
  Inline,
  Pagination,
  Row,
  SimpleStatusError,
  SimpleStatusSuccess,
  SimpleStatusWarning,
  Spacing,
  Spinner,
  Stack,
  Table,
  TextWithLabel,
  Tooltip,
  TooltipPlacement,
  type ComponentCell,
  type StringCell,
  type TableColumn,
  type TableRow,
  type TestId,
} from "@kentico/xperience-admin-components";
import * as React from "react";
import { useCommandResult } from "../hooks/useCommandResult";
import type {
  DiagnosticsPageArguments,
  DiagnosticsRunArguments,
  IndexRebuildAccessResult,
  IndexRebuildResult,
  IndexSummaryResult,
  LuceneDiagnosticCategory,
  LuceneDiagnosticItem,
  LuceneDiagnosticsPage,
  LuceneDiagnosticsRun,
  LuceneIndexStatus,
} from "../models/IndexInspectorModels";

const formatDate = (value: string | null): string =>
  value ? new Date(value).toLocaleString() : "Never";

const mutedStyle: React.CSSProperties = { color: Colors.TextLowEmphasis };
// Secondary code names use the admin label size (design token --font-size-s, 12px; not exposed by the host).
const codeStyle: React.CSSProperties = {
  ...mutedStyle,
  fontSize: "12px",
};
// The admin shell's inherited text color is red, so set the default text color explicitly.
const textStyle: React.CSSProperties = { color: Colors.TextDefaultOnLight };

const fieldGridStyle: React.CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fit, minmax(12rem, 1fr))",
  gap: Spacing.XL,
};

const asTestId = (value: string): TestId => value as TestId;

// Native Headline renders a div, so the semantic level is added here.
const SectionHeading = ({
  level,
  size,
  children,
}: {
  readonly level: 1 | 2 | 3;
  readonly size: HeadlineSize;
  readonly children: React.ReactNode;
}): React.JSX.Element => (
  <div role="heading" aria-level={level}>
    <Headline size={size}>{children}</Headline>
  </div>
);

// eslint-disable-next-line @typescript-eslint/no-unnecessary-type-parameters
const useCommand = <TResult, TData = void>(name: string) => {
  const command = useCommandResult<TResult, TData>(name);
  const [result, setResult] = React.useState<TResult | null>(null);
  const [loadedAt, setLoadedAt] = React.useState<string | null>(null);
  const [error, setError] = React.useState<string | null>(null);
  const [busy, setBusy] = React.useState(false);
  const mounted = React.useRef(true);

  React.useEffect(() => {
    mounted.current = true;
    return () => {
      mounted.current = false;
    };
  }, []);

  // Keeps the previous result while running so a refresh does not blank the page.
  const run = React.useCallback(
    async (data?: TData): Promise<void> => {
      setBusy(true);
      setError(null);
      try {
        const value = await command(data);
        if (mounted.current) {
          setResult(value);
          setLoadedAt(new Date().toLocaleString());
        }
      } catch (cause: unknown) {
        if (mounted.current) {
          setResult(null);
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

  return { result, loadedAt, error, busy, run };
};

const ErrorCallout = ({
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

const Loading = ({ label }: { readonly label: string }): React.JSX.Element => (
  <div
    role="status"
    aria-label={label}
    data-testid="inspector-loading"
    style={textStyle}
  >
    <Inline spacing={Spacing.S}>
      <Spinner />
      <span>{label}…</span>
    </Inline>
  </div>
);

const DataTable = ({
  headings,
  values,
  testId,
}: {
  readonly headings: string[];
  readonly values: React.ReactNode[][];
  readonly testId?: string;
}): React.JSX.Element => {
  const columns: TableColumn[] = headings.map((caption, index) => ({
    name: String(index),
    caption,
    visible: true,
    minWidth: 16,
    maxWidth: 80,
    contentType: ColumnContentType.Text,
    sortable: false,
    searchable: false,
  }));
  const rows: TableRow[] = values.map((rowValues, index) => ({
    identifier: index,
    disabled: false,
    cells: rowValues.map((value, columnIndex): ComponentCell | StringCell =>
      typeof value === "string" || typeof value === "number"
        ? {
            type: CellType.String,
            columnName: String(columnIndex),
            value: String(value),
          }
        : {
            type: CellType.Component,
            columnName: String(columnIndex),
            component: () => value,
          },
    ),
  }));
  return (
    <div data-testid={testId}>
      <Table columns={columns} rows={rows} />
    </div>
  );
};

const ContentTypeList = ({
  contentTypes,
  displayNames,
  emptyText,
  label,
  testId,
}: {
  readonly contentTypes: string[];
  readonly displayNames: Readonly<Record<string, string>>;
  readonly emptyText: string;
  readonly label: string;
  readonly testId: string;
}): React.JSX.Element => (
  // Block wrapper so the parent Stack spacing applies (it sets margin-top).
  <div>
    {contentTypes.length === 0 ? (
      <span data-testid={testId} style={mutedStyle}>
        {emptyText}
      </span>
    ) : (
      <ul
        data-testid={testId}
        aria-label={label}
        style={{
          display: "grid",
          gap: Spacing.XS,
          listStyle: "none",
          margin: 0,
          padding: 0,
        }}
      >
        {contentTypes.map((codeName) => {
          const displayName = displayNames[codeName];
          return (
            <li
              key={codeName}
              data-testid="overview-content-type"
              style={{
                display: "flex",
                flexWrap: "wrap",
                columnGap: Spacing.S,
              }}
            >
              {displayName && displayName !== codeName ? (
                <>
                  <span
                    data-testid="overview-content-type-display"
                    style={textStyle}
                  >
                    {displayName}
                  </span>
                  <span
                    data-testid="overview-content-type-code"
                    style={codeStyle}
                  >
                    {codeName}
                  </span>
                </>
              ) : (
                <span
                  data-testid="overview-content-type-code"
                  style={textStyle}
                >
                  {codeName}
                </span>
              )}
            </li>
          );
        })}
      </ul>
    )}
  </div>
);

const Configuration = ({
  summary,
}: {
  readonly summary: IndexSummaryResult;
}): React.JSX.Element => {
  const pathCount = summary.channels.reduce(
    (count, channel) => count + channel.paths.length,
    0,
  );
  const displayNames = summary.contentTypeDisplayNames ?? {};
  return (
    <Card fullHeight>
      <Stack spacing={Spacing.XL}>
        <SectionHeading level={2} size={HeadlineSize.M}>
          Configuration
        </SectionHeading>
        <div data-testid="overview-table" style={fieldGridStyle}>
          <TextWithLabel
            label="Indexing strategy"
            value={summary.strategyName}
          />
          <TextWithLabel label="Analyzer" value={summary.analyzerName} />
          <TextWithLabel
            label="Languages"
            value={summary.languages.join(", ") || "No languages selected"}
          />
        </div>
        <Stack spacing={Spacing.M}>
          <SectionHeading level={3} size={HeadlineSize.S}>
            Reusable content types
          </SectionHeading>
          <ContentTypeList
            testId="overview-reusable-types"
            label="Reusable content types"
            contentTypes={summary.reusableContentTypes}
            displayNames={displayNames}
            emptyText="No reusable content types are indexed."
          />
        </Stack>
        <Stack spacing={Spacing.M}>
          <SectionHeading level={3} size={HeadlineSize.S}>
            Website channel content
          </SectionHeading>
          {summary.channels.length > 0 && (
            <span style={mutedStyle}>
              {summary.channels.length}{" "}
              {summary.channels.length === 1 ? "channel" : "channels"},{" "}
              {pathCount} included {pathCount === 1 ? "path" : "paths"}
            </span>
          )}
          <div data-testid="overview-channels">
            {summary.channels.length === 0 ? (
              <span style={mutedStyle}>
                No website channel content is indexed.
              </span>
            ) : (
              <Stack spacing={Spacing.XL}>
                {summary.channels.map((channel) => (
                  <section
                    key={channel.channelName}
                    aria-label={channel.channelName}
                    data-testid="overview-channel"
                  >
                    <Stack spacing={Spacing.M}>
                      <div role="heading" aria-level={4}>
                        <strong>{channel.channelName}</strong>
                      </div>
                      {channel.paths.length === 0 ? (
                        <span style={mutedStyle}>No included paths.</span>
                      ) : (
                        channel.paths.map((path) => (
                          <div
                            key={path.aliasPath}
                            data-testid="overview-path"
                            style={fieldGridStyle}
                          >
                            <TextWithLabel
                              label="Path"
                              value={path.aliasPath}
                            />
                            <div data-testid="overview-path-types">
                              <Stack spacing={Spacing.XS}>
                                <span style={mutedStyle}>Content types</span>
                                <ContentTypeList
                                  testId="overview-path-type-list"
                                  label={`Content types for ${path.aliasPath}`}
                                  contentTypes={path.contentTypes}
                                  displayNames={displayNames}
                                  emptyText="No content types selected."
                                />
                              </Stack>
                            </div>
                          </div>
                        ))
                      )}
                    </Stack>
                  </section>
                ))}
              </Stack>
            )}
          </div>
        </Stack>
      </Stack>
    </Card>
  );
};

// Label with a native tooltip on a focusable info icon (LabelWithTooltip's icon is not focusable).
const PropertyLabel = ({
  label,
  help,
}: {
  readonly label: string;
  readonly help: string;
}): React.JSX.Element => (
  <span
    style={{ display: "inline-flex", alignItems: "center", gap: Spacing.S }}
  >
    <span>{label}</span>
    <Tooltip tooltipText={help} placement={TooltipPlacement.Top}>
      <span
        tabIndex={0}
        role="img"
        aria-label={help}
        data-testid="status-help"
        style={{ color: Colors.IconDefault, display: "inline-flex" }}
      >
        <Icon name="xp-i-circle" />
      </span>
    </Tooltip>
  </span>
);

type RebuildOutcome =
  | { readonly kind: "requested"; readonly at: string }
  | { readonly kind: "error"; readonly message: string };

/** Requests a rebuild like the listing action. Completion is not tracked, so only the request is reported. */
const RebuildAction = ({
  canRebuild,
  onOutcome,
}: {
  readonly canRebuild: boolean;
  readonly onOutcome: (outcome: RebuildOutcome) => void;
}): React.JSX.Element => {
  const rebuild = useCommandResult<IndexRebuildResult>("RebuildIndex");
  const [confirming, setConfirming] = React.useState(false);
  const [requesting, setRequesting] = React.useState(false);

  const confirm = async (): Promise<void> => {
    setRequesting(true);
    try {
      const result = await rebuild();
      onOutcome(
        result.requested
          ? { kind: "requested", at: new Date().toLocaleString() }
          : {
              kind: "error",
              message: result.error ?? "The rebuild could not be requested.",
            },
      );
    } catch (cause: unknown) {
      onOutcome({
        kind: "error",
        message:
          cause instanceof Error
            ? cause.message
            : "The rebuild could not be requested. Please try again.",
      });
    } finally {
      setRequesting(false);
      setConfirming(false);
    }
  };

  return (
    <>
      <Button
        type={ButtonType.Button}
        color={ButtonColor.Secondary}
        icon="xp-rotate-right"
        label="Rebuild"
        dataTestId={asTestId("overview-rebuild")}
        inProgress={requesting}
        disabled={!canRebuild || requesting}
        onClick={() => {
          setConfirming(true);
        }}
      />
      <Dialog
        isOpen={confirming}
        headline="Rebuild index?"
        isDismissable={!requesting}
        actionInProgress={requesting}
        headerCloseButton={{ tooltipText: "Close" }}
        onClose={() => {
          setConfirming(false);
        }}
        confirmAction={{
          label: "Rebuild",
          onClick: () => {
            void confirm();
          },
        }}
        cancelAction={{
          label: "Cancel",
          disabled: requesting,
          onClick: () => {
            setConfirming(false);
          },
        }}
      >
        <span style={textStyle}>
          The index is rebuilt from its current configuration in the background.
          Until it finishes, searches can return incomplete or no results.
          Progress and completion are not tracked here; refresh the status to
          check the index.
        </span>
      </Dialog>
    </>
  );
};

const RebuildMessage = ({
  canRebuild,
  outcome,
}: {
  readonly canRebuild: boolean | null;
  readonly outcome: RebuildOutcome | null;
}): React.JSX.Element | null => {
  if (canRebuild === false) {
    return (
      <span style={mutedStyle} data-testid="overview-rebuild-message">
        You don&apos;t have permission to rebuild this index.
      </span>
    );
  }
  if (!outcome) {
    return null;
  }
  return (
    <div role="status" data-testid="overview-rebuild-message">
      {outcome.kind === "requested" ? (
        <Stack spacing={Spacing.XS}>
          <div>
            <SimpleStatusSuccess content={{ label: "Rebuild requested" }} />
          </div>
          <span style={mutedStyle}>
            Requested at {outcome.at}. Completion is not tracked; refresh the
            status to check the index.
          </span>
        </Stack>
      ) : (
        <SimpleStatusError content={{ label: outcome.message }} />
      )}
    </div>
  );
};

const Health = ({
  status,
  checkedAt,
  refreshing,
  onRefresh,
}: {
  readonly status: LuceneIndexStatus;
  readonly checkedAt: string | null;
  readonly refreshing: boolean;
  readonly onRefresh: () => void;
}): React.JSX.Element => {
  const readable = status.hasPublishedGeneration && !status.readError;
  const unavailable = "Unavailable";
  const otherGenerations = status.generations.filter((g) => !g.isPublished);
  const access = useCommand<IndexRebuildAccessResult>("LoadRebuildAccess");
  const loadAccess = access.run;
  React.useEffect(() => {
    void loadAccess();
  }, [loadAccess]);
  // Unknown while loading; a failed check leaves the action disabled.
  const canRebuild = access.result?.canRebuild ?? (access.error ? false : null);
  const [rebuildOutcome, setRebuildOutcome] =
    React.useState<RebuildOutcome | null>(null);
  const onRebuildOutcome = (outcome: RebuildOutcome): void => {
    setRebuildOutcome(outcome);
    if (outcome.kind === "requested") {
      onRefresh();
    }
  };
  return (
    <Card
      fullHeight
      footer={
        <Button
          type={ButtonType.Button}
          label="Refresh status"
          inProgress={refreshing}
          disabled={refreshing}
          onClick={onRefresh}
        />
      }
    >
      <Stack spacing={Spacing.L}>
        <Stack spacing={Spacing.S}>
          <div
            style={{
              display: "flex",
              flexWrap: "wrap",
              alignItems: "center",
              justifyContent: "space-between",
              gap: Spacing.M,
            }}
          >
            <SectionHeading level={2} size={HeadlineSize.M}>
              Index health
            </SectionHeading>
            <RebuildAction
              canRebuild={canRebuild === true}
              onOutcome={onRebuildOutcome}
            />
          </div>
          <RebuildMessage canRebuild={canRebuild} outcome={rebuildOutcome} />
          <span style={mutedStyle} data-testid="status-checked-at">
            Read from this server&apos;s index storage at {checkedAt ?? "-"}{" "}
            (your local time).
          </span>
        </Stack>
        <div data-testid="status-health">
          {!status.hasPublishedGeneration ? (
            <SimpleStatusWarning
              content={{
                label:
                  "Not searchable: no generation of this index is published",
              }}
            />
          ) : status.readError ? (
            <SimpleStatusError
              content={{
                label: `Generation ${String(status.publishedGeneration)} is published but cannot be read`,
              }}
            />
          ) : (
            <SimpleStatusSuccess
              content={{
                label: `Searchable: generation ${String(status.publishedGeneration)} is published`,
              }}
            />
          )}
        </div>
        <DataTable
          testId="status-table"
          headings={["Property", "Value"]}
          values={[
            [
              <PropertyLabel
                label="Published generation"
                help="The generation that searches read."
              />,
              <span data-testid="status-published">
                {status.hasPublishedGeneration
                  ? `Generation ${String(status.publishedGeneration)}`
                  : "None"}
              </span>,
            ],
            [
              <PropertyLabel
                label="Stored documents"
                help="Live documents that searches can return."
              />,
              <span data-testid="status-documents">
                {readable ? status.documentCount : unavailable}
              </span>,
            ],
            [
              <PropertyLabel
                label="Deleted documents"
                help="Documents marked as deleted. They are not searchable and are removed when Lucene merges segments."
              />,
              <span data-testid="status-deleted">
                {readable ? status.deletedDocumentCount : unavailable}
              </span>,
            ],
            [
              <PropertyLabel
                label="Last write"
                help="Last change to the published generation's files, in your local time."
              />,
              <span data-testid="status-updated">
                {readable ? formatDate(status.lastUpdatedUtc) : unavailable}
              </span>,
            ],
            [
              <PropertyLabel
                label="Generations on disk"
                help={
                  otherGenerations.length > 0
                    ? "Only the published generation is searched. Other generations are older retained copies or one that is still being written."
                    : "Only the published generation is stored on disk."
                }
              />,
              <span data-testid="status-generations">
                {status.generations.length === 0
                  ? "None"
                  : status.generations
                      .map(
                        (g) =>
                          `${String(g.generation)} (${g.isPublished ? "published, searched" : "not searched"})`,
                      )
                      .join(", ")}
              </span>,
            ],
            [
              <PropertyLabel
                label="Last rebuild request"
                help="Recorded in memory on this server only. It is lost on restart, is not shared with other servers, and does not show whether the queued items were processed."
              />,
              <span data-testid="status-rebuild">
                {status.lastRebuildRequest
                  ? formatDate(status.lastRebuildRequest.requestedAtUtc)
                  : "None since this server started"}
              </span>,
            ],
            // Separate row: native table rows have a fixed height and clip wrapped values.
            ...(status.lastRebuildRequest
              ? [
                  [
                    <PropertyLabel
                      label="Items queued"
                      help="Items the last rebuild request on this server queued for indexing. Processing is not tracked."
                    />,
                    <span data-testid="status-rebuild-queued">
                      {status.lastRebuildRequest.queuedItemCount.toLocaleString()}
                    </span>,
                  ],
                ]
              : []),
          ]}
        />
      </Stack>
    </Card>
  );
};

const DiagnosticList = ({
  title,
  description,
  guidance,
  testId,
  count,
  items,
  runId,
  category,
  defaultExpanded = false,
}: {
  readonly title: string;
  readonly description: string;
  readonly guidance: string;
  readonly testId: string;
  readonly count: number;
  readonly items: LuceneDiagnosticItem[];
  readonly runId: string | null;
  readonly category: LuceneDiagnosticCategory;
  readonly defaultExpanded?: boolean;
}): React.JSX.Element => {
  const expandable = count > 0;
  const [expanded, setExpanded] = React.useState(defaultExpanded && expandable);
  const bodyId = `${testId}-body`;
  const ref = React.useRef<HTMLDivElement>(null);

  // The native BarItem header is a role="button" div without expanded state or Space key support.
  React.useEffect(() => {
    const header = ref.current?.querySelector<HTMLElement>('[role="button"]');
    if (!header) {
      return undefined;
    }
    header.querySelectorAll("button").forEach((button) => {
      button.tabIndex = -1;
      button.setAttribute("aria-hidden", "true");
    });
    if (!expandable) {
      header.removeAttribute("role");
      header.removeAttribute("tabindex");
      return undefined;
    }
    header.setAttribute("aria-expanded", String(expanded));
    header.setAttribute("aria-controls", bodyId);
    const onKeyDown = (event: KeyboardEvent): void => {
      if (event.key === " ") {
        event.preventDefault();
        setExpanded((value) => !value);
      }
    };
    header.addEventListener("keydown", onKeyDown);
    return () => {
      header.removeEventListener("keydown", onKeyDown);
    };
  }, [bodyId, expandable, expanded]);

  return (
    <BarItem
      ref={ref}
      dataTestId={asTestId(testId)}
      expandable={expandable}
      expanded={expanded}
      onHeaderClick={
        expandable
          ? () => {
              setExpanded((value) => !value);
            }
          : undefined
      }
      headerColumns={[
        {
          content: (
            <Stack spacing={Spacing.XS}>
              <strong>
                {title} ({count})
              </strong>
              <span>{description}</span>
            </Stack>
          ),
        },
      ]}
    >
      {expandable && (
        <div id={bodyId} data-testid={`${testId}-rows`}>
          <Stack spacing={Spacing.M}>
            <span>{guidance}</span>
            <DiagnosticRows
              runId={runId}
              category={category}
              testId={testId}
              count={count}
              firstPage={items}
            />
          </Stack>
        </div>
      )}
    </BarItem>
  );
};

const DIAGNOSTIC_PAGE_SIZE = 20;

const formatCount = (value: number): string => value.toLocaleString();

// Pages are read from the server's kept results for this run, so every row can be reached.
const DiagnosticRows = ({
  runId,
  category,
  testId,
  count,
  firstPage,
}: {
  readonly runId: string | null;
  readonly category: LuceneDiagnosticCategory;
  readonly testId: string;
  readonly count: number;
  readonly firstPage: LuceneDiagnosticItem[];
}): React.JSX.Element => {
  const [page, setPage] = React.useState(1);
  const {
    result: loaded,
    error: requestError,
    busy,
    run,
  } = useCommand<LuceneDiagnosticsPage, DiagnosticsPageArguments>(
    "LoadDiagnosticsPage",
  );
  const pageCount = runId ? Math.ceil(count / DIAGNOSTIC_PAGE_SIZE) : 1;
  const load = (target: number): void => {
    if (!runId) {
      return;
    }
    void run({
      runId,
      category,
      skip: (target - 1) * DIAGNOSTIC_PAGE_SIZE,
      take: DIAGNOSTIC_PAGE_SIZE,
    });
  };
  const items = page === 1 ? firstPage : (loaded?.items ?? []);
  const failure = page === 1 ? null : (requestError ?? loaded?.error ?? null);
  const first = (page - 1) * DIAGNOSTIC_PAGE_SIZE + 1;

  return (
    <Stack spacing={Spacing.M}>
      {busy ? (
        <Loading label={`Loading page ${String(page)}`} />
      ) : failure ? (
        <ErrorCallout
          message={failure}
          retry={
            requestError
              ? () => {
                  load(page);
                }
              : undefined
          }
        />
      ) : (
        <DataTable
          headings={["Item", "Language", "Content type", "Reason"]}
          values={items.map((item) => [
            item.name || item.itemGuid,
            item.languageName || "-",
            item.contentTypeName || "-",
            item.reason,
          ])}
        />
      )}
      {pageCount > 1 && (
        <nav
          aria-label={`${categoryLabels[category]} pages`}
          data-testid={`${testId}-pages`}
        >
          <Pagination
            selectedPage={page}
            totalPages={pageCount}
            onPageChange={(target) => {
              if (busy || target === page) {
                return;
              }
              setPage(target);
              if (target > 1) {
                load(target);
              }
            }}
          />
        </nav>
      )}
      {!busy && !failure && items.length > 0 && (
        <span
          aria-live="polite"
          data-testid={`${testId}-range`}
          style={mutedStyle}
        >
          {runId || count === items.length
            ? `Showing ${formatCount(first)}–${formatCount(first + items.length - 1)} of ${formatCount(count)} items.`
            : `Showing the first ${formatCount(items.length)} of ${formatCount(count)} items.`}
        </span>
      )}
    </Stack>
  );
};

const categoryLabels: Record<LuceneDiagnosticCategory, string> = {
  missing: "Indexable but missing from the index",
  orphans: "Stored but not selected",
  skipped: "Skipped by the strategy",
};

const Diagnostics = (): React.JSX.Element => {
  const {
    result: current,
    loadedAt,
    error,
    busy,
    run,
  } = useCommand<LuceneDiagnosticsRun, DiagnosticsRunArguments>(
    "StartDiagnostics",
  );
  const result = busy ? null : (current?.diagnostics ?? null);
  const runId = busy ? null : (current?.runId ?? null);
  const runDiagnostics = (): void => {
    void run({ take: DIAGNOSTIC_PAGE_SIZE });
  };
  const failure = error ?? result?.error;
  const healthy = result?.missingCount === 0 && result.orphanCount === 0;
  return (
    <Card>
      <Stack spacing={Spacing.L}>
        <Stack spacing={Spacing.S}>
          <SectionHeading level={2} size={HeadlineSize.M}>
            Indexing diagnostics
          </SectionHeading>
          <span style={mutedStyle}>
            Compares the items the index configuration selects with the
            documents stored in the published generation, and shows which items
            the indexing strategy skips. Read only: nothing is indexed or
            changed. Large indexes can take a while.
          </span>
        </Stack>
        <Inline spacing={Spacing.M}>
          <Button
            type={ButtonType.Button}
            label="Run diagnostics"
            inProgress={busy}
            disabled={busy}
            onClick={runDiagnostics}
          />
          {loadedAt && current && !busy && (
            <span data-testid="diagnostics-ran-at">
              Last run {loadedAt} (your local time)
            </span>
          )}
        </Inline>
        {busy && <Loading label="Running diagnostics" />}
        {!busy && failure && (
          <ErrorCallout message={failure} retry={runDiagnostics} />
        )}
        {result && !result.error && (
          <div data-testid="diagnostics-result" aria-live="polite">
            <Stack spacing={Spacing.M}>
              <div data-testid="diagnostics-health">
                {healthy ? (
                  <SimpleStatusSuccess
                    content={{
                      label:
                        "Every indexable item is stored and every stored document is still selected",
                    }}
                  />
                ) : (
                  <SimpleStatusWarning
                    content={{
                      label: `${String(result.missingCount)} indexable items are missing and ${String(result.orphanCount)} stored documents are no longer selected`,
                    }}
                  />
                )}
              </div>
              <span data-testid="diagnostics-summary">
                {result.selectedItemCount} items selected,{" "}
                {result.indexableCount} indexable, {result.skippedCount}{" "}
                skipped, {result.missingCount} not in the index,{" "}
                {result.orphanCount} stored but not selected.
              </span>
              <DiagnosticList
                title="Indexable but missing from the index"
                description="Indexable items with no document in the published generation."
                guidance="The strategy would index these items, but the published generation has no document for them. Indexing may still be queued, or the index is out of date and needs a rebuild."
                testId="diagnostics-missing"
                count={result.missingCount}
                items={result.missing}
                runId={runId}
                category="missing"
                defaultExpanded
              />
              <DiagnosticList
                title="Stored but not selected"
                description="Stored documents the current configuration no longer selects."
                guidance="The published generation has documents the current configuration no longer selects, such as removed content types, paths or languages. A rebuild removes them."
                testId="diagnostics-orphans"
                count={result.orphanCount}
                items={result.orphans}
                runId={runId}
                category="orphans"
                defaultExpanded
              />
              <DiagnosticList
                title="Skipped by the strategy"
                description="Selected items the indexing strategy does not index. Usually expected."
                guidance="These items match the index configuration, but the indexing strategy does not index them, for example secured items or items the strategy filters out. The reason column shows why."
                testId="diagnostics-skipped"
                count={result.skippedCount}
                items={result.skipped}
                runId={runId}
                category="skipped"
              />
            </Stack>
          </div>
        )}
      </Stack>
    </Card>
  );
};

/** Combined Overview: configuration and index health side by side on wide screens, diagnostics below. */
const OverviewContent = (): React.JSX.Element => {
  const {
    result: summary,
    loadedAt,
    error,
    busy,
    run,
  } = useCommand<IndexSummaryResult>("LoadSummary");
  const load = React.useCallback(() => {
    void run();
  }, [run]);
  React.useEffect(load, [load]);

  if (!summary) {
    return busy || !error ? (
      <Loading label="Loading index overview" />
    ) : (
      <ErrorCallout message={error} retry={load} />
    );
  }

  return (
    <Stack spacing={Spacing.XL}>
      {error && <ErrorCallout message={error} retry={load} />}
      {summary.status.readError && (
        <ErrorCallout message={summary.status.readError} retry={load} />
      )}
      {!summary.indexName && !summary.status.readError ? (
        <ErrorCallout message="The index was not found." />
      ) : (
        <>
          <header data-testid="overview-identity">
            <Stack spacing={Spacing.XS}>
              <SectionHeading level={1} size={HeadlineSize.L}>
                <span data-testid="overview-display-name">
                  {summary.displayName}
                </span>
              </SectionHeading>
              <span style={codeStyle}>
                Code name:{" "}
                <span data-testid="overview-code-name">
                  {summary.indexName}
                </span>
              </span>
            </Stack>
          </header>
          <div data-testid="overview-layout">
            <Row spacing={Spacing.XL}>
              <Column
                cols={Cols.Col12}
                colsSm={Cols.Col12}
                colsMd={Cols.Col6}
                colsLg={Cols.Col6}
                dataTestId={asTestId("overview-configuration")}
              >
                <section aria-label="Configuration">
                  <Configuration summary={summary} />
                </section>
              </Column>
              <Column
                cols={Cols.Col12}
                colsSm={Cols.Col12}
                colsMd={Cols.Col6}
                colsLg={Cols.Col6}
                dataTestId={asTestId("overview-health")}
              >
                <section aria-label="Index health">
                  <Health
                    status={summary.status}
                    checkedAt={loadedAt}
                    refreshing={busy}
                    onRefresh={load}
                  />
                </section>
              </Column>
            </Row>
          </div>
          <Diagnostics />
        </>
      )}
    </Stack>
  );
};

export const IndexOverviewTab = (): React.JSX.Element => (
  <div data-testid="overview-root" style={textStyle}>
    <OverviewContent />
  </div>
);
