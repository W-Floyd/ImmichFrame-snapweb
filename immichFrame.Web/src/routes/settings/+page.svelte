<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/state';
	import { goto } from '$app/navigation';
	import { init } from '$lib/index';
	import { authSecretStore } from '$lib/stores/persist.store';
	import { getFullSettings, saveSettings, type FullSettingsDto, type AccountSettingsDto } from '$lib/settingsApi';
	import { fetchOidcConfig, signinRedirect, getAccessToken, type OidcConfig } from '$lib/oidc';
	import { initOidc } from '$lib/index';

	let settings: FullSettingsDto = $state({});
	let loading = $state(true);
	let saving = $state(false);
	let error = $state('');
	let successMsg = $state('');
	let envVars = $state<string | null>(null);
	let envVarsCopied = $state(false);
	let oidcConfig = $state<OidcConfig | null>(null);
	let oidcSignedIn = $state(false);

	const authsecret = page.url.searchParams.get('authsecret');
	if (authsecret && authsecret !== $authSecretStore) {
		authSecretStore.set(authsecret);
		init();
	}

	onMount(async () => {
		[oidcConfig] = await Promise.all([
			fetchOidcConfig(),
			initOidc(),
		]);
		oidcSignedIn = oidcConfig.enabled ? !!(await getAccessToken()) : false;
		try {
			settings = await getFullSettings();
		} catch (e: unknown) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			loading = false;
		}
	});

	async function handleSave() {
		saving = true;
		error = '';
		successMsg = '';
		envVars = null;
		try {
			const result = await saveSettings(settings);
			successMsg = result.message;
			envVars = result.envVars ?? null;
			envVarsCopied = false;
		} catch (e: unknown) {
			error = e instanceof Error ? e.message : String(e);
		} finally {
			saving = false;
		}
	}

	function addAccount() {
		settings.Accounts = [
			...(settings.Accounts ?? []),
			{ immichServerUrl: '', apiKey: '', showMemories: false, showFavorites: false, showArchived: false, showVideos: false, albums: [], excludedAlbums: [], people: [], tags: [] }
		];
	}

	function removeAccount(i: number) {
		settings.Accounts = settings.Accounts?.filter((_, idx) => idx !== i) ?? [];
	}

	function webcalendarsText(): string {
		return (settings.General?.webcalendars ?? []).join('\n');
	}

	function setWebcalendars(val: string) {
		if (!settings.General) settings.General = {};
		settings.General.webcalendars = val.split('\n').map(s => s.trim()).filter(Boolean);
	}

	function albumsText(a: AccountSettingsDto, field: 'albums' | 'excludedAlbums' | 'people'): string {
		return (a[field] ?? []).join('\n');
	}

	function setAccountList(a: AccountSettingsDto, field: 'albums' | 'excludedAlbums' | 'people', val: string) {
		a[field] = val.split('\n').map(s => s.trim()).filter(Boolean) as string[] & typeof a[typeof field];
	}
</script>

<svelte:head>
	<title>ImmichFrame Settings</title>
</svelte:head>

<div class="min-h-screen bg-black text-white">
	<header class="sticky top-0 z-10 flex items-center justify-between bg-black/90 px-6 py-4 backdrop-blur border-b border-white/10">
		<div class="flex items-center gap-3">
			<button
				onclick={() => goto('/')}
				class="rounded-lg px-3 py-1.5 text-sm text-white/60 hover:text-white hover:bg-white/10 transition-colors"
			>
				← Back
			</button>
			<h1 class="text-lg font-semibold">Settings</h1>
		</div>
		<div class="flex items-center gap-3">
			{#if oidcConfig?.enabled && !oidcSignedIn}
				<button
					onclick={() => signinRedirect('/settings')}
					class="rounded-lg border border-white/20 px-4 py-2 text-sm font-medium text-white/70 hover:border-white/40 hover:text-white transition-colors"
				>
					Sign in with SSO
				</button>
			{/if}
			{#if oidcConfig?.enabled && oidcSignedIn}
				<span class="text-sm text-white/40">SSO ✓</span>
			{/if}
			{#if successMsg}
				<span class="text-sm text-green-400">{successMsg}</span>
			{/if}
			{#if error}
				<span class="text-sm text-red-400">{error}</span>
			{/if}
			<button
				onclick={handleSave}
				disabled={saving || loading}
				class="rounded-lg bg-primary px-4 py-2 text-sm font-medium text-black disabled:opacity-50 hover:opacity-90 transition-opacity"
				style="background-color: var(--color-primary, #d4a843);"
			>
				{saving ? 'Saving…' : 'Save'}
			</button>
		</div>
	</header>

	{#if loading}
		<div class="flex items-center justify-center p-16 text-white/40">Loading settings…</div>
	{:else}
		<div class="mx-auto max-w-3xl space-y-8 px-6 py-8">

			<!-- General -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">General</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Language</span>
						<input
							type="text"
							bind:value={settings.General!.language}
							class="settings-input"
							placeholder="en"
						/>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Layout</span>
						<select bind:value={settings.General!.layout} class="settings-input">
							<option value="splitview">Split View</option>
							<option value="fullscreen">Fullscreen</option>
						</select>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Slide Interval (seconds)</span>
						<input
							type="number"
							min="5"
							bind:value={settings.General!.interval}
							class="settings-input"
						/>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Transition Duration (seconds)</span>
						<input
							type="number"
							min="0"
							step="0.1"
							bind:value={settings.General!.transitionDuration}
							class="settings-input"
						/>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Renew Images Every (minutes)</span>
						<input
							type="number"
							min="1"
							bind:value={settings.General!.renewImagesDuration}
							class="settings-input"
						/>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Refresh Albums/People Every (hours)</span>
						<input
							type="number"
							min="1"
							bind:value={settings.General!.refreshAlbumPeopleInterval}
							class="settings-input"
						/>
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Display -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">Display</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showClock} />
						<span>Show Clock</span>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Clock Format</span>
						<input type="text" bind:value={settings.General!.clockFormat} class="settings-input" placeholder="hh:mm" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Clock Date Format</span>
						<input type="text" bind:value={settings.General!.clockDateFormat} class="settings-input" placeholder="eee, MMM d" />
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showProgressBar} />
						<span>Show Progress Bar</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showPhotoDate} />
						<span>Show Photo Date</span>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Photo Date Format</span>
						<input type="text" bind:value={settings.General!.photoDateFormat} class="settings-input" placeholder="MM/dd/yyyy" />
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showImageDesc} />
						<span>Show Image Description</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showPeopleDesc} />
						<span>Show People</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showTagsDesc} />
						<span>Show Tags</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showAlbumName} />
						<span>Show Album Name</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showImageLocation} />
						<span>Show Image Location</span>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Location Format</span>
						<input type="text" bind:value={settings.General!.imageLocationFormat} class="settings-input" placeholder="City,State,Country" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Style</span>
						<select bind:value={settings.General!.style} class="settings-input">
							<option value="none">None</option>
							<option value="faded">Faded</option>
						</select>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Base Font Size</span>
						<input type="text" bind:value={settings.General!.baseFontSize} class="settings-input" placeholder="e.g. 16px" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Primary Color</span>
						<div class="flex gap-2 items-center">
							<input type="color" bind:value={settings.General!.primaryColor} class="h-9 w-12 cursor-pointer rounded border border-white/20 bg-transparent p-0.5" />
							<input type="text" bind:value={settings.General!.primaryColor} class="settings-input flex-1" placeholder="#d4a843" />
						</div>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Secondary Color</span>
						<div class="flex gap-2 items-center">
							<input type="color" bind:value={settings.General!.secondaryColor} class="h-9 w-12 cursor-pointer rounded border border-white/20 bg-transparent p-0.5" />
							<input type="text" bind:value={settings.General!.secondaryColor} class="settings-input flex-1" placeholder="#ffffff" />
						</div>
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Image -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">Image</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.imageZoom} />
						<span>Image Zoom</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.imagePan} />
						<span>Image Pan</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.imageFill} />
						<span>Image Fill</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.downloadImages} />
						<span>Download Images (cache locally)</span>
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Weather -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">Weather</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">OpenWeatherMap API Key</span>
						<input type="password" autocomplete="off" bind:value={settings.General!.weatherApiKey} class="settings-input" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Lat,Long</span>
						<input type="text" bind:value={settings.General!.weatherLatLong} class="settings-input" placeholder="40.7128,74.0060" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Unit System</span>
						<select bind:value={settings.General!.unitSystem} class="settings-input">
							<option value="imperial">Imperial (°F)</option>
							<option value="metric">Metric (°C)</option>
						</select>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.showWeatherDescription} />
						<span>Show Weather Description</span>
					</label>
					<label class="col-span-2 flex flex-col gap-1">
						<span class="text-sm text-white/60">Weather Icon URL</span>
						<input type="text" bind:value={settings.General!.weatherIconUrl} class="settings-input" />
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Audio -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">Audio</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.playAudio} />
						<span>Play Audio</span>
					</label>
					<label class="settings-toggle">
						<input type="checkbox" bind:checked={settings.General!.snapAudio} />
						<span>Snapcast Audio</span>
					</label>
					<label class="col-span-2 flex flex-col gap-1">
						<span class="text-sm text-white/60">Snapserver URL</span>
						<input type="text" bind:value={settings.General!.snapserverUrl} class="settings-input" placeholder="ws://snapserver:1780/jsonrpc" />
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Calendars & Webhooks -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">Calendars & Webhooks</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="col-span-2 flex flex-col gap-1">
						<span class="text-sm text-white/60">Web Calendars (one URL per line)</span>
						<textarea
							rows="4"
							value={webcalendarsText()}
							oninput={(e) => setWebcalendars((e.target as HTMLTextAreaElement).value)}
							class="settings-input resize-none font-mono text-xs"
							placeholder="https://calendar.google.com/calendar/ical/..."
						></textarea>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Webhook URL</span>
						<input type="text" bind:value={settings.General!.webhook} class="settings-input" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Authentication Secret</span>
						<input type="password" autocomplete="new-password" bind:value={settings.General!.authenticationSecret} class="settings-input" />
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- OIDC -->
			<section>
				<h2 class="mb-4 text-base font-semibold text-white/80 uppercase tracking-wider">OIDC / SSO</h2>
				<div class="grid grid-cols-2 gap-4">
					<label class="col-span-2 flex flex-col gap-1">
						<span class="text-sm text-white/60">Authority URL</span>
						<input
							type="url"
							bind:value={settings.General!.oidcAuthority}
							class="settings-input"
							placeholder="https://auth.example.com/realms/myrealm"
						/>
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Client ID</span>
						<input type="text" bind:value={settings.General!.oidcClientId} class="settings-input" placeholder="immichframe" />
					</label>
					<label class="flex flex-col gap-1">
						<span class="text-sm text-white/60">Scopes</span>
						<input type="text" bind:value={settings.General!.oidcScopes} class="settings-input" placeholder="openid profile" />
					</label>
					<label class="settings-toggle col-span-2">
						<input type="checkbox" bind:checked={settings.General!.oidcProtectFrame} />
						<span>Require login to view frame</span>
					</label>
				</div>
			</section>

			<hr class="border-white/10" />

			<!-- Accounts -->
			<section>
				<div class="mb-4 flex items-center justify-between">
					<h2 class="text-base font-semibold text-white/80 uppercase tracking-wider">Accounts</h2>
					<button
						onclick={addAccount}
						class="rounded-lg px-3 py-1.5 text-sm text-white/60 hover:text-white hover:bg-white/10 transition-colors border border-white/20"
					>
						+ Add Account
					</button>
				</div>

				{#if !settings.Accounts?.length}
					<p class="text-sm text-white/40">No accounts configured.</p>
				{/if}

				{#each settings.Accounts ?? [] as account, i (i)}
					<div class="mb-6 rounded-xl border border-white/10 p-4">
						<div class="mb-3 flex items-center justify-between">
							<span class="text-sm font-medium text-white/60">Account {i + 1}</span>
							<button
								onclick={() => removeAccount(i)}
								class="text-xs text-red-400 hover:text-red-300 transition-colors"
							>Remove</button>
						</div>
						<div class="grid grid-cols-2 gap-4">
							<label class="col-span-2 flex flex-col gap-1">
								<span class="text-sm text-white/60">Immich Server URL</span>
								<input type="text" bind:value={account.immichServerUrl} class="settings-input" placeholder="https://immich.example.com" />
							</label>
							<label class="col-span-2 flex flex-col gap-1">
								<span class="text-sm text-white/60">API Key</span>
								<input type="password" autocomplete="new-password" bind:value={account.apiKey} class="settings-input" />
							</label>
							<label class="settings-toggle">
								<input type="checkbox" bind:checked={account.showMemories} />
								<span>Show Memories</span>
							</label>
							<label class="settings-toggle">
								<input type="checkbox" bind:checked={account.showFavorites} />
								<span>Show Favorites</span>
							</label>
							<label class="settings-toggle">
								<input type="checkbox" bind:checked={account.showArchived} />
								<span>Show Archived</span>
							</label>
							<label class="settings-toggle">
								<input type="checkbox" bind:checked={account.showVideos} />
								<span>Show Videos</span>
							</label>
							<label class="flex flex-col gap-1">
								<span class="text-sm text-white/60">Images From Days</span>
								<input type="number" bind:value={account.imagesFromDays} class="settings-input" placeholder="(any)" />
							</label>
							<label class="flex flex-col gap-1">
								<span class="text-sm text-white/60">Rating Filter</span>
								<input type="number" min="1" max="5" bind:value={account.rating} class="settings-input" placeholder="(any)" />
							</label>
							<label class="col-span-2 flex flex-col gap-1">
								<span class="text-sm text-white/60">Albums (one UUID per line)</span>
								<textarea
									rows="3"
									value={albumsText(account, 'albums')}
									oninput={(e) => setAccountList(account, 'albums', (e.target as HTMLTextAreaElement).value)}
									class="settings-input resize-none font-mono text-xs"
								></textarea>
							</label>
							<label class="col-span-2 flex flex-col gap-1">
								<span class="text-sm text-white/60">Excluded Albums (one UUID per line)</span>
								<textarea
									rows="3"
									value={albumsText(account, 'excludedAlbums')}
									oninput={(e) => setAccountList(account, 'excludedAlbums', (e.target as HTMLTextAreaElement).value)}
									class="settings-input resize-none font-mono text-xs"
								></textarea>
							</label>
							<label class="col-span-2 flex flex-col gap-1">
								<span class="text-sm text-white/60">People (one UUID per line)</span>
								<textarea
									rows="3"
									value={albumsText(account, 'people')}
									oninput={(e) => setAccountList(account, 'people', (e.target as HTMLTextAreaElement).value)}
									class="settings-input resize-none font-mono text-xs"
								></textarea>
							</label>
						</div>
					</div>
				{/each}
			</section>

			<!-- Env vars suggestion panel -->
			{#if envVars}
				<div class="rounded-lg border border-yellow-500/40 bg-yellow-500/10 p-4 space-y-3">
					<div class="flex items-start justify-between gap-4">
						<p class="text-sm text-yellow-300">
							Settings applied for this session. To persist across restarts, update your environment variables:
						</p>
						<button
							onclick={() => { navigator.clipboard.writeText(envVars!); envVarsCopied = true; }}
							class="shrink-0 rounded px-3 py-1 text-xs font-medium border transition-colors"
							class:border-green-500={envVarsCopied}
							class:text-green-400={envVarsCopied}
							class:border-yellow-500={!envVarsCopied}
							class:text-yellow-300={!envVarsCopied}
						>
							{envVarsCopied ? '✓ Copied' : 'Copy'}
						</button>
					</div>
					<pre class="overflow-x-auto rounded bg-black/40 p-3 text-xs text-white/80 leading-relaxed">{envVars}</pre>
				</div>
			{/if}

			<!-- Bottom save bar -->
			<div class="flex items-center justify-end gap-3 pb-8">
				{#if successMsg}
					<span class="text-sm text-green-400">{successMsg}</span>
				{/if}
				{#if error}
					<span class="text-sm text-red-400">{error}</span>
				{/if}
				<button
					onclick={handleSave}
					disabled={saving || loading}
					class="rounded-lg px-6 py-2.5 text-sm font-medium text-black disabled:opacity-50 hover:opacity-90 transition-opacity"
					style="background-color: var(--color-primary, #d4a843);"
				>
					{saving ? 'Saving…' : 'Save Settings'}
				</button>
			</div>
		</div>
	{/if}
</div>

<style>
	:global(.settings-input) {
		width: 100%;
		background: rgba(255, 255, 255, 0.05);
		border: 1px solid rgba(255, 255, 255, 0.15);
		border-radius: 0.5rem;
		color: white;
		padding: 0.5rem 0.75rem;
		font-size: 0.875rem;
		outline: none;
		transition: border-color 0.15s;
	}
	:global(.settings-input:focus) {
		border-color: rgba(255, 255, 255, 0.4);
	}
	:global(.settings-input option) {
		background: #1a1a1a;
	}
	:global(.settings-toggle) {
		display: flex;
		align-items: center;
		gap: 0.625rem;
		cursor: pointer;
		padding: 0.5rem 0;
	}
	:global(.settings-toggle input[type='checkbox']) {
		width: 1rem;
		height: 1rem;
		accent-color: var(--color-primary, #d4a843);
		cursor: pointer;
	}
	:global(.settings-toggle span) {
		font-size: 0.875rem;
		color: rgba(255, 255, 255, 0.8);
	}
</style>
