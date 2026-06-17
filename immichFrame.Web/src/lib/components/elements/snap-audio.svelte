<script lang="ts">
	import { onMount, onDestroy } from 'svelte';

	interface Props {
		snapserverUrl?: string | null;
	}

	let { snapserverUrl }: Props = $props();

	let iframeEl: HTMLIFrameElement | undefined = $state();
	let audioState: 'pending' | 'playing' | 'stopped' | 'failed' = $state('pending');

	function buildIframeSrc(): string {
		const params = new URLSearchParams();
		if (snapserverUrl) {
			params.set('host', snapserverUrl);
		}
		const qs = params.toString() ? `?${params.toString()}` : '';
		return `/audio/${qs}#autoplay`;
	}

	const iframeSrc = buildIframeSrc();

	function handleMessage(event: MessageEvent) {
		if (!event.data || event.data.source !== 'snapweb') return;
		if (event.data.type === 'playing') audioState = 'playing';
		else if (event.data.type === 'stopped') audioState = 'stopped';
		else if (event.data.type === 'autoplay-failed') audioState = 'failed';
	}

	function requestPlay() {
		iframeEl?.contentWindow?.postMessage({ source: 'immichframe', type: 'play' }, '*');
		audioState = 'pending';
	}

	onMount(() => {
		window.addEventListener('message', handleMessage);
	});

	onDestroy(() => {
		window.removeEventListener('message', handleMessage);
	});
</script>

<!-- Off-screen iframe carries the snapweb audio context -->
<iframe
	bind:this={iframeEl}
	src={iframeSrc}
	title="Snapcast audio"
	aria-hidden="true"
	allow="autoplay"
	style="position:absolute;width:1px;height:1px;top:-9999px;left:-9999px;border:0;"
></iframe>

<!-- Full-screen tap-to-play overlay shown when browser blocks autoplay -->
{#if audioState === 'failed' || audioState === 'stopped'}
	<button
		type="button"
		onclick={requestPlay}
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

<!-- Small floating badge while playing -->
{#if audioState === 'playing'}
	<div
		class="fixed bottom-4 right-4 z-40 flex items-center gap-1.5
		       rounded-full bg-black/50 px-3 py-1.5 text-white backdrop-blur-sm"
		title="Snapcast audio playing"
		aria-label="Audio playing"
	>
		<span class="animate-pulse text-base">♪</span>
	</div>
{/if}
