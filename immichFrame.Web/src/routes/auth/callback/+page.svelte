<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { signinCallback } from '$lib/oidc';

	let status = $state('Completing sign-in…');

	onMount(async () => {
		try {
			const returnTo = await signinCallback();
			goto(returnTo);
		} catch (e) {
			status = `Sign-in failed: ${e instanceof Error ? e.message : String(e)}`;
		}
	});
</script>

<svelte:head>
	<title>Signing in…</title>
</svelte:head>

<div class="flex min-h-screen items-center justify-center bg-black">
	<p class="text-sm text-white/40">{status}</p>
</div>
