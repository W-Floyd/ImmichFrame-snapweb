import { UserManager, WebStorageStateStore, type UserManagerSettings } from 'oidc-client-ts';

export interface OidcConfig {
	enabled: boolean;
	authority?: string;
	clientId?: string;
	scopes?: string;
	protectFrame?: boolean;
}

let _manager: UserManager | null = null;
let _config: OidcConfig | null = null;

export async function fetchOidcConfig(): Promise<OidcConfig> {
	if (_config) return _config;
	try {
		const res = await fetch('/api/Auth/oidc');
		_config = res.ok ? await res.json() : { enabled: false };
	} catch {
		_config = { enabled: false };
	}
	return _config!;
}

export function getOrCreateManager(config: OidcConfig): UserManager {
	if (_manager) return _manager;
	const settings: UserManagerSettings = {
		authority: config.authority!,
		client_id: config.clientId!,
		redirect_uri: `${window.location.origin}/auth/callback`,
		scope: config.scopes ?? 'openid profile',
		response_type: 'code',
		userStore: new WebStorageStateStore({ store: window.localStorage }),
	};
	_manager = new UserManager(settings);
	return _manager;
}

export async function getAccessToken(): Promise<string | null> {
	if (!_manager) return null;
	try {
		const user = await _manager.getUser();
		if (!user || user.expired) return null;
		return user.access_token;
	} catch {
		return null;
	}
}

export async function signinRedirect(returnTo?: string): Promise<void> {
	const config = await fetchOidcConfig();
	if (!config.enabled) return;
	const mgr = getOrCreateManager(config);
	await mgr.signinRedirect({ state: returnTo ?? window.location.pathname });
}

export async function signinCallback(): Promise<string> {
	const config = await fetchOidcConfig();
	const mgr = getOrCreateManager(config);
	const user = await mgr.signinRedirectCallback();
	return typeof user.state === 'string' ? user.state : '/';
}
