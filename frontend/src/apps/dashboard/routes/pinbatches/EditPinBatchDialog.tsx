import React, { useCallback, useState } from 'react';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import DialogActions from '@mui/material/DialogActions';
import Button from '@mui/material/Button';
import TextField from '@mui/material/TextField';
import Grid from '@mui/material/Grid';
import Alert from '@mui/material/Alert';
import CircularProgress from '@mui/material/CircularProgress';
import { useMutation } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import type { PinBatch, UpdatePinBatchRequest } from '../../types/pinBatch';

const updatePinBatch = async (apiClient: ApiClient, batchId: string, request: UpdatePinBatchRequest): Promise<void> => {
    await apiClient.ajax({
        type: 'PUT',
        url: apiClient.getUrl(`/PinBatches/${batchId}`),
        data: JSON.stringify(request),
        contentType: 'application/json'
    });
};

interface EditPinBatchDialogProps {
    open: boolean;
    onClose: () => void;
    batch: PinBatch;
}

export default function EditPinBatchDialog({ open, onClose, batch }: EditPinBatchDialogProps) {
    const { __legacyApiClient__ } = useApi();
    
    const [formData, setFormData] = useState<UpdatePinBatchRequest>({
        Name: batch.Name,
        Description: batch.Description || ''
    });

    const [errorMessage, setErrorMessage] = useState<string>('');

    const updateMutation = useMutation({
        mutationFn: (request: UpdatePinBatchRequest) => updatePinBatch(__legacyApiClient__!, batch.Id, request),
        onSuccess: () => {
            onClose();
            setErrorMessage('');
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'An unexpected error occurred');
        }
    });

    const handleInputChange = useCallback((field: keyof UpdatePinBatchRequest) => 
        (event: React.ChangeEvent<HTMLInputElement>) => {
            setFormData(prev => ({
                ...prev,
                [field]: event.target.value
            }));
        }, []);

    const handleSubmit = useCallback(() => {
        if (!formData.Name?.trim()) {
            setErrorMessage('Batch name is required');
            return;
        }

        updateMutation.mutate(formData);
    }, [formData, updateMutation]);

    const handleClose = useCallback(() => {
        if (!updateMutation.isPending) {
            onClose();
            setErrorMessage('');
        }
    }, [updateMutation.isPending, onClose]);

    return (
        <Dialog open={open} onClose={handleClose} maxWidth='sm' fullWidth>
            <DialogTitle>Edit PIN Batch</DialogTitle>
            <DialogContent>
                {errorMessage && (
                    <Alert severity='error' sx={{ mb: 2 }}>
                        {errorMessage}
                    </Alert>
                )}

                <Grid container spacing={2} sx={{ mt: 1 }}>
                    <Grid item xs={12}>
                        <TextField
                            fullWidth
                            label='Batch Name'
                            value={formData.Name}
                            onChange={handleInputChange('Name')}
                            required
                            disabled={updateMutation.isPending}
                        />
                    </Grid>

                    <Grid item xs={12}>
                        <TextField
                            fullWidth
                            label='Description'
                            value={formData.Description}
                            onChange={handleInputChange('Description')}
                            multiline
                            rows={3}
                            disabled={updateMutation.isPending}
                        />
                    </Grid>

                    <Grid item xs={12}>
                        <Alert severity='info'>
                            Only the name and description can be edited. Other batch properties are immutable after creation.
                        </Alert>
                    </Grid>
                </Grid>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose} disabled={updateMutation.isPending}>
                    Cancel
                </Button>
                <Button
                    onClick={handleSubmit}
                    variant='contained'
                    disabled={updateMutation.isPending}
                    startIcon={updateMutation.isPending ? <CircularProgress size={20} /> : undefined}
                >
                    {updateMutation.isPending ? 'Updating...' : 'Update Batch'}
                </Button>
            </DialogActions>
        </Dialog>
    );
}
