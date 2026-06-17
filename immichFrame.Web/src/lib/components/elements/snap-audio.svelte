<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { SnapStream } from '$lib/snapclient/snapstream';

	interface Props {
		snapserverUrl?: string | null;
	}

	let { snapserverUrl }: Props = $props();

	// Derive WebSocket URL from snapserverUrl config or fall back to same host.
	function resolveWsUrl(): string {
		if (snapserverUrl) return snapserverUrl;
		const proto = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
		return `${proto}//${window.location.host}`;
	}

	type AudioState = 'idle' | 'connecting' | 'playing' | 'stopped' | 'failed';
	let audioState: AudioState = $state('idle');

	let snapStream: SnapStream | null = null;

	function startStream() {
		const url = resolveWsUrl();
		snapStream = new SnapStream(url);
		audioState = 'playing';
	}

	function stopStream() {
		snapStream?.stop();
		snapStream = null;
		audioState = 'stopped';
	}

	async function tryAutoplay() {
		// Check browser autoplay policy before attempting.
		if ('getAutoplayPolicy' in navigator) {
			try {
				if ((navigator as any).getAutoplayPolicy('audiocontext') === 'disallowed') {
					audioState = 'failed';
					return;
				}
			} catch {}
		}

		// Use a silent Web Audio buffer to probe/unlock the AudioContext.
		// Avoids the data URI approach which triggers media decode errors in Firefox.
		try {
			const ctx = new AudioContext();
			if (ctx.state === 'suspended') {
				// Race resume() against a short timeout — succeeds only inside a user gesture.
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
			startStream();
		} catch {
			audioState = 'failed';
		}
	}

	function handlePlayClick() {
		if (audioState === 'playing') {
			stopStream();
		} else {
			// This call is directly inside a user gesture — AudioContext will unlock.
			tryAutoplay();
		}
	}

	onMount(() => {
		tryAutoplay();
	});

	onDestroy(() => {
		stopStream();
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

<!-- Small floating badge while playing — click to stop -->
{#if audioState === 'playing'}
	<button
		type="button"
		onclick={handlePlayClick}
		class="fixed bottom-4 right-4 z-[110] flex items-center gap-1.5
		       rounded-full bg-black/50 px-3 py-1.5 text-white backdrop-blur-sm border-0 cursor-pointer"
		title="Audio playing — click to stop"
		aria-label="Audio playing"
	>
		<span class="animate-pulse text-base">♪</span>
	</button>
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
