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
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';

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

export const Component = () => {
    const { __legacyApiClient__ } = useApi();
    const qc = useQueryClient();
    const { data, isLoading } = useQuery({
        queryKey: [ 'Subscriptions', 'Configurations' ],
        queryFn: () => fetchConfigs(__legacyApiClient__!)
    });

    const [ dialogOpen, setDialogOpen ] = useState(false);
    const [ draft, setDraft ] = useState<SubscriptionConfigurationDto>({ Name: '', DurationHours: 24, IsActive: true });

    const saveMutation = useMutation({
        mutationFn: (payload: SubscriptionConfigurationDto) => upsertConfig(__legacyApiClient__!, payload),
        onSuccess: () => qc.invalidateQueries({ queryKey: [ 'Subscriptions', 'Configurations' ] })
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => deleteConfig(__legacyApiClient__!, id),
        onSuccess: () => qc.invalidateQueries({ queryKey: [ 'Subscriptions', 'Configurations' ] })
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
        if (id) deleteMutation.mutate(id);
    }, [ deleteMutation ]);

    const onClose = useCallback(() => setDialogOpen(false), []);
    const onSave = useCallback(() => {
        saveMutation.mutate(draft, { onSettled: () => setDialogOpen(false) });
    }, [ draft, saveMutation ]);

    return (
        <Page id='subscriptions' className='type-interior' title={globalize.translate('HeaderSubscriptions')}>
            <Box className='content-primary'>
                <Stack direction='row' justifyContent='space-between' alignItems='center' mb={2}>
                    <Typography variant='h5'>{globalize.translate('HeaderSubscriptions')}</Typography>
                    <Button variant='contained' onClick={onNew}>{globalize.translate('ButtonNew')}</Button>
                </Stack>
                {isLoading ? (
                    <CircularProgress />
                ) : (
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>{globalize.translate('LabelName')}</TableCell>
                                <TableCell>{globalize.translate('LabelDurationHours')}</TableCell>
                                <TableCell>{globalize.translate('LabelActive')}</TableCell>
                                <TableCell align='right'>{globalize.translate('LabelActions')}</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {data?.map(c => (
                                <TableRow key={c.Id} hover>
                                    <TableCell>{c.Name}</TableCell>
                                    <TableCell>{c.DurationHours}</TableCell>
                                    <TableCell>{c.IsActive ? globalize.translate('Yes') : globalize.translate('No')}</TableCell>
                                    <TableCell align='right'>
                                        <Stack direction='row' gap={1} justifyContent='flex-end'>
                                            <Button size='small' onClick={onEdit(c)}>{globalize.translate('Edit')}</Button>
                                            <Button size='small' color='error' onClick={onDelete(c.Id!)}>{globalize.translate('Delete')}</Button>
                                        </Stack>
                                    </TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                )}
            </Box>

            <Dialog open={dialogOpen} onClose={onClose} fullWidth maxWidth='sm'>
                <DialogTitle>{draft?.Id ? globalize.translate('HeaderEditSubscription') : globalize.translate('HeaderNewSubscription')}</DialogTitle>
                <DialogContent>
                    <Stack gap={2} mt={1}>
                        <TextField label={globalize.translate('LabelName')} value={draft.Name} onChange={e => setDraft({ ...draft, Name: e.target.value })} />
                        <TextField label={globalize.translate('LabelDurationHours')} type='number' value={draft.DurationHours} onChange={e => setDraft({ ...draft, DurationHours: Number(e.target.value) })} />
                        <TextField label={globalize.translate('LabelActive')} value={draft.IsActive ? globalize.translate('Yes') : globalize.translate('No')} disabled />
                    </Stack>
                </DialogContent>
                <DialogActions>
                    <Button onClick={onClose}>{globalize.translate('ButtonCancel')}</Button>
                    <Button onClick={onSave} variant='contained' disabled={saveMutation.isPending}>{globalize.translate('Save')}</Button>
                </DialogActions>
            </Dialog>
        </Page>
    );
};

Component.displayName = 'SubscriptionsPage';


