/**
 * Centralized PIN batch types for the dashboard
 */

export type PinBatch = {
    Id: string;
    Name: string;
    Description?: string;
    SubscriptionType: number;
    PinPattern: number;
    PinLength: number;
    CustomCharacterSet?: string;
    TotalPins: number;
    UsedPins: number;
    ActivePins: number;
    ExpiredPins: number;
    Status: number;
    CreatedDate: string;
    ExpirationDate?: string;
    ModifiedDate?: string;
    CreatedByUserId: string;
    ModifiedByUserId?: string;
    MaxConcurrentSessions?: number;
    AllowRemoteAccess: boolean;
    MaxBitrate?: number;
    AllowTranscoding: boolean;
    MaxParentalRating?: number;
    AllowDownload: boolean;
    AllowSyncPlay: boolean;
    Price?: number;
    Currency?: string;
    Metadata?: string;
};

export type BatchStatus = 'Active' | 'Suspended' | 'Expired' | 'Deleted';

export type BatchStatistics = {
    TotalBatches: number;
    ActiveBatches: number;
    TotalPins: number;
    ActivePins: number;
    UsedPins: number;
    ExpiredPins: number;
};

export type PinBatchUser = {
    Id: string;
    BatchId: string;
    UserId?: string;
    IsActive: boolean;
    CreatedDate: string;
    FirstUsedDate?: string;
    LastUsedDate?: string;
    UsageCount: number;
    ExpirationDate?: string;
    DeactivatedDate?: string;
    DeactivationReason?: string;
    Metadata?: string;
    LastLoginIp?: string;
    LastLoginDevice?: string;
};

export type CreatePinBatchRequest = {
    Name: string;
    Description?: string;
    SubscriptionType: number;
    PinPattern: number;
    PinLength: number;
    PinCount: number;
    CustomCharacterSet?: string;
    ExpirationDate?: string;
    MaxConcurrentSessions?: number;
    AllowRemoteAccess: boolean;
    AllowTranscoding: boolean;
    AllowDownload: boolean;
    AllowSyncPlay: boolean;
    MaxBitrate?: number;
    MaxParentalRating?: number;
    Price?: number;
    Currency?: string;
    Metadata?: string;
};

export type UpdatePinBatchRequest = {
    Name?: string;
    Description?: string;
    Status?: number;
};

export type SubscriptionConfigurationDto = {
    Id?: string;
    Name: string;
    Description?: string;
    SubscriptionType: number;
    CustomDurationHours?: number;
    MaxConcurrentSessions: number;
    AllowRemoteAccess: boolean;
    MaxBitrate?: number;
    AllowTranscoding: boolean;
    MaxParentalRating?: number;
    AllowDownload: boolean;
    AllowSyncPlay: boolean;
    Price?: number;
    Currency?: string;
    IsActive: boolean;
    CreatedDate?: string;
    ModifiedDate?: string;
    CreatedByUserId?: string;
    ModifiedByUserId?: string;
};

/**
 * Utility functions for PIN batch operations
 */
export const PinBatchUtils = {
    /**
     * Calculates the time remaining until expiration
     * @param expirationDate The expiration date
     * @returns Time remaining in a human-readable format
     */
    getTimeRemaining: (expirationDate?: string): string => {
        if (!expirationDate) {
            return 'Lifetime';
        }

        const now = new Date();
        const expDate = new Date(expirationDate);
        const diffMs = expDate.getTime() - now.getTime();

        if (diffMs <= 0) {
            return 'Expired';
        }

        const days = Math.floor(diffMs / (1000 * 60 * 60 * 24));
        const hours = Math.floor((diffMs % (1000 * 60 * 60 * 24)) / (1000 * 60 * 60));
        const minutes = Math.floor((diffMs % (1000 * 60 * 60)) / (1000 * 60));

        if (days > 0) {
            return `${days}d ${hours}h ${minutes}m`;
        } else if (hours > 0) {
            return `${hours}h ${minutes}m`;
        } else {
            return `${minutes}m`;
        }
    },

    /**
     * Checks if a PIN is expired
     * @param expirationDate The expiration date
     * @returns True if expired, false otherwise
     */
    isExpired: (expirationDate?: string): boolean => {
        if (!expirationDate) {
            return false; // Lifetime PINs never expire
        }
        return new Date(expirationDate) <= new Date();
    },

    /**
     * Gets the status color for a PIN based on expiration
     * @param expirationDate The expiration date
     * @returns Color string for UI display
     */
    getStatusColor: (expirationDate?: string): 'success' | 'warning' | 'error' => {
        if (!expirationDate) {
            return 'success'; // Lifetime
        }

        const now = new Date();
        const expDate = new Date(expirationDate);
        const diffMs = expDate.getTime() - now.getTime();

        if (diffMs <= 0) {
            return 'error'; // Expired
        } else if (diffMs <= 24 * 60 * 60 * 1000) { // Less than 24 hours
            return 'warning'; // Expiring soon
        } else {
            return 'success'; // Active
        }
    }
};
