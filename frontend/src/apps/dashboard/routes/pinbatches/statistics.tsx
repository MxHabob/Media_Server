import React, { useCallback, useMemo, useState } from 'react';
import Page from 'components/Page';
import globalize from 'lib/globalize';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Grid from '@mui/material/Grid';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import CircularProgress from '@mui/material/CircularProgress';
import Alert from '@mui/material/Alert';
import Chip from '@mui/material/Chip';
import { useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import { useNavigate } from 'react-router-dom';
import RefreshIcon from '@mui/icons-material/Refresh';
import GetAppIcon from '@mui/icons-material/GetApp';

type PinBatch = {
    Id: string;
    Name: string;
    Description?: string;
    SubscriptionType: number;
    PinPattern: number;
    PinLength: number;
    CustomCharacterSet?: string;
    TotalPins: number;
    UsedPins: number;
    ActivePins: number;
    ExpiredPins: number;
    Status: number;
    CreatedDate: string;
    ExpirationDate?: string;
    ModifiedDate?: string;
    CreatedByUserId: string;
    ModifiedByUserId?: string;
    MaxConcurrentSessions?: number;
    AllowRemoteAccess: boolean;
    MaxBitrate?: number;
    AllowTranscoding: boolean;
    MaxParentalRating?: number;
    AllowDownload: boolean;
    AllowSyncPlay: boolean;
    Price?: number;
    Currency?: string;
    Metadata?: string;
};

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

const fetchPinBatches = async (apiClient: ApiClient, status?: number, subscriptionType?: number, createdByUserId?: string): Promise<PinBatch[]> => {
    const params = new URLSearchParams();
    if (status !== undefined) params.append('status', status.toString());
    if (subscriptionType !== undefined) params.append('subscriptionType', subscriptionType.toString());
    if (createdByUserId) params.append('createdByUserId', createdByUserId);

    const url = `/PinBatches${params.toString() ? `?${params.toString()}` : ''}`;
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl(url)
    });

    return res.json() as Promise<PinBatch[]>;
};

const fetchBatchStatistics = async (apiClient: ApiClient, batchId: string): Promise<BatchStatistics> => {
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Statistics`)
    });

    return res.json() as Promise<BatchStatistics>;
};

const exportMultipleBatches = async (apiClient: ApiClient, batchIds: string[], includeOriginalPins: boolean = false): Promise<void> => {
    const response = await fetch(apiClient.getUrl('/PinBatches/Export'), {
        method: 'POST',
        headers: {
            'Authorization': `MediaBrowser Token="${apiClient.accessToken()}"`,
            'Content-Type': 'application/json'
        },
        body: JSON.stringify({
            BatchIds: batchIds,
            IncludeOriginalPins: includeOriginalPins
        })
    });

    if (!response.ok) {
        throw new Error('Export failed');
    }

    const blob = await response.blob();
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = `PIN_Batches_${batchIds.length}_Batches_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
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

export const Component = () => {
    const { __legacyApiClient__ } = useApi();
    const navigate = useNavigate();
    const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
    const [subscriptionTypeFilter, setSubscriptionTypeFilter] = useState<number | undefined>(undefined);
    const [selectedBatches, setSelectedBatches] = useState<string[]>([]);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [successMessage, setSuccessMessage] = useState<string>('');

    const { data: batches, isLoading, refetch } = useQuery({
        queryKey: ['PinBatches', statusFilter, subscriptionTypeFilter],
        queryFn: () => fetchPinBatches(__legacyApiClient__!, statusFilter, subscriptionTypeFilter)
    });

    const handleErrorClose = useCallback(() => setErrorMessage(''), []);
    const handleSuccessClose = useCallback(() => setSuccessMessage(''), []);

    const handleStatusFilterChange = useCallback((value: string) => {
        setStatusFilter(value === '' ? undefined : Number(value));
    }, []);

    const handleSubscriptionTypeFilterChange = useCallback((value: string) => {
        setSubscriptionTypeFilter(value === '' ? undefined : Number(value));
    }, []);

    const handleBatchSelect = useCallback((batchId: string) => {
        setSelectedBatches(prev => 
            prev.includes(batchId) 
                ? prev.filter(id => id !== batchId)
                : [...prev, batchId]
        );
    }, []);

    const handleSelectAll = useCallback(() => {
        if (selectedBatches.length === batches?.length) {
            setSelectedBatches([]);
        } else {
            setSelectedBatches(batches?.map(b => b.Id) || []);
        }
    }, [selectedBatches.length, batches]);

    const handleExportSelected = useCallback(async () => {
        if (selectedBatches.length === 0) {
            setErrorMessage('Please select at least one batch to export');
            return;
        }

        try {
            const includeOriginalPins = window.confirm('Include original PINs in export? (This will show the actual PIN codes)');
            await exportMultipleBatches(__legacyApiClient__!, selectedBatches, includeOriginalPins);
            setSuccessMessage(`Exported ${selectedBatches.length} batches successfully`);
        } catch (error) {
            const message = error instanceof Error ? error.message : String(error);
            setErrorMessage(message || 'Export failed');
        }
    }, [selectedBatches, __legacyApiClient__]);

    const formatDate = useCallback((dateString: string) => {
        return new Date(dateString).toLocaleDateString();
    }, []);

    const formatDateTime = useCallback((dateString: string) => {
        return new Date(dateString).toLocaleString();
    }, []);

    // Calculate overall statistics
    const overallStats = useMemo(() => {
        if (!batches) return null;

        const totalPins = batches.reduce((sum, batch) => sum + batch.TotalPins, 0);
        const totalActivePins = batches.reduce((sum, batch) => sum + batch.ActivePins, 0);
        const totalUsedPins = batches.reduce((sum, batch) => sum + batch.UsedPins, 0);
        const totalExpiredPins = batches.reduce((sum, batch) => sum + batch.ExpiredPins, 0);
        const activeBatches = batches.filter(batch => batch.Status === 0).length;
        const totalBatches = batches.length;

        return {
            totalBatches,
            activeBatches,
            totalPins,
            totalActivePins,
            totalUsedPins,
            totalExpiredPins,
            usageRate: totalPins > 0 ? (totalUsedPins / totalPins) * 100 : 0
        };
    }, [batches]);

    return (
        <Page id='pinbatchstatistics' className='type-interior' title='PIN Batch Statistics'>
            <Box className='content-primary'>
                {errorMessage ? (
                    <Alert severity='error' onClose={handleErrorClose} sx={{ mb: 2 }}>
                        {errorMessage}
                    </Alert>
                ) : null}
                
                {successMessage ? (
                    <Alert severity='success' onClose={handleSuccessClose} sx={{ mb: 2 }}>
                        {successMessage}
                    </Alert>
                ) : null}

                <Stack direction='row' justifyContent='space-between' alignItems='center' mb={2}>
                    <Typography variant='h5'>PIN Batch Statistics</Typography>
                    <Stack direction='row' gap={2}>
                        <Button
                            variant='outlined'
                            startIcon={<RefreshIcon />}
                            onClick={() => void refetch()}
                            disabled={isLoading}
                        >
                            Refresh
                        </Button>
                        <Button
                            variant='contained'
                            startIcon={<GetAppIcon />}
                            onClick={handleExportSelected}
                            disabled={selectedBatches.length === 0}
                        >
                            Export Selected ({selectedBatches.length})
                        </Button>
                    </Stack>
                </Stack>

                <Stack direction='row' gap={2} mb={3}>
                    <TextField
                        label='Status Filter'
                        select
                        value={statusFilter ?? ''}
                        onChange={(e) => handleStatusFilterChange(e.target.value)}
                        size='small'
                        sx={{ minWidth: 150 }}
                    >
                        <MenuItem value=''>All Statuses</MenuItem>
                        <MenuItem value={0}>Active</MenuItem>
                        <MenuItem value={1}>Suspended</MenuItem>
                        <MenuItem value={2}>Expired</MenuItem>
                        <MenuItem value={3}>Deleted</MenuItem>
                    </TextField>
                    
                    <TextField
                        label='Subscription Type Filter'
                        select
                        value={subscriptionTypeFilter ?? ''}
                        onChange={(e) => handleSubscriptionTypeFilterChange(e.target.value)}
                        size='small'
                        sx={{ minWidth: 200 }}
                    >
                        <MenuItem value=''>All Types</MenuItem>
                        <MenuItem value={0}>None</MenuItem>
                        <MenuItem value={1}>6 Hours</MenuItem>
                        <MenuItem value={2}>12 Hours</MenuItem>
                        <MenuItem value={3}>Daily</MenuItem>
                        <MenuItem value={4}>Weekly</MenuItem>
                        <MenuItem value={5}>Monthly</MenuItem>
                        <MenuItem value={6}>Quarterly</MenuItem>
                        <MenuItem value={7}>Yearly</MenuItem>
                        <MenuItem value={8}>Lifetime</MenuItem>
                        <MenuItem value={9}>Custom</MenuItem>
                    </TextField>
                </Stack>

                {/* Overall Statistics */}
                {overallStats && (
                    <Grid container spacing={3} mb={3}>
                        <Grid item xs={12} sm={6} md={2}>
                            <Card>
                                <CardContent>
                                    <Typography variant='h4' color='primary'>{overallStats.totalBatches}</Typography>
                                    <Typography variant='body2' color='text.secondary'>Total Batches</Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                        <Grid item xs={12} sm={6} md={2}>
                            <Card>
                                <CardContent>
                                    <Typography variant='h4' color='success.main'>{overallStats.activeBatches}</Typography>
                                    <Typography variant='body2' color='text.secondary'>Active Batches</Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                        <Grid item xs={12} sm={6} md={2}>
                            <Card>
                                <CardContent>
                                    <Typography variant='h4' color='info.main'>{overallStats.totalPins}</Typography>
                                    <Typography variant='body2' color='text.secondary'>Total PINs</Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                        <Grid item xs={12} sm={6} md={2}>
                            <Card>
                                <CardContent>
                                    <Typography variant='h4' color='success.main'>{overallStats.totalActivePins}</Typography>
                                    <Typography variant='body2' color='text.secondary'>Active PINs</Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                        <Grid item xs={12} sm={6} md={2}>
                            <Card>
                                <CardContent>
                                    <Typography variant='h4' color='warning.main'>{overallStats.totalUsedPins}</Typography>
                                    <Typography variant='body2' color='text.secondary'>Used PINs</Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                        <Grid item xs={12} sm={6} md={2}>
                            <Card>
                                <CardContent>
                                    <Typography variant='h4' color='error.main'>{overallStats.totalExpiredPins}</Typography>
                                    <Typography variant='body2' color='text.secondary'>Expired PINs</Typography>
                                </CardContent>
                            </Card>
                        </Grid>
                    </Grid>
                )}

                {isLoading ? (
                    <Box display='flex' justifyContent='center' p={4}>
                        <CircularProgress />
                    </Box>
                ) : (
                    <Grid container spacing={2}>
                        {batches?.map((batch) => (
                            <Grid item xs={12} sm={6} md={4} key={batch.Id}>
                                <Card 
                                    sx={{ 
                                        cursor: 'pointer',
                                        border: selectedBatches.includes(batch.Id) ? '2px solid' : '1px solid',
                                        borderColor: selectedBatches.includes(batch.Id) ? 'primary.main' : 'divider'
                                    }}
                                    onClick={() => handleBatchSelect(batch.Id)}
                                >
                                    <CardContent>
                                        <Box display='flex' justifyContent='space-between' alignItems='flex-start' mb={2}>
                                            <Typography variant='h6' noWrap sx={{ flexGrow: 1, mr: 1 }}>
                                                {batch.Name}
                                            </Typography>
                                            <Chip
                                                label={getStatusText(batch.Status)}
                                                color={getStatusColor(batch.Status)}
                                                size='small'
                                            />
                                        </Box>
                                        
                                        <Typography variant='body2' color='text.secondary' mb={2}>
                                            {batch.Description || 'No description'}
                                        </Typography>

                                        <Grid container spacing={1} mb={2}>
                                            <Grid item xs={6}>
                                                <Typography variant='body2' color='text.secondary'>Subscription</Typography>
                                                <Typography variant='body2'>{getSubscriptionTypeText(batch.SubscriptionType)}</Typography>
                                            </Grid>
                                            <Grid item xs={6}>
                                                <Typography variant='body2' color='text.secondary'>Created</Typography>
                                                <Typography variant='body2'>{formatDate(batch.CreatedDate)}</Typography>
                                            </Grid>
                                        </Grid>

                                        <Grid container spacing={1}>
                                            <Grid item xs={3}>
                                                <Typography variant='h6' color='primary'>{batch.TotalPins}</Typography>
                                                <Typography variant='caption' color='text.secondary'>Total</Typography>
                                            </Grid>
                                            <Grid item xs={3}>
                                                <Typography variant='h6' color='success.main'>{batch.ActivePins}</Typography>
                                                <Typography variant='caption' color='text.secondary'>Active</Typography>
                                            </Grid>
                                            <Grid item xs={3}>
                                                <Typography variant='h6' color='info.main'>{batch.UsedPins}</Typography>
                                                <Typography variant='caption' color='text.secondary'>Used</Typography>
                                            </Grid>
                                            <Grid item xs={3}>
                                                <Typography variant='h6' color='error.main'>{batch.ExpiredPins}</Typography>
                                                <Typography variant='caption' color='text.secondary'>Expired</Typography>
                                            </Grid>
                                        </Grid>

                                        {batch.Price && (
                                            <Box mt={1}>
                                                <Typography variant='body2' color='text.secondary'>
                                                    Price: {batch.Currency} {batch.Price}
                                                </Typography>
                                            </Box>
                                        )}
                                    </CardContent>
                                </Card>
                            </Grid>
                        ))}
                    </Grid>
                )}

                {batches?.length === 0 && !isLoading && (
                    <Box textAlign='center' p={4}>
                        <Typography variant='h6' color='text.secondary'>
                            No PIN batches found
                        </Typography>
                        <Typography variant='body2' color='text.secondary' sx={{ mt: 1 }}>
                            Create your first PIN batch to get started
                        </Typography>
                        <Button
                            variant='contained'
                            sx={{ mt: 2 }}
                            onClick={() => navigate('/dashboard/pinbatches')}
                        >
                            Create Batch
                        </Button>
                    </Box>
                )}
            </Box>
        </Page>
    );
};
