<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { SnapStream } from '$lib/snapclient/snapstream';
	import { SnapControl } from '$lib/snapclient/snapcontrol';

	interface Props {
		snapserverUrl?: string | null;
	}

	let { snapserverUrl }: Props = $props();

	function resolveWsUrl(): string {
		if (snapserverUrl) return snapserverUrl;
		const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
		return `${proto}//${window.location.host}`;
	}

	type AudioState = 'idle' | 'connecting' | 'playing' | 'stopped' | 'failed';
	let audioState: AudioState = $state('idle');
	let showSettings = $state(false);
	let latency = $state(0);
	let clientFound = $state(false);

	let snapStream: SnapStream | null = null;
	let snapControl: SnapControl | null = null;
	let silenceCtx: AudioContext | null = null;

	const clientId = SnapStream.getClientId();

	// Keep a looping silent AudioContext alive once the user unlocks audio.
	// Browsers won't sleep/defocus a page that has a running audio graph.
	function ensureSilenceLoop() {
		if (silenceCtx) return;
		silenceCtx = new AudioContext();
		const buf = silenceCtx.createBuffer(1, silenceCtx.sampleRate, silenceCtx.sampleRate);
		const src = silenceCtx.createBufferSource();
		src.buffer = buf;
		src.loop = true;
		src.connect(silenceCtx.destination);
		src.start();
	}

	function startStream() {
		const url = resolveWsUrl();
		snapStream = new SnapStream(url);

		snapControl = new SnapControl();
		snapControl.onChange = (ctrl) => {
			try {
				const client = ctrl.getClient(clientId);
				latency = client.config.latency;
				clientFound = true;
			} catch {
				// client not yet visible in server state
			}
		};
		snapControl.connect(url);

		audioState = 'playing';
	}

	function stopStream() {
		snapStream?.stop();
		snapStream = null;
		snapControl?.disconnect();
		snapControl = null;
		showSettings = false;
		clientFound = false;
		audioState = 'stopped';
	}

	function adjustLatency(delta: number) {
		if (!snapControl || !clientFound) return;
		try {
			snapControl.setClientLatency(clientId, latency + delta);
			latency += delta;
		} catch {
			// client disappeared from server state
		}
	}

	async function tryAutoplay() {
		if ('getAutoplayPolicy' in navigator) {
			try {
				if ((navigator as any).getAutoplayPolicy('audiocontext') === 'disallowed') {
					audioState = 'failed';
					return;
				}
			} catch {}
		}

		try {
			const ctx = new AudioContext();
			if (ctx.state === 'suspended') {
				await Promise.race([
					ctx.resume(),
					new Promise<never>((_, reject) => setTimeout(() => reject(new Error('autoplay blocked')), 500))
				]);
			}
			const buf = ctx.createBuffer(1, 1, 22050);
			const src = ctx.createBufferSource();
			src.buffer = buf;
			src.connect(ctx.destination);
			src.start(0);
			await ctx.close();
			ensureSilenceLoop();
			startStream();
		} catch {
			audioState = 'failed';
		}
	}

	function handlePlayClick() {
		if (audioState === 'playing') {
			stopStream();
		} else {
			tryAutoplay();
		}
	}

	onMount(() => {
		tryAutoplay();
	});

	onDestroy(() => {
		stopStream();
		silenceCtx?.close();
		silenceCtx = null;
	});
</script>

<!-- Tap-to-play badge when autoplay was blocked -->
{#if audioState === 'failed' || audioState === 'stopped'}
	<button
		type="button"
		onclick={handlePlayClick}
		class="fixed bottom-4 right-4 z-[110] flex items-center gap-2
		       rounded-full bg-black/70 px-4 py-2 text-white backdrop-blur-sm border-0 cursor-pointer"
		aria-label="Tap to start audio"
	>
		<span class="text-base">▶</span>
		<span class="text-sm font-medium">Tap to start audio</span>
	</button>
{/if}

<!-- Playing badge with optional latency controls -->
{#if audioState === 'playing'}
	<div class="fixed bottom-4 right-4 z-[110] flex flex-col items-end gap-2">

		<!-- Latency panel (shown when settings open and client is known) -->
		{#if showSettings && clientFound}
			<div class="flex items-center gap-1 rounded-full bg-black/70 px-3 py-1.5 text-white backdrop-blur-sm text-sm">
				<button
					type="button"
					onclick={() => adjustLatency(-100)}
					class="rounded px-1.5 py-0.5 hover:bg-white/20 border-0 cursor-pointer text-white text-xs"
					aria-label="Decrease latency by 100ms"
				>−100</button>
				<button
					type="button"
					onclick={() => adjustLatency(-10)}
					class="rounded px-1.5 py-0.5 hover:bg-white/20 border-0 cursor-pointer text-white text-xs"
					aria-label="Decrease latency by 10ms"
				>−10</button>
				<span class="min-w-[4rem] text-center font-mono">{latency} ms</span>
				<button
					type="button"
					onclick={() => adjustLatency(10)}
					class="rounded px-1.5 py-0.5 hover:bg-white/20 border-0 cursor-pointer text-white text-xs"
					aria-label="Increase latency by 10ms"
				>+10</button>
				<button
					type="button"
					onclick={() => adjustLatency(100)}
					class="rounded px-1.5 py-0.5 hover:bg-white/20 border-0 cursor-pointer text-white text-xs"
					aria-label="Increase latency by 100ms"
				>+100</button>
			</div>
		{/if}

		<!-- Main badge row -->
		<div class="flex items-center gap-1">
			{#if clientFound}
				<button
					type="button"
					onclick={() => (showSettings = !showSettings)}
					class="rounded-full bg-black/50 px-2 py-1.5 text-white/70 backdrop-blur-sm border-0 cursor-pointer hover:text-white"
					title="Adjust latency"
					aria-label="Latency settings"
				>
					<span class="text-sm">⚙</span>
				</button>
			{/if}
			<button
				type="button"
				onclick={handlePlayClick}
				class="flex items-center gap-1.5 rounded-full bg-black/50 px-3 py-1.5 text-white backdrop-blur-sm border-0 cursor-pointer"
				title="Audio playing — click to stop"
				aria-label="Audio playing"
			>
				<span class="animate-pulse text-base">♪</span>
			</button>
		</div>
	</div>
{/if}

<!-- Subtle connecting indicator -->
{#if audioState === 'connecting'}
	<div
		class="fixed bottom-4 right-4 z-[110] flex items-center gap-1.5
		       rounded-full bg-black/40 px-3 py-1.5 text-white/60 backdrop-blur-sm"
		aria-label="Connecting to audio"
	>
		<span class="text-base opacity-60">♪</span>
	</div>
{/if}
