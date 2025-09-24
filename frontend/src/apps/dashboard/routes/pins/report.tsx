import React from 'react';
import Page from 'components/Page';
import globalize from 'lib/globalize';
import Box from '@mui/material/Box';
import Typography from '@mui/material/Typography';
import CircularProgress from '@mui/material/CircularProgress';
import Table from '@mui/material/Table';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TableCell from '@mui/material/TableCell';
import TableBody from '@mui/material/TableBody';
import { useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';

type PinReport = {
    TotalActive: number;
    TotalExpired: number;
    CreatedLast24h: number;
};

const fetchReport = async (apiClient: ApiClient): Promise<PinReport> => {
    const res = await fetch(apiClient.getUrl('/Users/PinReport'), {
        headers: apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : undefined
    });
    if (!res.ok) throw new Error('Failed to fetch PIN report');
    return res.json();
};

export const Component = () => {
    const { __legacyApiClient__ } = useApi();
    const { data, isLoading } = useQuery({
        queryKey: [ 'Pins', 'Report' ],
        queryFn: () => fetchReport(__legacyApiClient__!)
    });

    return (
        <Page id='pins-report' className='type-interior' title={globalize.translate('HeaderPinReport')}>
            <Box className='content-primary'>
                <Typography variant='h5' gutterBottom>{globalize.translate('HeaderPinReport')}</Typography>
                {isLoading ? (
                    <CircularProgress />
                ) : (
                    <Table sx={{ maxWidth: 480 }}>
                        <TableHead>
                            <TableRow>
                                <TableCell>{globalize.translate('LabelMetric')}</TableCell>
                                <TableCell align='right'>{globalize.translate('LabelValue')}</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            <TableRow>
                                <TableCell>{globalize.translate('LabelTotalActive')}</TableCell>
                                <TableCell align='right'>{data?.TotalActive}</TableCell>
                            </TableRow>
                            <TableRow>
                                <TableCell>{globalize.translate('LabelTotalExpired')}</TableCell>
                                <TableCell align='right'>{data?.TotalExpired}</TableCell>
                            </TableRow>
                            <TableRow>
                                <TableCell>{globalize.translate('LabelCreatedLast24h')}</TableCell>
                                <TableCell align='right'>{data?.CreatedLast24h}</TableCell>
                            </TableRow>
                        </TableBody>
                    </Table>
                )}
            </Box>
        </Page>
    );
};

Component.displayName = 'PinReportPage';
