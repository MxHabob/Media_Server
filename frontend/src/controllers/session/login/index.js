import DOMPurify from 'dompurify';
import markdownIt from 'markdown-it';

import { AppFeature } from 'constants/appFeature';
import { ServerConnections } from 'lib/jellyfin-apiclient';

import { appHost } from '../../../components/apphost';
import appSettings from '../../../scripts/settings/appSettings';
import dom from '../../../utils/dom';
import loading from '../../../components/loading/loading';
import layoutManager from '../../../components/layoutManager';
import libraryMenu from '../../../scripts/libraryMenu';
import browser from '../../../scripts/browser';
import globalize from '../../../lib/globalize';
import '../../../components/cardbuilder/card.scss';
import '../../../elements/emby-checkbox/emby-checkbox';
import Dashboard from '../../../utils/dashboard';
import toast from '../../../components/toast/toast';
import dialogHelper from '../../../components/dialogHelper/dialogHelper';
import prompt from '../../../components/prompt/prompt';
import baseAlert from '../../../components/alert';
import { getDefaultBackgroundClass } from '../../../components/cardbuilder/cardBuilderUtils';

import './login.scss';

const enableFocusTransform = !browser.slow && !browser.edge;

async function authenticateUserByName(page, apiClient, url, username, password) {
    loading.show();
    try {
        const result = await apiClient.authenticateUserByName(username, password);
        const user = result.User;
        loading.hide();

        await onLoginSuccessful(user.Id, result.AccessToken, apiClient, url);
    } catch (response) {
        page.querySelector('#txtManualPassword').value = '';
        loading.hide();

        const UnauthorizedOrForbidden = [401, 403];
        if (UnauthorizedOrForbidden.includes(response.status)) {
            const messageKey = response.status === 401 ? 'MessageInvalidUser' : 'MessageUnauthorizedUser';
            toast(globalize.translate(messageKey));
        } else {
            Dashboard.alert({
                message: globalize.translate('MessageUnableToConnectToServer'),
                title: globalize.translate('HeaderConnectionFailure')
            });
        }
    }
}

function authenticateQuickConnect(apiClient, targetUrl) {
    const url = apiClient.getUrl('/QuickConnect/Initiate');
    apiClient.ajax({ type: 'POST', url }, true).then(res => res.json()).then(function (json) {
        if (!json.Secret || !json.Code) {
            console.error('Malformed quick connect response', json);
            return false;
        }

        baseAlert({
            dialogOptions: {
                id: 'quickConnectAlert'
            },
            title: globalize.translate('QuickConnect'),
            text: globalize.translate('QuickConnectAuthorizeCode', json.Code)
        });

        const connectUrl = apiClient.getUrl('/QuickConnect/Connect?Secret=' + json.Secret);

        const interval = setInterval(function() {
            apiClient.getJSON(connectUrl).then(async function(data) {
                if (!data.Authenticated) {
                    return;
                }

                clearInterval(interval);

                // Close the QuickConnect dialog
                const dlg = document.getElementById('quickConnectAlert');
                if (dlg) {
                    dialogHelper.close(dlg);
                }

                const result = await apiClient.quickConnect(data.Secret);
                onLoginSuccessful(result.User.Id, result.AccessToken, apiClient, targetUrl);
            }, function (e) {
                clearInterval(interval);

                // Close the QuickConnect dialog
                const dlg = document.getElementById('quickConnectAlert');
                if (dlg) {
                    dialogHelper.close(dlg);
                }

                Dashboard.alert({
                    message: globalize.translate('QuickConnectDeactivated'),
                    title: globalize.translate('HeaderError')
                });

                console.error('Unable to login with quick connect', e);
            });
        }, 5000, connectUrl);

        return true;
    }, function(e) {
        Dashboard.alert({
            message: globalize.translate('QuickConnectNotActive'),
            title: globalize.translate('HeaderError')
        });

        console.error('Quick connect error: ', e);
        return false;
    });
}

async function authenticateWithPin(apiClient, targetUrl) {
    const pin = await prompt({
        title: globalize.translate('HeaderPinLogin'),
        label: globalize.translate('MessageEnterPin'),
        confirmText: globalize.translate('ButtonOk')
    }).catch(() => undefined);

    if (!pin) return;

    try {
        const result = await apiClient.ajax({
            type: 'POST',
            url: apiClient.getUrl('/Users/AuthenticateWithPin'),
            data: JSON.stringify({ Pin: pin }),
            contentType: 'application/json'
        }, true).then(r => r.json());

        // Wait for session to be fully established before navigation
        console.log('PIN authentication successful, proceeding with navigation');
        await onLoginSuccessfulWithDelay(result.User.Id, result.AccessToken, apiClient, targetUrl, result.User.SubscriptionExpirationDate);
    } catch (error) {
        console.error('PIN authentication failed:', error);
        Dashboard.alert({
            message: globalize.translate('MessageInvalidPin'),
            title: globalize.translate('HeaderError')
        });
    }
}

async function onLoginSuccessful(id, accessToken, apiClient, url) {
    Dashboard.onServerChanged(id, accessToken, apiClient);
    // Properly set authentication info on the API client
    apiClient.setAuthenticationInfo(accessToken, id);
    // Save credentials for session persistence
    await saveCredentialsForPinAuth(id, accessToken, apiClient);
    Dashboard.navigate(url || 'home');
}

async function saveCredentialsForPinAuth(userId, accessToken, apiClient, expirationDate = null) {
    try {
        // Get the current credentials
        const { ServerConnections: ServerConnectionsImport } = await import('../../../lib/jellyfin-apiclient');
        const credentialProvider = ServerConnectionsImport.credentialProvider();
        const credentials = credentialProvider.credentials();

        // Find the current server
        const server = credentials.Servers.find(s => s.Id === apiClient.serverId()) || apiClient.serverInfo();

        // Update server info with authentication details
        server.UserId = userId;
        server.AccessToken = accessToken;
        server.DateLastAccessed = new Date().getTime();

        // Store PIN expiration date if provided
        if (expirationDate) {
            server.PinExpirationDate = expirationDate;
        }

        // Update the credentials
        credentialProvider.addOrUpdateServer(credentials.Servers, server);
        credentialProvider.credentials(credentials);

        console.log('Credentials saved for PIN authentication');

        // Set up automatic logout when PIN expires
        if (expirationDate) {
            setupPinExpirationTimer(expirationDate);
        }
    } catch (error) {
        console.error('Failed to save credentials for PIN authentication:', error);
    }
}

function setupPinExpirationTimer(expirationDate) {
    if (!expirationDate) return;

    const expirationTime = new Date(expirationDate).getTime();
    const currentTime = new Date().getTime();
    const timeUntilExpiration = expirationTime - currentTime;

    if (timeUntilExpiration <= 0) {
        // PIN is already expired, logout immediately
        console.log('PIN has already expired, logging out');
        Dashboard.logout();
        return;
    }

    console.log(`PIN will expire in ${Math.round(timeUntilExpiration / 1000 / 60)} minutes`);

    // Set a timer to logout when the PIN expires
    setTimeout(() => {
        console.log('PIN has expired, logging out user');
        Dashboard.alert({
            message: globalize.translate('MessagePinExpired'),
            title: globalize.translate('HeaderSessionExpired')
        });
        Dashboard.logout();
    }, timeUntilExpiration);
}

async function checkPinExpirationOnLoad() {
    try {
        const { ServerConnections: ServerConnectionsImport } = await import('../../../lib/jellyfin-apiclient');
        const credentialProvider = ServerConnectionsImport.credentialProvider();
        const credentials = credentialProvider.credentials();

        // Check all servers for PIN expiration
        for (const server of credentials.Servers) {
            if (server.PinExpirationDate && server.UserId && server.AccessToken) {
                const expirationTime = new Date(server.PinExpirationDate).getTime();
                const currentTime = new Date().getTime();

                if (currentTime >= expirationTime) {
                    console.log('PIN has expired, clearing credentials');
                    // Clear expired credentials
                    server.UserId = null;
                    server.AccessToken = null;
                    server.PinExpirationDate = null;
                    credentialProvider.addOrUpdateServer(credentials.Servers, server);
                    credentialProvider.credentials(credentials);
                }
            }
        }
    } catch (error) {
        console.error('Failed to check PIN expiration on load:', error);
    }
}

async function onLoginSuccessfulWithDelay(id, accessToken, apiClient, url, expirationDate = null) {
    // Update the API client with new session info
    Dashboard.onServerChanged(id, accessToken, apiClient);

    // Properly set authentication info on the API client
    apiClient.setAuthenticationInfo(accessToken, id);

    // Save credentials to localStorage for session persistence
    await saveCredentialsForPinAuth(id, accessToken, apiClient, expirationDate);

    // Wait for session to be fully established on the backend
    await new Promise(resolve => setTimeout(resolve, 300));

    console.log('Proceeding with navigation after PIN authentication');

    // Navigate to the target URL
    try {
        await Dashboard.navigate(url || 'home');
    } catch (navError) {
        console.error('Navigation failed after PIN authentication:', navError);
        // Fallback - force page reload to home
        window.location.href = '/web/index.html#!/home';
    }
}

function showManualForm(context, showCancel, focusPassword) {
    context.querySelector('.chkRememberLogin').checked = appSettings.enableAutoLogin();
    context.querySelector('.manualLoginForm').classList.remove('hide');
    context.querySelector('.visualLoginForm').classList.add('hide');
    context.querySelector('.btnManual').classList.add('hide');

    if (focusPassword) {
        context.querySelector('#txtManualPassword').focus();
    } else {
        context.querySelector('#txtManualName').focus();
    }

    if (showCancel) {
        context.querySelector('.btnCancel').classList.remove('hide');
    } else {
        context.querySelector('.btnCancel').classList.add('hide');
    }
}

function loadUserList(context, apiClient, users) {
    let html = '';

    for (const user of users) {
        // TODO move card creation code to Card component
        let cssClass = 'card squareCard scalableCard squareCard-scalable';

        if (layoutManager.tv) {
            cssClass += ' show-focus';

            if (enableFocusTransform) {
                cssClass += ' show-animation';
            }
        }

        const cardBoxCssClass = 'cardBox cardBox-bottompadded';
        html += '<button type="button" class="' + cssClass + '">';
        html += '<div class="' + cardBoxCssClass + '">';
        html += '<div class="cardScalable">';
        html += '<div class="cardPadder cardPadder-square"></div>';
        html += `<div class="cardContent" data-haspw="${user.HasPassword}" data-username="${user.Name}" data-userid="${user.Id}">`;
        let imgUrl;

        if (user.PrimaryImageTag) {
            imgUrl = apiClient.getUserImageUrl(user.Id, {
                width: 300,
                tag: user.PrimaryImageTag,
                type: 'Primary'
            });

            html += '<div class="cardImageContainer coveredImage" style="background-image:url(\'' + imgUrl + "');\"></div>";
        } else {
            html += `<div class="cardImage flex align-items-center justify-content-center ${getDefaultBackgroundClass()}">`;
            html += '<span class="material-icons cardImageIcon person" aria-hidden="true"></span>';
            html += '</div>';
        }

        html += '</div>';
        html += '</div>';
        html += '<div class="cardFooter visualCardBox-cardFooter">';
        html += '<div class="cardText singleCardText cardTextCentered">' + user.Name + '</div>';
        html += '</div>';
        html += '</div>';
        html += '</button>';
    }

    context.querySelector('#divUsers').innerHTML = html;
}

export default function (view, params) {
    // Check for PIN expiration on page load
    checkPinExpirationOnLoad();

    function getApiClient() {
        const serverId = params.serverid;

        if (serverId) {
            return ServerConnections.getOrCreateApiClient(serverId);
        }

        return ApiClient;
    }

    function getTargetUrl() {
        if (params.url) {
            try {
                return decodeURIComponent(params.url);
            } catch (err) {
                console.warn('[LoginPage] unable to decode url param', params.url, err);
            }
        }

        return '/home';
    }

    function showVisualForm() {
        view.querySelector('.visualLoginForm').classList.remove('hide');
        view.querySelector('.manualLoginForm').classList.add('hide');
        view.querySelector('.btnManual').classList.remove('hide');

        import('../../../components/autoFocuser').then(({ default: autoFocuser }) => {
            autoFocuser.autoFocus(view);
        });
    }

    view.querySelector('#divUsers').addEventListener('click', function (e) {
        const card = dom.parentWithClass(e.target, 'card');
        const cardContent = card ? card.querySelector('.cardContent') : null;

        if (cardContent) {
            const context = view;
            const id = cardContent.getAttribute('data-userid');
            const name = cardContent.getAttribute('data-username');
            const haspw = cardContent.getAttribute('data-haspw');

            if (id === 'manual') {
                context.querySelector('#txtManualName').value = '';
                showManualForm(context, true);
            } else if (haspw == 'false') {
                authenticateUserByName(context, getApiClient(), getTargetUrl(), name, '');
            } else {
                context.querySelector('#txtManualName').value = name;
                context.querySelector('#txtManualPassword').value = '';
                showManualForm(context, true, true);
            }
        }
    });
    view.querySelector('.manualLoginForm').addEventListener('submit', function (e) {
        appSettings.enableAutoLogin(view.querySelector('.chkRememberLogin').checked);
        authenticateUserByName(view, getApiClient(), getTargetUrl(), view.querySelector('#txtManualName').value, view.querySelector('#txtManualPassword').value);
        e.preventDefault();
        return false;
    });
    view.querySelector('.btnForgotPassword').addEventListener('click', function () {
        Dashboard.navigate('forgotpassword');
    });
    view.querySelector('.btnCancel').addEventListener('click', showVisualForm);
    view.querySelector('.btnQuick').addEventListener('click', function () {
        authenticateQuickConnect(getApiClient(), getTargetUrl());
        return false;
    });
    view.querySelector('.btnPinLogin').addEventListener('click', function () {
        authenticateWithPin(getApiClient(), getTargetUrl());
        return false;
    });
    view.querySelector('.btnManual').addEventListener('click', function () {
        view.querySelector('#txtManualName').value = '';
        showManualForm(view, true);
    });
    view.querySelector('.btnSelectServer').addEventListener('click', function () {
        Dashboard.selectServer();
    });

    view.addEventListener('viewshow', function () {
        loading.show();
        libraryMenu.setTransparentMenu(true);

        if (!appHost.supports(AppFeature.MultiServer)) {
            view.querySelector('.btnSelectServer').classList.add('hide');
        }

        const apiClient = getApiClient();

        apiClient.getQuickConnect('Enabled')
            .then(enabled => {
                if (enabled === true) {
                    view.querySelector('.btnQuick').classList.remove('hide');
                }
            })
            .catch(() => {
                console.debug('Failed to get QuickConnect status');
            });

        apiClient.getPublicUsers().then(function (users) {
            if (users.length) {
                showVisualForm();
                loadUserList(view, apiClient, users);
            } else {
                view.querySelector('#txtManualName').value = '';
                showManualForm(view, false, false);
            }
        }).catch().then(function () {
            loading.hide();
        });
        apiClient.getJSON(apiClient.getUrl('Branding/Configuration')).then(function (options) {
            const loginDisclaimer = view.querySelector('.loginDisclaimer');

            // eslint-disable-next-line sonarjs/disabled-auto-escaping
            loginDisclaimer.innerHTML = DOMPurify.sanitize(markdownIt({ html: true }).render(options.LoginDisclaimer || ''));

            for (const elem of loginDisclaimer.querySelectorAll('a')) {
                elem.rel = 'noopener noreferrer';
                elem.target = '_blank';
                elem.classList.add('button-link');
                elem.setAttribute('is', 'emby-linkbutton');

                if (layoutManager.tv) {
                    // Disable links navigation on TV
                    elem.tabIndex = -1;
                }
            }
        });
    });
    view.addEventListener('viewhide', function () {
        libraryMenu.setTransparentMenu(false);
    });
}

