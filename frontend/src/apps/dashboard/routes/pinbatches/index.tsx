import React, { useCallback, useState } from 'react';
import Page from 'components/Page';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import Table from '@mui/material/Table';
import TableBody from '@mui/material/TableBody';
import TableCell from '@mui/material/TableCell';
import TableHead from '@mui/material/TableHead';
import TableRow from '@mui/material/TableRow';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import CircularProgress from '@mui/material/CircularProgress';
import Alert from '@mui/material/Alert';
import Chip from '@mui/material/Chip';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import AddIcon from '@mui/icons-material/Add';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import GetAppIcon from '@mui/icons-material/GetApp';
import VisibilityIcon from '@mui/icons-material/Visibility';
import CreatePinBatchDialog from './CreatePinBatchDialog';
import EditPinBatchDialog from './EditPinBatchDialog';
import PinBatchDetailsDialog from './PinBatchDetailsDialog';
import useLivePinUpdates from '../../features/pins/hooks/useLivePinUpdates';

import type { PinBatch } from '../../types/pinBatch';
import { PinBatchUtils } from '../../types/pinBatch';

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

const deletePinBatch = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'DELETE',
        url: apiClient.getUrl(`/PinBatches/${batchId}`)
    });
};

const activatePinBatch = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Activate`)
    });
};

const suspendPinBatch = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Suspend`)
    });
};

const exportPinBatch = async (apiClient: ApiClient, batchId: string, includeOriginalPins: boolean = false): Promise<void> => {
    const params = new URLSearchParams();
    if (includeOriginalPins) params.append('includeOriginalPins', 'true');

    const url = `/PinBatches/${batchId}/Export${params.toString() ? `?${params.toString()}` : ''}`;

    const response = await fetch(apiClient.getUrl(url), {
        method: 'GET',
        headers: {
            'Authorization': `MediaBrowser Token="${apiClient.accessToken()}"`
        }
    });

    if (!response.ok) {
        throw new Error('Export failed');
    }

    const blob = await response.blob();
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = `PIN_Batch_${batchId}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
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

export const Component = () => {
    const { __legacyApiClient__ } = useApi();
    const [statusFilter, setStatusFilter] = useState<number | undefined>(undefined);
    const [subscriptionTypeFilter, setSubscriptionTypeFilter] = useState<number | undefined>(undefined);
    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [editDialogOpen, setEditDialogOpen] = useState(false);
    const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);
    const [selectedBatch, setSelectedBatch] = useState<PinBatch | null>(null);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [successMessage, setSuccessMessage] = useState<string>('');

    // Enable real-time PIN updates
    useLivePinUpdates();

    const { data: batches, isLoading, refetch } = useQuery({
        queryKey: ['PinBatches', statusFilter, subscriptionTypeFilter],
        queryFn: () => fetchPinBatches(__legacyApiClient__!, statusFilter, subscriptionTypeFilter)
    });

    const deleteMutation = useMutation({
        mutationFn: (batchId: string) => deletePinBatch(__legacyApiClient__!, batchId),
        onSuccess: () => {
            setSuccessMessage('Batch deleted successfully');
            void refetch();
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'An unexpected error occurred');
        }
    });

    const activateMutation = useMutation({
        mutationFn: (batchId: string) => activatePinBatch(__legacyApiClient__!, batchId),
        onSuccess: () => {
            setSuccessMessage('Batch activated successfully');
            void refetch();
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'An unexpected error occurred');
        }
    });

    const suspendMutation = useMutation({
        mutationFn: (batchId: string) => suspendPinBatch(__legacyApiClient__!, batchId),
        onSuccess: () => {
            setSuccessMessage('Batch suspended successfully');
            void refetch();
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'An unexpected error occurred');
        }
    });

    const exportMutation = useMutation({
        mutationFn: ({ batchId, includeOriginalPins }: { batchId: string; includeOriginalPins: boolean }) =>
            exportPinBatch(__legacyApiClient__!, batchId, includeOriginalPins),
        onSuccess: () => {
            setSuccessMessage('Batch exported successfully');
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'Export failed');
        }
    });

    const handleErrorClose = useCallback(() => setErrorMessage(''), []);
    const handleSuccessClose = useCallback(() => setSuccessMessage(''), []);

    const handleCreateDialogClose = useCallback(() => {
        setCreateDialogOpen(false);
        void refetch();
    }, [refetch]);

    const handleEditDialogClose = useCallback(() => {
        setEditDialogOpen(false);
        setSelectedBatch(null);
        void refetch();
    }, [refetch]);

    const handleDetailsDialogClose = useCallback(() => {
        setDetailsDialogOpen(false);
        setSelectedBatch(null);
    }, []);

    const handleEdit = useCallback((batch: PinBatch) => {
        setSelectedBatch(batch);
        setEditDialogOpen(true);
    }, []);

    const handleDetails = useCallback((batch: PinBatch) => {
        setSelectedBatch(batch);
        setDetailsDialogOpen(true);
    }, []);

    const handleDelete = useCallback((batch: PinBatch) => {
        if (window.confirm(`Are you sure you want to delete batch "${batch.Name}"?`)) {
            deleteMutation.mutate(batch.Id);
        }
    }, [deleteMutation]);

    const handleActivate = useCallback((batch: PinBatch) => {
        activateMutation.mutate(batch.Id);
    }, [activateMutation]);

    const handleSuspend = useCallback((batch: PinBatch) => {
        suspendMutation.mutate(batch.Id);
    }, [suspendMutation]);

    const handleExport = useCallback((batch: PinBatch) => {
        const includeOriginalPins = window.confirm('Include original PINs in export? (This will show the actual PIN codes)');
        exportMutation.mutate({ batchId: batch.Id, includeOriginalPins });
    }, [exportMutation]);

    const handleStatusFilterChange = useCallback((value: string) => {
        setStatusFilter(value === '' ? undefined : Number(value));
    }, []);

    const handleSubscriptionTypeFilterChange = useCallback((value: string) => {
        setSubscriptionTypeFilter(value === '' ? undefined : Number(value));
    }, []);

    const formatDate = useCallback((dateString: string) => {
        return new Date(dateString).toLocaleDateString();
    }, []);

    const formatDateTime = useCallback((dateString: string) => {
        return new Date(dateString).toLocaleString();
    }, []);

    return (
        <Page id='pinbatches' className='type-interior' title='PIN Batch Management'>
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
                    <Typography variant='h5'>PIN Batch Management</Typography>
                    <Button
                        variant='contained'
                        startIcon={<AddIcon />}
                        onClick={() => setCreateDialogOpen(true)}
                    >
                        Create Batch
                    </Button>
                </Stack>

                <Stack direction='row' gap={2} mb={2}>
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

                {isLoading ? (
                    <Box display='flex' justifyContent='center' p={4}>
                        <CircularProgress />
                    </Box>
                ) : (
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>Name</TableCell>
                                <TableCell>Description</TableCell>
                                <TableCell>Status</TableCell>
                                <TableCell>Subscription Type</TableCell>
                                <TableCell>PIN Pattern</TableCell>
                                <TableCell>Total PINs</TableCell>
                                <TableCell>Active PINs</TableCell>
                                <TableCell>Used PINs</TableCell>
                                <TableCell>Created Date</TableCell>
                                <TableCell>Expiration Date</TableCell>
                                <TableCell>Time Remaining</TableCell>
                                <TableCell align='center'>Actions</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {batches?.map((batch) => (
                                <TableRow key={batch.Id}>
                                    <TableCell>{batch.Name}</TableCell>
                                    <TableCell>{batch.Description || 'N/A'}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={getStatusText(batch.Status ?? 0)}
                                            color={getStatusColor(batch.Status ?? 0)}
                                            size='small'
                                        />
                                    </TableCell>
                                    <TableCell>{getSubscriptionTypeText(batch.SubscriptionType ?? 0)}</TableCell>
                                    <TableCell>{getPatternText(batch.PinPattern ?? 0)}</TableCell>
                                    <TableCell>{batch.TotalPins}</TableCell>
                                    <TableCell>{batch.ActivePins}</TableCell>
                                    <TableCell>{batch.UsedPins}</TableCell>
                                    <TableCell>{formatDateTime(batch.CreatedDate)}</TableCell>
                                    <TableCell>{batch.ExpirationDate ? formatDate(batch.ExpirationDate) : 'Lifetime'}</TableCell>
                                    <TableCell>
                                        <Chip
                                            label={PinBatchUtils.getTimeRemaining(batch.ExpirationDate)}
                                            color={PinBatchUtils.getStatusColor(batch.ExpirationDate)}
                                            size='small'
                                        />
                                    </TableCell>
                                    <TableCell align='center'>
                                        <Stack direction='row' spacing={1} justifyContent='center'>
                                            <Tooltip title='View Details'>
                                                <IconButton
                                                    size='small'
                                                    onClick={() => handleDetails(batch)}
                                                >
                                                    <VisibilityIcon />
                                                </IconButton>
                                            </Tooltip>

                                            <Tooltip title='Edit'>
                                                <IconButton
                                                    size='small'
                                                    onClick={() => handleEdit(batch)}
                                                >
                                                    <EditIcon />
                                                </IconButton>
                                            </Tooltip>
                                            
                                            {batch.Status === 0 ? (
                                                <Tooltip title='Suspend'>
                                                    <IconButton
                                                        size='small'
                                                        onClick={() => handleSuspend(batch)}
                                                        disabled={suspendMutation.isPending}
                                                    >
                                                        <PauseIcon />
                                                    </IconButton>
                                                </Tooltip>
                                            ) : batch.Status === 1 ? (
                                                <Tooltip title='Activate'>
                                                    <IconButton
                                                        size='small'
                                                        onClick={() => handleActivate(batch)}
                                                        disabled={activateMutation.isPending}
                                                    >
                                                        <PlayArrowIcon />
                                                    </IconButton>
                                                </Tooltip>
                                            ) : null}

                                            <Tooltip title='Export'>
                                                <IconButton
                                                    size='small'
                                                    onClick={() => handleExport(batch)}
                                                    disabled={exportMutation.isPending}
                                                >
                                                    <GetAppIcon />
                                                </IconButton>
                                            </Tooltip>

                                            <Tooltip title='Delete'>
                                                <IconButton
                                                    size='small'
                                                    onClick={() => handleDelete(batch)}
                                                    disabled={deleteMutation.isPending}
                                                    color='error'
                                                >
                                                    <DeleteIcon />
                                                </IconButton>
                                            </Tooltip>
                                        </Stack>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                )}

                {batches?.length === 0 && !isLoading && (
                    <Box textAlign='center' p={4}>
                        <Typography variant='h6' color='text.secondary'>
                            No PIN batches found
                        </Typography>
                        <Typography variant='body2' color='text.secondary' sx={{ mt: 1 }}>
                            Create your first PIN batch to get started
                        </Typography>
                    </Box>
                )}
            </Box>

            <CreatePinBatchDialog
                open={createDialogOpen}
                onClose={handleCreateDialogClose}
            />

            {selectedBatch && (
                <EditPinBatchDialog
                    open={editDialogOpen}
                    onClose={handleEditDialogClose}
                    batch={selectedBatch}
                />
            )}

            {selectedBatch && (
                <PinBatchDetailsDialog
                    open={detailsDialogOpen}
                    onClose={handleDetailsDialogClose}
                    batch={selectedBatch}
                />
            )}
        </Page>
    );
};
