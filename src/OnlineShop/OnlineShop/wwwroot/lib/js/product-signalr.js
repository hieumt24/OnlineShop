// product-signalr.js - File này cần được tạo hoặc cập nhật
class ProductSignalR {
    constructor() {
        this.connection = null;
        this.isConnected = false;
        this.userName = document.querySelector('[data-username]')?.getAttribute('data-username') || 'Anonymous';
        this.initialize();
    }

    async initialize() {
        try {
            // Tạo connection
            this.connection = new signalR.HubConnectionBuilder()
                .withUrl("/productHub", {
                    transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
                    skipNegotiation: false
                })
                .withAutomaticReconnect([0, 2000, 10000, 30000])
                .configureLogging(signalR.LogLevel.Information)
                .build();

            // Setup event handlers
            this.setupEventHandlers();

            // Start connection
            await this.startConnection();
        } catch (error) {
            console.error('SignalR initialization error:', error);
            this.updateConnectionStatus('disconnected');
        }
    }

    setupEventHandlers() {
        // Connection events
        this.connection.onclose(() => {
            this.isConnected = false;
            this.updateConnectionStatus('disconnected');
        });

        this.connection.onreconnecting(() => {
            this.isConnected = false;
            this.updateConnectionStatus('connecting');
        });

        this.connection.onreconnected(() => {
            this.isConnected = true;
            this.updateConnectionStatus('connected');
        });

        // Hub events
        this.connection.on("StatisticsUpdated", (data) => {
            console.log('Statistics received:', data);
            this.updateStatistics(data);
        });

        this.connection.on("ActiveUsersUpdated", (data) => {
            console.log('Active users updated:', data);
            this.updateActiveUsers(data.ActiveUsers);
        });

        this.connection.on("UserConnected", (data) => {
            console.log('User connected:', data);
            this.updateActiveUsers(data.TotalUsers);
            this.addActivityItem(`${data.UserName || 'User'} connected`, 'success');
        });

        this.connection.on("UserDisconnected", (data) => {
            console.log('User disconnected:', data);
            this.updateActiveUsers(data.TotalUsers);
            this.addActivityItem(`${data.UserName || 'User'} disconnected`, 'warning');
        });

        this.connection.on("ProductCreated", (data) => {
            this.addActivityItem(`New product created: ${data.Title}`, 'success');
            this.showNotification(`Product "${data.Title}" was created`, 'success');
        });

        this.connection.on("ProductUpdated", (data) => {
            this.addActivityItem(`Product updated: ${data.Title}`, 'info');
            this.showNotification(`Product "${data.Title}" was updated`, 'info');
        });

        this.connection.on("ProductDeleted", (data) => {
            this.addActivityItem(`Product deleted: ${data.Title}`, 'danger');
            this.showNotification(`Product "${data.Title}" was deleted`, 'warning');
        });

        this.connection.on("QuantityUpdated", (data) => {
            this.addActivityItem(`Quantity updated for product ID: ${data.ProductId}`, 'info');
            this.updateQuantityField(data.ProductId, data.NewQuantity);
        });

        this.connection.on("LoadingNotification", (data) => {
            this.addActivityItem(`${data.User}: ${data.Message}`, 'info');
        });
    }

    async startConnection() {
        try {
            await this.connection.start();
            this.isConnected = true;
            this.updateConnectionStatus('connected');
            console.log('SignalR Connected');
        } catch (error) {
            console.error('SignalR Connection Error:', error);
            this.updateConnectionStatus('disconnected');

            // Retry after 5 seconds
            setTimeout(() => this.startConnection(), 5000);
        }
    }

    updateConnectionStatus(status) {
        const statusElement = document.getElementById('connectionStatus');
        const badge = document.getElementById('connectionBadge');

        if (statusElement) {
            statusElement.className = `connection-status ${status}`;

            switch (status) {
                case 'connected':
                    statusElement.innerHTML = '<i class="fas fa-circle"></i> Connected';
                    if (badge) badge.className = 'badge bg-success';
                    if (badge) badge.textContent = 'Connected';
                    break;
                case 'connecting':
                    statusElement.innerHTML = '<i class="fas fa-circle"></i> Connecting...';
                    if (badge) badge.className = 'badge bg-warning';
                    if (badge) badge.textContent = 'Connecting';
                    break;
                case 'disconnected':
                    statusElement.innerHTML = '<i class="fas fa-circle"></i> Disconnected';
                    if (badge) badge.className = 'badge bg-danger';
                    if (badge) badge.textContent = 'Disconnected';
                    break;
            }
        }
    }

    updateStatistics(data) {
        const totalProductsElement = document.getElementById('totalProducts');
        const activeUsersElement = document.getElementById('activeUsers');

        if (totalProductsElement) {
            totalProductsElement.textContent = data.TotalProducts || 0;
        }

        if (activeUsersElement) {
            activeUsersElement.textContent = data.ActiveUsers || 1;
        }
    }

    updateActiveUsers(count) {
        const activeUsersElement = document.getElementById('activeUsers');
        if (activeUsersElement) {
            activeUsersElement.textContent = count || 1;
        }
    }

    addActivityItem(message, type = 'info') {
        const activityFeed = document.getElementById('activityFeed');
        if (!activityFeed) return;

        const iconClass = {
            success: 'fas fa-check-circle text-success',
            info: 'fas fa-info-circle text-info',
            warning: 'fas fa-exclamation-triangle text-warning',
            danger: 'fas fa-times-circle text-danger'
        }[type] || 'fas fa-circle text-info';

        const activityItem = document.createElement('div');
        activityItem.className = 'activity-item';
        activityItem.innerHTML = `
            <i class="${iconClass}"></i>
            <span>${message}</span>
            <small class="text-muted ms-auto">${new Date().toLocaleTimeString()}</small>
        `;

        activityFeed.insertBefore(activityItem, activityFeed.firstChild);

        // Limit to 10 items
        while (activityFeed.children.length > 10) {
            activityFeed.removeChild(activityFeed.lastChild);
        }
    }

    showNotification(message, type = 'info') {
        const container = document.getElementById('notificationContainer');
        if (!container) return;

        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'error' ? 'danger' : type} alert-dismissible fade show notification-item`;
        notification.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
        `;

        container.appendChild(notification);

        // Auto remove after 5 seconds
        setTimeout(() => {
            if (notification.parentNode) {
                notification.remove();
            }
        }, 5000);
    }

    updateQuantityField(productId, newQuantity) {
        // Update quantity fields for specific product
        const quantityInputs = document.querySelectorAll(`[data-product-id="${productId}"] input[name="Qty"]`);
        quantityInputs.forEach(input => {
            input.value = newQuantity;
            input.classList.add('field-updated');
            setTimeout(() => input.classList.remove('field-updated'), 2000);
        });
    }
}

// Initialize SignalR when page loads
document.addEventListener('DOMContentLoaded', function() {
    window.productSignalR = new ProductSignalR();
});

// CSS cho notifications
const style = document.createElement('style');
style.textContent = `
    .notification-container {
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 9999;
        max-width: 350px;
    }
    
    .notification-item {
        margin-bottom: 10px;
        animation: slideInRight 0.3s ease;
    }
    
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes fieldPulse {
        0% { transform: scale(1); }
        50% { transform: scale(1.02); }
        100% { transform: scale(1); }
    }
`;
document.head.appendChild(style);