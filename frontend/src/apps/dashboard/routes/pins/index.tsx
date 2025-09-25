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
import Chip from '@mui/material/Chip';
import Tabs from '@mui/material/Tabs';
import Tab from '@mui/material/Tab';
import Card from '@mui/material/Card';
import CardContent from '@mui/material/CardContent';
import Grid from '@mui/material/Grid';
import { useMutation, useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import { useApi } from 'hooks/useApi';
import { useNavigate } from 'react-router-dom';
import AddIcon from '@mui/icons-material/Add';
import GetAppIcon from '@mui/icons-material/GetApp';
import AssessmentIcon from '@mui/icons-material/Assessment';
import ViewListIcon from '@mui/icons-material/ViewList';

type UserDto = {
    Id: string;
    Name: string;
    Username?: string;
    Policy?: unknown;
};

type PinStatus = 'active' | 'expired' | 'all';

type PinReport = {
    Total: number;
    Active: number;
    Expired: number;
    ByType: Array<{
        SubscriptionType: number;
        Total: number;
        Active: number;
        Expired: number;
    }>;
};

const fetchPinUsers = async (apiClient: ApiClient, status?: string, subscriptionType?: number): Promise<UserDto[]> => {
    const params: string[] = [];
    if (status) params.push(`status=${encodeURIComponent(status)}`);
    if (subscriptionType !== undefined) params.push(`subscriptionType=${subscriptionType}`);

    const queryString = params.length > 0 ? `?${params.join('&')}` : '';
    const url = `/Users/Pins${queryString}`;
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl(url)
    });

    return res.json() as Promise<UserDto[]>;
};

const fetchPinReport = async (apiClient: ApiClient): Promise<PinReport> => {
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl('/Users/PinReport')
    });

    return res.json() as Promise<PinReport>;
};

const generatePins = async (apiClient: ApiClient, count: number, subscriptionType: number): Promise<string[]> => {
    const res = await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl('/Users/GeneratePins'),
        data: JSON.stringify({ Count: count, SubscriptionType: subscriptionType }),
        contentType: 'application/json'
    });

    return res.json() as Promise<string[]>;
};

const exportPinReport = async (apiClient: ApiClient): Promise<void> => {
    const response = await fetch(apiClient.getUrl('/Users/PinReport'), {
        method: 'GET',
        headers: {
            'Authorization': `MediaBrowser Token="${apiClient.accessToken()}"`
        }
    });

    if (!response.ok) {
        throw new Error('Export failed');
    }

    const data = await response.json();

    // Create CSV content
    const csvContent = [
        ['Metric', 'Value'],
        ['Total PINs', data.Total],
        ['Active PINs', data.Active],
        ['Expired PINs', data.Expired],
        [''],
        ['Subscription Type', 'Total', 'Active', 'Expired'],
        ...data.ByType.map((type: { SubscriptionType: number; Total: number; Active: number; Expired: number }) => [
            getSubscriptionTypeText(type.SubscriptionType),
            type.Total,
            type.Active,
            type.Expired
        ])
    ].map(row => row.join(',')).join('\n');

    const blob = new Blob([csvContent], { type: 'text/csv' });
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = `PIN_Report_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.csv`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
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
    const navigate = useNavigate();
    const [tabValue, setTabValue] = useState(0);
    const [status, setStatus] = useState<PinStatus>('active');
    const [subscriptionType, setSubscriptionType] = useState<number | undefined>(undefined);
    const [count, setCount] = useState<number>(10);
    const [subTypeDraft, setSubTypeDraft] = useState<number>(0);
    const [generatedPins, setGeneratedPins] = useState<string[] | null>(null);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [successMessage, setSuccessMessage] = useState<string>('');

    const { data: users, isLoading, refetch } = useQuery({
        queryKey: ['Pins', 'Users', status, subscriptionType],
        queryFn: () => fetchPinUsers(__legacyApiClient__!, status, subscriptionType)
    });

    const { data: report, isLoading: reportLoading } = useQuery({
        queryKey: ['PinReport'],
        queryFn: () => fetchPinReport(__legacyApiClient__!)
    });

    const genMutation = useMutation({
        mutationFn: () => generatePins(__legacyApiClient__!, count, subTypeDraft),
        onSuccess: (pins: string[]) => {
            setGeneratedPins(pins);
            setSuccessMessage(`Generated ${pins.length} PINs successfully`);
            void refetch();
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || globalize.translate('MessageUnexpectedError'));
        }
    });

    const exportMutation = useMutation({
        mutationFn: () => exportPinReport(__legacyApiClient__!),
        onSuccess: () => {
            setSuccessMessage('PIN report exported successfully');
        },
        onError: (e: unknown) => {
            const message = e instanceof Error ? e.message : String(e);
            setErrorMessage(message || 'Export failed');
        }
    });

    const handleErrorClose = useCallback(() => setErrorMessage(''), []);
    const handleSuccessClose = useCallback(() => setSuccessMessage(''), []);
    const handleDialogClose = useCallback(() => setGeneratedPins(null), []);

    const canGenerate = useMemo(() => count > 0 && Number.isFinite(count), [count]);
    const onGenerate = useCallback(() => {
        if (!__legacyApiClient__) return;
        if (!canGenerate) {
            setErrorMessage(globalize.translate('MessageInvalidValue'));
            return;
        }
        genMutation.mutate();
    }, [genMutation, __legacyApiClient__, canGenerate]);

    const onStatusChange = useCallback((value: PinStatus) => setStatus(value), []);
    const onSubTypeFilterChange = useCallback((value: string) => setSubscriptionType(value === '' ? undefined : Number(value)), []);
    const onCountChange = useCallback((value: string) => setCount(Number(value)), []);
    const onSubTypeDraftChange = useCallback((value: string) => setSubTypeDraft(Number(value)), []);

    const handleCountChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onCountChange(e.target.value), [onCountChange]);
    const handleSubTypeDraftChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onSubTypeDraftChange(e.target.value), [onSubTypeDraftChange]);
    const handleStatusChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onStatusChange(e.target.value as PinStatus), [onStatusChange]);
    const handleSubTypeFilterChange = useCallback((e: React.ChangeEvent<HTMLInputElement>) => onSubTypeFilterChange(e.target.value), [onSubTypeFilterChange]);
    const handleTabChange = useCallback((event: React.SyntheticEvent, newValue: number) => setTabValue(newValue), []);

    const handleExportReport = useCallback(() => {
        exportMutation.mutate();
    }, [exportMutation]);

    const handleNavigateToBatches = useCallback(() => {
        navigate('/dashboard/pinbatches');
    }, [navigate]);

    const handleNavigateToStatistics = useCallback(() => {
        navigate('/dashboard/pinbatches/statistics');
    }, [navigate]);

    return (
        <Page id='pins' className='type-interior' title={globalize.translate('HeaderPins')}>
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
                    <Typography variant='h5'>{globalize.translate('HeaderPins')}</Typography>
                    <Stack direction='row' gap={2}>
                        <Button
                            variant='outlined'
                            startIcon={<ViewListIcon />}
                            onClick={handleNavigateToBatches}
                        >
                            Manage Batches
                        </Button>
                        <Button
                            variant='outlined'
                            startIcon={<AssessmentIcon />}
                            onClick={handleNavigateToStatistics}
                        >
                            Statistics
                        </Button>
                        <Button
                            variant='outlined'
                            startIcon={<GetAppIcon />}
                            onClick={handleExportReport}
                            disabled={exportMutation.isPending}
                        >
                            Export Report
                        </Button>
                    </Stack>
                </Stack>

                <Tabs value={tabValue} onChange={handleTabChange} sx={{ mb: 3 }}>
                    <Tab label='PIN Management' />
                    <Tab label='Overview' />
                </Tabs>

                {tabValue === 0 && (
                    <>
                        <Stack direction='row' justifyContent='space-between' alignItems='center' mb={2}>
                            <Typography variant='h6'>Generate New PINs</Typography>
                            <Stack direction='row' gap={2}>
                                <TextField 
                                    label={globalize.translate('LabelCount')} 
                                    type='number' 
                                    value={count} 
                                    onChange={handleCountChange} 
                                    size='small' 
                                />
                                <TextField 
                                    label={globalize.translate('LabelSubscriptionType')} 
                                    select 
                                    value={subTypeDraft} 
                                    onChange={handleSubTypeDraftChange} 
                                    size='small' 
                                    sx={{ minWidth: 180 }}
                                >
                                    <MenuItem value={0}>{globalize.translate('OptionSubscriptionBasic')}</MenuItem>
                                    <MenuItem value={1}>{globalize.translate('OptionSubscriptionPremium')}</MenuItem>
                                </TextField>
                                <Button 
                                    variant='contained' 
                                    onClick={onGenerate} 
                                    disabled={genMutation.isPending || !canGenerate}
                                    startIcon={<AddIcon />}
                                >
                                    {genMutation.isPending ? globalize.translate('ButtonPleaseWait') : globalize.translate('ButtonGenerate')}
                                </Button>
                            </Stack>
                        </Stack>

                        <Stack direction='row' gap={2} mb={2}>
                            <TextField 
                                label={globalize.translate('LabelStatus')} 
                                select 
                                value={status} 
                                onChange={handleStatusChange} 
                                size='small' 
                                sx={{ minWidth: 160 }}
                            >
                                <MenuItem value='active'>{globalize.translate('OptionActive')}</MenuItem>
                                <MenuItem value='expired'>{globalize.translate('OptionExpired')}</MenuItem>
                                <MenuItem value='all'>{globalize.translate('OptionAll')}</MenuItem>
                            </TextField>
                            <TextField 
                                label={globalize.translate('LabelSubscriptionType')} 
                                select 
                                value={subscriptionType ?? ''} 
                                onChange={handleSubTypeFilterChange} 
                                size='small' 
                                sx={{ minWidth: 180 }}
                            >
                                <MenuItem value=''>{globalize.translate('OptionAny')}</MenuItem>
                                <MenuItem value={0}>{globalize.translate('OptionSubscriptionBasic')}</MenuItem>
                                <MenuItem value={1}>{globalize.translate('OptionSubscriptionPremium')}</MenuItem>
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
                                        <TableCell>ID</TableCell>
                                        <TableCell>Name</TableCell>
                                        <TableCell>Username</TableCell>
                                        <TableCell>Status</TableCell>
                                    </TableRow>
                                </TableHead>
                                <TableBody>
                                    {users?.map((user) => (
                                        <TableRow key={user.Id}>
                                            <TableCell>{user.Id}</TableCell>
                                            <TableCell>{user.Name}</TableCell>
                                            <TableCell>{user.Username || 'N/A'}</TableCell>
                                            <TableCell>
                                                <Chip 
                                                    label={status === 'expired' ? 'Expired' : 'Active'} 
                                                    color={status === 'expired' ? 'error' : 'success'} 
                                                    size='small' 
                                                />
                                            </TableCell>
                                        </TableRow>
                                    ))}
                                </TableBody>
                            </Table>
                        )}

                        {users?.length === 0 && !isLoading && (
                            <Box textAlign='center' p={4}>
                                <Typography variant='h6' color='text.secondary'>
                                    No PIN users found
                                </Typography>
                                <Typography variant='body2' color='text.secondary' sx={{ mt: 1 }}>
                                    Generate some PINs to get started
                                </Typography>
                            </Box>
                        )}
                    </>
                )}

                {tabValue === 1 && (
                    <>
                        <Typography variant='h6' gutterBottom>PIN Overview</Typography>
                        {reportLoading ? (
                            <Box display='flex' justifyContent='center' p={4}>
                                <CircularProgress />
                            </Box>
                        ) : report ? (
                            <Grid container spacing={3}>
                                <Grid item xs={12} sm={6} md={3}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant='h4' color='primary'>{report.Total}</Typography>
                                            <Typography variant='body2' color='text.secondary'>Total PINs</Typography>
                                        </CardContent>
                                    </Card>
                                </Grid>
                                <Grid item xs={12} sm={6} md={3}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant='h4' color='success.main'>{report.Active}</Typography>
                                            <Typography variant='body2' color='text.secondary'>Active PINs</Typography>
                                        </CardContent>
                                    </Card>
                                </Grid>
                                <Grid item xs={12} sm={6} md={3}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant='h4' color='error.main'>{report.Expired}</Typography>
                                            <Typography variant='body2' color='text.secondary'>Expired PINs</Typography>
                                        </CardContent>
                                    </Card>
                                </Grid>
                                <Grid item xs={12} sm={6} md={3}>
                                    <Card>
                                        <CardContent>
                                            <Typography variant='h4' color='info.main'>
                                                {report.Total > 0 ? Math.round((report.Active / report.Total) * 100) : 0}%
                                            </Typography>
                                            <Typography variant='body2' color='text.secondary'>Active Rate</Typography>
                                        </CardContent>
                                    </Card>
                                </Grid>
                                
                                <Grid item xs={12}>
                                    <Typography variant='h6' gutterBottom sx={{ mt: 2 }}>By Subscription Type</Typography>
                                    <Table>
                                        <TableHead>
                                            <TableRow>
                                                <TableCell>Subscription Type</TableCell>
                                                <TableCell align='right'>Total</TableCell>
                                                <TableCell align='right'>Active</TableCell>
                                                <TableCell align='right'>Expired</TableCell>
                                            </TableRow>
                                        </TableHead>
                                        <TableBody>
                                            {report.ByType?.map((type, index) => (
                                                <TableRow key={index}>
                                                    <TableCell>{getSubscriptionTypeText(type.SubscriptionType)}</TableCell>
                                                    <TableCell align='right'>{type.Total}</TableCell>
                                                    <TableCell align='right'>{type.Active}</TableCell>
                                                    <TableCell align='right'>{type.Expired}</TableCell>
                                                </TableRow>
                                            )) || []}
                                        </TableBody>
                                    </Table>
                                </Grid>
                            </Grid>
                        ) : (
                            <Alert severity='error'>Failed to load PIN report</Alert>
                        )}
                    </>
                )}

                <Dialog open={generatedPins !== null} onClose={handleDialogClose} maxWidth='sm' fullWidth>
                    <DialogTitle>Generated PINs</DialogTitle>
                    <DialogContent>
                        <Typography variant='body2' color='text.secondary' gutterBottom>
                            The following PINs have been generated and users created:
                        </Typography>
                        <List>
                            {generatedPins?.map((pin, index) => (
                                <ListItem key={index}>
                                    <Typography variant='body1' fontFamily='monospace'>{pin}</Typography>
                                </ListItem>
                            ))}
                        </List>
                    </DialogContent>
                </Dialog>
            </Box>
        </Page>
    );
};