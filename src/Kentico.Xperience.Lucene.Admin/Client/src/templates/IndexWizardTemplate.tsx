import {
  usePageCommand,
  usePageCommandProvider,
} from "@kentico/xperience-admin-base";
import {
  Box,
  Button,
  ButtonColor,
  ButtonType,
  Callout,
  CalloutPlacementType,
  CalloutType,
  Checkbox,
  Colors,
  getDataTestId,
  Headline,
  HeadlineSize,
  Inline,
  Input,
  MenuItem,
  NotificationBarAlert,
  Paper,
  Select,
  Spacing,
  Stack,
  TextWithLabel,
} from "@kentico/xperience-admin-components";
import * as React from "react";
import { useCommandResult } from "../hooks/useCommandResult";
import { IndexWizardContentStep, TypeList } from "./IndexWizardContentStep";
import type {
  CreateIndexArguments,
  CreateIndexResult,
  IndexWizardProps,
  PathPreviewArguments,
  PathPreviewResult,
  WizardChannel,
  WizardOption,
} from "../models/IndexWizardModels";

const STEPS = [
  "Identity",
  "Content",
  "Languages and analyzer",
  "Strategy",
  "Review",
];

const CODE_NAME_PATTERN = /^[A-Za-z0-9][A-Za-z0-9_.-]*$/;

// Analyzer name fragment -> ISO language code
const ANALYZER_LANGUAGES: Readonly<Record<string, string>> = {
  english: "en",
  czech: "cs",
  german: "de",
  french: "fr",
  spanish: "es",
  italian: "it",
  dutch: "nl",
  portuguese: "pt",
  russian: "ru",
  polish: "pl",
  swedish: "sv",
  danish: "da",
  norwegian: "no",
  finnish: "fi",
  hungarian: "hu",
  turkish: "tr",
};

export const getAnalyzerLanguage = (analyzer: string): string | null => {
  const lower = analyzer.toLowerCase();
  const key = Object.keys(ANALYZER_LANGUAGES).find((k) => lower.includes(k));

  return key ? ANALYZER_LANGUAGES[key] : null;
};

export const getAnalyzerWarnings = (
  analyzer: string,
  languages: string[],
): string[] => {
  const analyzerLanguage = getAnalyzerLanguage(analyzer);
  if (!analyzerLanguage) {
    return [];
  }

  return languages
    .filter((l) => l.split("-")[0].toLowerCase() !== analyzerLanguage)
    .map(
      (l) =>
        `${analyzer} is designed for "${analyzerLanguage}" text, but the index includes "${l}". Words in that language may be analyzed poorly.`,
    );
};

const Alert = ({
  children,
  testId,
}: {
  readonly children: React.ReactNode;
  readonly testId: string;
}): React.JSX.Element => (
  <NotificationBarAlert dataTestId={getDataTestId(testId)}>
    {children}
  </NotificationBarAlert>
);

const Checks = ({
  options,
  selected,
  label,
  onChange,
  disabled,
}: {
  readonly options: WizardOption[];
  readonly selected: string[];
  readonly label: string;
  readonly onChange: (next: string[]) => void;
  readonly disabled: boolean;
}): React.JSX.Element => (
  <div role="group" aria-label={label}>
    <Stack spacing={Spacing.S}>
      <Headline size={HeadlineSize.S}>{label}</Headline>
      {options.length === 0 && <TextWithLabel value="No options available." />}
      {options.map((option) => (
        <Checkbox
          key={option.value}
          label={option.label}
          disabled={disabled}
          checked={selected.includes(option.value)}
          onChange={(_, checked) => {
            onChange(
              checked
                ? [...selected, option.value]
                : selected.filter((v) => v !== option.value),
            );
          }}
        />
      ))}
    </Stack>
  </div>
);

const textColorStyle: React.CSSProperties = {
  color: Colors.TextDefaultOnLight,
};
const mutedTextStyle: React.CSSProperties = {
  color: Colors.TextLowEmphasis,
};
const codeTextStyle: React.CSSProperties = {
  ...mutedTextStyle,
  fontSize: 12,
};
const wrapStyle: React.CSSProperties = {
  ...textColorStyle,
  overflowWrap: "anywhere",
};
const reviewGridStyle: React.CSSProperties = {
  ...textColorStyle,
  display: "grid",
  gridTemplateColumns: "minmax(8rem, 12rem) 1fr",
  margin: 0,
  borderTop: `1px solid ${Colors.BorderDefault}`,
};

const ReviewRow = ({
  label,
  children,
}: {
  readonly label: string;
  readonly children: React.ReactNode;
}): React.JSX.Element => (
  <>
    <dt
      style={{
        ...mutedTextStyle,
        padding: `${Spacing.M} ${Spacing.M} ${Spacing.M} 0`,
        borderBottom: `1px solid ${Colors.BorderDefault}`,
      }}
    >
      {label}
    </dt>
    <dd
      style={{
        ...wrapStyle,
        margin: 0,
        padding: `${Spacing.M} 0`,
        borderBottom: `1px solid ${Colors.BorderDefault}`,
      }}
    >
      {children}
    </dd>
  </>
);

const labelOf = (options: WizardOption[], value: string): string =>
  options.find((o) => o.value === value)?.label ?? value;

const labelsOf = (options: WizardOption[], values: string[]): string =>
  values.map((v) => labelOf(options, v)).join(", ");

export const IndexWizardTemplate = (
  props: IndexWizardProps,
): React.JSX.Element => {
  const { executeCommand } = usePageCommandProvider();
  const previewPath = useCommandResult<PathPreviewResult, PathPreviewArguments>(
    "PreviewPath",
  );
  const createIndex = useCommandResult<CreateIndexResult, CreateIndexArguments>(
    "CreateIndex",
  );
  usePageCommand<unknown, { indexId: number }>("OpenIndex");

  const [step, setStep] = React.useState(0);
  const [maxStep, setMaxStep] = React.useState(0);
  const [displayName, setDisplayName] = React.useState("");
  const [indexName, setIndexName] = React.useState("");
  const [channels, setChannels] = React.useState<WizardChannel[]>([]);
  const [reusable, setReusable] = React.useState<string[]>([]);
  const [languages, setLanguages] = React.useState<string[]>([]);
  const [analyzer, setAnalyzer] = React.useState(
    props.analyzers.find((a) => a.toLowerCase().includes("standard")) ??
      props.analyzers.at(0) ??
      "",
  );
  const [strategy, setStrategy] = React.useState(props.strategies.at(0) ?? "");
  const [errors, setErrors] = React.useState<string[]>([]);
  const [busy, setBusy] = React.useState(false);
  const requestActive = React.useRef(false);
  const disabled = busy;
  const previewLanguage = languages.at(0) ?? props.languages.at(0)?.value ?? "";
  const previewLanguageText = previewLanguage
    ? `${labelOf(props.languages, previewLanguage)} (${
        languages.length > 0
          ? "first selected language"
          : "default until languages are selected in step 3"
      })`
    : "No language available";
  const validate = (target: number): string[] => {
    const found: string[] = [];
    if (target === 0) {
      const code = indexName.trim();
      if (!CODE_NAME_PATTERN.test(code)) {
        found.push(
          "The code name is required and may contain only letters, numbers, '_', '-' and '.'.",
        );
      } else if (
        props.existingIndexNames.some(
          (n) => n.toLowerCase() === code.toLowerCase(),
        )
      ) {
        found.push(`An index with the code name '${code}' already exists.`);
      }
    }

    if (target === 1) {
      if (channels.length === 0 && reusable.length === 0) {
        found.push(
          "Add a channel with a path and content type, or select a reusable content type.",
        );
      }

      if (
        channels.some(
          (c) =>
            c.paths.length === 0 ||
            c.paths.some(
              (p) => p.aliasPath.trim() === "" || p.contentTypes.length === 0,
            ),
        )
      ) {
        found.push(
          "Every channel needs at least one path, and every path a value and at least one content type.",
        );
      }

      if (
        channels.some((c) =>
          c.paths.some(
            (p) =>
              p.aliasPath.trim() !== "" && !p.aliasPath.trim().startsWith("/"),
          ),
        )
      ) {
        found.push("Paths must start with '/', for example /Articles/%.");
      }

      if (
        channels.some((c) => {
          const paths = c.paths.map((p) => p.aliasPath.trim().toLowerCase());
          return new Set(paths).size !== paths.length;
        })
      ) {
        found.push("A channel cannot include the same path more than once.");
      }
    }

    if (target === 2) {
      if (languages.length === 0) {
        found.push("Select at least one language.");
      }

      if (!analyzer) {
        found.push("Select an analyzer.");
      }
    }

    if (target === 3 && !strategy) {
      found.push("Select an indexing strategy.");
    }

    return found;
  };

  const next = (): void => {
    const found = validate(step);
    setErrors(found);
    if (found.length === 0) {
      setStep(step + 1);
      setMaxStep(Math.max(maxStep, step + 1));
    }
  };

  // Jumping forward to a visited step revalidates the steps in between.
  const goTo = (target: number): void => {
    if (target === step || target > maxStep) {
      return;
    }

    for (let s = step; s < target; s++) {
      const found = validate(s);
      if (found.length > 0) {
        setErrors(found);
        setStep(s);
        return;
      }
    }

    setErrors([]);
    setStep(target);
  };

  const create = async (): Promise<void> => {
    if (requestActive.current) {
      return;
    }

    const found = [0, 1, 2, 3].flatMap((s) => validate(s));
    setErrors(found);
    if (found.length > 0) {
      return;
    }

    requestActive.current = true;
    setBusy(true);
    try {
      const result = await createIndex({
        displayName,
        indexName: indexName.trim(),
        channels,
        reusableContentTypes: reusable,
        languages,
        analyzerName: analyzer,
        strategyName: strategy,
      });

      if (!result.success) {
        setErrors(
          result.errors.length
            ? result.errors
            : ["The index could not be created."],
        );
        return;
      }

      await executeCommand<unknown, { indexId: number }>("OpenIndex", {
        indexId: result.indexId,
      });
    } catch {
      setErrors(["The index could not be created. Please try again."]);
    } finally {
      requestActive.current = false;
      setBusy(false);
    }
  };

  const warnings = getAnalyzerWarnings(analyzer, languages);

  return (
    <Box spacing={Spacing.XL} dataTestId={getDataTestId("wizard")}>
      <Paper>
        <Box spacing={Spacing.XL}>
          <Stack spacing={Spacing.XL}>
            <Headline size={HeadlineSize.L}>Create index</Headline>
            <nav aria-label="Creation steps" data-testid="wizard-steps">
              <Inline spacing={Spacing.S}>
                {STEPS.map((name, i) => (
                  <div
                    key={name}
                    aria-current={i === step ? "step" : undefined}
                  >
                    <Button
                      type={ButtonType.Button}
                      color={
                        i === step ? ButtonColor.Primary : ButtonColor.Tertiary
                      }
                      label={`${i + 1}. ${name}`}
                      active={i === step}
                      disabled={disabled || i > maxStep}
                      onClick={() => {
                        goTo(i);
                      }}
                    />
                  </div>
                ))}
              </Inline>
            </nav>

            {step === 0 && (
              <section data-testid="step-identity">
                <Stack spacing={Spacing.L}>
                  <Input
                    label="Display name"
                    value={displayName}
                    disabled={disabled}
                    explanationText="Shown in the administration. Defaults to the code name."
                    onChange={(e) => {
                      setDisplayName(e.target.value);
                    }}
                  />
                  <Input
                    label="Code name"
                    value={indexName}
                    disabled={disabled}
                    markAsRequired
                    invalid={errors.length > 0}
                    validationMessage={errors.at(0)}
                    explanationText="Used by application code to find the index. It cannot be changed after creation."
                    onChange={(e) => {
                      setIndexName(e.target.value);
                    }}
                  />
                </Stack>
              </section>
            )}

            {step === 1 && (
              <IndexWizardContentStep
                channels={channels}
                onChannelsChange={setChannels}
                reusable={reusable}
                onReusableChange={setReusable}
                channelOptions={props.channels}
                webContentTypes={props.webContentTypes}
                reusableContentTypes={props.reusableContentTypes}
                previewLanguage={previewLanguage}
                previewLanguageText={previewLanguageText}
                disabled={disabled}
                previewPath={previewPath}
              />
            )}
            {step === 2 && (
              <section data-testid="step-languages">
                <Stack spacing={Spacing.L}>
                  <Checks
                    label="Languages"
                    options={props.languages}
                    selected={languages}
                    disabled={disabled}
                    onChange={setLanguages}
                  />
                  <Select
                    label="Analyzer"
                    aria-label="Analyzer"
                    value={analyzer}
                    disabled={disabled}
                    markAsRequired
                    onChange={(value) => {
                      setAnalyzer(value ?? "");
                    }}
                  >
                    {props.analyzers.map((a) => (
                      <MenuItem key={a} value={a} primaryLabel={a} />
                    ))}
                  </Select>
                  {warnings.map((w) => (
                    <Callout
                      key={w}
                      dataTestId={getDataTestId("analyzer-warning")}
                      type={CalloutType.FriendlyWarning}
                      placement={CalloutPlacementType.OnPaper}
                    >
                      {w}
                    </Callout>
                  ))}
                </Stack>
              </section>
            )}

            {step === 3 && (
              <section data-testid="step-strategy">
                <Select
                  label="Indexing strategy"
                  aria-label="Indexing strategy"
                  value={strategy}
                  disabled={disabled}
                  markAsRequired
                  onChange={(value) => {
                    setStrategy(value ?? "");
                  }}
                >
                  {props.strategies.map((s) => (
                    <MenuItem key={s} value={s} primaryLabel={s} />
                  ))}
                </Select>
              </section>
            )}

            {step === 4 && (
              <section data-testid="step-review">
                <Stack spacing={Spacing.L}>
                  <dl data-testid="wizard-review" style={reviewGridStyle}>
                    <ReviewRow label="Display name">
                      {displayName.trim() || indexName.trim()}
                    </ReviewRow>
                    <ReviewRow label="Code name">
                      <span style={codeTextStyle}>{indexName.trim()}</span>
                    </ReviewRow>
                    <ReviewRow label="Website pages">
                      {channels.length === 0 ? (
                        <span style={mutedTextStyle}>None</span>
                      ) : (
                        <div style={{ display: "grid", gap: Spacing.L }}>
                          {channels.map((c) => {
                            const name = labelOf(props.channels, c.channelName);
                            return (
                              <div
                                key={c.channelName}
                                data-testid="wizard-review-channel"
                                style={{ display: "grid", gap: Spacing.S }}
                              >
                                <span>
                                  <strong>{name}</strong>
                                  {name !== c.channelName && (
                                    <span style={codeTextStyle}>
                                      {" "}
                                      {c.channelName}
                                    </span>
                                  )}
                                </span>
                                {c.paths.map((p) => (
                                  <div
                                    key={p.aliasPath}
                                    data-testid="wizard-review-path"
                                    style={{
                                      display: "grid",
                                      gap: Spacing.XS,
                                      paddingLeft: Spacing.L,
                                      borderLeft: `2px solid ${Colors.BorderDefault}`,
                                    }}
                                  >
                                    <code style={wrapStyle}>
                                      {p.aliasPath.trim()}
                                    </code>
                                    <TypeList
                                      options={props.webContentTypes}
                                      values={p.contentTypes}
                                      label={`Content types for ${p.aliasPath.trim()}`}
                                      emptyText="No content types"
                                    />
                                  </div>
                                ))}
                              </div>
                            );
                          })}
                        </div>
                      )}
                    </ReviewRow>
                    <ReviewRow label="Reusable content">
                      <TypeList
                        options={props.reusableContentTypes}
                        values={reusable}
                        label="Reusable content types"
                        emptyText="None"
                      />
                    </ReviewRow>
                    <ReviewRow label="Languages">
                      {labelsOf(props.languages, languages)}
                    </ReviewRow>
                    <ReviewRow label="Analyzer">{analyzer}</ReviewRow>
                    <ReviewRow label="Strategy">{strategy}</ReviewRow>
                  </dl>
                  {warnings.map((w) => (
                    <Callout
                      key={w}
                      dataTestId={getDataTestId("analyzer-warning")}
                      type={CalloutType.FriendlyWarning}
                      placement={CalloutPlacementType.OnPaper}
                    >
                      {w}
                    </Callout>
                  ))}
                  <Callout
                    type={CalloutType.QuickTip}
                    placement={CalloutPlacementType.OnPaper}
                  >
                    The index is created empty. Rebuild it from Overview or the
                    index list to fill it. Field mapping is defined in code by
                    the indexing strategy.
                  </Callout>
                </Stack>
              </section>
            )}

            {errors.map((e) => (
              <Alert key={e} testId="wizard-error">
                {e}
              </Alert>
            ))}

            <Inline spacing={Spacing.S}>
              {step > 0 && (
                <Button
                  type={ButtonType.Button}
                  label="Back"
                  color={ButtonColor.Secondary}
                  disabled={disabled}
                  onClick={() => {
                    setErrors([]);
                    setStep(step - 1);
                  }}
                />
              )}
              {step < STEPS.length - 1 && (
                <Button
                  type={ButtonType.Button}
                  label="Next"
                  color={ButtonColor.Primary}
                  disabled={disabled}
                  onClick={next}
                />
              )}
              {step === STEPS.length - 1 && (
                <Button
                  type={ButtonType.Button}
                  label="Create index"
                  color={ButtonColor.Primary}
                  inProgress={busy}
                  disabled={disabled}
                  onClick={() => {
                    void create();
                  }}
                />
              )}
            </Inline>
          </Stack>
        </Box>
      </Paper>
    </Box>
  );
};
