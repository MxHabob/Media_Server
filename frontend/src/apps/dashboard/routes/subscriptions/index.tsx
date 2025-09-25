import React, { useCallback, useState } from 'react';
import Page from 'components/Page';
import globalize from 'lib/globalize';
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
import IconButton from '@mui/material/IconButton';
import Tooltip from '@mui/material/Tooltip';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import AddIcon from '@mui/icons-material/Add';
import VpnKeyIcon from '@mui/icons-material/VpnKey';
import EditIcon from '@mui/icons-material/Edit';
import DeleteIcon from '@mui/icons-material/Delete';

type SubscriptionConfigurationDto = {
    Id?: string;
    Name: string;
    DurationHours: number;
    IsActive: boolean;
};

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
    const { data, isLoading } = useQuery({
        queryKey: ['Subscriptions', 'Configurations'],
        queryFn: () => fetchConfigs(__legacyApiClient__!)
    });

    const [dialogOpen, setDialogOpen] = useState(false);
    const [pinBatchDialogOpen, setPinBatchDialogOpen] = useState(false);
    const [draft, setDraft] = useState<SubscriptionConfigurationDto>({ Name: '', DurationHours: 24, IsActive: true });
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

    const saveMutation = useMutation({
        mutationFn: (payload: SubscriptionConfigurationDto) => upsertConfig(__legacyApiClient__!, payload),
        onSuccess: () => {
            qc.invalidateQueries({ queryKey: ['Subscriptions', 'Configurations'] });
            setDialogOpen(false);
            setDraft({ Name: '', DurationHours: 24, IsActive: true });
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
        setDraft({ Name: '', DurationHours: 24, IsActive: true });
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
            BatchDescription: `PIN batch for ${config.Name} subscription (${config.DurationHours} hours)`
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

    return (
        <Page id='subscriptions' className='type-interior' title={globalize.translate('HeaderSubscriptions')}>
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
                    <Typography variant='h5'>{globalize.translate('HeaderSubscriptions')}</Typography>
                    <Button variant='contained' onClick={onNew} startIcon={<AddIcon />}>
                        {globalize.translate('ButtonNew')}
                    </Button>
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
                                <TableCell>Duration (Hours)</TableCell>
                                <TableCell>Status</TableCell>
                                <TableCell align='center'>Actions</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {data?.map((config) => (
                                <TableRow key={config.Id}>
                                    <TableCell>{config.Name}</TableCell>
                                    <TableCell>{config.DurationHours}</TableCell>
                                    <TableCell>
                                        <Typography 
                                            variant='body2' 
                                            color={config.IsActive ? 'success.main' : 'error.main'}
                                        >
                                            {config.IsActive ? 'Active' : 'Inactive'}
                                        </Typography>
                                    </TableCell>
                                    <TableCell align='center'>
                                        <Stack direction='row' spacing={1} justifyContent='center'>
                                            <Tooltip title='Create PIN Batch'>
                                                <IconButton
                                                    size='small'
                                                    onClick={onCreatePinBatch(config)}
                                                    color='primary'
                                                >
                                                    <VpnKeyIcon />
                                                </IconButton>
                                            </Tooltip>
                                            
                                            <Tooltip title='Edit'>
                                                <IconButton
                                                    size='small'
                                                    onClick={onEdit(config)}
                                                >
                                                    <EditIcon />
                                                </IconButton>
                                            </Tooltip>
                                            
                                            <Tooltip title='Delete'>
                                                <IconButton
                                                    size='small'
                                                    onClick={onDelete(config.Id)}
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

                {data?.length === 0 && !isLoading && (
                    <Box textAlign='center' p={4}>
                        <Typography variant='h6' color='text.secondary'>
                            No subscription configurations found
                        </Typography>
                        <Typography variant='body2' color='text.secondary' sx={{ mt: 1 }}>
                            Create your first subscription configuration to get started
                        </Typography>
                    </Box>
                )}
            </Box>

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
                                label='Name'
                                value={draft.Name}
                                onChange={(e) => setDraft(prev => ({ ...prev, Name: e.target.value }))}
                                required
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Duration (Hours)'
                                type='number'
                                value={draft.DurationHours}
                                onChange={(e) => setDraft(prev => ({ ...prev, DurationHours: Number(e.target.value) }))}
                                required
                                inputProps={{ min: 1 }}
                            />
                        </Grid>
                        <Grid item xs={12}>
                            <FormControlLabel
                                control={
                                    <Checkbox
                                        checked={draft.IsActive}
                                        onChange={(e) => setDraft(prev => ({ ...prev, IsActive: e.target.checked }))}
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
        </Page>
    );
};
