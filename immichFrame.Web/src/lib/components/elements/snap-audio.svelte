<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { SnapControl } from '$lib/snapclient/snapcontrol';
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

	let snapControl: SnapControl | null = null;
	let snapStream: SnapStream | null = null;

	// Silence audio element used to unlock the AudioContext on first user gesture.
	// Once play() resolves, the AudioContext is active and SnapStream can connect.
	let silenceAudio: HTMLAudioElement | null = null;

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

	function tryAutoplay() {
		// Check browser autoplay policy before attempting.
		let policy = 'unknown';
		if ('getAutoplayPolicy' in navigator) {
			try {
				policy = (navigator as any).getAutoplayPolicy('mediaelement');
			} catch {}
		}

		if (policy === 'disallowed') {
			audioState = 'failed';
			return;
		}

		// Play a silent audio element to unlock the AudioContext, then start the stream.
		silenceAudio = new Audio();
		// 1-frame silent MP3 data URI — tiny, no external file needed.
		silenceAudio.src =
			'data:audio/mpeg;base64,//uQxAAAAAAAAAAAAAAAAAAAAAAAWGluZwAAAA8AAAACAAACcQCAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICAgICA//////////////////////////////////////////////////////////////////8AAAA5TGFNRTM' +
			'uOTlyBKkAAAAAAAAAABSAJAiUQAABAAAAAnEDML1YAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA';
		silenceAudio.volume = 0;

		audioState = 'connecting';

		silenceAudio.play().then(
			() => {
				silenceAudio?.pause();
				startStream();
			},
			() => {
				audioState = 'failed';
			}
		);
	}

	function handlePlayClick() {
		if (audioState === 'playing') {
			stopStream();
		} else {
			// This call is directly inside a user gesture — AudioContext will unlock.
			tryAutoplay();
		}
	}

	function connectControl() {
		snapControl = new SnapControl();
		snapControl.onConnectionChanged = (_ctrl, connected) => {
			if (!connected && audioState === 'playing') {
				stopStream();
				audioState = 'connecting';
				// Reconnect control; stream will restart on next connection.
				setTimeout(connectControl, 3000);
			}
		};
		snapControl.connect(resolveWsUrl());
	}

	onMount(() => {
		connectControl();
		tryAutoplay();
	});

	onDestroy(() => {
		stopStream();
		snapControl?.disconnect();
		silenceAudio?.pause();
	});
</script>

<!-- Tap-to-play overlay when autoplay was blocked -->
{#if audioState === 'failed' || audioState === 'stopped'}
	<button
		type="button"
		onclick={handlePlayClick}
		class="fixed inset-0 z-50 flex flex-col items-center justify-center gap-3
		       bg-black/40 backdrop-blur-sm text-white cursor-pointer border-0"
		aria-label="Tap to start audio"
	>
		<span class="flex h-16 w-16 items-center justify-center rounded-full bg-white/20 text-4xl">
			▶
		</span>
		<span class="text-sm font-medium tracking-wide opacity-80">Tap to start audio</span>
	</button>
{/if}

<!-- Small floating badge while playing — click to stop -->
{#if audioState === 'playing'}
	<button
		type="button"
		onclick={handlePlayClick}
		class="fixed bottom-4 right-4 z-40 flex items-center gap-1.5
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
		class="fixed bottom-4 right-4 z-40 flex items-center gap-1.5
		       rounded-full bg-black/40 px-3 py-1.5 text-white/60 backdrop-blur-sm"
		aria-label="Connecting to audio"
	>
		<span class="text-base opacity-60">♪</span>
	</div>
{/if}
