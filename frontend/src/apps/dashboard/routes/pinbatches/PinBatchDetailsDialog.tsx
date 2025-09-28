import React, { useCallback, useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid2';
import Box from '@mui/material/Box';
import Chip from '@mui/material/Chip';
import Divider from '@mui/material/Divider';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import CircularProgress from '@mui/material/CircularProgress';
import Alert from '@mui/material/Alert';
import { useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import { PinBatchUtils, type PinBatch, type PinBatchUser } from '../../types/pinBatch';

type BatchStatistics = {
    BatchId: string;
    BatchName: string;
    TotalPins: number;
    ActivePins: number;
    ExpiredPins: number;
    UsedPins: number;
    UnusedPins: number;
    TotalUsageCount: number;
    AverageUsagePerPin: number;
    CreatedDate: string;
    LastActivityDate?: string;
};

const fetchBatchStatistics = async (apiClient: ApiClient, batchId: string): Promise<BatchStatistics> => {
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Statistics`)
    });

    return res.json() as Promise<BatchStatistics>;
};

const fetchBatchPins = async (apiClient: ApiClient, batchId: string, includeInactive: boolean = false, includeExpired: boolean = false, includeOriginalPins: boolean = true): Promise<PinBatchUser[]> => {
    const queryParams = [];
    if (includeInactive) queryParams.push('includeInactive=true');
    if (includeExpired) queryParams.push('includeExpired=true');
    if (includeOriginalPins) queryParams.push('includeOriginalPins=true');

    const queryString = queryParams.length > 0 ? `?${queryParams.join('&')}` : '';
    const url = `/PinBatches/${batchId}/Pins${queryString}`;
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl(url)
    });

    return res.json() as Promise<PinBatchUser[]>;
};

const getStatusColor = (status: number): 'default' | 'primary' | 'secondary' | 'error' | 'info' | 'success' | 'warning' => {
    switch (status) {
        case 0: return 'success'; // Active
        case 1: return 'warning'; // Suspended
        case 2: return 'error'; // Expired
        case 3: return 'default'; // Deleted
        default: return 'default';
    }
};

const getStatusText = (status: number): string => {
    switch (status) {
        case 0: return 'Active';
        case 1: return 'Suspended';
        case 2: return 'Expired';
        case 3: return 'Deleted';
        default: return 'Unknown';
    }
};

const getPatternText = (pattern: number): string => {
    switch (pattern) {
        case 0: return 'Numeric';
        case 1: return 'Alphanumeric';
        case 2: return 'Alphanumeric (Mixed)';
        case 3: return 'Custom';
        default: return 'Unknown';
    }
};

const getSubscriptionTypeText = (type: number): string => {
    switch (type) {
        case 0: return 'None';
        case 1: return '6 Hours';
        case 2: return '12 Hours';
        case 3: return 'Daily';
        case 4: return 'Weekly';
        case 5: return 'Monthly';
        case 6: return 'Quarterly';
        case 7: return 'Yearly';
        case 8: return 'Lifetime';
        case 9: return 'Custom';
        default: return 'Unknown';
    }
};

interface PinBatchDetailsDialogProps {
    readonly open: boolean;
    readonly onClose: () => void;
    readonly batch: PinBatch;
}

export default function PinBatchDetailsDialog({ open, onClose, batch }: PinBatchDetailsDialogProps) {
    const { __legacyApiClient__ } = useApi();
    const [showAllPins, setShowAllPins] = useState(false);
    const [isGeneratingPins, setIsGeneratingPins] = useState(false);

    const { data: statistics, isLoading: statisticsLoading } = useQuery({
        queryKey: ['PinBatchStatistics', batch.Id],
        queryFn: () => fetchBatchStatistics(__legacyApiClient__!, batch.Id),
        enabled: open
    });

    const { data: pins, isLoading: pinsLoading } = useQuery({
        queryKey: ['PinBatchPins', batch.Id, showAllPins],
        queryFn: () => fetchBatchPins(__legacyApiClient__!, batch.Id, showAllPins, showAllPins, true),
        enabled: open
    });

    const formatDate = useCallback((dateString: string) => {
        return new Date(dateString).toLocaleString();
    }, []);

    const formatDateOnly = useCallback((dateString: string) => {
        return new Date(dateString).toLocaleDateString();
    }, []);

    const handleToggleShowAllPins = useCallback(() => {
        setShowAllPins(!showAllPins);
    }, [showAllPins]);

    const handleGeneratePins = useCallback(async () => {
        if (!__legacyApiClient__) return;

        const pinCount = window.prompt('How many PINs would you like to generate?', '10');
        if (!pinCount || isNaN(Number(pinCount)) || Number(pinCount) <= 0) {
            return;
        }

        setIsGeneratingPins(true);
        try {
            const response = await __legacyApiClient__.ajax({
                type: 'POST',
                url: __legacyApiClient__.getUrl(`/PinBatches/${batch.Id}/GeneratePins`),
                data: JSON.stringify({ PinCount: Number(pinCount) }),
                contentType: 'application/json'
            });

            if (response.status === 200) {
                // Refresh the data
                window.location.reload();
            } else {
                alert('Failed to generate PINs. Please try again.');
            }
        } catch (error) {
            console.error('Error generating PINs:', error);
            alert('Failed to generate PINs. Please try again.');
        } finally {
            setIsGeneratingPins(false);
        }
    }, [__legacyApiClient__, batch.Id]);

    const renderPinTable = useCallback(() => {
        if (pinsLoading) {
            return (
                <Box display='flex' justifyContent='center' p={2}>
                    <CircularProgress />
                </Box>
            );
        }

        if (pins && pins.length > 0) {
            return (
                <Table size='small'>
                    <TableHead>
                        <TableRow>
                            <TableCell>PIN Code</TableCell>
                            <TableCell>User ID</TableCell>
                            <TableCell>Status</TableCell>
                            <TableCell>Created</TableCell>
                            <TableCell>First Used</TableCell>
                            <TableCell>Last Used</TableCell>
                            <TableCell>Usage Count</TableCell>
                            <TableCell>Expiration</TableCell>
                            <TableCell>Time Remaining</TableCell>
                            <TableCell>Last Login IP</TableCell>
                        </TableRow>
                    </TableHead>
                    <TableBody>
                        {pins?.slice(0, 50).map((pin) => (
                            <TableRow key={pin.Id}>
                                <TableCell>
                                    <Typography
                                        variant='body2'
                                        sx={{
                                            fontFamily: 'monospace',
                                            fontWeight: 'bold',
                                            color: 'primary.main'
                                        }}
                                    >
                                        {pin.OriginalPin || 'N/A'}
                                    </Typography>
                                </TableCell>
                                <TableCell>{pin.UserId || 'N/A'}</TableCell>
                                <TableCell>
                                    <Chip
                                        label={pin.IsActive ? 'Active' : 'Inactive'}
                                        color={pin.IsActive ? 'success' : 'default'}
                                        size='small'
                                    />
                                </TableCell>
                                <TableCell>{formatDate(pin.CreatedDate)}</TableCell>
                                <TableCell>{pin.FirstUsedDate ? formatDate(pin.FirstUsedDate) : 'Never'}</TableCell>
                                <TableCell>{pin.LastUsedDate ? formatDate(pin.LastUsedDate) : 'Never'}</TableCell>
                                <TableCell>{pin.UsageCount || 0}</TableCell>
                                <TableCell>{pin.ExpirationDate ? formatDateOnly(pin.ExpirationDate) : 'Lifetime'}</TableCell>
                                <TableCell>
                                    <Chip
                                        label={PinBatchUtils.getTimeRemaining(pin.ExpirationDate)}
                                        color={PinBatchUtils.getStatusColor(pin.ExpirationDate)}
                                        size='small'
                                    />
                                </TableCell>
                                <TableCell>{pin.LastLoginIp || 'N/A'}</TableCell>
                            </TableRow>
                        )) || []}
                    </TableBody>
                </Table>
            );
        }

        return (
            <Box>
                <Alert severity='info' sx={{ mb: 2 }}>
                    No PINs found for this batch.
                </Alert>
                <Button
                    variant='contained'
                    color='primary'
                    onClick={handleGeneratePins}
                    disabled={isGeneratingPins}
                >
                    {isGeneratingPins ? 'Generating...' : 'Generate PINs'}
                </Button>
            </Box>
        );
    }, [pinsLoading, pins, formatDate, formatDateOnly, handleGeneratePins, isGeneratingPins]);

    const renderStatistics = useCallback(() => {
        if (statisticsLoading) {
            return (
                <Box display='flex' justifyContent='center' p={2}>
                    <CircularProgress />
                </Box>
            );
        }

        if (!statistics) {
            return <Alert severity='error'>Failed to load statistics</Alert>;
        }

        return (
            <Grid container spacing={2}>
                <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <Box textAlign='center' p={2} bgcolor='primary.light' borderRadius={1}>
                        <Typography variant='h4' color='primary.contrastText'>{statistics.TotalPins}</Typography>
                        <Typography variant='body2' color='primary.contrastText'>Total PINs</Typography>
                    </Box>
                </Grid>
                <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <Box textAlign='center' p={2} bgcolor='success.light' borderRadius={1}>
                        <Typography variant='h4' color='success.contrastText'>{statistics.ActivePins}</Typography>
                        <Typography variant='body2' color='success.contrastText'>Active PINs</Typography>
                    </Box>
                </Grid>
                <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <Box textAlign='center' p={2} bgcolor='info.light' borderRadius={1}>
                        <Typography variant='h4' color='info.contrastText'>{statistics.UsedPins}</Typography>
                        <Typography variant='body2' color='info.contrastText'>Used PINs</Typography>
                    </Box>
                </Grid>
                <Grid size={{ xs: 12, sm: 6, md: 3 }}>
                    <Box textAlign='center' p={2} bgcolor='warning.light' borderRadius={1}>
                        <Typography variant='h4' color='warning.contrastText'>{statistics.ExpiredPins}</Typography>
                        <Typography variant='body2' color='warning.contrastText'>Expired PINs</Typography>
                    </Box>
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                    <Typography variant='body2' color='text.secondary'>Total Usage Count</Typography>
                    <Typography variant='body1'>{statistics.TotalUsageCount}</Typography>
                </Grid>
                <Grid size={{ xs: 12, sm: 6 }}>
                    <Typography variant='body2' color='text.secondary'>Average Usage per PIN</Typography>
                    <Typography variant='body1'>{statistics.AverageUsagePerPin.toFixed(2)}</Typography>
                </Grid>
                {statistics.LastActivityDate && (
                    <Grid size={{ xs: 12 }}>
                        <Typography variant='body2' color='text.secondary'>Last Activity</Typography>
                        <Typography variant='body1'>{formatDate(statistics.LastActivityDate)}</Typography>
                    </Grid>
                )}
            </Grid>
        );
    }, [statisticsLoading, statistics, formatDate]);

    return (
        <Dialog open={open} onClose={onClose} maxWidth='lg' fullWidth>
            <DialogTitle>
                <Box display='flex' justifyContent='space-between' alignItems='center'>
                    <Typography variant='h6'>{batch.Name}</Typography>
                    <Chip
                        label={getStatusText(batch.Status)}
                        color={getStatusColor(batch.Status)}
                        size='small'
                    />
                </Box>
            </DialogTitle>
            <DialogContent>
                <Grid container spacing={3}>
                    {/* Basic Information */}
                    <Grid size={{ xs: 12 }}>
                        <Typography variant='h6' gutterBottom>Basic Information</Typography>
                        <Grid container spacing={2}>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Description</Typography>
                                <Typography variant='body1'>{batch.Description || 'N/A'}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Subscription Type</Typography>
                                <Typography variant='body1'>{getSubscriptionTypeText(batch.SubscriptionType ?? 0)}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>PIN Pattern</Typography>
                                <Typography variant='body1'>{getPatternText(batch.PinPattern ?? 0)}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>PIN Length</Typography>
                                <Typography variant='body1'>{batch.PinLength}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Created Date</Typography>
                                <Typography variant='body1'>{formatDate(batch.CreatedDate)}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Expiration Date</Typography>
                                <Typography variant='body1'>{batch.ExpirationDate ? formatDateOnly(batch.ExpirationDate) : 'Lifetime'}</Typography>
                            </Grid>
                        </Grid>
                    </Grid>

                    <Grid size={{ xs: 12 }}>
                        <Divider />
                    </Grid>

                    {/* Statistics */}
                    <Grid size={{ xs: 12 }}>
                        <Typography variant='h6' gutterBottom>Statistics</Typography>
                        {renderStatistics()}
                    </Grid>

                    <Grid size={{ xs: 12 }}>
                        <Divider />
                    </Grid>

                    {/* Configuration */}
                    <Grid size={{ xs: 12 }}>
                        <Typography variant='h6' gutterBottom>Configuration</Typography>
                        <Grid container spacing={2}>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Max Concurrent Sessions</Typography>
                                <Typography variant='body1'>{batch.MaxConcurrentSessions || 'Unlimited'}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Max Bitrate</Typography>
                                <Typography variant='body1'>{batch.MaxBitrate ? `${batch.MaxBitrate} kbps` : 'Unlimited'}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Max Parental Rating</Typography>
                                <Typography variant='body1'>{batch.MaxParentalRating || 'Unlimited'}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12, sm: 6 }}>
                                <Typography variant='body2' color='text.secondary'>Price</Typography>
                                <Typography variant='body1'>{batch.Price ? `${batch.Currency} ${batch.Price}` : 'Free'}</Typography>
                            </Grid>
                            <Grid size={{ xs: 12 }}>
                                <Typography variant='body2' color='text.secondary'>Permissions</Typography>
                                <Box display='flex' gap={1} flexWrap='wrap'>
                                    <Chip label='Remote Access' color={batch.AllowRemoteAccess ? 'success' : 'default'} size='small' />
                                    <Chip label='Transcoding' color={batch.AllowTranscoding ? 'success' : 'default'} size='small' />
                                    <Chip label='Download' color={batch.AllowDownload ? 'success' : 'default'} size='small' />
                                    <Chip label='Sync Play' color={batch.AllowSyncPlay ? 'success' : 'default'} size='small' />
                                </Box>
                            </Grid>
                        </Grid>
                    </Grid>

                    <Grid size={{ xs: 12 }}>
                        <Divider />
                    </Grid>

                    {/* PIN List */}
                    <Grid size={{ xs: 12 }}>
                        <Box display='flex' justifyContent='space-between' alignItems='center' mb={2}>
                            <Typography variant='h6'>PINs</Typography>
                            <Button
                                variant='outlined'
                                size='small'
                                onClick={handleToggleShowAllPins}
                            >
                                {showAllPins ? 'Show Active Only' : 'Show All PINs'}
                            </Button>
                        </Box>

                        <Alert severity='warning' sx={{ mb: 2 }}>
                            <Typography variant='body2'>
                                <strong>Security Warning:</strong> PIN codes are displayed below and are highly confidential.
                                Only authorized administrators should have access to this information.
                            </Typography>
                        </Alert>

                        {renderPinTable()}

                        {pins && pins.length > 50 && (
                            <Typography variant='body2' color='text.secondary' sx={{ mt: 1 }}>
                                Showing first 50 PINs of {pins.length} total
                            </Typography>
                        )}
                    </Grid>
                </Grid>
            </DialogContent>
            <DialogActions>
                <Button onClick={onClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}
