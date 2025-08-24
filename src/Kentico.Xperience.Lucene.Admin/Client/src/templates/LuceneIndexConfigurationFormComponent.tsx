import {
  ActionCell,
  Button,
  ButtonType,
  CellType,
  ColumnContentType,
  Stack,
  StringCell,
  Table,
  TableAction,
  TableCell,
  TableColumn,
  TableRow
} from '@kentico/xperience-admin-components';
import React, { useEffect, useState } from 'react';
import Select, { SingleValue } from 'react-select';
import { LuceneIndexConfigurationComponentClientProperties } from '../models/LuceneIndexConfigurationComponentClientProperties';
import { LuceneIndexChannelConfiguration } from '../models/LuceneIndexChannelConfiguration';
import { OptionType } from '../models/OptionType';
import { IncludedPath } from '../models/IncludedPath';
import { LuceneIndexChannel } from '../models/LuceneIndexChannel';

export const LuceneIndexConfigurationFormComponent = (
    props: LuceneIndexConfigurationComponentClientProperties
): JSX.Element => {
  const [rows, setRows] = useState<TableRow[]>([]);
  const [showChannelEdit, setShowChannelEdit] = useState<boolean>(false);
  const [showAddNewChannelConfiguration, setShowAddNewChannelConfiguration] = useState<boolean>(true);

  const getEmptyChannel = (): LuceneIndexChannelConfiguration => {
    const emptyChannel: LuceneIndexChannelConfiguration = {
      channelDisplayName: null,
      websiteChannelName: null,
      includedPaths: []
    };

    return emptyChannel;
  };
  const getUnconfiguredChannelOptions = (): OptionType[] => {
    const filteredOptions: OptionType[] =
    props.possibleChannels
    ?.filter(
      (possibleChannel) =>
        !props.value.some(
          (existingChannel) =>
            existingChannel.websiteChannelName === possibleChannel.channelName
        )
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

  const [channel, setChannel] = useState<LuceneIndexChannelConfiguration>(getEmptyChannel());
  const [selectedChannelOption, setSelectedChannelOption] = useState<OptionType | null>(null);
  const [channelOptions, setChannelOptions] = useState<OptionType[]>(getUnconfiguredChannelOptions());

  const prepareRows = (channels: LuceneIndexChannelConfiguration[]): TableRow[] => {
    if (channels === undefined) {
      return [];
    }

    const getCells = (channel: LuceneIndexChannelConfiguration): TableCell[] => {
      const channelName = channel.websiteChannelName?.toString() ?? '';
      if (channelName === null) {
        return [];
      }
      const cell: StringCell = {
        type: CellType.String,
        value: channelName
      };
      const deleteAction: TableAction = {
        label: 'delete',
        icon: 'xp-bin',
        disabled: false,
        destructive: true
      };

      const deleteWebsiteChannelConfiguration: () => Promise<void> = async () => {
        await new Promise(() => {
          props.value = props.value.filter((x) => x.websiteChannelName !== channelName) ?? [];

          if (props.onChange) {
            props.onChange(props.value);
          }

          setRows(prepareRows(props.value));
          setShowChannelEdit(false);
          setChannelOptions(getUnconfiguredChannelOptions());
          setShowAddNewChannelConfiguration(true);
          setEmptyChannel();
        });
      };

      const deletePathCell: ActionCell = {
        actions: [deleteAction],
        type: CellType.Action,
        onInvokeAction: deleteWebsiteChannelConfiguration,
      };

      const cells: TableCell[] = [cell, deletePathCell];
      return cells;
    }

    return channels.map((channel) => {
      const row: TableRow = {
        identifier: channel.websiteChannelName ?? '',
        cells: getCells(channel),
        disabled: false
      };

      return row;
    });
  };

  useEffect(() => {
    if (props.value === null || props.value === undefined) {
      props.value = [];
    }
    if (props.onChange !== null && props.onChange !== undefined) {
      props.onChange(props.value);
    }
    setRows(() => prepareRows(props.value));
  }, [props?.value]);

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

  const addNewChannelConfiguration = (): void => {
    setShowChannelEdit(true);
    setShowAddNewChannelConfiguration(false);
    setEmptyChannel();
  };

  const showChannelDetail = (identifier: unknown): void => {
    let rowIndex = -1;
    for (let i = 0; i < rows.length; i++) {
      if ((rows[i].identifier as string) === (identifier as string)) {
        rowIndex = i;
      }
    }
    const row = rows[rowIndex];
    if (!showChannelEdit) {
      const selectedChannelName = (row.cells[0] as StringCell).value
      const selectedChannel = props.value.find(
        (x) => x.websiteChannelName === selectedChannelName
      ) as LuceneIndexChannelConfiguration;

      setChannel(selectedChannel);
      
      const option: OptionType = selectChannelOption(selectedChannelName);
      
      const filteredOptions: OptionType[] = getUnconfiguredChannelOptions();
      filteredOptions.push(option);
      setChannelOptions(filteredOptions);
    } else {
      setEmptyChannel();
    }

    setShowChannelEdit(!showChannelEdit);
    setShowAddNewChannelConfiguration(!showAddNewChannelConfiguration);
  };

  const selectChannelOption = (selectedChannelName: string): OptionType => {
    const selectedChannel: LuceneIndexChannel = props.possibleChannels?.find((x) => x.channelName === selectedChannelName) as LuceneIndexChannel;

    const selectedOption: OptionType = {
      value: selectedChannel.channelName,
      label: selectedChannel.channelDisplayName
    };

    setSelectedChannelOption(selectedOption);
    return selectedOption;
  };

  const setEmptyChannel = (): void => {
    const emptyChannel = getEmptyChannel();
    setChannel(emptyChannel);
    setSelectedChannelOption(null);
  };

  const saveChannelConfiguration = (): void => {
    if(!rows.some((x) => {
      return x.identifier === channel.websiteChannelName;
    })) {
      props.value.push(channel);
      setRows(prepareRows(props.value));
    } else {
      const rowIndex = rows.findIndex((x) => {
        return x.identifier === channel.websiteChannelName;
      });

      if (rowIndex === -1) {
        alert('Invalid edit');
      }

      const newRows = rows;
      const editedRow = rows[rowIndex];
      const cellInNewRow = rows[rowIndex].cells[0] as StringCell;
      cellInNewRow.value = channel.websiteChannelName ?? '';
      const propPathIndex = props.value.findIndex(
        (x) => { return x.websiteChannelName === channel.websiteChannelName; }
      );

      const updatedChannel: LuceneIndexChannelConfiguration = {
        websiteChannelName: channel.websiteChannelName,
        channelDisplayName: channel.channelDisplayName,
        includedPaths: channel.includedPaths
      };
      props.value[propPathIndex] = updatedChannel;

      editedRow.cells[0] = cellInNewRow;
      editedRow.identifier = channel.websiteChannelName ?? '';

      newRows[rowIndex] = editedRow;
      setRows(newRows);
    }

    setShowAddNewChannelConfiguration(true);
    setEmptyChannel();
    setShowChannelEdit(false);
    setChannelOptions(getUnconfiguredChannelOptions());
  };

  const selectChannel = (newValue: SingleValue<OptionType>): void => {
    if (!newValue) {
      setEmptyChannel();
      return;
    }

    setChannel((prev) => ({
      ...prev,
      websiteChannelName: newValue.value,
      channelDisplayName: newValue.label
    }));

    selectChannelOption(newValue.value);
  };

  return (
    <Stack>
      <Table
        columns={prepareColumns()}
        rows={rows}
        onRowClick={showChannelDetail}
      />
      {showChannelEdit && (
        <div>
          <br></br>
          <Select
            closeMenuOnSelect={true}
            isMulti={false}
            placeholder="Select website channel"
            value={selectedChannelOption}
            options={channelOptions}
            onChange={selectChannel}
          />
          <br></br>
          <Button
            type={ButtonType.Button}
            label="Save website channel configuration"
            onClick={saveChannelConfiguration}
          ></Button>
        </div>
      )}
      <br></br>
      {showAddNewChannelConfiguration && (
        <Button
          type={ButtonType.Button}
          label="Add new website channel configuration"
          onClick={addNewChannelConfiguration}
        ></Button>
      )}
    </Stack>
  );
};