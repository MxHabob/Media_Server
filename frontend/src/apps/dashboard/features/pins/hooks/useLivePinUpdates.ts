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

        const onUserCreated = (_e: Event, _apiClient: ApiClient, data: any) => {
            console.log('User created:', data);
            // Invalidate user queries to refresh the users list
            void queryClient.invalidateQueries({
                queryKey: ['Users']
            });
        };

        const onUserDeleted = (_e: Event, _apiClient: ApiClient, data: any) => {
            console.log('User deleted:', data);
            // Invalidate user queries to refresh the users list
            void queryClient.invalidateQueries({
                queryKey: ['Users']
            });
        };

        // Register event listeners for PIN-related events
        Events.on(serverNotifications, 'PinBatchCreated', onPinBatchCreated);
        Events.on(serverNotifications, 'PinBatchDeleted', onPinBatchDeleted);
        Events.on(serverNotifications, 'PinUsed', onPinUsed);
        Events.on(serverNotifications, 'PinExpired', onPinExpired);
        
        // Register event listeners for user-related events
        Events.on(serverNotifications, 'UserCreated', onUserCreated);
        Events.on(serverNotifications, 'UserDeleted', onUserDeleted);

        // Start listening for PIN events via WebSocket
        // Note: PIN events are handled via general events for now
        // __legacyApiClient__?.sendMessage(SessionMessageType.PinEventsStart, '1000,1000');

        return () => {
            // Clean up event listeners
            Events.off(serverNotifications, 'PinBatchCreated', onPinBatchCreated);
            Events.off(serverNotifications, 'PinBatchDeleted', onPinBatchDeleted);
            Events.off(serverNotifications, 'PinUsed', onPinUsed);
            Events.off(serverNotifications, 'PinExpired', onPinExpired);
            Events.off(serverNotifications, 'UserCreated', onUserCreated);
            Events.off(serverNotifications, 'UserDeleted', onUserDeleted);

            // Stop listening for PIN events
            // Note: PIN events are handled via general events for now
            // __legacyApiClient__?.sendMessage(SessionMessageType.PinEventsStop, null);
        };
    }, [__legacyApiClient__]);
};

export default useLivePinUpdates;
