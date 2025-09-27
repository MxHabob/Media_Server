import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

const suspendPinBatch = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Suspend`)
    });
};

export const useSuspendPinBatch = () => {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: ({ apiClient, batchId }: { apiClient: ApiClient; batchId: string }) => 
            suspendPinBatch(apiClient, batchId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['PinBatches'] });
        }
    });
};
