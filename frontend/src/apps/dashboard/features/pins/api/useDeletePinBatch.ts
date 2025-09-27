import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

const deletePinBatch = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'DELETE',
        url: apiClient.getUrl(`/PinBatches/${batchId}`)
    });
};

export const useDeletePinBatch = () => {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: ({ apiClient, batchId }: { apiClient: ApiClient; batchId: string }) => 
            deletePinBatch(apiClient, batchId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['PinBatches'] });
        }
    });
};
