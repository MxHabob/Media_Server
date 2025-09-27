import { useMutation, useQueryClient } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';
import type { CreatePinBatchRequest } from '../../../types/pinBatch';

const createPinBatch = async (apiClient: ApiClient, request: CreatePinBatchRequest): Promise<void> => {
    await apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl('/PinBatches'),
        data: JSON.stringify(request),
        contentType: 'application/json'
    });
};

export const useCreatePinBatch = () => {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: ({ apiClient, request }: { apiClient: ApiClient; request: CreatePinBatchRequest }) =>
            createPinBatch(apiClient, request),
        onSuccess: () => {
            void queryClient.invalidateQueries({ queryKey: ['PinBatches'] });
        }
    });
};
