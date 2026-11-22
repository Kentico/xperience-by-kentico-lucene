import type {
  ActionCell,
  StringCell,
  TableAction,
  TableCell,
  TableColumn,
  TableRow,
} from '@kentico/xperience-admin-components';
import {
  Button,
  ButtonType,
  CellType,
  ColumnContentType,
  Stack,
  Table,
} from '@kentico/xperience-admin-components';
import { useFormComponentCommandProvider } from '@kentico/xperience-admin-base';
import React, { useState } from 'react';
import type {
  CSSObjectWithLabel,
  GroupBase,
  SingleValue,
  StylesConfig,
} from 'react-select';
import Select from 'react-select';
import type { LuceneIndexConfigurationFormComponentProps } from '../models/LuceneIndexConfigurationFormComponentProps';
import type { LuceneIndexChannelConfiguration } from '../models/LuceneIndexChannelConfiguration';
import type { OptionType } from '../models/OptionType';
import type { LuceneIndexChannel } from '../models/LuceneIndexChannel';
import { LuceneIncludedPathConfiguration } from './LuceneIncludedPathConfiguration';
import type { RowActionSuccessResult } from '../models/RowActionSuccessResult';

export const LuceneSearchIndexConfigurationFormComponent = (
  props: LuceneIndexConfigurationFormComponentProps,
): React.JSX.Element => {
  const [showChannelEdit, setShowChannelEdit] = useState<boolean>(false);
  const [showAddNewChannelConfiguration, setShowAddNewChannelConfiguration] =
    useState<boolean>(true);
  const [showPathConfiguration, setShowPathConfiguration] =
    useState<boolean>(false);
  const { executeCommand } = useFormComponentCommandProvider();

  const getEmptyChannel = (): LuceneIndexChannelConfiguration => {
    const emptyChannel: LuceneIndexChannelConfiguration = {
      channelDisplayName: null,
      websiteChannelName: null,
      includedPaths: [],
    };

    return emptyChannel;
  };
  const getUnconfiguredChannelOptions = (
    configuredChannels: LuceneIndexChannelConfiguration[] = props.value,
  ): OptionType[] => {
    const filteredOptions: OptionType[] =
      props.possibleChannels
        ?.filter(
          (possibleChannel) =>
            !configuredChannels.some(
              (existingChannel) =>
                existingChannel.websiteChannelName ===
                possibleChannel.channelName,
            ),
        )
        .map((channel) => {
          const option: OptionType = {
            value: channel.channelName,
            label: channel.channelDisplayName,
          };
          return option;
        }) ?? [];

    return filteredOptions;
  };

  const [channel, setChannel] =
    useState<LuceneIndexChannelConfiguration>(getEmptyChannel());
  const [selectedChannelOption, setSelectedChannelOption] =
    useState<OptionType | null>(null);
  const [channelOptions, setChannelOptions] = useState<OptionType[]>(
    getUnconfiguredChannelOptions(),
  );

  const prepareRows = (
    channels: LuceneIndexChannelConfiguration[],
  ): TableRow[] => {
    const getCells = (
      channel: LuceneIndexChannelConfiguration,
    ): TableCell[] => {
      const channelName = channel.websiteChannelName?.toString() ?? '';
      const channelDisplayName = channel.channelDisplayName?.toString() ?? '';
      const cell: StringCell = {
        type: CellType.String,
        value: channelDisplayName,
      };
      const deleteAction: TableAction = {
        label: 'delete',
        icon: 'xp-bin',
        disabled: false,
        destructive: true,
      };

      const deleteWebsiteChannelConfiguration: () => Promise<void> =
        async () => {
          const response = await executeCommand<RowActionSuccessResult, string>(
            props,
            'DeleteWebsiteChannelConfiguration',
            channelName,
          );
          if (!response?.success) {
            return;
          }

          const newValue = props.value.filter(
            (x) => x.websiteChannelName !== channelName,
          );
          props.onChange?.(newValue);
          setShowChannelEdit(false);
          setChannelOptions(getUnconfiguredChannelOptions(newValue));
          setShowAddNewChannelConfiguration(true);
          setEmptyChannel();
        };

      const deletePathCell: ActionCell = {
        actions: [deleteAction],
        type: CellType.Action,
        onInvokeAction: deleteWebsiteChannelConfiguration,
      };

      const cells: TableCell[] = [cell, deletePathCell];
      return cells;
    };

    return channels.map((channel) => {
      const row: TableRow = {
        identifier: channel.websiteChannelName ?? '',
        cells: getCells(channel),
        disabled: false,
      };

      return row;
    });
  };

  const rows = prepareRows(props.value);

  const prepareColumns = (): TableColumn[] => {
    const columns: TableColumn[] = [];

    const column: TableColumn = {
      name: 'Website Channel',
      visible: true,
      contentType: ColumnContentType.Text,
      caption: '',
      minWidth: 0,
      maxWidth: 1000,
      sortable: true,
      searchable: true,
    };

    const actionColumn: TableColumn = {
      name: 'Actions',
      visible: true,
      contentType: ColumnContentType.Action,
      caption: '',
      minWidth: 0,
      maxWidth: 1000,
      sortable: false,
      searchable: false,
    };

    columns.push(column);
    columns.push(actionColumn);
    return columns;
  };

  const addWebsiteChannelConfiguration = (): void => {
    setShowChannelEdit(true);
    setShowAddNewChannelConfiguration(false);
    setEmptyChannel();
    const filteredOptions: OptionType[] = getUnconfiguredChannelOptions();
    setChannelOptions(filteredOptions);
  };

  const showChannelDetail = (identifier: unknown): void => {
    if (!showChannelEdit) {
      if (typeof identifier !== 'string') {
        return;
      }

      const selectedChannelName = identifier;
      const selectedChannel = props.value.find(
        (x) => x.websiteChannelName === selectedChannelName,
      );
      if (!selectedChannel) {
        return;
      }

      setChannel(selectedChannel);

      const option = selectChannelOption(selectedChannelName);
      if (!option) {
        return;
      }

      const filteredOptions: OptionType[] = getUnconfiguredChannelOptions();
      filteredOptions.push(option);
      setChannelOptions(filteredOptions);
    } else {
      setEmptyChannel();
    }

    setShowChannelEdit(!showChannelEdit);
    setShowAddNewChannelConfiguration(!showAddNewChannelConfiguration);
  };

  const selectChannelOption = (
    selectedChannelName: string,
  ): OptionType | undefined => {
    const selectedChannel: LuceneIndexChannel | undefined =
      props.possibleChannels?.find(
        (x) => x.channelName === selectedChannelName,
      );
    if (!selectedChannel) {
      return undefined;
    }

    const selectedOption: OptionType = {
      value: selectedChannel.channelName,
      label: selectedChannel.channelDisplayName,
    };

    setSelectedChannelOption(selectedOption);
    setShowPathConfiguration(true);
    return selectedOption;
  };

  const setEmptyChannel = (): void => {
    const emptyChannel = getEmptyChannel();
    setChannel(emptyChannel);
    setSelectedChannelOption(null);
  };

  const saveWebsiteChannelConfiguration = async (): Promise<void> => {
    if (
      !rows.some((x) => {
        return x.identifier === channel.websiteChannelName;
      })
    ) {
      if (channel.websiteChannelName === '' || !channel.websiteChannelName) {
        alert('Invalid Channel');
      } else {
        const response = await executeCommand<RowActionSuccessResult, string>(
          props,
          'AddWebsiteChannelConfiguration',
          channel.websiteChannelName,
        );
        if (!response?.success) {
          return;
        }

        const newValue = [...props.value, channel];
        props.onChange?.(newValue);
        setChannelOptions(getUnconfiguredChannelOptions(newValue));
      }
    } else {
      const rowIndex = rows.findIndex((x) => {
        return x.identifier === channel.websiteChannelName;
      });

      if (rowIndex === -1) {
        alert('Invalid edit');
        return;
      }

      const propPathIndex = props.value.findIndex((x) => {
        return x.websiteChannelName === channel.websiteChannelName;
      });
      if (propPathIndex === -1) {
        alert('Invalid edit');
        return;
      }

      const updatedChannel: LuceneIndexChannelConfiguration = {
        websiteChannelName: channel.websiteChannelName,
        channelDisplayName: channel.channelDisplayName,
        includedPaths: channel.includedPaths,
      };

      const response = await executeCommand<
        RowActionSuccessResult,
        LuceneIndexChannelConfiguration
      >(props, 'SaveWebsiteChannelConfiguration', updatedChannel);
      if (!response?.success) {
        return;
      }

      const newValue = props.value.map((value, index) =>
        index === propPathIndex ? updatedChannel : value,
      );
      props.onChange?.(newValue);
      setChannelOptions(getUnconfiguredChannelOptions(newValue));
    }

    setShowAddNewChannelConfiguration(true);
    setEmptyChannel();
    setShowChannelEdit(false);
  };

  const selectChannel = (newValue: SingleValue<OptionType>): void => {
    if (!newValue) {
      setEmptyChannel();
      return;
    }

    setChannel((prev) => ({
      ...prev,
      websiteChannelName: newValue.value,
      channelDisplayName: newValue.label,
    }));

    selectChannelOption(newValue.value);
  };

  const customStyle: StylesConfig<OptionType, false, GroupBase<OptionType>> = {
    control: (styles, { isFocused }) => ({
      ...styles,
      backgroundColor: 'white',
      borderColor: isFocused ? 'black' : 'gray',
      '&:hover': {
        borderColor: 'black',
      },
      borderRadius: 20,
      boxShadow: 'gray',
      padding: 2,
      minHeight: 'fit-content',
    }),
    option: (styles, { isSelected }) => {
      return {
        ...styles,
        backgroundColor: isSelected ? '#bab4f0' : 'white',
        '&:hover': {
          backgroundColor: isSelected ? '#a097f7' : 'lightgray',
        },
        color: isSelected ? 'purple' : 'black',
        cursor: 'pointer',
      };
    },
    input: (styles) => ({ ...styles }),
    container: (styles) => ({ ...styles, borderColor: 'gray' }),
    placeholder: (styles) => ({ ...styles }),
    dropdownIndicator: (styles, state): CSSObjectWithLabel => ({
      ...styles,
      transform: state.selectProps.menuIsOpen
        ? 'rotate(180deg)'
        : 'rotate(0deg)',
    }),
    menu: (styles) => ({
      ...styles,
      zIndex: 9999,
    }),
  };

  return (
    <Stack>
      <Table
        columns={prepareColumns()}
        rows={rows}
        onRowClick={showChannelDetail}
      />
      {showChannelEdit && (
        <div style={{ marginTop: '20px' }}>
          <Select
            closeMenuOnSelect={true}
            isMulti={false}
            placeholder="Select website channel"
            value={selectedChannelOption}
            options={channelOptions}
            onChange={selectChannel}
            styles={customStyle}
          />
          {showPathConfiguration && (
            <div style={{ maxWidth: '95%', margin: '20px auto' }}>
              <LuceneIncludedPathConfiguration
                possibleContentTypeItems={props.possibleContentTypeItems}
                value={channel.includedPaths}
                onChange={(newPaths) => {
                  setChannel((prev) => ({
                    ...prev,
                    includedPaths: newPaths,
                  }));
                }}
              />
            </div>
          )}
          <div style={{ marginTop: '20px' }}>
            <Button
              type={ButtonType.Button}
              label="Save website channel configuration"
              onClick={() => {
                void saveWebsiteChannelConfiguration();
              }}
            ></Button>
          </div>
        </div>
      )}
      {showAddNewChannelConfiguration && (
        <div style={{ marginTop: '20px' }}>
          <Button
            type={ButtonType.Button}
            label="Add new website channel configuration"
            onClick={addWebsiteChannelConfiguration}
          ></Button>
        </div>
      )}
    </Stack>
  );
};
