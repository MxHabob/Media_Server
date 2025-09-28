import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import AddIcon from '@mui/icons-material/Add';
import DeleteIcon from '@mui/icons-material/Delete';
import VpnKeyIcon from '@mui/icons-material/VpnKey';
import GetAppIcon from '@mui/icons-material/GetApp';
import VisibilityIcon from '@mui/icons-material/Visibility';
import PlayArrowIcon from '@mui/icons-material/PlayArrow';
import PauseIcon from '@mui/icons-material/Pause';
import BlockIcon from '@mui/icons-material/Block';
import ClearAllIcon from '@mui/icons-material/ClearAll';
import { type MRT_ColumnDef, useMaterialReactTable } from 'material-react-table';
import React, { useCallback, useMemo, useState } from 'react';

import TablePage, { DEFAULT_TABLE_OPTIONS } from 'apps/dashboard/components/table/TablePage';
import { usePinBatches } from 'apps/dashboard/features/pins/api/usePinBatches';
import { useDeletePinBatch } from 'apps/dashboard/features/pins/api/useDeletePinBatch';
import { useActivatePinBatch } from 'apps/dashboard/features/pins/api/useActivatePinBatch';
import { useSuspendPinBatch } from 'apps/dashboard/features/pins/api/useSuspendPinBatch';
import { useExportPinBatch } from 'apps/dashboard/features/pins/api/useExportPinBatch';
import { useDeleteAllBatchPins } from 'apps/dashboard/features/pins/api/useDeleteAllBatchPins';
import { useDeactivateAllBatchPins } from 'apps/dashboard/features/pins/api/useDeactivateAllBatchPins';
import confirm from 'components/confirm/confirm';
import { useApi } from 'hooks/useApi';
import globalize from 'lib/globalize';
import CreatePinBatchDialog from '../pinbatches/CreatePinBatchDialog';
import EditPinBatchDialog from '../pinbatches/EditPinBatchDialog';
import PinBatchDetailsDialog from '../pinbatches/PinBatchDetailsDialog';

import type { PinBatch } from '../../types/pinBatch';

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
    const { data, isLoading } = usePinBatches(__legacyApiClient__!);
    const batches = useMemo(() => (
        data || []
    ), [data]);

    const deletePinBatch = useDeletePinBatch();
    const activatePinBatch = useActivatePinBatch();
    const suspendPinBatch = useSuspendPinBatch();
    const exportPinBatch = useExportPinBatch();
    const deleteAllBatchPins = useDeleteAllBatchPins();
    const deactivateAllBatchPins = useDeactivateAllBatchPins();

    const [createDialogOpen, setCreateDialogOpen] = useState(false);
    const [editDialogOpen, setEditDialogOpen] = useState(false);
    const [detailsDialogOpen, setDetailsDialogOpen] = useState(false);
    const [selectedBatch, setSelectedBatch] = useState<PinBatch | null>(null);

    const columns = useMemo<MRT_ColumnDef<PinBatch>[]>(() => [
        {
            id: 'Name',
            accessorKey: 'Name',
            header: 'Batch Name',
            size: 200
        },
        {
            id: 'Description',
            accessorKey: 'Description',
            header: 'Description',
            size: 250
        },
        {
            id: 'Status',
            accessorFn: item => getStatusText(item.Status),
            header: 'Status',
            size: 100
        },
        {
            id: 'SubscriptionType',
            accessorFn: item => getSubscriptionTypeText(item.SubscriptionType),
            header: 'Subscription Type',
            size: 150
        },
        {
            id: 'PinPattern',
            accessorFn: item => getPatternText(item.PinPattern),
            header: 'PIN Pattern',
            size: 150
        },
        {
            id: 'TotalPins',
            accessorKey: 'TotalPins',
            header: 'Total PINs',
            size: 100
        },
        {
            id: 'ActivePins',
            accessorKey: 'ActivePins',
            header: 'Active PINs',
            size: 100
        },
        {
            id: 'UsedPins',
            accessorKey: 'UsedPins',
            header: 'Used PINs',
            size: 100
        },
        {
            id: 'CreatedDate',
            accessorFn: item => item.CreatedDate ? new Date(item.CreatedDate).toLocaleString() : '',
            header: 'Created Date',
            size: 150
        },
        {
            id: 'ExpirationDate',
            accessorFn: item => item.ExpirationDate ? item.ExpirationDate : 'Lifetime',
            header: 'Expiration Date',
            filterVariant: 'datetime-range',
            size: 150
        }
    ], []);

    const table = useMaterialReactTable({
        ...DEFAULT_TABLE_OPTIONS,

        columns,
        data: batches,

        state: {
            isLoading
        },

        // Enable (delete) row actions
        enableRowActions: true,
        positionActionsColumn: 'last',
        displayColumnDefOptions: {
            'mrt-row-actions': {
                header: '',
                size: 250
            }
        },

        renderTopToolbarCustomActions: () => (
            <Button
                startIcon={<AddIcon />}
                onClick={handleCreateClick}
            >
                Create PIN Batch
            </Button>
        ),

        renderRowActions: ({ row }) => {
            const batch = row.original;
            return (
                <Box sx={{ display: 'flex' }}>
                    <Tooltip title='View Details'>
                        <IconButton
                            onClick={handleDetailsClickWrapper(batch)}
                        >
                            <VisibilityIcon />
                        </IconButton>
                    </Tooltip>

                    <Tooltip title='Edit'>
                        <IconButton
                            onClick={handleEditClickWrapper(batch)}
                        >
                            <VpnKeyIcon />
                        </IconButton>
                    </Tooltip>

                    {renderStatusActions(batch)}

                    <Tooltip title='Export'>
                        <IconButton
                            onClick={handleExportClickWrapper(batch.Id)}
                        >
                            <GetAppIcon />
                        </IconButton>
                    </Tooltip>

                    <Tooltip title='Deactivate All PINs'>
                        <IconButton
                            color='warning'
                            onClick={handleDeactivateAllClickWrapper(batch.Id, batch.Name)}
                        >
                            <BlockIcon />
                        </IconButton>
                    </Tooltip>

                    <Tooltip title='Delete All PINs'>
                        <IconButton
                            color='error'
                            onClick={handleDeleteAllClickWrapper(batch.Id, batch.Name)}
                        >
                            <ClearAllIcon />
                        </IconButton>
                    </Tooltip>

                    <Tooltip title='Delete Batch'>
                        <IconButton
                            color='error'
                            onClick={handleDeleteClickWrapper(batch.Id, batch.Name)}
                        >
                            <DeleteIcon />
                        </IconButton>
                    </Tooltip>
                </Box>
            );
        }
    });

    const onDeleteBatch = useCallback((batchId: string, batchName: string) => {
        if (!__legacyApiClient__) return;

        confirm(`Are you sure you want to delete batch "${batchName}"?`, 'Confirm Delete Batch').then(function () {
            deletePinBatch.mutate({
                apiClient: __legacyApiClient__,
                batchId: batchId
            });
        }).catch(err => {
            console.error('[pinbatches] failed to show confirmation dialog', err);
        });
    }, [__legacyApiClient__, deletePinBatch]);

    const onActivateBatch = useCallback((batchId: string) => {
        if (!__legacyApiClient__) return;

        activatePinBatch.mutate({
            apiClient: __legacyApiClient__,
            batchId: batchId
        });
    }, [__legacyApiClient__, activatePinBatch]);

    const onSuspendBatch = useCallback((batchId: string) => {
        if (!__legacyApiClient__) return;

        suspendPinBatch.mutate({
            apiClient: __legacyApiClient__,
            batchId: batchId
        });
    }, [__legacyApiClient__, suspendPinBatch]);

    const onExportBatch = useCallback((batchId: string) => {
        if (!__legacyApiClient__) return;

        // Show security warning before export
        const confirmed = window.confirm(
            'Security Warning: This export will include PIN codes which are highly confidential.\n\n'
            + 'Only authorized administrators should access this data.\n\n'
            + 'Do you want to proceed with the export?'
        );

        if (confirmed) {
            exportPinBatch.mutate({
                apiClient: __legacyApiClient__,
                batchId: batchId,
                includeOriginalPins: true
            });
        }
    }, [__legacyApiClient__, exportPinBatch]);

    const onDeactivateAllBatchPins = useCallback((batchId: string, batchName: string) => {
        if (!__legacyApiClient__) return;

        confirm(`Are you sure you want to deactivate all PINs in batch "${batchName}"?`, 'Confirm Deactivate All PINs').then(function () {
            deactivateAllBatchPins.mutate({
                apiClient: __legacyApiClient__,
                batchId: batchId
            });
        }).catch(err => {
            console.error('[pinbatches] failed to show confirmation dialog', err);
        });
    }, [__legacyApiClient__, deactivateAllBatchPins]);

    const onDeleteAllBatchPins = useCallback((batchId: string, batchName: string) => {
        if (!__legacyApiClient__) return;

        confirm(`Are you sure you want to delete all PINs in batch "${batchName}"? This action cannot be undone.`, 'Confirm Delete All PINs').then(function () {
            deleteAllBatchPins.mutate({
                apiClient: __legacyApiClient__,
                batchId: batchId
            });
        }).catch(err => {
            console.error('[pinbatches] failed to show confirmation dialog', err);
        });
    }, [__legacyApiClient__, deleteAllBatchPins]);

    const handleCreateDialogClose = useCallback(() => {
        setCreateDialogOpen(false);
    }, []);

    const handleEditDialogClose = useCallback(() => {
        setEditDialogOpen(false);
        setSelectedBatch(null);
    }, []);

    const handleDetailsDialogClose = useCallback(() => {
        setDetailsDialogOpen(false);
        setSelectedBatch(null);
    }, []);

    const handleCreateClick = useCallback(() => {
        setCreateDialogOpen(true);
    }, []);

    const handleDetailsClick = useCallback((batch: PinBatch) => {
        setSelectedBatch(batch);
        setDetailsDialogOpen(true);
    }, []);

    const handleEditClick = useCallback((batch: PinBatch) => {
        setSelectedBatch(batch);
        setEditDialogOpen(true);
    }, []);

    const handleSuspendClick = useCallback((batchId: string) => {
        onSuspendBatch(batchId);
    }, [onSuspendBatch]);

    const handleActivateClick = useCallback((batchId: string) => {
        onActivateBatch(batchId);
    }, [onActivateBatch]);

    const handleExportClick = useCallback((batchId: string) => {
        onExportBatch(batchId);
    }, [onExportBatch]);

    const handleDeactivateAllClick = useCallback((batchId: string, batchName: string) => {
        onDeactivateAllBatchPins(batchId, batchName);
    }, [onDeactivateAllBatchPins]);

    const handleDeleteAllClick = useCallback((batchId: string, batchName: string) => {
        onDeleteAllBatchPins(batchId, batchName);
    }, [onDeleteAllBatchPins]);

    const handleDeleteClick = useCallback((batchId: string, batchName: string) => {
        onDeleteBatch(batchId, batchName);
    }, [onDeleteBatch]);

    const handleSuspendClickWrapper = useCallback((batchId: string) => {
        return () => handleSuspendClick(batchId);
    }, [handleSuspendClick]);

    const handleActivateClickWrapper = useCallback((batchId: string) => {
        return () => handleActivateClick(batchId);
    }, [handleActivateClick]);

    const renderStatusActions = useCallback((batch: PinBatch) => {
        if (batch.Status === 0) {
            return (
                <Tooltip title='Suspend'>
                    <IconButton
                        onClick={handleSuspendClickWrapper(batch.Id)}
                    >
                        <PauseIcon />
                    </IconButton>
                </Tooltip>
            );
        }
        if (batch.Status === 1) {
            return (
                <Tooltip title='Activate'>
                    <IconButton
                        onClick={handleActivateClickWrapper(batch.Id)}
                    >
                        <PlayArrowIcon />
                    </IconButton>
                </Tooltip>
            );
        }
        return null;
    }, [handleSuspendClickWrapper, handleActivateClickWrapper]);

    const handleDetailsClickWrapper = useCallback((batch: PinBatch) => {
        return () => handleDetailsClick(batch);
    }, [handleDetailsClick]);

    const handleEditClickWrapper = useCallback((batch: PinBatch) => {
        return () => handleEditClick(batch);
    }, [handleEditClick]);

    const handleExportClickWrapper = useCallback((batchId: string) => {
        return () => handleExportClick(batchId);
    }, [handleExportClick]);

    const handleDeactivateAllClickWrapper = useCallback((batchId: string, batchName: string) => {
        return () => handleDeactivateAllClick(batchId, batchName);
    }, [handleDeactivateAllClick]);

    const handleDeleteAllClickWrapper = useCallback((batchId: string, batchName: string) => {
        return () => handleDeleteAllClick(batchId, batchName);
    }, [handleDeleteAllClick]);

    const handleDeleteClickWrapper = useCallback((batchId: string, batchName: string) => {
        return () => handleDeleteClick(batchId, batchName);
    }, [handleDeleteClick]);

    return (
        <TablePage
            id='pinBatchesPage'
            title={globalize.translate('HeaderPinBatches')}
            subtitle={globalize.translate('HeaderPinBatchesHelp')}
            className='mainAnimatedPage type-interior'
            table={table}
        >
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
        </TablePage>
    );
};

Component.displayName = 'PinBatchesPage';
