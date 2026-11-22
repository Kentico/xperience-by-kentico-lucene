import {
  type ActionCell,
  Button,
  ButtonType,
  CellType,
  ColumnContentType,
  Input,
  Stack,
  type StringCell,
  Table,
  type TableAction,
  type TableCell,
  type TableColumn,
  type TableRow,
} from '@kentico/xperience-admin-components';
import type * as React from 'react';
import { type CSSProperties, useState } from 'react';
import { IoCheckmarkSharp } from 'react-icons/io5';
import { MdOutlineCancel } from 'react-icons/md';
import { RxCross1 } from 'react-icons/rx';
import Select, {
  type CSSObjectWithLabel,
  type ClearIndicatorProps,
  type GroupBase,
  type MultiValue,
  type MultiValueRemoveProps,
  type OptionProps,
  type StylesConfig,
  components,
} from 'react-select';
import { Tooltip } from 'react-tooltip';
import type { IncludedPath } from '../models/IncludedPath';
import type { LuceneIncludedPathConfigurationProps } from '../models/LuceneIncludedPathConfigurationProps';
import type { OptionType } from '../models/OptionType';

export const LuceneIncludedPathConfiguration = (
  props: LuceneIncludedPathConfigurationProps,
): React.JSX.Element => {
  const [showPathEdit, setShowPathEdit] = useState<boolean>(false);
  const [contentTypesValue, setContentTypesValue] = useState<OptionType[]>([]);
  const [path, setPath] = useState<string>('');
  const [editedIdentifier, setEditedIdentifier] = useState<string>('');
  const [showAddNewPath, setShowAddNewPath] = useState<boolean>(true);
  const [isClearIndicatorHover, setIsClearIndicatorHover] = useState(false);

  const prepareRows = (paths: IncludedPath[]): TableRow[] => {
    const getCells = (path: IncludedPath): TableCell[] => {
      const pathVal: string = path.aliasPath?.toString() ?? '';
      if (path.aliasPath === null) {
        return [];
      }
      const cell: StringCell = {
        type: CellType.String,
        value: pathVal,
      };
      const deleteAction: TableAction = {
        label: 'delete',
        icon: 'xp-bin',
        disabled: false,
        destructive: true,
      };

      const deletePath = (): Promise<void> => {
        const newValue = props.value.filter((x) => x.aliasPath !== pathVal);
        props.onChange?.(newValue);
        setShowPathEdit(false);
        setContentTypesValue([]);
        setEditedIdentifier('');
        setPath('');
        setShowAddNewPath(true);
        return Promise.resolve();
      };

      const deletePathCell: ActionCell = {
        actions: [deleteAction],
        type: CellType.Action,
        onInvokeAction: deletePath,
      };

      const cells: TableCell[] = [cell, deletePathCell];
      return cells;
    };

    return paths.map((path) => {
      const row: TableRow = {
        identifier: path.aliasPath ?? '',
        cells: getCells(path),
        disabled: false,
      };
      return row;
    });
  };
  const rows = prepareRows(props.value);

  const prepareColumns = (): TableColumn[] => {
    const columns: TableColumn[] = [];

    const column: TableColumn = {
      name: 'Path',
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
  const showContentItems = (identifier: unknown): void => {
    const row = rows.find((item) => item.identifier === identifier);
    if (!row) {
      return;
    }

    setPath((row.cells[0] as StringCell).value);

    if (!showPathEdit) {
      setEditedIdentifier((row.cells[0] as StringCell).value);
    } else {
      setEditedIdentifier('');
    }

    const contentTypes: OptionType[] =
      props.value
        .find((x) => x.aliasPath === identifier)
        ?.contentTypes.map((x) => {
          const option: OptionType = {
            value: x.contentTypeName,
            label: x.contentTypeDisplayName,
          };
          return option;
        }) ?? [];

    setContentTypesValue(contentTypes);
    setShowPathEdit(!showPathEdit);
    setShowAddNewPath(!showAddNewPath);
  };
  const handleInputChange = (
    event: React.ChangeEvent<HTMLInputElement>,
  ): void => {
    setPath(event.target.value);
  };
  const savePath = (): void => {
    if (editedIdentifier === '') {
      if (!rows.some((x) => x.identifier === path)) {
        if (path === '') {
          alert('Invalid path');
        } else {
          const newPath: IncludedPath = {
            aliasPath: path,
            identifier: null,
            contentTypes: contentTypesValue.map((x) => ({
              contentTypeDisplayName: x.label,
              contentTypeName: x.value,
            })),
          };
          // Create a new array instead of mutating props.value
          const newPaths = [...props.value, newPath];
          props.onChange?.(newPaths);
        }
      } else {
        alert('This path already exists!');
      }
    } else {
      const propPathIndex = props.value.findIndex(
        (p) => p.aliasPath === editedIdentifier,
      );
      if (propPathIndex === -1) {
        alert('Invalid edit');
        return;
      }
      const updatedPath: IncludedPath = {
        aliasPath: path,
        identifier: props.value[propPathIndex].identifier,
        contentTypes: contentTypesValue.map((x) => ({
          contentTypeDisplayName: x.label,
          contentTypeName: x.value,
        })),
      };
      // Create a new array with the updated path
      const newPaths = props.value.map((p, idx) =>
        idx === propPathIndex ? updatedPath : p,
      );
      props.onChange?.(newPaths);
    }

    setEditedIdentifier('');
    setShowPathEdit(false);
    setShowAddNewPath(true);
  };
  const addNewPath = (): void => {
    setShowPathEdit(true);
    setContentTypesValue([]);
    setPath('');
    setEditedIdentifier('');
    setShowAddNewPath(false);
  };
  const options: OptionType[] =
    props.possibleContentTypeItems?.map((x) => {
      const option: OptionType = {
        value: x.contentTypeName,
        label: x.contentTypeDisplayName,
      };
      return option;
    }) ?? [];
  const selectContentTypes = (newValue: MultiValue<OptionType>): void => {
    setContentTypesValue([...newValue]);
  };

  const customStyle: StylesConfig<OptionType, true, GroupBase<OptionType>> = {
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
    multiValue: (styles) => ({
      ...styles,
      backgroundColor: '#287ab5',
      borderRadius: 10,
      height: 35,
      alignItems: 'center',
    }),
    multiValueLabel: (styles) => ({
      ...styles,
      color: 'white',
      fontSize: 14,
      alignContent: 'center',
    }),
    indicatorSeparator: () => ({}),
    dropdownIndicator: (styles, state): CSSObjectWithLabel => ({
      ...styles,
      transform: state.selectProps.menuIsOpen
        ? 'rotate(180deg)'
        : 'rotate(0deg)',
    }),
    multiValueRemove: (styles) => ({
      ...styles,
      '&:hover': {
        background: '#287ab5',
        borderRadius: 10,
        cursor: 'pointer',
        filter: 'grayscale(40%)',
        height: '100%',
      },
    }),
  };

  const MultiValueRemoveStyle: CSSProperties = {
    color: 'white',
    height: '20',
    width: '30',
  };
  const MultiValueRemove = (
    props: MultiValueRemoveProps<OptionType>,
  ): React.JSX.Element => {
    return (
      <components.MultiValueRemove {...props}>
        <RxCross1 style={MultiValueRemoveStyle} />
      </components.MultiValueRemove>
    );
  };

  const Option = (
    props: OptionProps<OptionType, true, GroupBase<OptionType>>,
  ): React.JSX.Element => {
    return (
      <components.Option {...props}>
        {props.isSelected ? (
          <IoCheckmarkSharp style={{ width: 30, alignContent: 'center' }} />
        ) : (
          <span style={{ width: 30, display: 'inline-block' }}></span>
        )}
        {props.children}
      </components.Option>
    );
  };

  const handleMouseEnter = (): void => {
    setIsClearIndicatorHover(true);
  };
  const handleMouseLeave = (): void => {
    setIsClearIndicatorHover(false);
  };
  const IndicatorStyle: CSSProperties = {
    color: 'black',
    width: '80%',
    height: '80%',
  };
  const ClearIndicator = (
    props: ClearIndicatorProps<OptionType>,
  ): React.JSX.Element => {
    return (
      <components.ClearIndicator {...props}>
        <Tooltip id="clear-content-type-select-tooltip-1" />
        <span
          style={{
            background: isClearIndicatorHover ? 'lightgray' : 'white',
            width: 25,
            height: 25,
            display: 'flex',
            justifyContent: 'center',
            alignItems: 'center',
            borderRadius: 5,
            cursor: isClearIndicatorHover ? 'pointer' : 'default',
          }}
        >
          <MdOutlineCancel
            style={IndicatorStyle}
            onMouseEnter={handleMouseEnter}
            onMouseLeave={handleMouseLeave}
            data-tooltip-id="clear-content-type-select-tooltip-1"
            data-tooltip-content="Clear selection"
          />
        </span>
      </components.ClearIndicator>
    );
  };

  return (
    <Stack>
      <Table
        columns={prepareColumns()}
        rows={rows}
        onRowClick={showContentItems}
      />
      {showPathEdit && (
        <div style={{ marginTop: '20px' }}>
          <Input
            label="Included Path"
            value={path}
            onChange={handleInputChange}
          />
          <div style={{ marginTop: '20px' }}>
            <div className="label-wrapper___AcszK">
              <label className="label___WET63">Included content types</label>
            </div>
            <Select
              isMulti
              closeMenuOnSelect={false}
              defaultValue={contentTypesValue}
              placeholder="Select content types"
              options={options}
              onChange={selectContentTypes}
              styles={customStyle}
              hideSelectedOptions={false}
              components={{ MultiValueRemove, ClearIndicator, Option }}
              theme={(theme) => ({
                ...theme,
                height: 40,
                borderRadius: 0,
                borderColor: 'gray',
              })}
            />
          </div>
          <div style={{ marginTop: '20px' }}>
            <Button
              type={ButtonType.Button}
              label="Save Path"
              onClick={savePath}
            ></Button>
          </div>
        </div>
      )}
      {showAddNewPath && (
        <div style={{ marginTop: '20px' }}>
          <Button
            type={ButtonType.Button}
            label="Add new path"
            onClick={addNewPath}
          ></Button>
        </div>
      )}
    </Stack>
  );
};
