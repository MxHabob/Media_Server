import { useEffect } from 'react';
import { useApi } from 'hooks/useApi';
import { queryClient } from 'utils/query/queryClient';
import { ApiClient } from 'jellyfin-apiclient';
import Events, { Event } from 'utils/events';
import serverNotifications from 'scripts/serverNotifications';
import { SessionMessageType } from '@jellyfin/sdk/lib/generated-client/models/session-message-type';

/**
 * Hook for real-time PIN updates via WebSocket
 */
const useLivePinUpdates = () => {
    const { __legacyApiClient__ } = useApi();

    useEffect(() => {
        const onPinBatchCreated = (_e: Event, _apiClient: ApiClient, data: any) => {
            console.log('PIN batch created:', data);
            // Invalidate PIN batch queries to refresh the list
            void queryClient.invalidateQueries({
                queryKey: ['PinBatches']
            });
            void queryClient.invalidateQueries({
                queryKey: ['Subscriptions', 'Configurations']
            });
        };

        const onPinBatchDeleted = (_e: Event, _apiClient: ApiClient, data: any) => {
            console.log('PIN batch deleted:', data);
            // Invalidate PIN batch queries to refresh the list
            void queryClient.invalidateQueries({
                queryKey: ['PinBatches']
            });
            void queryClient.invalidateQueries({
                queryKey: ['Subscriptions', 'Configurations']
            });
        };

        const onPinUsed = (_e: Event, _apiClient: ApiClient, data: any) => {
            console.log('PIN used:', data);
            // Invalidate PIN batch queries to update usage statistics
            void queryClient.invalidateQueries({
                queryKey: ['PinBatches']
            });
        };

        const onPinExpired = (_e: Event, _apiClient: ApiClient, data: any) => {
            console.log('PIN expired:', data);
            // Invalidate PIN batch queries to update status
            void queryClient.invalidateQueries({
                queryKey: ['PinBatches']
            });
        };

        // Register event listeners for PIN-related events
        Events.on(serverNotifications, 'PinBatchCreated', onPinBatchCreated);
        Events.on(serverNotifications, 'PinBatchDeleted', onPinBatchDeleted);
        Events.on(serverNotifications, 'PinUsed', onPinUsed);
        Events.on(serverNotifications, 'PinExpired', onPinExpired);

        // Start listening for PIN events via WebSocket
        __legacyApiClient__?.sendMessage(SessionMessageType.PinEventsStart, '1000,1000');

        return () => {
            // Clean up event listeners
            Events.off(serverNotifications, 'PinBatchCreated', onPinBatchCreated);
            Events.off(serverNotifications, 'PinBatchDeleted', onPinBatchDeleted);
            Events.off(serverNotifications, 'PinUsed', onPinUsed);
            Events.off(serverNotifications, 'PinExpired', onPinExpired);

            // Stop listening for PIN events
            __legacyApiClient__?.sendMessage(SessionMessageType.PinEventsStop, null);
        };
    }, [__legacyApiClient__]);
};

export default useLivePinUpdates;
