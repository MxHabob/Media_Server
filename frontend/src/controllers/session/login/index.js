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
import baseAlert from '../../../components/alert';
import { getDefaultBackgroundClass } from '../../../components/cardbuilder/cardBuilderUtils';

import './login.scss';

const enableFocusTransform = !browser.slow && !browser.edge;

function authenticateUserByName(page, apiClient, url, username, password) {
    loading.show();
    apiClient.authenticateUserByName(username, password).then(function (result) {
        const user = result.User;
        loading.hide();

        onLoginSuccessful(user.Id, result.AccessToken, apiClient, url);
    }, function (response) {
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
    });
}

function authenticateUserByPin(page, apiClient, url, pin) { // New function
    loading.show();
    // Call the same shape as AuthenticateByName: expect AuthenticationResult
    apiClient.ajax({
        type: 'POST',
        url: apiClient.getUrl('/Users/AuthenticateWithPin'),
        data: JSON.stringify({ Pin: pin }),
        contentType: 'application/json'
    }, true).then(res => res.json()).then(function (result) {
        const user = result.User;
        loading.hide();
        onLoginSuccessful(user.Id, result.AccessToken, apiClient, url);
    }, function (response) {
        page.querySelector('#txtPin').value = '';
        loading.hide();

        const UnauthorizedOrForbidden = [401, 403];
        if (UnauthorizedOrForbidden.includes(response.status)) {
            toast(globalize.translate('MessageInvalidPinOrExpired')); // New localization key
        } else {
            Dashboard.alert({
                message: globalize.translate('MessageUnableToConnectToServer'),
                title: globalize.translate('HeaderConnectionFailure')
            });
        }
    });
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

function onLoginSuccessful(id, accessToken, apiClient, url) {
    Dashboard.onServerChanged(id, accessToken, apiClient);
    Dashboard.navigate(url || 'home');
}

function showManualForm(context, showCancel, focusPassword) {
    context.querySelector('.chkRememberLogin').checked = appSettings.enableAutoLogin();
    context.querySelector('.manualLoginForm').classList.remove('hide');
    context.querySelector('.pinLoginForm').classList.add('hide'); // Hide PIN form
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

function showPinForm(context, showCancel) { // New function
    context.querySelector('.pinLoginForm').classList.remove('hide');
    context.querySelector('.manualLoginForm').classList.add('hide');
    context.querySelector('.visualLoginForm').classList.add('hide');
    context.querySelector('.btnPin').classList.add('hide');

    if (showCancel) {
        context.querySelector('.btnCancelPin').classList.remove('hide');
    } else {
        context.querySelector('.btnCancelPin').classList.add('hide');
    }

    context.querySelector('#txtPin').focus();
}

function loadUserList(context, apiClient, users) {
    const html = users.map(function (user) {
        let avatarHtml = '';
        
        if (user.PrimaryImageTag) {
            const imgUrl = apiClient.getUserImageUrl(user.Id, {
                width: 300,
                tag: user.PrimaryImageTag,
                type: 'Primary'
            });
            avatarHtml = `<div class="user-avatar" style="background-image: url('${imgUrl}'); background-size: cover; background-position: center;"></div>`;
        } else {
            const initials = user.Name.split(' ').map(n => n[0]).join('').toUpperCase().substring(0, 2);
            avatarHtml = `<div class="user-avatar">${initials}</div>`;
        }

        return `<div class="user-card" data-userid="${user.Id}" data-username="${user.Name}" data-haspw="${user.HasPassword}">
            ${avatarHtml}
            <div class="user-name">${user.Name}</div>
        </div>`;
    }).join('');

    context.querySelector('#divUsers').innerHTML = html;
}

export default function (view, params) {
    function getApiClient() {
        const serverId = params.serverid;

        if (serverId) {
            return ServerConnections.getOrCreateApiClient(serverId);
        }

        return ServerConnections.getApiClient();
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
        // Hide all forms
        view.querySelectorAll('.login-form').forEach(form => form.classList.remove('active'));
        // Show visual form
        view.querySelector('.visual-login-form').classList.add('active');
        // Update method tabs
        view.querySelectorAll('.method-tab').forEach(tab => tab.classList.remove('active'));
        view.querySelector('[data-method="visual"]').classList.add('active');

        import('../../../components/autoFocuser').then(({ default: autoFocuser }) => {
            autoFocuser.autoFocus(view);
        });
    }

    function showManualForm(context, focusUsername = false, focusPassword = false) {
        // Hide all forms
        view.querySelectorAll('.login-form').forEach(form => form.classList.remove('active'));
        // Show manual form
        view.querySelector('.manual-login-form').classList.add('active');
        // Update method tabs
        view.querySelectorAll('.method-tab').forEach(tab => tab.classList.remove('active'));
        view.querySelector('[data-method="manual"]').classList.add('active');

        if (focusUsername) {
            context.querySelector('#txtManualName').focus();
        } else if (focusPassword) {
            context.querySelector('#txtManualPassword').focus();
        }
    }

    function showPinForm() {
        // Hide all forms
        view.querySelectorAll('.login-form').forEach(form => form.classList.remove('active'));
        // Show PIN form
        view.querySelector('.pin-login-form').classList.add('active');
        // Update method tabs
        view.querySelectorAll('.method-tab').forEach(tab => tab.classList.remove('active'));
        view.querySelector('[data-method="pin"]').classList.add('active');

        // Focus PIN input
        view.querySelector('#txtPin').focus();
    }

    function showQuickConnectForm() {
        // Hide all forms
        view.querySelectorAll('.login-form').forEach(form => form.classList.remove('active'));
        // Show quick connect form
        view.querySelector('.quick-connect-form').classList.add('active');
        // Update method tabs
        view.querySelectorAll('.method-tab').forEach(tab => tab.classList.remove('active'));
        view.querySelector('[data-method="quick"]').classList.add('active');
    }

    view.querySelector('#divUsers').addEventListener('click', function (e) {
        const userCard = dom.parentWithClass(e.target, 'user-card');

        if (userCard) {
            const context = view;
            const id = userCard.getAttribute('data-userid');
            const name = userCard.getAttribute('data-username');
            const haspw = userCard.getAttribute('data-haspw');

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
    view.querySelector('.pinLoginForm').addEventListener('submit', function (e) { // New event listener
        authenticateUserByPin(view, getApiClient(), getTargetUrl(), view.querySelector('#txtPin').value);
        e.preventDefault();
        return false;
    });
    view.querySelector('.btnForgotPassword').addEventListener('click', function () {
        Dashboard.navigate('forgotpassword');
    });
    // Method tab event listeners
    view.querySelectorAll('.method-tab').forEach(tab => {
        tab.addEventListener('click', function() {
            const method = this.getAttribute('data-method');
            switch(method) {
                case 'visual':
                    showVisualForm();
                    break;
                case 'manual':
                    showManualForm(view, true);
                    break;
                case 'pin':
                    showPinForm();
                    break;
                case 'quick':
                    showQuickConnectForm();
                    break;
            }
        });
    });

    view.querySelector('.btnCancel').addEventListener('click', showVisualForm);
    view.querySelector('.btnCancelPin').addEventListener('click', showVisualForm); // New event listener
    view.querySelector('.btnQuick').addEventListener('click', function () {
        authenticateQuickConnect(getApiClient(), getTargetUrl());
        return false;
    });
    view.querySelector('.btnManual').addEventListener('click', function () {
        view.querySelector('#txtManualName').value = '';
        showManualForm(view, true);
    });
    view.querySelector('.btnPin').addEventListener('click', function () { // New event listener
        showPinForm(view, true);
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