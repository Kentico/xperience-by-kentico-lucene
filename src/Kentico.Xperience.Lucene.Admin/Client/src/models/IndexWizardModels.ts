export interface WizardOption {
  readonly value: string;
  readonly label: string;
}

export interface IndexWizardProps {
  readonly channels: WizardOption[];
  readonly webContentTypes: WizardOption[];
  readonly reusableContentTypes: WizardOption[];
  readonly languages: WizardOption[];
  readonly analyzers: string[];
  readonly strategies: string[];
  readonly existingIndexNames: string[];
}

export interface WizardPath {
  aliasPath: string;
  contentTypes: string[];
}

export interface WizardChannel {
  channelName: string;
  paths: WizardPath[];
}

export interface CreateIndexArguments {
  readonly displayName: string;
  readonly indexName: string;
  readonly channels: WizardChannel[];
  readonly reusableContentTypes: string[];
  readonly languages: string[];
  readonly analyzerName: string;
  readonly strategyName: string;
}

export interface PathPreviewArguments {
  readonly channelName: string;
  readonly aliasPath: string;
  readonly contentTypes: string[];
  readonly language: string;
}

export interface PathPreviewItem {
  readonly name: string;
  readonly path: string;
  readonly contentType: string;
}

export interface PathPreviewResult {
  readonly total: number;
  readonly isWildcard: boolean;
  readonly items: PathPreviewItem[];
  readonly error: string | null;
}

export interface CreateIndexResult {
  readonly success: boolean;
  readonly indexId: number;
  readonly errors: string[];
}
