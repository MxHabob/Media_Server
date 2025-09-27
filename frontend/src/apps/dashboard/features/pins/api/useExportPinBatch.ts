import { useMutation } from '@tanstack/react-query';
import type { ApiClient } from 'jellyfin-apiclient';

const exportPinBatch = async (apiClient: ApiClient, batchId: string, includeOriginalPins: boolean = false): Promise<void> => {
    const params = new URLSearchParams();
    if (includeOriginalPins) params.append('includeOriginalPins', 'true');
    
    const url = `/PinBatches/${batchId}/Export${params.toString() ? `?${params.toString()}` : ''}`;
    
    const response = await fetch(apiClient.getUrl(url), {
        method: 'GET',
        headers: {
            'Authorization': `MediaBrowser Token="${apiClient.accessToken()}"`
        }
    });

    if (!response.ok) {
        throw new Error('Export failed');
    }

    const blob = await response.blob();
    const downloadUrl = window.URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = downloadUrl;
    link.download = `PIN_Batch_${batchId}_${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.xlsx`;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
    window.URL.revokeObjectURL(downloadUrl);
};

export const useExportPinBatch = () => {
    return useMutation({
        mutationFn: ({ apiClient, batchId, includeOriginalPins }: { 
            apiClient: ApiClient; 
            batchId: string; 
            includeOriginalPins?: boolean 
        }) => exportPinBatch(apiClient, batchId, includeOriginalPins)
    });
};
