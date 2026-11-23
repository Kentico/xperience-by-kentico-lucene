import {
  BorderRadius,
  Box,
  Button,
  ButtonColor,
  ButtonType,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Colors,
  Checkbox,
  Divider,
  DividerOrientation,
  Headline,
  HeadlineSize,
  Inline,
  Input,
  MenuItem,
  Paper,
  PaperElevation,
  Select,
  SidePanel,
  SidePanelSize,
  Spacing,
  Stack,
  TextWithLabel,
} from "@kentico/xperience-admin-components";
import * as React from "react";
import { ContentTypeSelector } from "../components/ContentTypeSelector";
import { RemoveButton } from "../components/RemoveButton";
import { useCommandResult } from "../hooks/useCommandResult";
import type {
  ChannelSectionArguments,
  ChannelSectionPath,
  ConfigurationOption,
  IndexConfigurationChannel,
  IndexConfigurationProps,
  IndexConfigurationSaveResult,
} from "../models/IndexConfigurationModels";

type Draft =
  | {
      readonly kind: "settings";
      displayName: string;
      strategyName: string;
      analyzerName: string;
      rebuildHook: string;
    }
  | { readonly kind: "languages"; languageNames: string[] }
  | { readonly kind: "reusable"; contentTypeNames: string[] }
  | {
      readonly kind: "channel";
      readonly isNew: boolean;
      websiteChannelName: string;
      paths: ChannelSectionPath[];
    };

const valueGrid: React.CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fill, minmax(14rem, 1fr))",
  gap: "var(--spacing-m, 16px)",
};

const headerRow: React.CSSProperties = {
  display: "flex",
  alignItems: "center",
  justifyContent: "space-between",
  gap: "var(--spacing-m, 16px)",
};

const pathRow: React.CSSProperties = {
  display: "flex",
  alignItems: "flex-start",
  gap: "var(--spacing-m, 16px)",
};

const optionLabel = (
  options: readonly ConfigurationOption[],
  value: string,
  fallback?: string,
): string => {
  const option = options.find(
    (o) => o.value.toLowerCase() === value.toLowerCase(),
  );
  return option
    ? option.label || option.value
    : `${fallback ?? value} (unavailable)`;
};

const panelTitle = (draft: Draft): string => {
  switch (draft.kind) {
    case "settings":
      return "Edit index settings";
    case "languages":
      return "Edit languages";
    case "reusable":
      return "Edit reusable content";
    case "channel": {
      if (draft.isNew) {
        return "Add website channel";
      }
      return "Edit website channel";
    }
  }
};

interface NamedValue {
  readonly primary: string;
  readonly secondary: string;
}

// Known values show their display name with the code name as secondary text; unknown values keep the code name.
const describe = (
  options: readonly ConfigurationOption[],
  value: string,
): NamedValue => {
  const option = options.find(
    (o) => o.value.toLowerCase() === value.toLowerCase(),
  );
  if (!option) {
    return { primary: value, secondary: "unavailable" };
  }
  const primary = option.label || option.value;
  return {
    primary,
    secondary: primary === option.value ? "" : option.value,
  };
};

// Unstyled admin text inherits an error-like red, so set the native default text color explicitly.
const defaultText: React.CSSProperties = { color: Colors.TextDefaultOnLight };

const secondaryText: React.CSSProperties = {
  color: Colors.TextLowEmphasis,
};

// Code names are secondary: muted and 12px, matching native MenuItem secondaryLabel.
const codeText: React.CSSProperties = {
  ...secondaryText,
  fontSize: 12,
  lineHeight: 1.1,
};

// Matches the Overview tab's read-only path and content type layout.
const plainList: React.CSSProperties = {
  display: "grid",
  gap: Spacing.XS,
  listStyle: "none",
  margin: 0,
  padding: 0,
};

const nameCodeRow: React.CSSProperties = {
  display: "flex",
  flexWrap: "wrap",
  alignItems: "baseline",
  columnGap: Spacing.S,
};

const pathGrid: React.CSSProperties = {
  display: "grid",
  gridTemplateColumns: "repeat(auto-fit, minmax(12rem, 1fr))",
  gap: Spacing.XL,
};

// Native TextWithLabel takes only string values, so immutable code names reuse its label look with codeText.
const CodeNameValue = ({
  label,
  value,
}: {
  readonly label: string;
  readonly value: string;
}): React.JSX.Element => (
  <div data-testid="config-code-name">
    <div
      style={{
        ...secondaryText,
        fontSize: 14,
        lineHeight: "16px",
        marginBottom: Spacing.XS,
      }}
    >
      {label}
    </div>
    <span data-testid="config-value-code" style={codeText}>
      {value}
    </span>
  </div>
);

const ReadOnlyList = ({
  label,
  values,
  emptyText,
}: {
  readonly label: string;
  readonly values: readonly NamedValue[];
  readonly emptyText: string;
}): React.JSX.Element =>
  values.length === 0 ? (
    <span style={secondaryText}>{emptyText}</span>
  ) : (
    <ul aria-label={label} style={plainList}>
      {values.map((value) => (
        <li
          key={`${value.primary}|${value.secondary}`}
          data-testid="config-named-value"
          style={nameCodeRow}
        >
          <span data-testid="config-value-name" style={defaultText}>
            {value.primary}
          </span>
          {value.secondary !== "" && (
            <span data-testid="config-value-code" style={codeText}>
              {value.secondary}
            </span>
          )}
        </li>
      ))}
    </ul>
  );

const SectionCard = ({
  title,
  testId,
  editLabel,
  onEdit,
  editDisabled = false,
  children,
}: {
  readonly title: string;
  readonly testId: string;
  readonly editLabel: string;
  readonly onEdit: () => void;
  readonly editDisabled?: boolean;
  readonly children: React.ReactNode;
}): React.JSX.Element => (
  <section aria-label={title} data-testid={testId}>
    <Paper>
      <Box spacing={Spacing.L}>
        <Stack spacing={Spacing.L}>
          <div style={headerRow}>
            <Headline size={HeadlineSize.M}>{title}</Headline>
            <Button
              type={ButtonType.Button}
              color={ButtonColor.Secondary}
              label={editLabel === "Add website channel" ? editLabel : "Edit"}
              aria-label={editLabel}
              icon={editLabel === "Add website channel" ? "xp-plus" : "xp-edit"}
              disabled={editDisabled}
              onClick={onEdit}
            />
          </div>
          {children}
        </Stack>
      </Box>
    </Paper>
  </section>
);

const ChannelReadOnly = ({
  channel,
  contentTypes,
  canEdit,
  onEdit,
}: {
  readonly channel: IndexConfigurationChannel;
  readonly contentTypes: readonly ConfigurationOption[];
  readonly canEdit: boolean;
  readonly onEdit: () => void;
}): React.JSX.Element => (
  <section
    aria-label={`Website channel ${channel.channelDisplayName}`}
    data-testid="config-channel"
    data-channel={channel.websiteChannelName}
  >
    <Stack spacing={Spacing.M}>
      <div style={headerRow}>
        <Stack spacing={Spacing.XS}>
          <div role="heading" aria-level={4} style={defaultText}>
            <strong>{channel.channelDisplayName}</strong>
          </div>
          <span
            style={codeText}
          >{`Code name: ${channel.websiteChannelName}`}</span>
        </Stack>
        <Button
          type={ButtonType.Button}
          color={ButtonColor.Secondary}
          label="Edit"
          icon="xp-edit"
          aria-label={`Edit ${channel.channelDisplayName}`}
          disabled={!canEdit}
          onClick={onEdit}
        />
      </div>
      {!canEdit && (
        <span>You do not have permission to view this website channel.</span>
      )}
      <div
        role="list"
        aria-label={`Included paths of ${channel.channelDisplayName}`}
      >
        <Stack spacing={Spacing.M}>
          {channel.paths.map((path) => (
            <div
              role="listitem"
              key={path.identifier ?? path.aliasPath}
              data-testid="config-path"
              data-path={path.aliasPath}
            >
              <div style={pathGrid}>
                <TextWithLabel label="Path" value={path.aliasPath} />
                <Stack spacing={Spacing.XS}>
                  <span style={secondaryText}>Content types</span>
                  <ReadOnlyList
                    label={`Content types for ${path.aliasPath}`}
                    values={path.contentTypes.map((t) =>
                      describe(contentTypes, t.value),
                    )}
                    emptyText="No content types selected."
                  />
                </Stack>
              </div>
            </div>
          ))}
        </Stack>
      </div>
    </Stack>
  </section>
);

const toDraftPaths = (
  channel: IndexConfigurationChannel | undefined,
): ChannelSectionPath[] =>
  channel
    ? channel.paths.map((p) => ({
        identifier: p.identifier,
        aliasPath: p.aliasPath,
        contentTypes: p.contentTypes.map((t) => t.value),
      }))
    : [{ identifier: null, aliasPath: "/%", contentTypes: [] }];

export const IndexConfigurationTemplate = (
  props: IndexConfigurationProps,
): React.JSX.Element => {
  const [config, setConfig] = React.useState(props.configuration);
  const [draft, setDraft] = React.useState<Draft | null>(null);
  const [initial, setInitial] = React.useState("");
  const [errors, setErrors] = React.useState<string[]>([]);
  const [busy, setBusy] = React.useState(false);

  const saveSettings = useCommandResult<
    IndexConfigurationSaveResult,
    {
      displayName: string;
      strategyName: string;
      analyzerName: string;
      rebuildHook: string;
    }
  >("SaveIndexSettings");
  const saveLanguages = useCommandResult<
    IndexConfigurationSaveResult,
    { languageNames: string[] }
  >("SaveLanguages");
  const saveReusable = useCommandResult<
    IndexConfigurationSaveResult,
    { contentTypeNames: string[] }
  >("SaveReusableContent");
  const saveChannel = useCommandResult<
    IndexConfigurationSaveResult,
    ChannelSectionArguments
  >("SaveChannel");
  const removeChannel = useCommandResult<
    IndexConfigurationSaveResult,
    { websiteChannelName: string }
  >("RemoveChannel");

  const dirty = draft !== null && JSON.stringify(draft) !== initial;

  React.useEffect(() => {
    if (!dirty) {
      return undefined;
    }
    const warn = (e: BeforeUnloadEvent): void => {
      e.preventDefault();
    };
    window.addEventListener("beforeunload", warn);
    return () => {
      window.removeEventListener("beforeunload", warn);
    };
  }, [dirty]);

  if (config === null) {
    return (
      <Callout
        type={CalloutType.FriendlyWarning}
        placement={CalloutPlacementType.OnPaper}
        headline="The index no longer exists."
      />
    );
  }

  const open = (next: Draft): void => {
    if (dirty && !window.confirm("Discard your unsaved changes?")) {
      return;
    }
    setDraft(next);
    setInitial(JSON.stringify(next));
    setErrors([]);
  };

  const closePanel = (): void => {
    setDraft(null);
    setErrors([]);
  };

  const requestClose = (): void => {
    if (busy) {
      return;
    }
    if (dirty && !window.confirm("Discard your unsaved changes?")) {
      return;
    }
    closePanel();
  };

  const run = async (
    action: () => Promise<IndexConfigurationSaveResult>,
  ): Promise<void> => {
    setBusy(true);
    setErrors([]);
    try {
      const result = await action();
      if (result.saved && result.configuration) {
        setConfig(result.configuration);
        closePanel();
      } else {
        setErrors(
          result.errors.length > 0 ? result.errors : ["Could not save."],
        );
      }
    } catch (e) {
      setErrors([e instanceof Error ? e.message : String(e)]);
    } finally {
      setBusy(false);
    }
  };

  const save = (): void => {
    if (draft === null || busy) {
      return;
    }
    switch (draft.kind) {
      case "settings":
        void run(() =>
          saveSettings({
            displayName: draft.displayName,
            strategyName: draft.strategyName,
            analyzerName: draft.analyzerName,
            rebuildHook: draft.rebuildHook,
          }),
        );
        break;
      case "languages":
        void run(() => saveLanguages({ languageNames: draft.languageNames }));
        break;
      case "reusable":
        void run(() =>
          saveReusable({ contentTypeNames: draft.contentTypeNames }),
        );
        break;
      case "channel":
        void run(() =>
          saveChannel({
            websiteChannelName: draft.websiteChannelName,
            isNew: draft.isNew,
            paths: draft.paths,
          }),
        );
        break;
    }
  };

  const remove = (): void => {
    if (draft?.kind !== "channel" || draft.isNew || busy) {
      return;
    }
    const title =
      config.channels.find(
        (c) => c.websiteChannelName === draft.websiteChannelName,
      )?.channelDisplayName ??
      optionLabel(props.channels, draft.websiteChannelName);
    if (
      !window.confirm(
        `Remove ${title} from the index? Its paths and content types are no longer indexed after the next rebuild.`,
      )
    ) {
      return;
    }
    void run(() =>
      removeChannel({ websiteChannelName: draft.websiteChannelName }),
    );
  };

  const update = (change: Partial<Draft>): void => {
    setDraft((d) => (d ? ({ ...d, ...change } as Draft) : d));
  };

  const updatePath = (
    index: number,
    change: Partial<ChannelSectionPath>,
  ): void => {
    setDraft((d) =>
      d?.kind === "channel"
        ? {
            ...d,
            paths: d.paths.map((p, i) =>
              i === index ? { ...p, ...change } : p,
            ),
          }
        : d,
    );
  };

  const configured = new Set(
    config.channels.map((c) => c.websiteChannelName.toLowerCase()),
  );
  const unusedChannels = props.channels.filter(
    (c) => !configured.has(c.value.toLowerCase()),
  );
  const canEditChannel = (name: string): boolean =>
    props.channels.some((c) => c.value.toLowerCase() === name.toLowerCase());
  const storedChannel =
    draft?.kind === "channel"
      ? config.channels.find(
          (c) => c.websiteChannelName === draft.websiteChannelName,
        )
      : undefined;
  const storedTypeLabels: Record<string, string> = Object.fromEntries(
    (storedChannel?.paths ?? []).flatMap((p) =>
      p.contentTypes.map((t) => [t.value, t.label]),
    ),
  );
  const reusableLabels: Record<string, string> = Object.fromEntries(
    config.reusableContentTypes.map((t) => [t.value, t.label]),
  );

  const renderFields = (): React.ReactNode => {
    if (draft === null) {
      return null;
    }
    switch (draft.kind) {
      case "settings":
        return (
          <Stack spacing={Spacing.L}>
            <Input
              label="Display name"
              value={draft.displayName}
              disabled={busy}
              explanationText="Shown in the administration only. Leave empty to use the code name."
              onChange={(e) => {
                update({ displayName: e.target.value });
              }}
            />
            <CodeNameValue
              label="Code name (cannot be changed)"
              value={config.indexName}
            />
            <Select
              label="Indexing strategy"
              value={draft.strategyName}
              disabled={busy}
              markAsRequired
              explanationText="Decides which fields are indexed for each content item."
              onChange={(value) => {
                update({ strategyName: value ?? "" });
              }}
            >
              {props.strategies.map((o) => (
                <MenuItem
                  key={o.value}
                  value={o.value}
                  primaryLabel={o.label}
                />
              ))}
            </Select>
            <Select
              label="Lucene analyzer"
              value={draft.analyzerName}
              disabled={busy}
              markAsRequired
              explanationText="Splits and normalizes text when indexing and searching."
              onChange={(value) => {
                update({ analyzerName: value ?? "" });
              }}
            >
              {props.analyzers.map((o) => (
                <MenuItem
                  key={o.value}
                  value={o.value}
                  primaryLabel={o.label}
                />
              ))}
            </Select>
            <Input
              label="Rebuild hook"
              value={draft.rebuildHook}
              disabled={busy}
              explanationText="Optional value stored with the index for custom rebuild integrations."
              onChange={(e) => {
                update({ rebuildHook: e.target.value });
              }}
            />
          </Stack>
        );
      case "languages":
        return (
          <div role="group" aria-label="Indexed languages">
            <Stack spacing={Spacing.S}>
              <TextWithLabel
                label="Indexed languages"
                value={`${String(draft.languageNames.length)} selected`}
              />
              {[
                ...props.languages,
                ...draft.languageNames
                  .filter((l) => !props.languages.some((o) => o.value === l))
                  .map((l) => ({ value: l, label: `${l} (unavailable)` })),
              ].map((o) => (
                <Checkbox
                  key={o.value}
                  label={o.label}
                  disabled={busy}
                  checked={draft.languageNames.includes(o.value)}
                  onChange={(_, checked) => {
                    update({
                      languageNames: checked
                        ? [...draft.languageNames, o.value]
                        : draft.languageNames.filter((l) => l !== o.value),
                    });
                  }}
                />
              ))}
            </Stack>
          </div>
        );
      case "reusable":
        return (
          <ContentTypeSelector
            label="Reusable content types"
            dataTestId="config-reusable-content-types"
            options={props.reusableContentTypes}
            selected={draft.contentTypeNames}
            selectedLabels={reusableLabels}
            disabled={busy}
            explanationText="Reusable content items of these types are indexed."
            onChange={(contentTypeNames) => {
              update({ contentTypeNames });
            }}
          />
        );
      case "channel":
        return (
          <Stack spacing={Spacing.L}>
            {draft.isNew ? (
              <Select
                label="Website channel"
                value={draft.websiteChannelName}
                placeholder="Select a channel"
                markAsRequired
                disabled={busy}
                onChange={(value) => {
                  update({ websiteChannelName: value ?? "" });
                }}
              >
                {unusedChannels.map((c) => (
                  <MenuItem
                    key={c.value}
                    value={c.value}
                    primaryLabel={c.label}
                  />
                ))}
              </Select>
            ) : (
              <div
                role="group"
                aria-label="Website channel"
                data-testid="config-panel-channel"
                style={headerRow}
              >
                <Stack spacing={Spacing.XS}>
                  <Headline size={HeadlineSize.M}>
                    {storedChannel?.channelDisplayName ??
                      draft.websiteChannelName}
                  </Headline>
                  <span
                    style={codeText}
                  >{`Website channel · ${draft.websiteChannelName}`}</span>
                </Stack>
                <RemoveButton
                  title={`Remove ${storedChannel?.channelDisplayName ?? draft.websiteChannelName} from index`}
                  disabled={busy}
                  onClick={remove}
                />
              </div>
            )}
            <Divider orientation={DividerOrientation.Horizontal} />
            <Stack spacing={Spacing.XS}>
              <Headline size={HeadlineSize.S}>Included paths</Headline>
              <span>
                Each path indexes the pages below it that use the selected
                content types.
              </span>
            </Stack>
            {draft.paths.map((path, index) => {
              const pathName =
                path.aliasPath.trim() === ""
                  ? "new included path"
                  : `included path ${path.aliasPath.trim()}`;
              return (
                <div
                  key={index}
                  role="group"
                  aria-label={pathName.replace(/^./, (c) => c.toUpperCase())}
                  data-testid="config-panel-path"
                >
                  <Paper
                    elevation={PaperElevation.XS}
                    borderRadius={BorderRadius.RadiusM}
                  >
                    <Box spacing={Spacing.L}>
                      <Stack spacing={Spacing.L}>
                        <div style={pathRow}>
                          <div style={{ flex: 1 }}>
                            <Input
                              label="Path"
                              value={path.aliasPath}
                              disabled={busy}
                              markAsRequired
                              placeholder="/Articles/%"
                              explanationText="End the path with /% to include all pages below it."
                              onChange={(e) => {
                                updatePath(index, {
                                  aliasPath: e.target.value,
                                });
                              }}
                            />
                          </div>
                          <RemoveButton
                            title={`Remove ${pathName}`}
                            disabled={busy}
                            onClick={() => {
                              setDraft((d) =>
                                d?.kind === "channel"
                                  ? {
                                      ...d,
                                      paths: d.paths.filter(
                                        (_, i) => i !== index,
                                      ),
                                    }
                                  : d,
                              );
                            }}
                          />
                        </div>
                        <ContentTypeSelector
                          label="Content types"
                          dataTestId="path-content-types"
                          options={props.webContentTypes}
                          selected={path.contentTypes}
                          selectedLabels={storedTypeLabels}
                          disabled={busy}
                          onChange={(contentTypes) => {
                            updatePath(index, { contentTypes });
                          }}
                        />
                      </Stack>
                    </Box>
                  </Paper>
                </div>
              );
            })}
            <Inline spacing={Spacing.M}>
              <Button
                type={ButtonType.Button}
                color={ButtonColor.Secondary}
                icon="xp-plus"
                label="Add included path"
                disabled={busy}
                onClick={() => {
                  setDraft((d) =>
                    d?.kind === "channel"
                      ? {
                          ...d,
                          paths: [
                            ...d.paths,
                            {
                              identifier: null,
                              aliasPath: "",
                              contentTypes: [],
                            },
                          ],
                        }
                      : d,
                  );
                }}
              />
            </Inline>
          </Stack>
        );
    }
  };

  const title = draft ? panelTitle(draft) : "";

  return (
    <div data-testid="index-configuration" style={defaultText}>
      <Stack spacing={Spacing.L}>
        <SectionCard
          title="Index settings"
          testId="config-settings"
          editLabel="Edit index settings"
          onEdit={() => {
            open({
              kind: "settings",
              displayName: config.displayName,
              strategyName: config.strategyName,
              analyzerName: config.analyzerName,
              rebuildHook: config.rebuildHook,
            });
          }}
        >
          <div style={valueGrid}>
            <TextWithLabel label="Display name" value={config.displayName} />
            <CodeNameValue label="Code name" value={config.indexName} />
            <TextWithLabel
              label="Indexing strategy"
              value={optionLabel(props.strategies, config.strategyName)}
            />
            <TextWithLabel
              label="Lucene analyzer"
              value={optionLabel(props.analyzers, config.analyzerName)}
            />
            <TextWithLabel
              label="Rebuild hook"
              value={config.rebuildHook === "" ? "Not set" : config.rebuildHook}
            />
          </div>
        </SectionCard>
        <SectionCard
          title="Languages"
          testId="config-languages"
          editLabel="Edit languages"
          onEdit={() => {
            open({
              kind: "languages",
              languageNames: [...config.languageNames],
            });
          }}
        >
          <ReadOnlyList
            label="Indexed languages"
            values={config.languageNames.map((l) =>
              describe(props.languages, l),
            )}
            emptyText="No languages"
          />
        </SectionCard>
        <SectionCard
          title="Website channels"
          testId="config-channels"
          editLabel="Add website channel"
          editDisabled={unusedChannels.length === 0}
          onEdit={() => {
            open({
              kind: "channel",
              isNew: true,
              websiteChannelName: "",
              paths: toDraftPaths(undefined),
            });
          }}
        >
          {config.channels.length === 0 ? (
            <span>No website channels are indexed.</span>
          ) : (
            <Stack spacing={Spacing.XL}>
              {config.channels.map((channel) => (
                <ChannelReadOnly
                  key={channel.websiteChannelName}
                  channel={channel}
                  contentTypes={props.webContentTypes}
                  canEdit={canEditChannel(channel.websiteChannelName)}
                  onEdit={() => {
                    open({
                      kind: "channel",
                      isNew: false,
                      websiteChannelName: channel.websiteChannelName,
                      paths: toDraftPaths(channel),
                    });
                  }}
                />
              ))}
            </Stack>
          )}
        </SectionCard>
        <SectionCard
          title="Reusable content"
          testId="config-reusable"
          editLabel="Edit reusable content"
          onEdit={() => {
            open({
              kind: "reusable",
              contentTypeNames: config.reusableContentTypes.map((t) => t.value),
            });
          }}
        >
          <ReadOnlyList
            label="Reusable content types"
            values={config.reusableContentTypes.map((t) =>
              describe(props.reusableContentTypes, t.value),
            )}
            emptyText="No reusable content types"
          />
        </SectionCard>
      </Stack>
      <SidePanel
        headline={title}
        size={
          draft?.kind === "channel" || draft?.kind === "reusable"
            ? SidePanelSize.Full
            : SidePanelSize.Stackable
        }
        isVisible={draft !== null}
        isOutsideClickCloseable={false}
        onClose={requestClose}
        tooltips={{ close: "Close" }}
        footer={
          <Inline spacing={Spacing.M}>
            <Button
              type={ButtonType.Button}
              color={ButtonColor.Primary}
              label="Save"
              inProgress={busy}
              disabled={busy}
              onClick={save}
            />
            <Button
              type={ButtonType.Button}
              color={ButtonColor.Secondary}
              label="Cancel"
              disabled={busy}
              onClick={closePanel}
            />
          </Inline>
        }
      >
        {draft !== null && (
          <div
            role="region"
            aria-label={title}
            data-testid="config-panel"
            style={defaultText}
          >
            <Stack spacing={Spacing.L}>
              {errors.length > 0 && (
                <div role="alert" data-testid="config-panel-errors">
                  <Callout
                    type={CalloutType.FriendlyWarning}
                    placement={CalloutPlacementType.OnPaper}
                    headline="Fix these before saving"
                  >
                    <ul>
                      {errors.map((e) => (
                        <li key={e}>{e}</li>
                      ))}
                    </ul>
                  </Callout>
                </div>
              )}
              {renderFields()}
            </Stack>
          </div>
        )}
      </SidePanel>
    </div>
  );
};
