import React, { useCallback, useMemo, useState } from 'react';
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
import MenuItem from '@mui/material/MenuItem';
import CircularProgress from '@mui/material/CircularProgress';
import Dialog from '@mui/material/Dialog';
import DialogTitle from '@mui/material/DialogTitle';
import DialogContent from '@mui/material/DialogContent';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import Alert from '@mui/material/Alert';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';

type UserDto = {
    Id: string;
    Name: string;
    Username?: string;
    Policy?: unknown;
};

type PinStatus = 'active' | 'expired' | 'all';

const fetchPinUsers = async (apiClient: ApiClient, status?: PinStatus, subscriptionType?: number): Promise<UserDto[]> => {
    const params: string[] = [];
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (subscriptionType != null) params.push(`subscriptionType=${encodeURIComponent(String(subscriptionType))}`);
    const query = params.length ? `?${params.join('&')}` : '';
    const url = apiClient.getUrl(`/Users/Pins${query}`);
    const res = await fetch(url, {
        headers: apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : undefined
    });
    if (!res.ok) throw new Error('Failed to fetch PIN users');
    return res.json();
};

const generatePins = async (apiClient: ApiClient, count: number, subscriptionType: number): Promise<string[]> => {
    const res = await fetch(apiClient.getUrl('/Users/GeneratePins'), {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            ...(apiClient.accessToken() ? { Authorization: `MediaBrowser Token=${apiClient.accessToken()}` } : {})
        },
        body: JSON.stringify({ count, subscriptionType })
    });
    if (!res.ok) throw new Error('Failed to generate PINs');
    return res.json() as Promise<string[]>;
};

export const Component = () => {
    const { __legacyApiClient__ } = useApi();
    const [ status, setStatus ] = useState<PinStatus>('active');
    const [ subscriptionType, setSubscriptionType ] = useState<number | undefined>(undefined);
    const { data, isLoading, refetch } = useQuery({
        queryKey: [ 'Pins', 'Users', status, subscriptionType ],
        queryFn: () => fetchPinUsers(__legacyApiClient__!, status, subscriptionType)
    });

    const [ count, setCount ] = useState<number>(10);
    const [ subTypeDraft, setSubTypeDraft ] = useState<number>(0);
    const [ generatedPins, setGeneratedPins ] = useState<string[] | null>(null);
    const [ errorMessage, setErrorMessage ] = useState<string>('');
    const handleErrorClose = useCallback(() => setErrorMessage(''), []);
    const handleDialogClose = useCallback(() => setGeneratedPins(null), []);
    const genMutation = useMutation({
        mutationFn: () => generatePins(__legacyApiClient__!, count, subTypeDraft),
        onSuccess: (pins: string[]) => {
            setGeneratedPins(pins);
            void refetch();
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || globalize.translate('MessageUnexpectedError'));
        }
    });

    const canGenerate = useMemo(() => count > 0 && Number.isFinite(count), [ count ]);
    const onGenerate = useCallback(() => {
        if (!__legacyApiClient__) return;
        if (!canGenerate) {
            setErrorMessage(globalize.translate('MessageInvalidValue'));
            return;
        }
        genMutation.mutate();
    }, [ genMutation, __legacyApiClient__, canGenerate ]);
    const onStatusChange = useCallback((value: PinStatus) => setStatus(value), []);
    const onSubTypeFilterChange = useCallback((value: string) => setSubscriptionType(value === '' ? undefined : Number(value)), []);
    const onCountChange = useCallback((value: string) => setCount(Number(value)), []);
    const onSubTypeDraftChange = useCallback((value: string) => setSubTypeDraft(Number(value)), []);

    const handleCountChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onCountChange(e.target.value), [ onCountChange ]);
    const handleSubTypeDraftChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onSubTypeDraftChange(e.target.value), [ onSubTypeDraftChange ]);
    const handleStatusChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onStatusChange(e.target.value as PinStatus), [ onStatusChange ]);
    const handleSubTypeFilterChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onSubTypeFilterChange(e.target.value), [ onSubTypeFilterChange ]);

    return (
        <Page id='pins' className='type-interior' title={globalize.translate('HeaderPins')}>
            <Box className='content-primary'>
                {errorMessage ? (
                    <Alert severity='error' onClose={handleErrorClose} sx={{ mb: 2 }}>
                        {errorMessage}
                    </Alert>
                ) : null}
                <Stack direction='row' justifyContent='space-between' alignItems='center' mb={2}>
                    <Typography variant='h5'>{globalize.translate('HeaderPins')}</Typography>
                    <Stack direction='row' gap={2}>
                        <TextField label={globalize.translate('LabelCount')} type='number' value={count} onChange={handleCountChange} size='small' />
                        <TextField label={globalize.translate('LabelSubscriptionType')} select value={subTypeDraft} onChange={handleSubTypeDraftChange} size='small' sx={{ minWidth: 180 }}>
                            <MenuItem value={0}>{globalize.translate('OptionSubscriptionBasic')}</MenuItem>
                            <MenuItem value={1}>{globalize.translate('OptionSubscriptionPremium')}</MenuItem>
                        </TextField>
                        <Button variant='contained' onClick={onGenerate} disabled={genMutation.isPending || !canGenerate}>
                            {genMutation.isPending ? globalize.translate('ButtonPleaseWait') : globalize.translate('ButtonGenerate')}
                        </Button>
                    </Stack>
                </Stack>

                <Stack direction='row' gap={2} mb={2}>
                    <TextField label={globalize.translate('LabelStatus')} select value={status} onChange={handleStatusChange} size='small' sx={{ minWidth: 160 }}>
                        <MenuItem value='active'>{globalize.translate('OptionActive')}</MenuItem>
                        <MenuItem value='expired'>{globalize.translate('OptionExpired')}</MenuItem>
                        <MenuItem value='all'>{globalize.translate('OptionAll')}</MenuItem>
                    </TextField>
                    <TextField label={globalize.translate('LabelSubscriptionType')} select value={subscriptionType ?? ''} onChange={handleSubTypeFilterChange} size='small' sx={{ minWidth: 180 }}>
                        <MenuItem value=''>{globalize.translate('OptionAny')}</MenuItem>
                        <MenuItem value={0}>{globalize.translate('OptionSubscriptionBasic')}</MenuItem>
                        <MenuItem value={1}>{globalize.translate('OptionSubscriptionPremium')}</MenuItem>
                    </TextField>
                </Stack>

                {isLoading ? (
                    <CircularProgress />
                ) : (
                    <Table>
                        <TableHead>
                            <TableRow>
                                <TableCell>{globalize.translate('LabelName')}</TableCell>
                            </TableRow>
                        </TableHead>
                        <TableBody>
                            {data?.map(u => (
                                <TableRow key={u.Id} hover>
                                    <TableCell>{u.Name || u.Username}</TableCell>
                                </TableRow>
                            ))}
                        </TableBody>
                    </Table>
                )}
            </Box>

            <Dialog open={!!generatedPins} onClose={handleDialogClose} maxWidth='xs' fullWidth>
                <DialogTitle>{globalize.translate('HeaderPins')}</DialogTitle>
                <DialogContent>
                    {genMutation.isPending ? (
                        <CircularProgress />
                    ) : (
                        <List>
                            {(generatedPins || []).map((pin) => (
                                <ListItem key={pin}>{pin}</ListItem>
                            ))}
                        </List>
                    )}
                </DialogContent>
            </Dialog>
        </Page>
    );
};

Component.displayName = 'PinsPage';
