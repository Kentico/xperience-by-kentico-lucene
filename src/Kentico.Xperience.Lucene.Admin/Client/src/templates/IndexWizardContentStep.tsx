import {
  Button,
  type ButtonProps,
  ButtonColor,
  ButtonSize,
  ButtonType,
  Colors,
  getDataTestId,
  Headline,
  HeadlineSize,
  Inline,
  Input,
  MenuItem,
  NotificationBarAlert,
  Select,
  SidePanel,
  SidePanelSize,
  Spacing,
  Stack,
  TextWithLabel,
} from "@kentico/xperience-admin-components";
import * as React from "react";
import { RemoveButton } from "../components/RemoveButton";
import { ContentTypeSelector } from "../components/ContentTypeSelector";
import type {
  PathPreviewArguments,
  PathPreviewResult,
  WizardChannel,
  WizardOption,
} from "../models/IndexWizardModels";

const textStyle: React.CSSProperties = { color: Colors.TextDefaultOnLight };
const mutedStyle: React.CSSProperties = { color: Colors.TextLowEmphasis };
// Code names: muted 12px, matching the native MenuItem secondary label.
const codeStyle: React.CSSProperties = { ...mutedStyle, fontSize: 12 };
const listStyle: React.CSSProperties = {
  listStyle: "none",
  margin: 0,
  padding: 0,
  display: "grid",
  gap: Spacing.S,
};

const labelOf = (options: WizardOption[], value: string): string =>
  options.find((o) => o.value === value)?.label ?? value;

// Native Headline renders a div, so the semantic level is added here.
const SectionHeading = ({
  children,
}: {
  readonly children: string;
}): React.JSX.Element => (
  <div role="heading" aria-level={2} style={textStyle}>
    <Headline size={HeadlineSize.S}>{children}</Headline>
  </div>
);

// Known types: display name first, code name secondary; unknown: code name only.
const TypeName = ({
  options,
  value,
}: {
  readonly options: WizardOption[];
  readonly value: string;
}): React.JSX.Element => {
  const label = options.find((o) => o.value === value)?.label;
  return label && label !== value ? (
    <span data-testid="wizard-type-name">
      <span style={textStyle}>{label}</span>{" "}
      <span style={codeStyle}>{value}</span>
    </span>
  ) : (
    <span data-testid="wizard-type-name" style={textStyle}>
      {value}
    </span>
  );
};

export // The native Button does not forward aria-label, so set the contextual name on the rendered button.
const ActionButton = ({
  ariaLabel,
  ...props
}: ButtonProps & { readonly ariaLabel: string }): React.JSX.Element => {
  const buttonRef = React.useRef<HTMLButtonElement>(null);
  React.useLayoutEffect(() => {
    buttonRef.current?.setAttribute("aria-label", ariaLabel);
  }, [ariaLabel]);
  return <Button {...props} buttonRef={buttonRef} />;
};

export const TypeList = ({
  options,
  values,
  label,
  emptyText,
}: {
  readonly options: WizardOption[];
  readonly values: string[];
  readonly label: string;
  readonly emptyText: string;
}): React.JSX.Element =>
  values.length === 0 ? (
    <span style={mutedStyle}>{emptyText}</span>
  ) : (
    <div>
      <ul
        aria-label={label}
        style={{ ...listStyle, display: "grid", gap: Spacing.XS }}
      >
        {values.map((v) => (
          <li key={v}>
            <TypeName options={options} value={v} />
          </li>
        ))}
      </ul>
    </div>
  );

export const typeText = (options: WizardOption[], value: string): string => {
  const label = options.find((o) => o.value === value)?.label;
  return label && label !== value ? `${label} (${value})` : value;
};

const cellTextStyle: React.CSSProperties = {
  ...textStyle,
  overflowWrap: "anywhere",
  whiteSpace: "normal",
};

// Two-line name: display name, then muted code name; unknown types show the code name only.
const TypeCell = ({
  options,
  value,
}: {
  readonly options: WizardOption[];
  readonly value: string;
}): React.JSX.Element => {
  const label = options.find((o) => o.value === value)?.label;
  return (
    <span style={{ ...cellTextStyle, display: "grid" }}>
      <span>{label ?? value}</span>
      {label && label !== value && (
        <span style={{ ...codeStyle, overflowWrap: "anywhere" }}>{value}</span>
      )}
    </span>
  );
};

const previewGridStyle: React.CSSProperties = {
  display: "grid",
  // Matches the native 14px body text used by TextWithLabel.
  fontSize: "14px",
  lineHeight: "20px",
  gridTemplateColumns: "minmax(0, 3fr) minmax(0, 2fr)",
  columnGap: Spacing.L,
};

interface PathEditor {
  readonly kind: "path";
  // null when the editor adds a new channel.
  readonly channelName: string | null;
  // null when the editor adds a new path.
  readonly pathIndex: number | null;
  readonly draftChannel: string;
  readonly aliasPath: string;
  readonly contentTypes: string[];
  readonly attempted: boolean;
}

interface ReusableEditor {
  readonly kind: "reusable";
  readonly selected: string[];
}

interface PreviewState {
  readonly args: PathPreviewArguments;
  readonly result?: PathPreviewResult;
  readonly loading: boolean;
}

export interface IndexWizardContentStepProps {
  readonly channels: WizardChannel[];
  readonly onChannelsChange: (channels: WizardChannel[]) => void;
  readonly reusable: string[];
  readonly onReusableChange: (reusable: string[]) => void;
  readonly channelOptions: WizardOption[];
  readonly webContentTypes: WizardOption[];
  readonly reusableContentTypes: WizardOption[];
  readonly previewLanguage: string;
  readonly previewLanguageText: string;
  readonly disabled: boolean;
  readonly previewPath: (
    args: PathPreviewArguments,
  ) => Promise<PathPreviewResult>;
}

const getPathIssues = (
  editor: PathEditor,
  channels: WizardChannel[],
): { channel?: string; path?: string; types?: string } => {
  const path = editor.aliasPath.trim();
  const siblings =
    channels
      .find((c) => c.channelName === editor.draftChannel)
      ?.paths.filter((_, i) => i !== editor.pathIndex) ?? [];
  return {
    channel: editor.draftChannel ? undefined : "Select a website channel.",
    path:
      path === ""
        ? "Enter a path."
        : !path.startsWith("/")
          ? "Paths must start with '/', for example /Articles/%."
          : siblings.some(
                (p) => p.aliasPath.trim().toLowerCase() === path.toLowerCase(),
              )
            ? "This channel already includes this path."
            : undefined,
    types:
      editor.contentTypes.length === 0
        ? "Select at least one content type."
        : undefined,
  };
};

export const IndexWizardContentStep = (
  props: IndexWizardContentStepProps,
): React.JSX.Element => {
  const [editor, setEditor] = React.useState<
    PathEditor | ReusableEditor | null
  >(null);
  const [preview, setPreview] = React.useState<PreviewState | null>(null);
  const request = React.useRef(0);
  const { channels, disabled } = props;

  const channelLabel = (name: string): string =>
    labelOf(props.channelOptions, name);
  const unusedChannels = props.channelOptions.filter(
    (c) => !channels.some((x) => x.channelName === c.value),
  );

  const openPathEditor = (
    channelName: string | null,
    pathIndex: number | null,
  ): void => {
    const existing =
      channelName !== null && pathIndex !== null
        ? channels.find((c) => c.channelName === channelName)?.paths[pathIndex]
        : undefined;
    setEditor({
      kind: "path",
      channelName,
      pathIndex,
      draftChannel: channelName ?? "",
      aliasPath: existing?.aliasPath ?? "",
      contentTypes: existing?.contentTypes ?? [],
      attempted: false,
    });
  };

  const applyPath = (draft: PathEditor): void => {
    const issues = getPathIssues(draft, channels);
    if (issues.channel || issues.path || issues.types) {
      setEditor({ ...draft, attempted: true });
      return;
    }

    const value = {
      aliasPath: draft.aliasPath.trim(),
      contentTypes: draft.contentTypes,
    };
    if (draft.channelName === null) {
      props.onChannelsChange([
        ...channels,
        { channelName: draft.draftChannel, paths: [value] },
      ]);
    } else {
      props.onChannelsChange(
        channels.map((c) =>
          c.channelName !== draft.channelName
            ? c
            : {
                ...c,
                paths:
                  draft.pathIndex === null
                    ? [...c.paths, value]
                    : c.paths.map((p, i) =>
                        i === draft.pathIndex ? value : p,
                      ),
              },
        ),
      );
    }
    setEditor(null);
  };

  const runPreview = async (args: PathPreviewArguments): Promise<void> => {
    const id = ++request.current;
    setPreview({ args, loading: true });
    let result: PathPreviewResult;
    try {
      result = await props.previewPath(args);
    } catch {
      result = {
        total: 0,
        isWildcard: false,
        items: [],
        error: "The preview could not be loaded. Please try again.",
      };
    }
    // Ignore results of a preview that was closed or replaced meanwhile.
    if (id === request.current) {
      setPreview({ args, result, loading: false });
    }
  };

  const closePreview = (): void => {
    request.current++;
    setPreview(null);
  };

  const pathIssues =
    editor?.kind === "path" && editor.attempted
      ? getPathIssues(editor, channels)
      : {};
  const pathCount = channels.reduce((n, c) => n + c.paths.length, 0);

  return (
    <section data-testid="step-content" style={textStyle}>
      <Stack spacing={Spacing.XXL}>
        <section aria-label="Website pages" data-testid="website-pages-section">
          <Stack spacing={Spacing.M}>
            <SectionHeading>Website pages</SectionHeading>
            <span style={mutedStyle}>
              {channels.length === 0
                ? "No website pages are included."
                : `${String(channels.length)} ${
                    channels.length === 1 ? "channel" : "channels"
                  }, ${String(pathCount)} ${pathCount === 1 ? "path" : "paths"}`}
            </span>
            {channels.length > 0 && (
              <div>
                <ul
                  aria-label="Website channels"
                  style={{ ...listStyle, gap: Spacing.L }}
                >
                  {channels.map((channel) => {
                    const name = channelLabel(channel.channelName);
                    return (
                      <li
                        key={channel.channelName}
                        aria-label={name}
                        data-testid="channel-block"
                      >
                        <Stack spacing={Spacing.S}>
                          <div
                            style={{
                              display: "flex",
                              alignItems: "center",
                              justifyContent: "space-between",
                              gap: Spacing.M,
                            }}
                          >
                            <span
                              style={{
                                display: "flex",
                                flexWrap: "wrap",
                                alignItems: "baseline",
                                gap: Spacing.M,
                              }}
                            >
                              <strong style={textStyle}>{name}</strong>
                              {name !== channel.channelName && (
                                <span style={codeStyle}>
                                  {channel.channelName}
                                </span>
                              )}
                            </span>
                            <RemoveButton
                              title={`Remove channel ${name}`}
                              disabled={disabled}
                              onClick={() => {
                                props.onChannelsChange(
                                  channels.filter(
                                    (c) =>
                                      c.channelName !== channel.channelName,
                                  ),
                                );
                              }}
                            />
                          </div>
                          {channel.paths.length === 0 ? (
                            <span style={mutedStyle}>
                              No paths. Add a path or remove the channel.
                            </span>
                          ) : (
                            <ul
                              aria-label={`Paths in ${name}`}
                              style={{
                                ...listStyle,
                                gap: 0,
                                borderTop: `1px solid ${Colors.BorderDefault}`,
                              }}
                            >
                              {channel.paths.map((path, pi) => (
                                <li
                                  key={`${channel.channelName}|${path.aliasPath}|${String(pi)}`}
                                  aria-label={`Path ${path.aliasPath}`}
                                  data-testid="path-block"
                                  style={{
                                    display: "grid",
                                    gridTemplateColumns:
                                      "minmax(10rem, 2fr) minmax(12rem, 3fr) auto",
                                    alignItems: "start",
                                    columnGap: Spacing.XL,
                                    rowGap: Spacing.S,
                                    padding: `${Spacing.M} 0`,
                                    borderBottom: `1px solid ${Colors.BorderDefault}`,
                                  }}
                                >
                                  <code
                                    data-testid="path-value"
                                    style={{
                                      ...textStyle,
                                      overflowWrap: "anywhere",
                                    }}
                                  >
                                    {path.aliasPath}
                                  </code>
                                  <div>
                                    <TypeList
                                      options={props.webContentTypes}
                                      values={path.contentTypes}
                                      label={`Content types for ${path.aliasPath}`}
                                      emptyText="No content types"
                                    />
                                  </div>
                                  <Inline spacing={Spacing.S}>
                                    <ActionButton
                                      type={ButtonType.Button}
                                      size={ButtonSize.S}
                                      color={ButtonColor.Tertiary}
                                      label="Edit"
                                      ariaLabel={`Edit path ${path.aliasPath}`}
                                      disabled={disabled}
                                      onClick={() => {
                                        openPathEditor(channel.channelName, pi);
                                      }}
                                    />
                                    <ActionButton
                                      type={ButtonType.Button}
                                      size={ButtonSize.S}
                                      color={ButtonColor.Tertiary}
                                      label="Preview"
                                      ariaLabel={`Preview path ${path.aliasPath}`}
                                      disabled={disabled}
                                      onClick={() => {
                                        void runPreview({
                                          channelName: channel.channelName,
                                          aliasPath: path.aliasPath,
                                          contentTypes: path.contentTypes,
                                          language: props.previewLanguage,
                                        });
                                      }}
                                    />
                                    <RemoveButton
                                      title={`Remove path ${path.aliasPath}`}
                                      disabled={disabled}
                                      onClick={() => {
                                        props.onChannelsChange(
                                          channels.map((c) =>
                                            c.channelName !==
                                            channel.channelName
                                              ? c
                                              : {
                                                  ...c,
                                                  paths: c.paths.filter(
                                                    (_, j) => j !== pi,
                                                  ),
                                                },
                                          ),
                                        );
                                      }}
                                    />
                                  </Inline>
                                </li>
                              ))}
                            </ul>
                          )}
                          <div>
                            <ActionButton
                              type={ButtonType.Button}
                              size={ButtonSize.S}
                              color={ButtonColor.Tertiary}
                              icon="xp-plus"
                              label="Add path"
                              title={`Add path to ${name}`}
                              ariaLabel={`Add path to ${name}`}
                              disabled={disabled}
                              onClick={() => {
                                openPathEditor(channel.channelName, null);
                              }}
                            />
                          </div>
                        </Stack>
                      </li>
                    );
                  })}
                </ul>
              </div>
            )}
            <div>
              <Button
                type={ButtonType.Button}
                color={ButtonColor.Secondary}
                label="Add website channel"
                disabled={disabled || unusedChannels.length === 0}
                onClick={() => {
                  openPathEditor(null, null);
                }}
              />
              {unusedChannels.length === 0 && (
                <span style={{ ...mutedStyle, marginLeft: Spacing.M }}>
                  All website channels are added.
                </span>
              )}
            </div>
          </Stack>
        </section>

        <section
          aria-label="Reusable content"
          data-testid="reusable-content-section"
        >
          <Stack spacing={Spacing.M}>
            <SectionHeading>Reusable content</SectionHeading>
            <TypeList
              options={props.reusableContentTypes}
              values={props.reusable}
              label="Reusable content types"
              emptyText="No reusable content types are included."
            />
            <div>
              <Button
                type={ButtonType.Button}
                color={ButtonColor.Secondary}
                label={
                  props.reusable.length === 0
                    ? "Select reusable content types"
                    : "Edit reusable content types"
                }
                disabled={disabled}
                onClick={() => {
                  setEditor({ kind: "reusable", selected: props.reusable });
                }}
              />
            </div>
          </Stack>
        </section>
      </Stack>

      <SidePanel
        dataTestId={getDataTestId("wizard-source-panel")}
        size={SidePanelSize.Stackable}
        // Hidden, not discarded, while the preview is open; closing the preview shows the same draft again.
        isVisible={editor !== null && preview === null}
        isOutsideClickCloseable={false}
        tooltips={{ close: "Cancel" }}
        onClose={() => {
          if (preview === null) {
            setEditor(null);
          }
        }}
        headline={
          <div role="heading" aria-level={2} style={textStyle}>
            <Headline size={HeadlineSize.M}>
              {editor?.kind === "reusable"
                ? "Reusable content types"
                : editor?.channelName === null
                  ? "Add website channel"
                  : editor?.pathIndex === null
                    ? "Add path"
                    : "Edit path"}
            </Headline>
          </div>
        }
        footer={
          editor && (
            <Inline spacing={Spacing.M}>
              <Button
                type={ButtonType.Button}
                color={ButtonColor.Primary}
                label={
                  editor.kind === "reusable"
                    ? "Apply"
                    : editor.channelName === null
                      ? "Add channel"
                      : editor.pathIndex === null
                        ? "Add path"
                        : "Apply"
                }
                onClick={() => {
                  if (editor.kind === "reusable") {
                    props.onReusableChange(editor.selected);
                    setEditor(null);
                  } else {
                    applyPath(editor);
                  }
                }}
              />
              {editor.kind === "path" && (
                <Button
                  type={ButtonType.Button}
                  color={ButtonColor.Secondary}
                  label="Preview path"
                  disabled={
                    !editor.draftChannel ||
                    editor.aliasPath.trim() === "" ||
                    editor.contentTypes.length === 0
                  }
                  onClick={() => {
                    void runPreview({
                      channelName: editor.draftChannel,
                      aliasPath: editor.aliasPath.trim(),
                      contentTypes: editor.contentTypes,
                      language: props.previewLanguage,
                    });
                  }}
                />
              )}
              <Button
                type={ButtonType.Button}
                color={ButtonColor.Tertiary}
                label="Cancel"
                onClick={() => {
                  setEditor(null);
                }}
              />
            </Inline>
          )
        }
      >
        {editor && (
          <div data-testid="wizard-source-form" style={textStyle}>
            <Stack spacing={Spacing.L}>
              <span style={mutedStyle}>
                Changes apply to this wizard. Nothing is saved until you create
                the index.
              </span>
              {editor.kind === "reusable" ? (
                <ContentTypeSelector
                  label="Reusable content types"
                  showCount={false}
                  dataTestId="wizard-reusable-content-types"
                  explanationText="Items of these types are indexed in the selected languages."
                  options={props.reusableContentTypes}
                  selected={editor.selected}
                  onChange={(selected) => {
                    setEditor({ ...editor, selected });
                  }}
                />
              ) : (
                <>
                  {editor.channelName === null ? (
                    <Select
                      label="Website channel"
                      aria-label="Website channel"
                      markAsRequired
                      value={editor.draftChannel}
                      placeholder="Select a channel"
                      invalid={pathIssues.channel !== undefined}
                      validationMessage={pathIssues.channel}
                      onChange={(value) => {
                        setEditor({ ...editor, draftChannel: value ?? "" });
                      }}
                    >
                      {unusedChannels.map((c) => (
                        <MenuItem
                          key={c.value}
                          value={c.value}
                          primaryLabel={c.label}
                          secondaryLabel={c.value}
                          dataTestId={getDataTestId("wizard-channel-option")}
                        />
                      ))}
                    </Select>
                  ) : (
                    <TextWithLabel
                      label="Website channel"
                      value={channelLabel(editor.channelName)}
                    />
                  )}
                  <Input
                    label="Path"
                    value={editor.aliasPath}
                    markAsRequired
                    placeholder="/Articles/%"
                    explanationText="End with /% to include all pages below the path."
                    invalid={pathIssues.path !== undefined}
                    validationMessage={pathIssues.path}
                    onChange={(e) => {
                      setEditor({ ...editor, aliasPath: e.target.value });
                    }}
                  />
                  <ContentTypeSelector
                    label="Content types"
                    showCount={false}
                    dataTestId="wizard-content-types"
                    explanationText="Pages of these types under the path are indexed."
                    options={props.webContentTypes}
                    selected={editor.contentTypes}
                    onChange={(contentTypes) => {
                      setEditor({ ...editor, contentTypes });
                    }}
                  />
                  {pathIssues.types && (
                    <NotificationBarAlert
                      dataTestId={getDataTestId("wizard-source-error")}
                    >
                      {pathIssues.types}
                    </NotificationBarAlert>
                  )}
                </>
              )}
            </Stack>
          </div>
        )}
      </SidePanel>

      <SidePanel
        dataTestId={getDataTestId("path-preview-panel")}
        size={SidePanelSize.Stackable}
        isVisible={preview !== null}
        tooltips={{ close: "Close" }}
        onClose={closePreview}
        headline={
          <div role="heading" aria-level={2} style={textStyle}>
            <Headline size={HeadlineSize.M}>Path preview</Headline>
          </div>
        }
        footer={
          <Inline spacing={Spacing.M}>
            <Button
              type={ButtonType.Button}
              color={ButtonColor.Secondary}
              label="Refresh preview"
              disabled={preview?.loading !== false}
              inProgress={preview?.loading === true}
              onClick={() => {
                if (preview) {
                  void runPreview(preview.args);
                }
              }}
            />
            <Button
              type={ButtonType.Button}
              color={ButtonColor.Tertiary}
              label="Close"
              onClick={closePreview}
            />
          </Inline>
        }
      >
        {preview && (
          <section
            aria-label="Path preview"
            data-testid="path-preview-body"
            style={textStyle}
          >
            <Stack spacing={Spacing.L}>
              <span style={mutedStyle}>
                Read-only sample of matching pages. Not saved with the index.
              </span>
              <div
                style={{ display: "flex", flexWrap: "wrap", gap: Spacing.XL }}
              >
                <TextWithLabel
                  label="Channel"
                  value={channelLabel(preview.args.channelName)}
                  dataTestId={getDataTestId("path-preview-channel")}
                />
                <TextWithLabel
                  label="Path"
                  value={preview.args.aliasPath}
                  dataTestId={getDataTestId("path-preview-path")}
                />
                <TextWithLabel
                  label="Preview language"
                  value={
                    preview.args.language === props.previewLanguage
                      ? props.previewLanguageText
                      : preview.args.language
                  }
                  dataTestId={getDataTestId("path-preview-language")}
                />
              </div>
              <TextWithLabel
                label="Content types"
                dataTestId={getDataTestId("path-preview-content-types")}
                value={preview.args.contentTypes
                  .map((t) => typeText(props.webContentTypes, t))
                  .join(", ")}
              />
              {preview.loading && (
                <span style={mutedStyle}>Loading matching pages…</span>
              )}
              {preview.result?.error && (
                <NotificationBarAlert
                  dataTestId={getDataTestId("path-preview-error")}
                >
                  {preview.result.error}
                </NotificationBarAlert>
              )}
              {preview.result && !preview.result.error && (
                <Stack spacing={Spacing.M}>
                  <TextWithLabel
                    label="Matches"
                    dataTestId={getDataTestId("path-preview-summary")}
                    value={`${
                      preview.result.isWildcard
                        ? "Wildcard path: matches all pages below the path, not the path page itself."
                        : "Exact path: matches only this page."
                    } ${String(preview.result.total)} matching pages.`}
                  />
                  {preview.result.items.length > 0 && (
                    <div>
                      <div
                        aria-hidden="true"
                        style={{
                          ...previewGridStyle,
                          ...mutedStyle,
                          paddingBottom: Spacing.XS,
                        }}
                      >
                        <span>Page and path</span>
                        <span>Content type</span>
                      </div>
                      <ul
                        aria-label="Matching pages"
                        data-testid="path-preview"
                        style={{
                          ...listStyle,
                          gap: 0,
                          borderTop: `1px solid ${Colors.BorderDefault}`,
                        }}
                      >
                        {preview.result.items.map((item) => (
                          <li
                            key={item.path}
                            data-testid="path-preview-item"
                            style={{
                              ...previewGridStyle,
                              padding: `${Spacing.S} 0`,
                              borderBottom: `1px solid ${Colors.BorderDefault}`,
                            }}
                          >
                            <span style={{ ...cellTextStyle, display: "grid" }}>
                              <span>{item.name}</span>
                              <span
                                style={{
                                  ...mutedStyle,
                                  overflowWrap: "anywhere",
                                }}
                              >
                                {item.path}
                              </span>
                            </span>
                            <TypeCell
                              options={props.webContentTypes}
                              value={item.contentType}
                            />
                          </li>
                        ))}
                      </ul>
                    </div>
                  )}
                  {preview.result.total > preview.result.items.length && (
                    <span style={mutedStyle}>
                      Showing the first {preview.result.items.length}.
                    </span>
                  )}
                </Stack>
              )}
            </Stack>
          </section>
        )}
      </SidePanel>
    </section>
  );
};
