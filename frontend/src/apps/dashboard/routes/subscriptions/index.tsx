import React, { useCallback, useState, useMemo } from 'react';
import Page from 'components/Page';
import globalize from 'lib/globalize';
import Box from '@mui/material/Box';
import Button from '@mui/material/Button';
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import TextField from '@mui/material/TextField';
import Dialog from '@mui/material/Dialog';
import DialogActions from '@mui/material/DialogActions';
import DialogContent from '@mui/material/DialogContent';
import DialogTitle from '@mui/material/DialogTitle';
import CircularProgress from '@mui/material/CircularProgress';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import FormLabel from '@mui/material/FormLabel';
import FormGroup from '@mui/material/FormGroup';
import FormControlLabel from '@mui/material/FormControlLabel';
import Checkbox from '@mui/material/Checkbox';
import Grid from '@mui/material/Grid';
import Alert from '@mui/material/Alert';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import { useMaterialReactTable, type MRT_ColumnDef } from 'material-react-table';
import TablePage, { DEFAULT_TABLE_OPTIONS } from 'apps/dashboard/components/table/TablePage';
import AddIcon from '@mui/icons-material/Add';
import VpnKeyIcon from '@mui/icons-material/VpnKey';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';

import type { SubscriptionConfigurationDto } from '../../types/pinBatch';
import useLivePinUpdates from '../../features/pins/hooks/useLivePinUpdates';

const fetchConfigs = async (apiClient: ApiClient): Promise<SubscriptionConfigurationDto[]> => {
    const res = await fetch(apiClient.getUrl('/Subscriptions/Configurations'), {
        headers: apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : undefined
    });
    if (!res.ok) throw new Error('Failed to fetch subscription configurations');
    return res.json();
};

const upsertConfig = async (apiClient: ApiClient, payload: SubscriptionConfigurationDto) => {
    const method = payload.Id ? 'PUT' : 'POST';
    const url = payload.Id ? apiClient.getUrl(`/Subscriptions/Configurations/${payload.Id}`) : apiClient.getUrl('/Subscriptions/Configurations');
    const res = await fetch(url, {
        method,
        headers: {
            'Content-Type': 'application/json',
            ...(apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : {})
        },
        body: JSON.stringify(payload)
    });
    if (!res.ok) throw new Error('Failed to save configuration');
};

const deleteConfig = async (apiClient: ApiClient, id: string) => {
    const res = await fetch(apiClient.getUrl(`/Subscriptions/Configurations/${id}`), {
        method: 'DELETE',
        headers: apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : undefined
    });
    if (!res.ok) throw new Error('Failed to delete configuration');
};

const createPinBatchFromSubscription = async (apiClient: ApiClient, configurationId: string, request: Record<string, unknown>) => {
    const res = await fetch(apiClient.getUrl(`/Subscriptions/Configurations/${configurationId}/CreatePinBatch`), {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...(apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : {})
        },
        body: JSON.stringify(request)
    });
    if (!res.ok) throw new Error('Failed to create PIN batch');
    return res.json();
};

export const Component = () => {
    const { __legacyApiClient__ } = useApi();
    const qc = useQueryClient();
    
    // Enable real-time PIN updates
    useLivePinUpdates();

    const { data, isLoading } = useQuery({
        queryKey: ['Subscriptions', 'Configurations'],
        queryFn: () => fetchConfigs(__legacyApiClient__!)
    });

    const subscriptions = useMemo(() => (data || []), [data]);

    const [dialogOpen, setDialogOpen] = useState(false);
    const [pinBatchDialogOpen, setPinBatchDialogOpen] = useState(false);
    const [draft, setDraft] = useState<SubscriptionConfigurationDto>({ 
        Name: '',
        SubscriptionType: 3,
        CustomDurationHours: 24,
        MaxConcurrentSessions: 1,
        AllowRemoteAccess: false,
        AllowTranscoding: true,
        AllowDownload: false,
        AllowSyncPlay: false,
        IsActive: true
    });
    const [selectedConfig, setSelectedConfig] = useState<SubscriptionConfigurationDto | null>(null);
    const [pinBatchForm, setPinBatchForm] = useState({
        BatchName: '',
        BatchDescription: '',
        PinCount: 10,
        PinPattern: 0,
        PinLength: 6,
        CustomCharacterSet: '',
        ExpirationDate: '',
        MaxConcurrentSessions: undefined,
        AllowRemoteAccess: true,
        AllowTranscoding: true,
        AllowDownload: false,
        AllowSyncPlay: true,
        MaxBitrate: undefined,
        MaxParentalRating: undefined,
        Price: undefined,
        Currency: 'USD',
        Metadata: ''
    });
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [successMessage, setSuccessMessage] = useState<string>('');

    const columns = useMemo<MRT_ColumnDef<SubscriptionConfigurationDto>[]>(() => [
        {
            id: 'Name',
            accessorKey: 'Name',
            header: globalize.translate('HeaderName'),
            size: 200
        },
        {
            id: 'CustomDurationHours',
            accessorKey: 'CustomDurationHours',
            header: globalize.translate('HeaderDuration'),
            Cell: ({ cell }) => {
                const value = cell.getValue<number>();
                return value ? `${value} hours` : '0 hours';
            }
        },
        {
            id: 'IsActive',
            accessorKey: 'IsActive',
            header: globalize.translate('HeaderStatus'),
            Cell: ({ cell }) => cell.getValue<boolean>() ? 'Active' : 'Inactive'
        }
    ], []);

    const saveMutation = useMutation({
        mutationFn: (payload: SubscriptionConfigurationDto) => upsertConfig(__legacyApiClient__!, payload),
        onSuccess: () => {
            void qc.invalidateQueries({ queryKey: ['Subscriptions', 'Configurations'] });
            setDialogOpen(false);
            setDraft({ 
                Name: '', 
                SubscriptionType: 3, // Daily
                CustomDurationHours: 24, 
                MaxConcurrentSessions: 1,
                AllowRemoteAccess: false,
                AllowTranscoding: true,
                AllowDownload: false,
                AllowSyncPlay: false,
                IsActive: true 
            });
        }
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => deleteConfig(__legacyApiClient__!, id),
        onSuccess: () => void qc.invalidateQueries({ queryKey: ['Subscriptions', 'Configurations'] })
    });

    const pinBatchMutation = useMutation({
        mutationFn: ({ configId, request }: { configId: string; request: Record<string, unknown> }) =>
            createPinBatchFromSubscription(__legacyApiClient__!, configId, request),
        onSuccess: () => {
            setSuccessMessage('PIN batch created successfully');
            setPinBatchDialogOpen(false);
            setSelectedConfig(null);
            setPinBatchForm({
                BatchName: '',
                BatchDescription: '',
                PinCount: 10,
                PinPattern: 0,
                PinLength: 6,
                CustomCharacterSet: '',
                ExpirationDate: '',
                MaxConcurrentSessions: undefined,
                AllowRemoteAccess: true,
                AllowTranscoding: true,
                AllowDownload: false,
                AllowSyncPlay: true,
                MaxBitrate: undefined,
                MaxParentalRating: undefined,
                Price: undefined,
                Currency: 'USD',
                Metadata: ''
            });
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'Failed to create PIN batch');
        }
    });

    const onNew = useCallback(() => {
        setDraft({ 
            Name: '', 
            SubscriptionType: 3, // Daily
            CustomDurationHours: 24, 
            MaxConcurrentSessions: 1,
            AllowRemoteAccess: false,
            AllowTranscoding: true,
            AllowDownload: false,
            AllowSyncPlay: false,
            IsActive: true 
        });
        setDialogOpen(true);
    }, []);

    const onEdit = useCallback((c: SubscriptionConfigurationDto) => () => {
        setDraft(c);
        setDialogOpen(true);
    }, []);

    const onDelete = useCallback((id?: string) => () => {
        if (id && window.confirm('Are you sure you want to delete this subscription configuration?')) {
            deleteMutation.mutate(id);
        }
    }, [deleteMutation]);

    const onClose = useCallback(() => setDialogOpen(false), []);
    const onSave = useCallback(() => {
        saveMutation.mutate(draft, { onSettled: () => setDialogOpen(false) });
    }, [draft, saveMutation]);

    const onCreatePinBatch = useCallback((config: SubscriptionConfigurationDto) => () => {
        setSelectedConfig(config);
        setPinBatchForm(prev => ({
            ...prev,
            BatchName: `${config.Name} PIN Batch`,
            BatchDescription: `PIN batch for ${config.Name} subscription (${config.CustomDurationHours || 24} hours)`
        }));
        setPinBatchDialogOpen(true);
    }, []);

    const onPinBatchClose = useCallback(() => {
        setPinBatchDialogOpen(false);
        setSelectedConfig(null);
        setErrorMessage('');
    }, []);

    const onPinBatchSave = useCallback(() => {
        if (!selectedConfig?.Id) return;

        if (!pinBatchForm.BatchName.trim()) {
            setErrorMessage('Batch name is required');
            return;
        }

        if (pinBatchForm.PinCount <= 0) {
            setErrorMessage('PIN count must be greater than 0');
            return;
        }

        if (pinBatchForm.PinLength < 4 || pinBatchForm.PinLength > 20) {
            setErrorMessage('PIN length must be between 4 and 20 characters');
            return;
        }

        if (pinBatchForm.PinPattern === 3 && !pinBatchForm.CustomCharacterSet?.trim()) {
            setErrorMessage('Custom character set is required for custom PIN pattern');
            return;
        }

        const request = {
            ...pinBatchForm,
            ExpirationDate: pinBatchForm.ExpirationDate || undefined,
            MaxConcurrentSessions: pinBatchForm.MaxConcurrentSessions || undefined,
            MaxBitrate: pinBatchForm.MaxBitrate || undefined,
            MaxParentalRating: pinBatchForm.MaxParentalRating || undefined,
            Price: pinBatchForm.Price || undefined,
            Currency: pinBatchForm.Currency || undefined,
            Metadata: pinBatchForm.Metadata || undefined
        };

        pinBatchMutation.mutate({ configId: selectedConfig.Id, request });
    }, [selectedConfig, pinBatchForm, pinBatchMutation]);

    const handleInputChange = useCallback((field: string) =>
        (event: React.ChangeEvent<HTMLInputElement>) => {
            const value = event.target.type === 'checkbox'
                ? event.target.checked
                : event.target.value;

            setPinBatchForm(prev => ({
                ...prev,
                [field]: value
            }));
        }, []);

    const handleNumberInputChange = useCallback((field: string) =>
        (event: React.ChangeEvent<HTMLInputElement>) => {
            const value = event.target.value === '' ? undefined : Number(event.target.value);
            setPinBatchForm(prev => ({
                ...prev,
                [field]: value
            }));
        }, []);

    const handleErrorClose = useCallback(() => setErrorMessage(''), []);
    const handleSuccessClose = useCallback(() => setSuccessMessage(''), []);

    const table = useMaterialReactTable({
        ...DEFAULT_TABLE_OPTIONS,
        columns,
        data: subscriptions,
        state: {
            isLoading
        },
        enableRowActions: true,
        positionActionsColumn: 'last',
        displayColumnDefOptions: {
            'mrt-row-actions': {
                header: '',
                size: 100
            }
        },
        renderTopToolbarCustomActions: () => (
            <Button
                startIcon={<AddIcon />}
                onClick={onNew}
            >
                {globalize.translate('ButtonNew')}
            </Button>
        ),
        renderRowActions: ({ row }) => {
            return (
                <Box sx={{ display: 'flex' }}>
                    <Tooltip title={globalize.translate('Create')}>
                        <IconButton
                            color='primary'
                            onClick={() => onCreatePinBatch(row.original)}
                        >
                            <VpnKeyIcon />
                        </IconButton>
                    </Tooltip>
                    <Tooltip title={globalize.translate('Edit')}>
                        <IconButton
                            color='primary'
                            onClick={() => onEdit(row.original)}
                        >
                            <EditIcon />
                        </IconButton>
                    </Tooltip>
                    <Tooltip title={globalize.translate('Delete')}>
                        <IconButton
                            color='error'
                            onClick={() => onDelete(row.original.Id!)}
                        >
                            <DeleteIcon />
                        </IconButton>
                    </Tooltip>
                </Box>
            );
        }
    });

    return (
        <Page id='subscriptions' className='type-interior' title='Subscriptions'>
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

                <TablePage
                    id='subscriptionsPage'
                    title={globalize.translate('HeaderSubscriptions')}
                    subtitle={globalize.translate('HeaderSubscriptionsHelp')}
                    className='mainAnimatedPage type-interior'
                    table={table}
                />

            {/* Subscription Configuration Dialog */}
            <Dialog open={dialogOpen} onClose={onClose} maxWidth='sm' fullWidth>
                <DialogTitle>
                    {draft.Id ? 'Edit Subscription Configuration' : 'New Subscription Configuration'}
                </DialogTitle>
                <DialogContent>
                    <Grid container spacing={2} sx={{ mt: 1 }}>
                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Duration (Hours)'
                                type='number'
                                value={draft.CustomDurationHours || 24}
                                onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, CustomDurationHours: Number(e.target.value) }))}
                                required
                                inputProps={{ min: 1 }}
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Subscription Name'
                                value={draft.Name}
                                onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, Name: e.target.value }))}
                                required
                                helperText='Enter a descriptive name for your subscription (e.g., "Silver 4 Hours", "Golden 9 Hours", "Weekly Package", "Lifetime Package")'
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Max Concurrent Sessions'
                                type='number'
                                value={draft.MaxConcurrentSessions}
                                onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, MaxConcurrentSessions: Number(e.target.value) }))}
                                required
                                inputProps={{ min: 1 }}
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={draft.AllowRemoteAccess}
                                        onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, AllowRemoteAccess: e.target.checked }))}
                                    />
                                }
                                label='Allow Remote Access'
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={draft.AllowTranscoding}
                                        onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, AllowTranscoding: e.target.checked }))}
                                    />
                                }
                                label='Allow Transcoding'
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={draft.AllowDownload}
                                        onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, AllowDownload: e.target.checked }))}
                                    />
                                }
                                label='Allow Download'
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={draft.AllowSyncPlay}
                                        onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, AllowSyncPlay: e.target.checked }))}
                                    />
                                }
                                label='Allow Sync Play'
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={draft.IsActive}
                                        onChange={(e) => setDraft((prev: SubscriptionConfigurationDto) => ({ ...prev, IsActive: e.target.checked }))}
                                    />
                                }
                                label='Active'
                            />
                        </Grid>
                    </Grid>
                </DialogContent>
                <DialogActions>
                    <Button onClick={onClose}>Cancel</Button>
                    <Button
                        onClick={onSave}
                        variant='contained'
                        disabled={saveMutation.isPending}
                        startIcon={saveMutation.isPending ? <CircularProgress size={20} /> : undefined}
                    >
                        {saveMutation.isPending ? 'Saving...' : 'Save'}
                    </Button>
                </DialogActions>
            </Dialog>

            {/* PIN Batch Creation Dialog */}
            <Dialog open={pinBatchDialogOpen} onClose={onPinBatchClose} maxWidth='md' fullWidth>
                <DialogTitle>
                    Create PIN Batch for {selectedConfig?.Name}
                </DialogTitle>
                <DialogContent>
                    <Grid container spacing={2} sx={{ mt: 1 }}>
                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Batch Name'
                                value={pinBatchForm.BatchName}
                                onChange={handleInputChange('BatchName')}
                                required
                                disabled={pinBatchMutation.isPending}
                            />
                        </Grid>
                        
                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='PIN Count'
                                type='number'
                                value={pinBatchForm.PinCount}
                                onChange={handleNumberInputChange('PinCount')}
                                required
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ min: 1, max: 10000 }}
                            />
                        </Grid>

                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Description'
                                value={pinBatchForm.BatchDescription}
                                onChange={handleInputChange('BatchDescription')}
                                multiline
                                rows={2}
                                disabled={pinBatchMutation.isPending}
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='PIN Pattern'
                                select
                                value={pinBatchForm.PinPattern}
                                onChange={handleInputChange('PinPattern')}
                                disabled={pinBatchMutation.isPending}
                            >
                                <MenuItem value={0}>Numeric (0-9)</MenuItem>
                                <MenuItem value={1}>Alphanumeric (0-9, A-Z)</MenuItem>
                                <MenuItem value={2}>Alphanumeric Mixed (0-9, A-Z, a-z)</MenuItem>
                                <MenuItem value={3}>Custom</MenuItem>
                            </TextField>
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='PIN Length'
                                type='number'
                                value={pinBatchForm.PinLength}
                                onChange={handleNumberInputChange('PinLength')}
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ min: 4, max: 20 }}
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Expiration Date'
                                type='datetime-local'
                                value={pinBatchForm.ExpirationDate}
                                onChange={handleInputChange('ExpirationDate')}
                                disabled={pinBatchMutation.isPending}
                                InputLabelProps={{ shrink: true }}
                            />
                        </Grid>

                        {pinBatchForm.PinPattern === 3 && (
                            <Grid item xs={12}>
                                <TextField
                                    fullWidth
                                    label='Custom Character Set'
                                    value={pinBatchForm.CustomCharacterSet}
                                    onChange={handleInputChange('CustomCharacterSet')}
                                    disabled={pinBatchMutation.isPending}
                                    helperText='Enter the characters that can be used in PINs (e.g., ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789)'
                                />
                            </Grid>
                        )}

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Max Concurrent Sessions'
                                type='number'
                                value={pinBatchForm.MaxConcurrentSessions || ''}
                                onChange={handleNumberInputChange('MaxConcurrentSessions')}
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ min: 1, max: 100 }}
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Max Bitrate (kbps)'
                                type='number'
                                value={pinBatchForm.MaxBitrate || ''}
                                onChange={handleNumberInputChange('MaxBitrate')}
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ min: 1, max: 100000000 }}
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Max Parental Rating'
                                type='number'
                                value={pinBatchForm.MaxParentalRating || ''}
                                onChange={handleNumberInputChange('MaxParentalRating')}
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ min: 0, max: 18 }}
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Price'
                                type='number'
                                value={pinBatchForm.Price || ''}
                                onChange={handleNumberInputChange('Price')}
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ min: 0, max: 999999.99, step: 0.01 }}
                            />
                        </Grid>

                        <Grid item xs={12} sm={6}>
                            <TextField
                                fullWidth
                                label='Currency'
                                value={pinBatchForm.Currency}
                                onChange={handleInputChange('Currency')}
                                disabled={pinBatchMutation.isPending}
                                inputProps={{ maxLength: 3 }}
                            />
                        </Grid>

                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Metadata'
                                value={pinBatchForm.Metadata}
                                onChange={handleInputChange('Metadata')}
                                multiline
                                rows={2}
                                disabled={pinBatchMutation.isPending}
                            />
                        </Grid>

                        <Grid item xs={12}>
                            <FormControl component='fieldset'>
                                <FormLabel component='legend'>Permissions</FormLabel>
                                <FormGroup>
                                    <FormControlLabel
                                        control={
                                            <Checkbox
                                                checked={pinBatchForm.AllowRemoteAccess}
                                                onChange={handleInputChange('AllowRemoteAccess')}
                                                disabled={pinBatchMutation.isPending}
                                            />
                                        }
                                        label='Allow Remote Access'
                                    />
                                    <FormControlLabel
                                        control={
                                            <Checkbox
                                                checked={pinBatchForm.AllowTranscoding}
                                                onChange={handleInputChange('AllowTranscoding')}
                                                disabled={pinBatchMutation.isPending}
                                            />
                                        }
                                        label='Allow Transcoding'
                                    />
                                    <FormControlLabel
                                        control={
                                            <Checkbox
                                                checked={pinBatchForm.AllowDownload}
                                                onChange={handleInputChange('AllowDownload')}
                                                disabled={pinBatchMutation.isPending}
                                            />
                                        }
                                        label='Allow Download'
                                    />
                                    <FormControlLabel
                                        control={
                                            <Checkbox
                                                checked={pinBatchForm.AllowSyncPlay}
                                                onChange={handleInputChange('AllowSyncPlay')}
                                                disabled={pinBatchMutation.isPending}
                                            />
                                        }
                                        label='Allow Sync Play'
                                    />
                                </FormGroup>
                            </FormControl>
                        </Grid>
                    </Grid>
                </DialogContent>
                <DialogActions>
                    <Button onClick={onPinBatchClose} disabled={pinBatchMutation.isPending}>
                        Cancel
                    </Button>
                    <Button
                        onClick={onPinBatchSave}
                        variant='contained'
                        disabled={pinBatchMutation.isPending}
                        startIcon={pinBatchMutation.isPending ? <CircularProgress size={20} /> : undefined}
                    >
                        {pinBatchMutation.isPending ? 'Creating...' : 'Create PIN Batch'}
                    </Button>
                </DialogActions>
            </Dialog>
            </Box>
        </Page>
    );
};
