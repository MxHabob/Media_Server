import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

const deactivateAllBatchPins = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Pins/Deactivate`)
    });
};

export const useDeactivateAllBatchPins = () => {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: ({ apiClient, batchId }: { apiClient: ApiClient; batchId: string }) => 
            deactivateAllBatchPins(apiClient, batchId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['PinBatches'] });
        }
    });
};
