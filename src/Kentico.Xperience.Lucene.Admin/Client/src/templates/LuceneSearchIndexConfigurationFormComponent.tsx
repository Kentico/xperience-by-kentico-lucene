import { useFormComponentCommandProvider } from "@kentico/xperience-admin-base";
import {
  Box,
  Button,
  ButtonColor,
  ButtonType,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Card,
  FormItemWrapper,
  getDataTestId,
  Inline,
  Input,
  MenuItem,
  Select,
  Spacing,
  Stack,
  TextWithLabel,
} from "@kentico/xperience-admin-components";
import React, { useState } from "react";
import {
  ContentTypeSelector,
  type ContentTypeSelectorOption,
} from "../components/ContentTypeSelector";
import { RemoveButton } from "../components/RemoveButton";
import type { IncludedPath } from "../models/IncludedPath";
import type { LuceneIndexChannelConfiguration } from "../models/LuceneIndexChannelConfiguration";
import type { LuceneIndexConfigurationFormComponentProps } from "../models/LuceneIndexConfigurationFormComponentProps";
import type { LuceneIndexContentType } from "../models/LuceneIndexContentType";
import type { RowActionSuccessResult } from "../models/RowActionSuccessResult";

const PATH_EXPLANATION =
  "Use /% at the end to include all pages below the path, for example /Articles/%.";

const getPathIssue = (
  paths: IncludedPath[],
  index: number,
): string | undefined => {
  const aliasPath = (paths[index]?.aliasPath ?? "").trim();
  if (aliasPath === "") {
    return "Enter a path.";
  }
  const duplicate = paths.some(
    (p, i) =>
      i < index &&
      (p.aliasPath ?? "").trim().toLowerCase() === aliasPath.toLowerCase(),
  );
  return duplicate
    ? "This path is already included for this channel."
    : undefined;
};

const serialize = (value: LuceneIndexChannelConfiguration[]): string =>
  JSON.stringify(
    value.map((c) => ({
      channel: c.websiteChannelName,
      paths: c.includedPaths.map((p) => ({
        path: p.aliasPath ?? "",
        types: p.contentTypes.map((t) => t.contentTypeName).sort(),
      })),
    })),
  );

export const LuceneSearchIndexConfigurationFormComponent = (
  props: LuceneIndexConfigurationFormComponentProps,
): React.JSX.Element => {
  const { executeCommand } = useFormComponentCommandProvider();
  const value = props.value;
  const [baseline, setBaseline] = useState(value);
  const [emitted, setEmitted] = useState(value);
  const [busyChannel, setBusyChannel] = useState<string | null>(null);

  // A value the component did not emit (initial load or reload after save) becomes the new baseline.
  if (value !== emitted) {
    setBaseline(value);
    setEmitted(value);
  }

  const editMode: string | undefined = props.editMode;
  const disabled = editMode === "disabled" || editMode === "readOnly";
  const changed = serialize(value) !== serialize(baseline);

  const emit = (next: LuceneIndexChannelConfiguration[]): void => {
    setEmitted(next);
    props.onChange?.(next);
  };

  const channelLabel = (channel: LuceneIndexChannelConfiguration): string =>
    props.possibleChannels?.find(
      (c) => c.channelName === channel.websiteChannelName,
    )?.channelDisplayName ??
    channel.channelDisplayName ??
    channel.websiteChannelName ??
    "";

  const contentTypeOptions: ContentTypeSelectorOption[] = (
    props.possibleContentTypeItems ?? []
  ).map((t) => ({
    value: t.contentTypeName,
    label: t.contentTypeDisplayName || t.contentTypeName,
  }));

  const updateChannel = (
    channelIndex: number,
    update: (
      channel: LuceneIndexChannelConfiguration,
    ) => LuceneIndexChannelConfiguration,
  ): void => {
    emit(value.map((c, i) => (i === channelIndex ? update(c) : c)));
  };

  const updatePath = (
    channelIndex: number,
    pathIndex: number,
    update: { aliasPath?: string; contentTypes?: LuceneIndexContentType[] },
  ): void => {
    updateChannel(channelIndex, (c) => ({
      ...c,
      includedPaths: c.includedPaths.map((p, j) =>
        j === pathIndex
          ? {
              ...p,
              aliasPath: update.aliasPath ?? p.aliasPath,
              contentTypes: update.contentTypes ?? p.contentTypes,
            }
          : p,
      ),
    }));
  };

  const addChannel = async (channelName: string): Promise<void> => {
    setBusyChannel(channelName);
    try {
      const response = await executeCommand<RowActionSuccessResult, string>(
        props,
        "AddWebsiteChannelConfiguration",
        channelName,
      );
      if (!response?.success) {
        return;
      }
      const option = props.possibleChannels?.find(
        (c) => c.channelName === channelName,
      );
      emit([
        ...value,
        {
          websiteChannelName: channelName,
          channelDisplayName: option?.channelDisplayName ?? channelName,
          includedPaths: [
            { aliasPath: "/%", contentTypes: [], identifier: null },
          ],
        },
      ]);
    } finally {
      setBusyChannel(null);
    }
  };

  const removeChannel = async (channelName: string): Promise<void> => {
    setBusyChannel(channelName);
    try {
      const response = await executeCommand<RowActionSuccessResult, string>(
        props,
        "DeleteWebsiteChannelConfiguration",
        channelName,
      );
      if (!response?.success) {
        return;
      }
      emit(value.filter((c) => c.websiteChannelName !== channelName));
    } finally {
      setBusyChannel(null);
    }
  };

  const unusedChannels =
    props.possibleChannels?.filter(
      (c) => !value.some((v) => v.websiteChannelName === c.channelName),
    ) ?? [];

  return (
    <FormItemWrapper
      label={props.label}
      explanationText={props.explanationText}
      invalid={props.invalid}
      validationMessage={props.validationMessage}
      dataTestId={getDataTestId("channel-configuration")}
    >
      <Stack spacing={Spacing.L}>
        {value.length === 0 && (
          <Callout
            type={CalloutType.FriendlyWarning}
            placement={CalloutPlacementType.OnPaper}
            headline="No website channels"
          >
            Pages are indexed only from website channels added here.
          </Callout>
        )}
        {value.map((channel, ci) => {
          const name = channel.websiteChannelName ?? "";
          const label = channelLabel(channel);
          const pathCount = channel.includedPaths.length;
          return (
            <section
              key={name}
              aria-label={`Website channel ${label}`}
              data-testid="configured-channel"
              data-channel={name}
            >
              <Card
                headline={label}
                description={`Code name: ${name} · ${String(pathCount)} ${
                  pathCount === 1 ? "path" : "paths"
                }`}
                footer={
                  <Inline spacing={Spacing.S}>
                    <Button
                      type={ButtonType.Button}
                      color={ButtonColor.Secondary}
                      label="Add path"
                      disabled={disabled}
                      onClick={() => {
                        updateChannel(ci, (c) => ({
                          ...c,
                          includedPaths: [
                            ...c.includedPaths,
                            {
                              aliasPath: "",
                              contentTypes: [],
                              identifier: null,
                            },
                          ],
                        }));
                      }}
                    />
                    <RemoveButton
                      title={`Remove ${label} from index`}
                      disabled={disabled || busyChannel !== null}
                      inProgress={busyChannel === name}
                      onClick={() => {
                        void removeChannel(name);
                      }}
                    />
                  </Inline>
                }
              >
                <Stack spacing={Spacing.L}>
                  {pathCount === 0 && (
                    <Callout
                      type={CalloutType.FriendlyWarning}
                      placement={CalloutPlacementType.OnPaper}
                      headline="No paths"
                    >
                      Add a path, or the channel is removed when you save.
                    </Callout>
                  )}
                  {channel.includedPaths.map((path, pi) => {
                    const issue = getPathIssue(channel.includedPaths, pi);
                    const selected = path.contentTypes.map(
                      (t) => t.contentTypeName,
                    );
                    return (
                      <Box
                        key={`${name}|${String(pi)}`}
                        spacingY={Spacing.S}
                        dataTestId={getDataTestId("included-path")}
                      >
                        <Stack spacing={Spacing.M}>
                          <Input
                            label="Path"
                            value={path.aliasPath ?? ""}
                            disabled={disabled}
                            markAsRequired
                            placeholder="/Articles/%"
                            explanationText={PATH_EXPLANATION}
                            invalid={issue !== undefined}
                            validationMessage={issue}
                            onChange={(e) => {
                              updatePath(ci, pi, { aliasPath: e.target.value });
                            }}
                          />
                          <ContentTypeSelector
                            label={`Content types for ${path.aliasPath ?? ""}`}
                            dataTestId="path-content-types"
                            options={contentTypeOptions}
                            selected={selected}
                            selectedLabels={Object.fromEntries(
                              path.contentTypes.map((t) => [
                                t.contentTypeName,
                                t.contentTypeDisplayName || t.contentTypeName,
                              ]),
                            )}
                            disabled={disabled}
                            onChange={(names) => {
                              updatePath(ci, pi, {
                                contentTypes: names.map(
                                  (n) =>
                                    path.contentTypes.find(
                                      (t) => t.contentTypeName === n,
                                    ) ??
                                    props.possibleContentTypeItems?.find(
                                      (t) => t.contentTypeName === n,
                                    ) ?? {
                                      contentTypeName: n,
                                      contentTypeDisplayName: n,
                                    },
                                ),
                              });
                            }}
                          />
                          {selected.length === 0 && (
                            <Callout
                              type={CalloutType.FriendlyWarning}
                              placement={CalloutPlacementType.OnPaper}
                            >
                              No pages are indexed from this path until you
                              select a content type.
                            </Callout>
                          )}
                          {pathCount > 1 && (
                            <Inline>
                              <RemoveButton
                                title={`Remove path ${path.aliasPath ?? ""}`.trim()}
                                disabled={disabled}
                                onClick={() => {
                                  updateChannel(ci, (c) => ({
                                    ...c,
                                    includedPaths: c.includedPaths.filter(
                                      (_, j) => j !== pi,
                                    ),
                                  }));
                                }}
                              />
                            </Inline>
                          )}
                        </Stack>
                      </Box>
                    );
                  })}
                </Stack>
              </Card>
            </section>
          );
        })}
        <Inline spacing={Spacing.M}>
          <Select
            label="Add website channel"
            aria-label="Add website channel"
            value=""
            placeholder={
              unusedChannels.length === 0
                ? "All website channels are added"
                : "Select a channel"
            }
            disabled={
              disabled || unusedChannels.length === 0 || busyChannel !== null
            }
            onChange={(channelName) => {
              if (channelName) {
                void addChannel(channelName);
              }
            }}
          >
            {unusedChannels.map((c) => (
              <MenuItem
                key={c.channelName}
                value={c.channelName}
                primaryLabel={c.channelDisplayName}
              />
            ))}
          </Select>
        </Inline>
        {changed && (
          <Inline spacing={Spacing.M}>
            <Button
              type={ButtonType.Button}
              color={ButtonColor.Secondary}
              label="Discard website channel changes"
              disabled={disabled}
              onClick={() => {
                emit(baseline);
              }}
            />
            <TextWithLabel value="Website channel changes are saved with the rest of the configuration." />
          </Inline>
        )}
      </Stack>
    </FormItemWrapper>
  );
};
