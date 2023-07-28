import React from 'react'
import { Box, Headline, HeadlineSize, Spacing, Stack, Table, TableColumn, TableRow } from '@kentico/xperience-admin-components'
import { RoutingContentPlaceholder, usePageCommand } from '@kentico/xperience-admin-base'
import localization from '../localization/localization.json'

interface IndexedContentPageProps {
  readonly pathColumns: TableColumn[]
  readonly pathRows: TableRow[]
  readonly propertyColumns: TableColumn[]
  readonly propertyRows: TableRow[]
}

interface PathDetailArguments {
  readonly identifier: string
}

const Commands = {
  ShowPathDetail: 'ShowPathDetail'
}

export const IndexedContentTemplate = ({ pathColumns, pathRows, propertyColumns, propertyRows }: IndexedContentPageProps): JSX.Element => {
  const { execute: showPathDetail } = usePageCommand<void, PathDetailArguments>(Commands.ShowPathDetail)

  const pathClicked = (index: number): void => {
    // Send path identifier to back-end
    const row = pathRows[index]
    if (row !== undefined) {
      showPathDetail({ identifier: row.identifier }).catch(() => {})
    }
  }

  return (
        <RoutingContentPlaceholder>
            <Stack spacing={Spacing.XXL}>
                <Headline size={HeadlineSize.M}>{localization.integrations.lucene.content.headlines.main}</Headline>
                <Box>
                    <Headline size={HeadlineSize.S}>{localization.integrations.lucene.content.headlines.paths}</Headline>
                    <Table columns={pathColumns} rows={pathRows} onRowClick={pathClicked}/>
                </Box>
                <Box>
                    <Headline size={HeadlineSize.S}>{localization.integrations.lucene.content.headlines.properties}</Headline>
                    <Table columns={propertyColumns} rows={propertyRows} />
                </Box>
            </Stack>
        </RoutingContentPlaceholder>
  )
}
