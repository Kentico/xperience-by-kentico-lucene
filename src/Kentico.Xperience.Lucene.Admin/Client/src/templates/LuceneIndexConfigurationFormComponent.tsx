import { type FormComponentProps } from '@kentico/xperience-admin-base';
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
import React, { type CSSProperties, useEffect, useState } from 'react';
import { IoCheckmarkSharp } from 'react-icons/io5';
import { MdOutlineCancel } from 'react-icons/md';
import { RxCross1 } from 'react-icons/rx';
import Select, { type CSSObjectWithLabel, type ClearIndicatorProps, type GroupBase, type MultiValue, type MultiValueRemoveProps, type OptionProps, type StylesConfig, components } from 'react-select';
import { Tooltip } from 'react-tooltip';

export interface LuceneIndexContentType {
    contentTypeName: string;
    contentTypeDisplayName: string;
}

export interface IncludedPath {
    aliasPath: string | null;
    contentTypes: LuceneIndexContentType[];
    identifier: string | null;
}

export interface LuceneIndexConfigurationComponentClientProperties
    extends FormComponentProps {
    value: IncludedPath[];
    possibleContentTypeItems: LuceneIndexContentType[] | null;
}

interface OptionType {
    value: string;
    label: string;
}

export interface TextAreaCell extends TableCell {
    /**
     * Value of the cell.
     */
    value: HTMLTextAreaElement;
}

export const LuceneIndexConfigurationFormComponent = (
    props: LuceneIndexConfigurationComponentClientProperties,
): JSX.Element => {
    const [rows, setRows] = useState<TableRow[]>([]);
    const [showPathEdit, setShowPathEdit] = useState<boolean>(false);
    const [contentTypesValue, setContentTypesValue] = useState<OptionType[]>([]);
    const [path, setPath] = useState<string>('');
    const [editedIdentifier, setEditedIdentifier] = useState<string>('');
    const [showAddNewPath, setShowAddNewPath] = useState<boolean>(true);
    const [isClearIndicatorHover, setIsClearIndicatorHover] = useState(false);

    const prepareRows = (paths: IncludedPath[]): TableRow[] => {
        if (paths === undefined) {
            return [];
        }
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

            const deletePath: () => Promise<void> = async () => {
                await new Promise(() => {
                    props.value = props.value.filter((x) => x.aliasPath !== pathVal);

                    if (props.onChange !== null && props.onChange !== undefined) {
                        props.onChange(props.value);
                    }

                    setRows(prepareRows(props.value));
                    setShowPathEdit(false);
                    setContentTypesValue([]);
                    setEditedIdentifier('');
                    setPath('');
                    setShowAddNewPath(true);
                });
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
                identifier: path.aliasPath,
                cells: getCells(path),
                disabled: false,
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
        let rowIndex = -1;
        for (let i = 0; i < rows.length; i++) {
            if ((rows[i].identifier as string) === (identifier as string)) {
                rowIndex = i;
            }
        }
        const row = rows[rowIndex];

        setPath((row.cells[0] as StringCell).value);

        if (!showPathEdit) {
            setEditedIdentifier((row.cells[0] as StringCell).value);
        } else {
            setEditedIdentifier('');
        }

        const contentTypes: OptionType[] = props.value.find((x) => x.aliasPath === identifier)?.contentTypes.map(x => {
            const option: OptionType = {
                value: x.contentTypeName,
                label: x.contentTypeDisplayName
            };
            return option;
        }) ?? [];


        setContentTypesValue(contentTypes ?? []);
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
            if (!rows.some((x) => {
                return x.identifier === path;
            })) {
                if (path === '') {
                    alert('Invalid path');
                } else {
                    const newPath: IncludedPath = {
                        aliasPath: path,
                        identifier: null,
                        contentTypes: contentTypesValue.map(x => {
                            const contentType: LuceneIndexContentType = {
                                contentTypeDisplayName: x.label,
                                contentTypeName: x.value
                            };

                            return contentType;
                        })
                    };
                    props.value.push(newPath);
                    setRows(prepareRows(props.value));
                }
            } else {
                alert('This path already exists!');
            }
        } else {
            const rowIndex = rows.findIndex((x) => {
                return x.identifier === editedIdentifier;
            });

            if (rowIndex === -1) {
                alert('Invalid edit');
            }

            const newRows = rows;
            const editedRow = rows[rowIndex];
            const pathCellInNewRow = rows[rowIndex].cells[0] as StringCell;
            pathCellInNewRow.value = path;
            const propPathIndex = props.value.findIndex(
                (p) => p.aliasPath === editedIdentifier,
            );

            const updatedPath: IncludedPath = {
                aliasPath: path,
                identifier: props.value[propPathIndex].identifier,
                contentTypes: contentTypesValue.map(x => {
                    const contentType: LuceneIndexContentType = {
                        contentTypeDisplayName: x.label,
                        contentTypeName: x.value
                    };

                    return contentType;
                })
            };

            props.value[propPathIndex] = updatedPath;

            editedRow.cells[0] = pathCellInNewRow;
            editedRow.identifier = path;

            newRows[rowIndex] = editedRow;
            setRows(newRows);
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
    const options: OptionType[] = props.possibleContentTypeItems?.map(x => {
        const option: OptionType = {
            value: x.contentTypeName,
            label: x.contentTypeDisplayName
        };
        return option;
    }) ?? [];
    const selectContentTypes = (newValue: MultiValue<OptionType>): void => {
        setContentTypesValue(newValue as OptionType[]);
    }

    /* eslint-disable @typescript-eslint/naming-convention */
    /* eslint-disable @typescript-eslint/consistent-type-assertions */
    const customStyle: StylesConfig<OptionType, true, GroupBase<OptionType>> = {
        control: (styles, { isFocused }) => ({ 
            ...styles,
            backgroundColor: 'white',
            borderColor: isFocused ? 'black' : 'gray',
            '&:hover': {
                borderColor: 'black'
            },
            borderRadius: 20,
            boxShadow: 'gray',
            padding: 2,
            minHeight: 'fit-content',
        } as CSSObjectWithLabel),
        option: (styles, { isSelected }) => {
            return { 
                ...styles,
                backgroundColor: isSelected ? '#bab4f0' : 'white',
                '&:hover': {
                    backgroundColor: isSelected ? '#a097f7' : 'lightgray'
                },
                color: isSelected ? 'purple' : 'black',
                cursor: 'pointer'
            } as CSSObjectWithLabel;
        },
        input: (styles) => ({ ...styles }),
        container: (styles) => ({ ...styles, borderColor: 'gray' } as CSSObjectWithLabel),
        placeholder: (styles) => ({ ...styles }),
        multiValue: (styles) => ({
            ...styles,
            backgroundColor: '#287ab5',
            borderRadius: 10,
            height: 35,
            alignItems: 'center',
        } as CSSObjectWithLabel),
        multiValueLabel: (styles) => ({
            ...styles,
            color: 'white',
            fontSize: 14,
            alignContent: 'center'
        } as CSSObjectWithLabel),
        indicatorSeparator: () => ({}),
        dropdownIndicator: (styles, state): CSSObjectWithLabel => ({ 
            ...styles,
            transform: state.selectProps.menuIsOpen ? 'rotate(180deg)' : 'rotate(0deg)',
        } as CSSObjectWithLabel),
        multiValueRemove: (styles) => ({
            ...styles,
            '&:hover': {
                background: '#287ab5',
                borderRadius: 10,
                cursor: 'pointer',
                filter: 'grayscale(40%)',
                height: '100%'
            }
        } as CSSObjectWithLabel)
        /* eslint-enable @typescript-eslint/naming-convention */
        /* eslint-enable @typescript-eslint/consistent-type-assertions */
    };

    const MultiValueRemoveStyle: CSSProperties = {
        color: 'white',
        height: '20',
        width: '30'
    };
    const MultiValueRemove = (props: MultiValueRemoveProps<OptionType>): JSX.Element => {
        return (
            <components.MultiValueRemove {...props}>
                <RxCross1 style={MultiValueRemoveStyle} />
            </components.MultiValueRemove>
        );
    };

    const Option = (props: OptionProps<OptionType, true, GroupBase<OptionType>>): JSX.Element => {
        return (
            <components.Option {...props}>
                {props.isSelected ? <IoCheckmarkSharp style={{ width: 30, alignContent: 'center' }} /> : <span style={{ width: 30, display: 'inline-block' }}></span>}
                {props.children}
            </components.Option>
        );
    }

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
    }
    const ClearIndicator = (props: ClearIndicatorProps<OptionType>): JSX.Element => {
        return (
            <components.ClearIndicator {...props}>
                <Tooltip id="clear-content-type-select-tooltip-1" />
                <span style={{
                    background: isClearIndicatorHover ? 'lightgray' : 'white',
                    width: 25,
                    height: 25,
                    display: 'flex',
                    justifyContent: 'center',
                    alignItems: 'center',
                    borderRadius: 5,
                    cursor: isClearIndicatorHover ? 'pointer' : 'default'
                }}>
                    <MdOutlineCancel style={IndicatorStyle}
                        onMouseEnter={handleMouseEnter}
                        onMouseLeave={handleMouseLeave}
                        data-tooltip-id="clear-content-type-select-tooltip-1"
                        data-tooltip-content="Clear selection"
                    />
                </span >
            </components.ClearIndicator>
        );
    }

    return (
        <Stack>
            <Table
                columns={prepareColumns()}
                rows={rows}
                onRowClick={showContentItems}
            />
            {showPathEdit && (
                <div>
                    <br></br>
                    <Input
                        label="Included Path"
                        value={path}
                        onChange={handleInputChange}
                    />
                    <br></br>
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
                        })} />
                    <br></br>
                    <Button
                        type={ButtonType.Button}
                        label="Save Path"
                        onClick={savePath}></Button>
                </div>
            )}
            <br></br>
            {showAddNewPath && (
                <Button
                    type={ButtonType.Button}
                    label="Add new path"
                    onClick={addNewPath}
                ></Button>
            )}
        </Stack>
    );
};
