import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

const deleteAllBatchPins = async (apiClient: ApiClient, batchId: string): Promise<void> => {
    await apiClient.ajax({
        type: 'DELETE',
        url: apiClient.getUrl(`/PinBatches/${batchId}/Pins`)
    });
};

export const useDeleteAllBatchPins = () => {
    const queryClient = useQueryClient();
    
    return useMutation({
        mutationFn: ({ apiClient, batchId }: { apiClient: ApiClient; batchId: string }) => 
            deleteAllBatchPins(apiClient, batchId),
        onSuccess: () => {
            queryClient.invalidateQueries({ queryKey: ['PinBatches'] });
        }
    });
};
