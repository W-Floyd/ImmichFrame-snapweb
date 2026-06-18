<script lang="ts">
	import HomePage from '$lib/components/home-page/home-page.svelte';
	import { page } from '$app/state';

	const configured: boolean = (page.data as { configured?: boolean })?.configured ?? true;
</script>

<svelte:head>
	<title>immichFrame</title>
</svelte:head>

{#if !configured}
	<div class="fixed inset-0 flex flex-col items-center justify-center gap-6 bg-black text-white">
		<svg xmlns="http://www.w3.org/2000/svg" class="h-16 w-16 text-white/20" viewBox="0 0 24 24" fill="currentColor">
			<path d="M21 19V5c0-1.1-.9-2-2-2H5c-1.1 0-2 .9-2 2v14c0 1.1.9 2 2 2h14c1.1 0 2-.9 2-2zM8.5 13.5l2.5 3.01L14.5 12l4.5 6H5l3.5-4.5z"/>
		</svg>
		<div class="text-center">
			<h1 class="text-2xl font-semibold">Welcome to ImmichFrame</h1>
			<p class="mt-2 text-white/50">No accounts have been configured yet.</p>
		</div>
		<a
			href="/settings"
			class="rounded-lg px-6 py-3 text-sm font-medium text-black transition-opacity hover:opacity-90"
			style="background-color: var(--color-primary, #d4a843);"
		>
			Open Settings
		</a>
	</div>
{:else}
	<HomePage />
{/if}
