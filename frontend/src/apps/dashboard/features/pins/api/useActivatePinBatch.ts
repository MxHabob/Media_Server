import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

const activatePinBatch = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Activate`)
    });
};

export const useActivatePinBatch = () => {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: ({ apiClient, batchId }: { apiClient: ApiClient; batchId: string }) => 
            activatePinBatch(apiClient, batchId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['PinBatches'] });
        }
    });
};
