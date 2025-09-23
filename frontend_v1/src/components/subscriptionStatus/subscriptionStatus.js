import DOMPurify from 'dompurify';
import { ApiClient } from 'jellyfin-apiclient';

/**
 * Subscription Status Component
 * Displays subscription information and expiration warnings
 */
class SubscriptionStatus {
    constructor() {
        this.apiClient = null;
        this.currentUser = null;
        this.statusElement = null;
        this.warningThreshold = 24; // Hours before expiration to show warning
    }

    /**
     * Initialize the subscription status component
     * @param {ApiClient} apiClient - The API client instance
     * @param {Object} user - The current user object
     */
    init(apiClient, user) {
        this.apiClient = apiClient;
        this.currentUser = user;
        this.createStatusElement();
        this.updateStatus();
    }

    /**
     * Create the subscription status element
     */
    createStatusElement() {
        if (this.statusElement) {
            return;
        }

        this.statusElement = document.createElement('div');
        this.statusElement.className = 'subscription-status';
        this.statusElement.innerHTML = this.getStatusHTML();
        
        // Add to the main header or navigation area
        const header = document.querySelector('.mainDrawerHeader') || document.querySelector('.header');
        if (header) {
            header.appendChild(this.statusElement);
        }
    }

    /**
     * Get the HTML for the subscription status
     */
    getStatusHTML() {
        if (!this.currentUser || !this.currentUser.SubscriptionType) {
            return '';
        }

        const subscriptionType = this.getSubscriptionTypeName(this.currentUser.SubscriptionType);
        const expirationDate = this.currentUser.SubscriptionExpirationDate;
        const isExpired = expirationDate && new Date(expirationDate) < new Date();
        const isExpiringSoon = this.isExpiringSoon(expirationDate);

        let statusClass = 'subscription-active';
        let statusIcon = 'verified_user';
        let statusText = 'Active';
        let warningMessage = '';

        if (isExpired) {
            statusClass = 'subscription-expired';
            statusIcon = 'error';
            statusText = 'Expired';
            warningMessage = 'Your subscription has expired. Please renew to continue accessing the service.';
        } else if (isExpiringSoon) {
            statusClass = 'subscription-expiring';
            statusIcon = 'warning';
            statusText = 'Expiring Soon';
            const hoursLeft = this.getHoursUntilExpiration(expirationDate);
            warningMessage = `Your subscription expires in ${hoursLeft} hours. Consider renewing soon.`;
        }

        return `
            <div class="subscription-status-container ${statusClass}">
                <div class="subscription-info">
                    <i class="md-icon subscription-icon">${statusIcon}</i>
                    <div class="subscription-details">
                        <div class="subscription-type">${subscriptionType}</div>
                        <div class="subscription-status-text">${statusText}</div>
                        ${expirationDate ? `<div class="subscription-expiry">Expires: ${this.formatDate(expirationDate)}</div>` : ''}
                    </div>
                </div>
                ${warningMessage ? `
                    <div class="subscription-warning">
                        <i class="md-icon">info</i>
                        <span>${warningMessage}</span>
                        <button class="subscription-renew-btn" onclick="subscriptionStatus.showRenewalOptions()">
                            <i class="md-icon">refresh</i>
                            Renew
                        </button>
                    </div>
                ` : ''}
            </div>
        `;
    }

    /**
     * Update the subscription status display
     */
    updateStatus() {
        if (!this.statusElement) {
            return;
        }

        this.statusElement.innerHTML = this.getStatusHTML();
        this.addStatusStyles();
    }

    /**
     * Add CSS styles for the subscription status
     */
    addStatusStyles() {
        if (document.getElementById('subscription-status-styles')) {
            return;
        }

        const style = document.createElement('style');
        style.id = 'subscription-status-styles';
        style.textContent = `
            .subscription-status {
                margin: 10px 0;
            }

            .subscription-status-container {
                background: #f8f9fa;
                border-radius: 8px;
                padding: 12px;
                border-left: 4px solid #28a745;
                transition: all 0.3s ease;
            }

            .subscription-status-container.subscription-expired {
                background: #f8d7da;
                border-left-color: #dc3545;
            }

            .subscription-status-container.subscription-expiring {
                background: #fff3cd;
                border-left-color: #ffc107;
            }

            .subscription-info {
                display: flex;
                align-items: center;
                gap: 12px;
            }

            .subscription-icon {
                font-size: 20px;
                color: #28a745;
            }

            .subscription-expired .subscription-icon {
                color: #dc3545;
            }

            .subscription-expiring .subscription-icon {
                color: #ffc107;
            }

            .subscription-details {
                flex: 1;
            }

            .subscription-type {
                font-weight: 600;
                color: #2d3748;
                font-size: 14px;
            }

            .subscription-status-text {
                color: #4a5568;
                font-size: 12px;
                margin-top: 2px;
            }

            .subscription-expiry {
                color: #718096;
                font-size: 11px;
                margin-top: 2px;
            }

            .subscription-warning {
                margin-top: 10px;
                padding: 8px 12px;
                background: rgba(0, 0, 0, 0.05);
                border-radius: 6px;
                display: flex;
                align-items: center;
                gap: 8px;
                font-size: 13px;
                color: #4a5568;
            }

            .subscription-renew-btn {
                margin-left: auto;
                background: #667eea;
                color: white;
                border: none;
                border-radius: 4px;
                padding: 4px 8px;
                font-size: 11px;
                cursor: pointer;
                display: flex;
                align-items: center;
                gap: 4px;
                transition: background 0.3s ease;
            }

            .subscription-renew-btn:hover {
                background: #5a67d8;
            }

            .subscription-renew-btn i {
                font-size: 12px;
            }

            /* Dark mode support */
            @media (prefers-color-scheme: dark) {
                .subscription-status-container {
                    background: #2d3748;
                    color: #e2e8f0;
                }

                .subscription-status-container.subscription-expired {
                    background: #4a1a1a;
                }

                .subscription-status-container.subscription-expiring {
                    background: #4a3a1a;
                }

                .subscription-type {
                    color: #e2e8f0;
                }

                .subscription-status-text {
                    color: #a0aec0;
                }

                .subscription-expiry {
                    color: #718096;
                }

                .subscription-warning {
                    background: rgba(255, 255, 255, 0.1);
                    color: #a0aec0;
                }
            }
        `;

        document.head.appendChild(style);
    }

    /**
     * Get subscription type name
     */
    getSubscriptionTypeName(type) {
        const typeNames = {
            0: 'None',
            1: '6 Hours',
            2: '12 Hours',
            3: 'Daily',
            4: 'Weekly',
            5: 'Monthly',
            6: 'Quarterly',
            7: 'Yearly',
            8: 'Lifetime',
            9: 'Custom'
        };
        return typeNames[type] || 'Unknown';
    }

    /**
     * Check if subscription is expiring soon
     */
    isExpiringSoon(expirationDate) {
        if (!expirationDate) {
            return false;
        }

        const now = new Date();
        const expiry = new Date(expirationDate);
        const hoursUntilExpiry = (expiry - now) / (1000 * 60 * 60);

        return hoursUntilExpiry <= this.warningThreshold && hoursUntilExpiry > 0;
    }

    /**
     * Get hours until expiration
     */
    getHoursUntilExpiration(expirationDate) {
        if (!expirationDate) {
            return 0;
        }

        const now = new Date();
        const expiry = new Date(expirationDate);
        return Math.ceil((expiry - now) / (1000 * 60 * 60));
    }

    /**
     * Format date for display
     */
    formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    /**
     * Show renewal options
     */
    showRenewalOptions() {
        // This would typically open a modal or navigate to a renewal page
        console.log('Show renewal options for user:', this.currentUser);
        
        // For now, just show an alert
        alert('Renewal options would be displayed here. This feature can be extended to show available subscription plans and payment options.');
    }

    /**
     * Destroy the subscription status component
     */
    destroy() {
        if (this.statusElement) {
            this.statusElement.remove();
            this.statusElement = null;
        }

        const styles = document.getElementById('subscription-status-styles');
        if (styles) {
            styles.remove();
        }
    }
}

// Create a global instance
const subscriptionStatus = new SubscriptionStatus();

export default subscriptionStatus;
