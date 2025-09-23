import React, { useEffect, useState } from 'react';
import Page from 'components/Page';
import Box from '@mui/material/Box';
import Stack from '@mui/material/Stack';
import Typography from '@mui/material/Typography';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import Select from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';
import Button from '@mui/material/Button';
import CircularProgress from '@mui/material/CircularProgress';
import { ServerConnections } from 'lib/jellyfin-apiclient';

type PinUser = {
    Id: string;
    Name: string;
    SubscriptionType: number;
    SubscriptionExpirationDate?: string | null;
};

export default function PinReportsPage() {
    const [status, setStatus] = useState<'active' | 'expired' | 'all'>('active');
    const [subscriptionType, setSubscriptionType] = useState<number | ''>('');
    const [items, setItems] = useState<PinUser[]>([]);
    const [report, setReport] = useState<any>(null);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);

    useEffect(() => {
        let mounted = true;
        (async () => {
            setLoading(true);
            setError(null);
            try {
                const apiClient = ServerConnections.currentApiClient();
                if (!apiClient) {
                    throw new Error('No server connected');
                }
                const query: any = { status };
                if (subscriptionType !== '') query.subscriptionType = subscriptionType;
                const list: PinUser[] = await (apiClient as any).ajax({
                    type: 'GET',
                    url: apiClient.getUrl('/Users/Pins' + (Object.keys(query).length ? ('?' + new URLSearchParams(query).toString()) : '')),
                    dataType: 'json'
                });
                const rep = await (apiClient as any).ajax({
                    type: 'GET',
                    url: apiClient.getUrl('/Users/PinReport'),
                    dataType: 'json'
                });
                if (!mounted) return;
                setItems(list || []);
                setReport(rep || null);
            } catch (e: any) {
                if (!mounted) return;
                setError(e?.message || 'Failed to load report');
            } finally {
                if (mounted) setLoading(false);
            }
        })();
        return () => { mounted = false; };
    }, [status, subscriptionType]);

    return (
        <Page id='pinsReportsPage' title='PIN Reports' className='mainAnimatedPage type-interior'>
            <Box className='content-primary'>
                <Stack spacing={3}>
                    <Typography variant='h1'>PIN Users</Typography>
                    {error && <Typography color='error'>{error}</Typography>}
                    <Stack direction='row' spacing={2} alignItems='flex-end'>
                        <FormControl>
                            <InputLabel id='pin-status-label'>Status</InputLabel>
                            <Select
                                labelId='pin-status-label'
                                id='pin-status'
                                label='Status'
                                value={status}
                                onChange={(e) => setStatus(e.target.value as any)}
                            >
                                <MenuItem value='active'>Active</MenuItem>
                                <MenuItem value='expired'>Expired</MenuItem>
                                <MenuItem value='all'>All</MenuItem>
                            </Select>
                        </FormControl>
                        <FormControl>
                            <InputLabel id='pin-type-label'>Subscription</InputLabel>
                            <Select
                                labelId='pin-type-label'
                                id='pin-type'
                                label='Subscription'
                                value={subscriptionType}
                                onChange={(e) => setSubscriptionType(e.target.value === '' ? '' : parseInt(String(e.target.value), 10))}
                            >
                                <MenuItem value=''>All</MenuItem>
                                <MenuItem value={1}>Six Hours</MenuItem>
                                <MenuItem value={2}>Twelve Hours</MenuItem>
                                <MenuItem value={3}>Weekly</MenuItem>
                                <MenuItem value={4}>Monthly</MenuItem>
                                <MenuItem value={5}>Yearly</MenuItem>
                                <MenuItem value={6}>Lifetime</MenuItem>
                            </Select>
                        </FormControl>
                        <Button
                            variant='contained'
                            disabled={loading}
                            startIcon={loading ? <CircularProgress size={16} /> : null}
                            onClick={() => {
                                const csv = ['Id,Name,SubscriptionType,ExpirationDate']
                                    .concat(items.map(u => `${u.Id},${JSON.stringify(u.Name)},${u.SubscriptionType},${u.SubscriptionExpirationDate || ''}`))
                                    .join('\n');
                                const blob = new Blob([csv], { type: 'text/csv' });
                                const url = URL.createObjectURL(blob);
                                const a = document.createElement('a');
                                a.href = url;
                                a.download = 'pin-users.csv';
                                a.click();
                                URL.revokeObjectURL(url);
                            }}
                        >Export CSV</Button>
                    </Stack>

                    {report && (
                        <Box>
                            <Typography variant='h2'>Summary</Typography>
                            <div>Total: {report.Total} • Active: {report.Active} • Expired: {report.Expired}</div>
                        </Box>
                    )}

                    <Box sx={{ overflowX: 'auto' }}>
                        <table className='detailTable'>
                            <thead>
                                <tr>
                                    <th>Name</th>
                                    <th>Subscription</th>
                                    <th>Expiration</th>
                                </tr>
                            </thead>
                            <tbody>
                                {items.map(u => (
                                    <tr key={u.Id}>
                                        <td>{u.Name}</td>
                                        <td>{u.SubscriptionType}</td>
                                        <td>{u.SubscriptionExpirationDate ? new Date(u.SubscriptionExpirationDate).toLocaleString() : 'Never'}</td>
                                    </tr>
                                ))}
                            </tbody>
                        </table>
                    </Box>
                </Stack>
            </Box>
        </Page>
    );
}

