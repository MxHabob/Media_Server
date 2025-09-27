import { useQuery } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

import type { PinBatch } from '../../../types/pinBatch';

const fetchPinBatches = async (apiClient: ApiClient, status?: number, subscriptionType?: number, createdByUserId?: string): Promise<PinBatch[]> => {
    const params = new URLSearchParams();
    if (status !== undefined) params.append('status', status.toString());
    if (subscriptionType !== undefined) params.append('subscriptionType', subscriptionType.toString());
    if (createdByUserId) params.append('createdByUserId', createdByUserId);

    const url = `/PinBatches${params.toString() ? `?${params.toString()}` : ''}`;
    const res = await apiClient.ajax({
        type: 'GET',
        url: apiClient.getUrl(url)
    });

    return res.json() as Promise<PinBatch[]>;
};

export const usePinBatches = (apiClient: ApiClient, status?: number, subscriptionType?: number, createdByUserId?: string) => {
    return useQuery({
        queryKey: ['PinBatches', status, subscriptionType, createdByUserId],
        queryFn: () => fetchPinBatches(apiClient, status, subscriptionType, createdByUserId)
    });
};
