import React, { useMemo, useState } from 'react';

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

    const content = useMemo(() => (
        <div className="padded page type-interior">
            <h2>Generate PINs</h2>
            <div className="inputContainer">
                <label htmlFor="pin-count">Count</label>
                <input id="pin-count" type="number" min={1} max={1000} value={count} onChange={(e) => setCount(parseInt(e.target.value || '0', 10))} />
            </div>
            <div className="inputContainer">
                <label htmlFor="pin-subscription">Subscription Type</label>
                <select id="pin-subscription" value={subscriptionType} onChange={(e) => setSubscriptionType(parseInt(e.target.value, 10))}>
                    {subscriptionOptions.map(o => (
                        <option key={o.value} value={o.value}>{o.label}</option>
                    ))}
                </select>
            </div>
            <div style={{ marginTop: '.5rem' }}>
                <button
                    is="emby-button"
                    className="raised"
                    disabled={loading || count < 1}
                    onClick={async () => {
                        setError(null);
                        setLoading(true);
                        try {
                            // @ts-ignore - global Dashboard provides apiClient via page context
                            const apiClient = window.Dashboard?.getCurrentApiClient?.() || window.ServerConnections?.getApiClient?.();
                            const result: string[] = await apiClient.generatePins(count, subscriptionType);
                            setPins(result || []);
                        } catch (e: any) {
                            setError(e?.message || 'Failed to generate PINs');
                        } finally {
                            setLoading(false);
                        }
                    }}
                >Generate</button>
            </div>

            {error && <div className="warning" style={{ marginTop: '.5rem' }}>{error}</div>}

            {pins.length > 0 && (
                <div style={{ marginTop: '1rem' }}>
                    <h3>Generated PINs</h3>
                    <pre>{pins.join('\n')}</pre>
                    <button
                        is="emby-button"
                        className="raised"
                        onClick={() => {
                            const blob = new Blob([pins.join('\n')], { type: 'text/plain' });
                            const url = URL.createObjectURL(blob);
                            const a = document.createElement('a');
                            a.href = url;
                            a.download = 'pins.txt';
                            a.click();
                            URL.revokeObjectURL(url);
                        }}
                    >Download</button>
                </div>
            )}
        </div>
    ), [count, subscriptionType, pins, loading, error]);

    return content as any;
}


