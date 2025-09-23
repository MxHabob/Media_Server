import { ApiClient } from 'jellyfin-apiclient';
import { ServerConnections } from 'lib/jellyfin-apiclient';
import globalize from 'globalize';

/**
 * Subscription Management Controller
 */
export default function (view, params) {
    let apiClient;
    let currentTab = 'configurations';
    let configurations = [];
    let users = [];
    let statistics = null;

    /**
     * Initialize the controller
     */
    function init() {
        apiClient = ServerConnections.getApiClient();
        setupEventListeners();
        loadStatistics();
        loadConfigurations();
        loadUsers();
        loadExpiringUsers();
    }

    /**
     * Setup event listeners
     */
    function setupEventListeners() {
        // Tab switching
        view.querySelectorAll('.tab-button').forEach(button => {
            button.addEventListener('click', function() {
                const tab = this.getAttribute('data-tab');
                switchTab(tab);
            });
        });

        // Action buttons
        view.querySelector('#btnCreateConfiguration').addEventListener('click', showConfigurationModal);
        view.querySelector('#btnCreateUser').addEventListener('click', showUserModal);
        view.querySelector('#btnBulkOperations').addEventListener('click', showBulkOperations);
        view.querySelector('#btnExportData').addEventListener('click', exportData);

        // Modal controls
        setupModalControls();

        // Search and filter
        view.querySelector('#configSearch').addEventListener('input', filterConfigurations);
        view.querySelector('#configFilter').addEventListener('change', filterConfigurations);
        view.querySelector('#userSearch').addEventListener('input', filterUsers);
        view.querySelector('#userFilter').addEventListener('change', filterUsers);
        view.querySelector('#expiringFilter').addEventListener('change', loadExpiringUsers);

        // Form submissions
        view.querySelector('#configurationForm').addEventListener('submit', saveConfiguration);
        view.querySelector('#userForm').addEventListener('submit', createUser);

        // Configuration type change
        view.querySelector('#configType').addEventListener('change', function() {
            const customGroup = view.querySelector('#customDurationGroup');
            if (this.value === '9') { // Custom
                customGroup.style.display = 'block';
            } else {
                customGroup.style.display = 'none';
            }
        });

        // User configuration change
        view.querySelector('#userConfiguration').addEventListener('change', function() {
            const customGroup = view.querySelector('#customDurationUserGroup');
            const selectedConfig = configurations.find(c => c.Id === this.value);
            if (selectedConfig && selectedConfig.SubscriptionType === 9) { // Custom
                customGroup.style.display = 'block';
            } else {
                customGroup.style.display = 'none';
            }
        });
    }

    /**
     * Setup modal controls
     */
    function setupModalControls() {
        // Configuration modal
        view.querySelector('#closeConfigurationModal').addEventListener('click', hideConfigurationModal);
        view.querySelector('#cancelConfiguration').addEventListener('click', hideConfigurationModal);
        view.querySelector('#saveConfiguration').addEventListener('click', function() {
            view.querySelector('#configurationForm').dispatchEvent(new Event('submit'));
        });

        // User modal
        view.querySelector('#closeUserModal').addEventListener('click', hideUserModal);
        view.querySelector('#cancelUser').addEventListener('click', hideUserModal);
        view.querySelector('#saveUser').addEventListener('click', function() {
            view.querySelector('#userForm').dispatchEvent(new Event('submit'));
        });

        // Close modals when clicking outside
        view.querySelectorAll('.modal').forEach(modal => {
            modal.addEventListener('click', function(e) {
                if (e.target === this) {
                    this.style.display = 'none';
                }
            });
        });
    }

    /**
     * Switch between tabs
     */
    function switchTab(tab) {
        currentTab = tab;

        // Update tab buttons
        view.querySelectorAll('.tab-button').forEach(button => {
            button.classList.remove('active');
        });
        view.querySelector(`[data-tab="${tab}"]`).classList.add('active');

        // Update tab panels
        view.querySelectorAll('.tab-panel').forEach(panel => {
            panel.classList.remove('active');
        });
        view.querySelector(`#${tab}-tab`).classList.add('active');

        // Load tab-specific data
        switch (tab) {
            case 'configurations':
                loadConfigurations();
                break;
            case 'users':
                loadUsers();
                break;
            case 'expiring':
                loadExpiringUsers();
                break;
            case 'analytics':
                loadAnalytics();
                break;
        }
    }

    /**
     * Load subscription statistics
     */
    async function loadStatistics() {
        try {
            const response = await apiClient.get('/Subscriptions/Statistics');
            statistics = response.data;
            updateStatisticsDisplay();
        } catch (error) {
            console.error('Failed to load statistics:', error);
        }
    }

    /**
     * Update statistics display
     */
    function updateStatisticsDisplay() {
        if (!statistics) return;

        view.querySelector('#totalActiveSubscriptions').textContent = statistics.TotalActiveSubscriptions || 0;
        view.querySelector('#totalExpiredSubscriptions').textContent = statistics.TotalExpiredSubscriptions || 0;
        view.querySelector('#totalLifetimeSubscriptions').textContent = statistics.TotalLifetimeSubscriptions || 0;
        view.querySelector('#totalRevenue').textContent = `$${(statistics.TotalRevenue || 0).toFixed(2)}`;
    }

    /**
     * Load subscription configurations
     */
    async function loadConfigurations() {
        try {
            const response = await apiClient.get('/Subscriptions/Configurations');
            configurations = response.data;
            renderConfigurations();
        } catch (error) {
            console.error('Failed to load configurations:', error);
        }
    }

    /**
     * Render configurations list
     */
    function renderConfigurations() {
        const container = view.querySelector('#configurationsList');
        const searchTerm = view.querySelector('#configSearch').value.toLowerCase();
        const filter = view.querySelector('#configFilter').value;

        let filteredConfigurations = configurations.filter(config => {
            const matchesSearch = config.Name.toLowerCase().includes(searchTerm) ||
                                (config.Description && config.Description.toLowerCase().includes(searchTerm));
            
            if (filter === 'active') return matchesSearch && config.IsActive;
            if (filter === 'inactive') return matchesSearch && !config.IsActive;
            return matchesSearch;
        });

        if (filteredConfigurations.length === 0) {
            container.innerHTML = '<div class="empty-state">No configurations found</div>';
            return;
        }

        const html = filteredConfigurations.map(config => `
            <div class="configuration-card">
                <div class="configuration-header">
                    <h3>${config.Name}</h3>
                    <div class="configuration-actions">
                        <button class="btn-icon" onclick="editConfiguration('${config.Id}')" title="Edit">
                            <i class="md-icon">edit</i>
                        </button>
                        <button class="btn-icon" onclick="deleteConfiguration('${config.Id}')" title="Delete">
                            <i class="md-icon">delete</i>
                        </button>
                    </div>
                </div>
                <div class="configuration-details">
                    <div class="configuration-info">
                        <span class="info-item">
                            <i class="md-icon">schedule</i>
                            ${getSubscriptionTypeName(config.SubscriptionType)}
                        </span>
                        <span class="info-item">
                            <i class="md-icon">people</i>
                            ${config.MaxConcurrentSessions} sessions
                        </span>
                        <span class="info-item">
                            <i class="md-icon">wifi</i>
                            ${config.AllowRemoteAccess ? 'Remote' : 'Local'} access
                        </span>
                        ${config.Price ? `<span class="info-item">
                            <i class="md-icon">attach_money</i>
                            $${config.Price} ${config.Currency || 'USD'}
                        </span>` : ''}
                    </div>
                    <div class="configuration-status">
                        <span class="status-badge ${config.IsActive ? 'active' : 'inactive'}">
                            ${config.IsActive ? 'Active' : 'Inactive'}
                        </span>
                    </div>
                </div>
                ${config.Description ? `<div class="configuration-description">${config.Description}</div>` : ''}
            </div>
        `).join('');

        container.innerHTML = html;
    }

    /**
     * Load users with subscriptions
     */
    async function loadUsers() {
        try {
            const response = await apiClient.get('/Users/Pins');
            users = response.data;
            renderUsers();
        } catch (error) {
            console.error('Failed to load users:', error);
        }
    }

    /**
     * Render users list
     */
    function renderUsers() {
        const container = view.querySelector('#usersList');
        const searchTerm = view.querySelector('#userSearch').value.toLowerCase();
        const filter = view.querySelector('#userFilter').value;

        let filteredUsers = users.filter(user => {
            const matchesSearch = user.Name.toLowerCase().includes(searchTerm);
            
            if (filter === 'active') return matchesSearch && isUserActive(user);
            if (filter === 'expired') return matchesSearch && isUserExpired(user);
            if (filter === 'lifetime') return matchesSearch && user.SubscriptionType === 8;
            return matchesSearch;
        });

        if (filteredUsers.length === 0) {
            container.innerHTML = '<div class="empty-state">No users found</div>';
            return;
        }

        const html = filteredUsers.map(user => `
            <div class="user-card">
                <div class="user-header">
                    <div class="user-avatar">
                        ${user.Name.charAt(0).toUpperCase()}
                    </div>
                    <div class="user-info">
                        <h3>${user.Name}</h3>
                        <p>${getSubscriptionTypeName(user.SubscriptionType)}</p>
                    </div>
                    <div class="user-actions">
                        <button class="btn-icon" onclick="extendUserSubscription('${user.Id}')" title="Extend">
                            <i class="md-icon">schedule</i>
                        </button>
                        <button class="btn-icon" onclick="updateUserSubscription('${user.Id}')" title="Update">
                            <i class="md-icon">edit</i>
                        </button>
                    </div>
                </div>
                <div class="user-details">
                    <div class="user-status">
                        <span class="status-badge ${getUserStatusClass(user)}">
                            ${getUserStatusText(user)}
                        </span>
                    </div>
                    ${user.SubscriptionExpirationDate ? `
                        <div class="user-expiry">
                            Expires: ${formatDate(user.SubscriptionExpirationDate)}
                        </div>
                    ` : ''}
                </div>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    /**
     * Load expiring users
     */
    async function loadExpiringUsers() {
        try {
            const hours = view.querySelector('#expiringFilter').value;
            const response = await apiClient.get(`/Subscriptions/Expiring?hoursBeforeExpiration=${hours}`);
            const expiringUsers = response.data;
            renderExpiringUsers(expiringUsers);
        } catch (error) {
            console.error('Failed to load expiring users:', error);
        }
    }

    /**
     * Render expiring users
     */
    function renderExpiringUsers(expiringUsers) {
        const container = view.querySelector('#expiringList');

        if (expiringUsers.length === 0) {
            container.innerHTML = '<div class="empty-state">No users expiring soon</div>';
            return;
        }

        const html = expiringUsers.map(user => `
            <div class="expiring-user-card">
                <div class="user-info">
                    <div class="user-avatar">
                        ${user.Name.charAt(0).toUpperCase()}
                    </div>
                    <div class="user-details">
                        <h3>${user.Name}</h3>
                        <p>${getSubscriptionTypeName(user.SubscriptionType)}</p>
                        <p class="expiry-warning">
                            Expires in ${getHoursUntilExpiration(user.SubscriptionExpirationDate)} hours
                        </p>
                    </div>
                </div>
                <div class="user-actions">
                    <button class="btn-primary" onclick="extendUserSubscription('${user.Id}')">
                        <i class="md-icon">schedule</i>
                        Extend
                    </button>
                    <button class="btn-secondary" onclick="notifyUser('${user.Id}')">
                        <i class="md-icon">notifications</i>
                        Notify
                    </button>
                </div>
            </div>
        `).join('');

        container.innerHTML = html;
    }

    /**
     * Load analytics data
     */
    async function loadAnalytics() {
        // This would load chart data and render analytics
        console.log('Loading analytics...');
    }

    /**
     * Show configuration modal
     */
    function showConfigurationModal() {
        view.querySelector('#configurationModalTitle').textContent = 'Create Configuration';
        view.querySelector('#configurationForm').reset();
        view.querySelector('#customDurationGroup').style.display = 'none';
        view.querySelector('#configurationModal').style.display = 'block';
    }

    /**
     * Hide configuration modal
     */
    function hideConfigurationModal() {
        view.querySelector('#configurationModal').style.display = 'none';
    }

    /**
     * Show user modal
     */
    function showUserModal() {
        // Load configurations for the dropdown
        const configSelect = view.querySelector('#userConfiguration');
        configSelect.innerHTML = configurations
            .filter(c => c.IsActive)
            .map(c => `<option value="${c.Id}">${c.Name}</option>`)
            .join('');

        view.querySelector('#userForm').reset();
        view.querySelector('#customDurationUserGroup').style.display = 'none';
        view.querySelector('#userModal').style.display = 'block';
    }

    /**
     * Hide user modal
     */
    function hideUserModal() {
        view.querySelector('#userModal').style.display = 'none';
    }

    /**
     * Save configuration
     */
    async function saveConfiguration(e) {
        e.preventDefault();

        const formData = {
            Name: view.querySelector('#configName').value,
            Description: view.querySelector('#configDescription').value,
            SubscriptionType: parseInt(view.querySelector('#configType').value),
            CustomDurationHours: view.querySelector('#customDuration').value ? parseInt(view.querySelector('#customDuration').value) : null,
            MaxConcurrentSessions: parseInt(view.querySelector('#maxSessions').value),
            AllowRemoteAccess: view.querySelector('#allowRemoteAccess').checked,
            MaxBitrate: view.querySelector('#maxBitrate').value ? parseInt(view.querySelector('#maxBitrate').value) : null,
            AllowTranscoding: view.querySelector('#allowTranscoding').checked,
            MaxParentalRating: view.querySelector('#maxParentalRating').value ? parseInt(view.querySelector('#maxParentalRating').value) : null,
            AllowDownload: view.querySelector('#allowDownload').checked,
            AllowSyncPlay: view.querySelector('#allowSyncPlay').checked,
            Price: view.querySelector('#price').value ? parseFloat(view.querySelector('#price').value) : null,
            Currency: view.querySelector('#currency').value || null,
            IsActive: view.querySelector('#isActive').checked,
            SortOrder: parseInt(view.querySelector('#sortOrder').value)
        };

        try {
            await apiClient.post('/Subscriptions/Configurations', formData);
            hideConfigurationModal();
            loadConfigurations();
            showNotification('Configuration created successfully', 'success');
        } catch (error) {
            console.error('Failed to save configuration:', error);
            showNotification('Failed to create configuration', 'error');
        }
    }

    /**
     * Create user with subscription
     */
    async function createUser(e) {
        e.preventDefault();

        const formData = {
            Username: view.querySelector('#userName').value,
            ConfigurationId: view.querySelector('#userConfiguration').value,
            CustomDurationHours: view.querySelector('#customDurationUser').value ? parseInt(view.querySelector('#customDurationUser').value) : null
        };

        try {
            const response = await apiClient.post('/Subscriptions/Users', formData);
            const result = response.data;
            
            hideUserModal();
            loadUsers();
            showNotification(`User created successfully. PIN: ${result.Pin}`, 'success');
        } catch (error) {
            console.error('Failed to create user:', error);
            showNotification('Failed to create user', 'error');
        }
    }

    /**
     * Filter configurations
     */
    function filterConfigurations() {
        renderConfigurations();
    }

    /**
     * Filter users
     */
    function filterUsers() {
        renderUsers();
    }

    /**
     * Show bulk operations
     */
    function showBulkOperations() {
        // Implement bulk operations modal
        console.log('Show bulk operations');
    }

    /**
     * Export data
     */
    function exportData() {
        // Implement data export
        console.log('Export data');
    }

    /**
     * Utility functions
     */
    function getSubscriptionTypeName(type) {
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

    function isUserActive(user) {
        if (!user.SubscriptionExpirationDate) return true;
        return new Date(user.SubscriptionExpirationDate) > new Date();
    }

    function isUserExpired(user) {
        if (!user.SubscriptionExpirationDate) return false;
        return new Date(user.SubscriptionExpirationDate) <= new Date();
    }

    function getUserStatusClass(user) {
        if (isUserExpired(user)) return 'expired';
        if (isUserActive(user)) return 'active';
        return 'inactive';
    }

    function getUserStatusText(user) {
        if (isUserExpired(user)) return 'Expired';
        if (isUserActive(user)) return 'Active';
        return 'Inactive';
    }

    function formatDate(dateString) {
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    function getHoursUntilExpiration(dateString) {
        const now = new Date();
        const expiry = new Date(dateString);
        return Math.ceil((expiry - now) / (1000 * 60 * 60));
    }

    function showNotification(message, type = 'info') {
        // Simple notification implementation
        const notification = document.createElement('div');
        notification.className = `notification notification-${type}`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            padding: 12px 20px;
            border-radius: 6px;
            color: white;
            font-weight: 500;
            z-index: 10000;
            animation: slideIn 0.3s ease;
        `;

        if (type === 'success') notification.style.background = '#28a745';
        if (type === 'error') notification.style.background = '#dc3545';
        if (type === 'info') notification.style.background = '#17a2b8';

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 5000);
    }

    // Global functions for inline event handlers
    window.editConfiguration = function(id) {
        console.log('Edit configuration:', id);
    };

    window.deleteConfiguration = function(id) {
        if (confirm('Are you sure you want to delete this configuration?')) {
            console.log('Delete configuration:', id);
        }
    };

    window.extendUserSubscription = function(id) {
        console.log('Extend user subscription:', id);
    };

    window.updateUserSubscription = function(id) {
        console.log('Update user subscription:', id);
    };

    window.notifyUser = function(id) {
        console.log('Notify user:', id);
    };

    // Initialize when the view is ready
    init();
}
