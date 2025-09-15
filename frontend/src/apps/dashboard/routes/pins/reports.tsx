import React, { useEffect, useMemo, useState } from 'react';

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
                // @ts-ignore
                const apiClient = window.Dashboard?.getCurrentApiClient?.() || window.ServerConnections?.getApiClient?.();
                const query: any = { status };
                if (subscriptionType !== '') query.subscriptionType = subscriptionType;
                const list: PinUser[] = await apiClient.getPinUsers(query);
                const rep = await apiClient.getPinReport();
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

    const content = useMemo(() => (
        <div className='padded page type-interior'>
            <h2>PIN Users</h2>
            <div style={{ display: 'flex', gap: '1rem', flexWrap: 'wrap' }}>
                <div className='inputContainer'>
                    <label htmlFor='pin-status'>Status</label>
                    <select id='pin-status' value={status} onChange={(e) => setStatus(e.target.value as any)}>
                        <option value='active'>Active</option>
                        <option value='expired'>Expired</option>
                        <option value='all'>All</option>
                    </select>
                </div>
                <div className='inputContainer'>
                    <label htmlFor='pin-type'>Subscription Type</label>
                    <select id='pin-type' value={subscriptionType} onChange={(e) => setSubscriptionType(e.target.value === '' ? '' : parseInt(e.target.value, 10))}>
                        <option value=''>All</option>
                        <option value={1}>Six Hours</option>
                        <option value={2}>Twelve Hours</option>
                        <option value={3}>Weekly</option>
                        <option value={4}>Monthly</option>
                        <option value={5}>Yearly</option>
                        <option value={6}>Lifetime</option>
                    </select>
                </div>
                <div style={{ alignSelf: 'flex-end' }}>
                    <button
                        is='emby-button'
                        className='raised'
                        disabled={loading}
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
                    >Export CSV</button>
                </div>
            </div>

            {error && <div className='warning' style={{ marginTop: '.5rem' }}>{error}</div>}

            {report && (
                <div style={{ marginTop: '1rem' }}>
                    <h3>Summary</h3>
                    <div>Total: {report.Total} • Active: {report.Active} • Expired: {report.Expired}</div>
                </div>
            )}

            <div style={{ marginTop: '1rem', overflowX: 'auto' }}>
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
            </div>
        </div>
    ), [status, subscriptionType, items, loading, error, report]);

    return content as any;
}

