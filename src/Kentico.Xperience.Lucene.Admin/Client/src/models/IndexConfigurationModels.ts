export interface ConfigurationOption {
  readonly value: string;
  readonly label: string;
}

export interface IndexConfigurationPath {
  readonly identifier: number | null;
  readonly aliasPath: string;
  readonly contentTypes: ConfigurationOption[];
}

export interface IndexConfigurationChannel {
  readonly websiteChannelName: string;
  readonly channelDisplayName: string;
  readonly paths: IndexConfigurationPath[];
}

export interface IndexConfigurationData {
  readonly id: number;
  readonly indexName: string;
  readonly displayName: string;
  readonly strategyName: string;
  readonly analyzerName: string;
  readonly rebuildHook: string;
  readonly languageNames: string[];
  readonly reusableContentTypes: ConfigurationOption[];
  readonly channels: IndexConfigurationChannel[];
}

export interface IndexConfigurationProps {
  readonly configuration: IndexConfigurationData | null;
  readonly strategies: ConfigurationOption[];
  readonly analyzers: ConfigurationOption[];
  readonly languages: ConfigurationOption[];
  readonly reusableContentTypes: ConfigurationOption[];
  readonly webContentTypes: ConfigurationOption[];
  readonly channels: ConfigurationOption[];
}

export interface IndexConfigurationSaveResult {
  readonly saved: boolean;
  readonly errors: string[];
  readonly configuration: IndexConfigurationData | null;
}

export interface ChannelSectionPath {
  identifier: number | null;
  aliasPath: string;
  contentTypes: string[];
}

export interface ChannelSectionArguments {
  readonly websiteChannelName: string;
  readonly isNew: boolean;
  readonly paths: ChannelSectionPath[];
}
