import React, { useCallback, useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import MenuItem from '@mui/material/MenuItem';
import FormControl from '@mui/material/FormControl';
import FormLabel from '@mui/material/FormLabel';
import FormGroup from '@mui/material/FormGroup';
import FormControlLabel from '@mui/material/FormControlLabel';
import Checkbox from '@mui/material/Checkbox';
import Grid from '@mui/material/Grid';
import Alert from '@mui/material/Alert';
import CircularProgress from '@mui/material/CircularProgress';
import { useMutation } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import type { CreatePinBatchRequest } from '../../types/pinBatch';

const createPinBatch = async (apiClient: ApiClient, request: CreatePinBatchRequest): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl('/PinBatches'),
        data: JSON.stringify(request),
        contentType: 'application/json'
    });
};

interface CreatePinBatchDialogProps {
    open: boolean;
    onClose: () => void;
}

export default function CreatePinBatchDialog({ open, onClose }: CreatePinBatchDialogProps) {
    const { __legacyApiClient__ } = useApi();
    
    const [formData, setFormData] = useState<CreatePinBatchRequest>({
        Name: '',
        Description: '',
        SubscriptionType: 3, // Daily
        PinPattern: 0, // Numeric
        PinLength: 6,
        PinCount: 10,
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

    const createMutation = useMutation({
        mutationFn: (request: CreatePinBatchRequest) => createPinBatch(__legacyApiClient__!, request),
        onSuccess: () => {
            onClose();
            setFormData({
                Name: '',
                Description: '',
                SubscriptionType: 3,
                PinPattern: 0,
                PinLength: 6,
                PinCount: 10,
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
            setErrorMessage('');
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'An unexpected error occurred');
        }
    });

    const handleInputChange = useCallback((field: keyof CreatePinBatchRequest) => 
        (event: React.ChangeEvent<HTMLInputElement>) => {
            const value = event.target.type === 'checkbox' 
                ? event.target.checked 
                : event.target.value;
            
            setFormData(prev => ({
                ...prev,
                [field]: value
            }));
        }, []);

    const handleNumberInputChange = useCallback((field: keyof CreatePinBatchRequest) => 
        (event: React.ChangeEvent<HTMLInputElement>) => {
            const value = event.target.value === '' ? undefined : Number(event.target.value);
            setFormData(prev => ({
                ...prev,
                [field]: value
            }));
        }, []);

    const handleSubmit = useCallback(() => {
        if (!formData.Name.trim()) {
            setErrorMessage('Batch name is required');
            return;
        }

        if (formData.PinCount <= 0) {
            setErrorMessage('PIN count must be greater than 0');
            return;
        }

        if (formData.PinLength < 4 || formData.PinLength > 20) {
            setErrorMessage('PIN length must be between 4 and 20 characters');
            return;
        }

        if (formData.PinPattern === 3 && !formData.CustomCharacterSet?.trim()) {
            setErrorMessage('Custom character set is required for custom PIN pattern');
            return;
        }

        const request: CreatePinBatchRequest = {
            ...formData,
            ExpirationDate: formData.ExpirationDate || undefined,
            MaxConcurrentSessions: formData.MaxConcurrentSessions || undefined,
            MaxBitrate: formData.MaxBitrate || undefined,
            MaxParentalRating: formData.MaxParentalRating || undefined,
            Price: formData.Price || undefined,
            Currency: formData.Currency || undefined,
            Metadata: formData.Metadata || undefined
        };

        createMutation.mutate(request);
    }, [formData, createMutation]);

    const handleClose = useCallback(() => {
        if (!createMutation.isPending) {
            onClose();
            setErrorMessage('');
        }
    }, [createMutation.isPending, onClose]);

    return (
        <Dialog open={open} onClose={handleClose} maxWidth='md' fullWidth>
            <DialogTitle>Create PIN Batch</DialogTitle>
            <DialogContent>
                {errorMessage && (
                    <Alert severity='error' sx={{ mb: 2 }}>
                        {errorMessage}
                    </Alert>
                )}

                <Grid container spacing={2} sx={{ mt: 1 }}>
                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Batch Name'
                            value={formData.Name}
                            onChange={handleInputChange('Name')}
                            required
                            disabled={createMutation.isPending}
                        />
                    </Grid>
                    
                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='PIN Count'
                            type='number'
                            value={formData.PinCount}
                            onChange={handleNumberInputChange('PinCount')}
                            required
                            disabled={createMutation.isPending}
                            inputProps={{ min: 1, max: 10000 }}
                        />
                    </Grid>

                    <Grid item xs={12}>
                        <TextField
                            fullWidth
                            label='Description'
                            value={formData.Description}
                            onChange={handleInputChange('Description')}
                            multiline
                            rows={2}
                            disabled={createMutation.isPending}
                        />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Subscription Type'
                            select
                            value={formData.SubscriptionType}
                            onChange={handleInputChange('SubscriptionType')}
                            disabled={createMutation.isPending}
                        >
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
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='PIN Pattern'
                            select
                            value={formData.PinPattern}
                            onChange={handleInputChange('PinPattern')}
                            disabled={createMutation.isPending}
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
                            value={formData.PinLength}
                            onChange={handleNumberInputChange('PinLength')}
                            disabled={createMutation.isPending}
                            inputProps={{ min: 4, max: 20 }}
                        />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Expiration Date'
                            type='datetime-local'
                            value={formData.ExpirationDate}
                            onChange={handleInputChange('ExpirationDate')}
                            disabled={createMutation.isPending}
                            InputLabelProps={{ shrink: true }}
                        />
                    </Grid>

                    {formData.PinPattern === 3 && (
                        <Grid item xs={12}>
                            <TextField
                                fullWidth
                                label='Custom Character Set'
                                value={formData.CustomCharacterSet}
                                onChange={handleInputChange('CustomCharacterSet')}
                                disabled={createMutation.isPending}
                                helperText='Enter the characters that can be used in PINs (e.g., ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789)'
                            />
                        </Grid>
                    )}

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Max Concurrent Sessions'
                            type='number'
                            value={formData.MaxConcurrentSessions || ''}
                            onChange={handleNumberInputChange('MaxConcurrentSessions')}
                            disabled={createMutation.isPending}
                            inputProps={{ min: 1, max: 100 }}
                        />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Max Bitrate (kbps)'
                            type='number'
                            value={formData.MaxBitrate || ''}
                            onChange={handleNumberInputChange('MaxBitrate')}
                            disabled={createMutation.isPending}
                            inputProps={{ min: 1, max: 100000000 }}
                        />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Max Parental Rating'
                            type='number'
                            value={formData.MaxParentalRating || ''}
                            onChange={handleNumberInputChange('MaxParentalRating')}
                            disabled={createMutation.isPending}
                            inputProps={{ min: 0, max: 18 }}
                        />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Price'
                            type='number'
                            value={formData.Price || ''}
                            onChange={handleNumberInputChange('Price')}
                            disabled={createMutation.isPending}
                            inputProps={{ min: 0, max: 999999.99, step: 0.01 }}
                        />
                    </Grid>

                    <Grid item xs={12} sm={6}>
                        <TextField
                            fullWidth
                            label='Currency'
                            value={formData.Currency}
                            onChange={handleInputChange('Currency')}
                            disabled={createMutation.isPending}
                            inputProps={{ maxLength: 3 }}
                        />
                    </Grid>

                    <Grid item xs={12}>
                        <TextField
                            fullWidth
                            label='Metadata'
                            value={formData.Metadata}
                            onChange={handleInputChange('Metadata')}
                            multiline
                            rows={2}
                            disabled={createMutation.isPending}
                        />
                    </Grid>

                    <Grid item xs={12}>
                        <FormControl component='fieldset'>
                            <FormLabel component='legend'>Permissions</FormLabel>
                            <FormGroup>
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.AllowRemoteAccess}
                                            onChange={handleInputChange('AllowRemoteAccess')}
                                            disabled={createMutation.isPending}
                                        />
                                    }
                                    label='Allow Remote Access'
                                />
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.AllowTranscoding}
                                            onChange={handleInputChange('AllowTranscoding')}
                                            disabled={createMutation.isPending}
                                        />
                                    }
                                    label='Allow Transcoding'
                                />
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.AllowDownload}
                                            onChange={handleInputChange('AllowDownload')}
                                            disabled={createMutation.isPending}
                                        />
                                    }
                                    label='Allow Download'
                                />
                                <FormControlLabel
                                    control={
                                        <Checkbox
                                            checked={formData.AllowSyncPlay}
                                            onChange={handleInputChange('AllowSyncPlay')}
                                            disabled={createMutation.isPending}
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
                <Button onClick={handleClose} disabled={createMutation.isPending}>
                    Cancel
                </Button>
                <Button
                    onClick={handleSubmit}
                    variant='contained'
                    disabled={createMutation.isPending}
                    startIcon={createMutation.isPending ? <CircularProgress size={20} /> : undefined}
                >
                    {createMutation.isPending ? 'Creating...' : 'Create Batch'}
                </Button>
            </DialogActions>
        </Dialog>
    );
}
