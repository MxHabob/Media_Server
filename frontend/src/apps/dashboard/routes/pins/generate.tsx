import React, { useState } from 'react';
import Page from 'components/Page';
import Box from '@mui/material/Box';
import Stack from '@mui/material/Stack';
import TextField from '@mui/material/TextField';
import FormControl from '@mui/material/FormControl';
import InputLabel from '@mui/material/InputLabel';
import Select from '@mui/material/Select';
import MenuItem from '@mui/material/MenuItem';
import Button from '@mui/material/Button';
import Typography from '@mui/material/Typography';
import { ServerConnections } from 'lib/jellyfin-apiclient';
import Snackbar from '@mui/material/Snackbar';
import Alert from '@mui/material/Alert';
import CircularProgress from '@mui/material/CircularProgress';

const subscriptionOptions = [
    { label: 'Six Hours', value: 1 },
    { label: 'Twelve Hours', value: 2 },
    { label: 'Weekly', value: 3 },
    { label: 'Monthly', value: 4 },
    { label: 'Yearly', value: 5 },
    { label: 'Lifetime', value: 6 }
];

export default function PinGeneratePage() {
    const [count, setCount] = useState(10);
    const [subscriptionType, setSubscriptionType] = useState<number>(1);
    const [pins, setPins] = useState<string[]>([]);
    const [loading, setLoading] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [successOpen, setSuccessOpen] = useState(false);

    return (
        <Page id='pinsGeneratePage' title='Generate PINs' className='mainAnimatedPage type-interior'>
            <Box className='content-primary'>
                <Stack spacing={3}>
                    <Typography variant='h1'>Generate PINs</Typography>
                    {error && <Typography color='error'>{error}</Typography>}
                    <Stack direction='row' spacing={2}>
                        <TextField
                            id='pin-count'
                            type='number'
                            label='Count'
                            inputProps={{ min: 1, max: 1000 }}
                            value={count}
                            onChange={(e) => setCount(parseInt(e.target.value || '0', 10))}
                            helperText='1 - 1000'
                            error={count < 1 || count > 1000}
                        />
                        <FormControl>
                            <InputLabel id='pin-subscription-label'>Subscription</InputLabel>
                            <Select
                                labelId='pin-subscription-label'
                                id='pin-subscription'
                                label='Subscription'
                                value={subscriptionType}
                                onChange={(e) => setSubscriptionType(Number(e.target.value))}
                            >
                                {subscriptionOptions.map(o => (
                                    <MenuItem key={o.value} value={o.value}>{o.label}</MenuItem>
                                ))}
                            </Select>
                        </FormControl>
                        <Button
                            variant='contained'
                            disabled={loading || count < 1 || count > 1000}
                            onClick={async () => {
                                setError(null);
                                setLoading(true);
                                try {
                                    const apiClient = ServerConnections.currentApiClient();
                                    if (!apiClient) {
                                        throw new Error('No server connected');
                                    }
                                    const result: string[] = await (apiClient as any).ajax({
                                        type: 'POST',
                                        url: apiClient.getUrl('/Users/GeneratePins'),
                                        data: JSON.stringify({ Count: count, SubscriptionType: subscriptionType }),
                                        contentType: 'application/json',
                                        dataType: 'json'
                                    });
                                    setPins(result || []);
                                    setSuccessOpen(true);
                                } catch (e: any) {
                                    setError(e?.message || 'Failed to generate PINs');
                                } finally {
                                    setLoading(false);
                                }
                            }}
                            startIcon={loading ? <CircularProgress size={16} /> : null}
                        >Generate</Button>
                    </Stack>

                    {pins.length > 0 && (
                        <Box>
                            <Typography variant='h2'>Generated PINs</Typography>
                            <pre>{pins.join('\n')}</pre>
                            <Stack direction='row' spacing={2}>
                                <Button
                                    variant='outlined'
                                    onClick={() => navigator.clipboard?.writeText(pins.join('\n'))}
                                >Copy</Button>
                                <Button
                                    variant='outlined'
                                    onClick={() => {
                                        const blob = new Blob([pins.join('\n')], { type: 'text/plain' });
                                        const url = URL.createObjectURL(blob);
                                        const a = document.createElement('a');
                                        a.href = url;
                                        a.download = 'pins.txt';
                                        a.click();
                                        URL.revokeObjectURL(url);
                                    }}
                                >Download</Button>
                            </Stack>
                        </Box>
                    )}
                </Stack>
            </Box>
            <Snackbar open={successOpen} autoHideDuration={3000} onClose={() => setSuccessOpen(false)}>
                <Alert onClose={() => setSuccessOpen(false)} severity='success' sx={{ width: '100%' }}>
                    Generated {pins.length} PIN(s)
                </Alert>
            </Snackbar>
        </Page>
    );
}

