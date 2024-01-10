import { type FormComponentProps } from '@kentico/xperience-admin-base';
import {
    type ActionCell,
    Button,
    ButtonType,
    CellType,
    ColumnContentType,
    DropDownSelectMenu,
    Input,
    MenuItem,
    Select,
    SelectMenu,
    Stack,
    type StringCell,
    Table,
    type TableAction,
    type TableCell,
    type TableColumn,
    type TableRow,
    TextArea,
} from '@kentico/xperience-admin-components';
import React, { Component, ReactNode, useEffect, useState } from 'react';

export interface IncludedPath {
    aliasPath: string | null;
    contentTypes: string[];
    identifier: string | null;
}

export interface ListComponentClientProperties extends FormComponentProps {
    value: IncludedPath[];
    possibleItems: string[];
}

export interface TextAreaCell extends TableCell {
    /**
     * Value of the cell.
     */
    value: HTMLTextAreaElement;
}

export const ListingFormComponent = (props: ListComponentClientProperties): JSX.Element => {
    const [rows, setRows] = useState<TableRow[]>([]);
    const [showPathEdit, setShowPathEdit] = useState<boolean>(false);
    const [contentTypesValue, setContentTypesValue] = useState<string>('');
    const [path, setPath] = useState<string>('');
    const [editedIdentifier, setEditedIdentifier] = useState<string>('');
    const [showAddNewPath, setShowAddNewPath] = useState<boolean>(true);

    const getPossibleContentTypeItems = (): string => {
        let msg = 'Included Content Types(';

        for (let i = 0; i < props.possibleItems.length - 1; i++){
            msg += props.possibleItems[i] + ', ';
        }

        if (props.possibleItems.length > 0) {
            msg += props.possibleItems[props.possibleItems.length - 1];
        }
        msg += ') - Separate by new line';
        return msg;
    }

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
                value: pathVal
            };
            const deleteAction: TableAction = {
                label: 'delete',
                icon: 'xp-bin',
                disabled: false,
                destructive: true
            };
            
            const deletePath: () => Promise<void> = async () => {
                await new Promise(() => {
                    props.value = props.value.filter(x => x.aliasPath !== pathVal);

                    if (props.onChange != null) {
                        props.onChange(props.value);
                    }

                    setRows(prepareRows(props.value));
                    setShowPathEdit(false);
                    setContentTypesValue('');
                    setEditedIdentifier('');
                    setPath('');
                    setShowAddNewPath(true);
                });
            }

            const deletePathCell: ActionCell = {
                actions: [deleteAction],
                type: CellType.Action,
                onInvokeAction: deletePath
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
        if (props.onChange != null) {
            props.onChange(props.value);
        }
        setRows(prevRows => prepareRows(props.value));
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
            searchable: true
        };

        const actionColumn: TableColumn = {
            name: 'Actions',
            visible: true,
            contentType: ColumnContentType.Action,
            caption: '',
            minWidth: 0,
            maxWidth: 1000,
            sortable: false,
            searchable: false
        }

        columns.push(column);
        columns.push(actionColumn);
        return columns;
    }
    const showContentItems = (identifier: unknown): void => {
        let rowIndex = -1;
        for (let i = 0; i < rows.length; i++) {
            if (rows[i].identifier as string === identifier as string) {
                rowIndex = i;
            }
        }
        const row = rows[rowIndex];

        setPath((row.cells[0] as StringCell).value);

        if (!showPathEdit) {
            setEditedIdentifier((row.cells[0] as StringCell).value);
        }
        else {
            setEditedIdentifier('');
        }

        const contentTypes = props.value.find((x) => {
            return x.aliasPath === identifier
        })?.contentTypes;

        let contentTypesAsString: string = '';
        contentTypes?.forEach(x => {
            contentTypesAsString += x + '\n';
        })

        setContentTypesValue(contentTypesAsString);
        setShowPathEdit(!showPathEdit);
        setShowAddNewPath(!showAddNewPath);
    }
    const handleTextareaChange = (event: React.ChangeEvent<HTMLTextAreaElement>): void => {
        setContentTypesValue(event.target.value);
    };
    const handleInputChange = (event: React.ChangeEvent<HTMLInputElement>): void => {
        setPath(event.target.value);
    };
    const savePath = (): void => {
        const contentTypesSplit = contentTypesValue.split('\n').filter(x => {
            return x !== '' && x != '' && x != null && x != undefined;
        });
        if (editedIdentifier === '') {
            if (!rows.some(x => {
                return x.identifier === path;
            })) {
                if (path === '') {
                    alert('Invalid path');
                }
                else {
                    const newPath: IncludedPath = {
                        aliasPath: path,
                        identifier: null,
                        contentTypes: contentTypesSplit
                    };
                    props.value.push(newPath);
                    setRows(prepareRows(props.value));
                }
            }
            else {
                alert('This path already exists!');
            }
        }
        else {
            const rowIndex = rows.findIndex(x => {
                return x.identifier === editedIdentifier;
            });

            if (rowIndex === -1) {
                alert('Invalid edit');
            }

            const newRows = rows;
            const editedRow = rows[rowIndex];
            const pathCellInNewRow = rows[rowIndex].cells[0] as StringCell;
            pathCellInNewRow.value = path;

            editedRow.cells[0] = pathCellInNewRow;
            editedRow.identifier = path;

            newRows[rowIndex] = editedRow;
            setRows(newRows);

            const pathInProps = props.value.find(x => {
                return x.aliasPath === editedIdentifier;
            });

            if (pathInProps == undefined) {
                alert('Invalid edit');
            }

            const pathInPropsDefined = pathInProps!;
            pathInPropsDefined.aliasPath = path;
            pathInPropsDefined.contentTypes = contentTypesSplit;
        }

        setEditedIdentifier('');
        setShowPathEdit(false);
        setShowAddNewPath(true);
    }
    const addNewPath = (): void => {
        setShowPathEdit(true);
        setContentTypesValue('');
        setPath('');
        setEditedIdentifier('');
        setShowAddNewPath(false);
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
                    <Input label="Path" value={path} onChange={handleInputChange} />
                    <br></br>
                    <TextArea label={getPossibleContentTypeItems()} value={contentTypesValue} onChange={handleTextareaChange} />
                    <br></br>
                    <Button type={ButtonType.Button} label="Save Path" onClick={savePath}></Button>
                </div>
            )}
            <br></br>
            {showAddNewPath && (
                <Button type={ButtonType.Button} label="Add new path" onClick={addNewPath}></Button>
            )}
        </Stack>
    );
};
