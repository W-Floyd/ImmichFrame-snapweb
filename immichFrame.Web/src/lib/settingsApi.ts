import { defaults } from './immichFrameApi.js';

export type GeneralSettingsDto = {
	downloadImages?: boolean;
	language?: string;
	imageLocationFormat?: string | null;
	photoDateFormat?: string | null;
	interval?: number;
	transitionDuration?: number;
	showClock?: boolean;
	clockFormat?: string | null;
	clockDateFormat?: string | null;
	showProgressBar?: boolean;
	showPhotoDate?: boolean;
	showImageDesc?: boolean;
	showPeopleDesc?: boolean;
	showTagsDesc?: boolean;
	showAlbumName?: boolean;
	showImageLocation?: boolean;
	primaryColor?: string | null;
	secondaryColor?: string | null;
	style?: string;
	baseFontSize?: string | null;
	showWeatherDescription?: boolean;
	weatherIconUrl?: string | null;
	imageZoom?: boolean;
	imagePan?: boolean;
	imageFill?: boolean;
	playAudio?: boolean;
	layout?: string;
	snapAudio?: boolean;
	snapserverUrl?: string | null;
	renewImagesDuration?: number;
	webcalendars?: string[];
	refreshAlbumPeopleInterval?: number;
	weatherApiKey?: string | null;
	unitSystem?: string | null;
	weatherLatLong?: string | null;
	webhook?: string | null;
	authenticationSecret?: string | null;
	oidcAuthority?: string | null;
	oidcClientId?: string | null;
	oidcScopes?: string | null;
	oidcProtectFrame?: boolean;
};

export type AccountSettingsDto = {
	immichServerUrl?: string;
	apiKey?: string;
	apiKeyFile?: string | null;
	showMemories?: boolean;
	showFavorites?: boolean;
	showArchived?: boolean;
	showVideos?: boolean;
	imagesFromDays?: number | null;
	imagesFromDate?: string | null;
	imagesUntilDate?: string | null;
	albums?: string[];
	excludedAlbums?: string[];
	people?: string[];
	tags?: string[];
	rating?: number | null;
};

export type FullSettingsDto = {
	General?: GeneralSettingsDto;
	Accounts?: AccountSettingsDto[];
};

function authHeaders(): Record<string, string> {
	return (defaults.headers as Record<string, string>) ?? {};
}

export async function getFullSettings(): Promise<FullSettingsDto> {
	const res = await fetch('/api/Config/Full', {
		headers: authHeaders()
	});
	if (!res.ok) throw new Error(`Failed to load settings: ${res.status} ${res.statusText}`);
	return res.json();
}

export async function saveSettings(settings: FullSettingsDto): Promise<{ message: string; envVars?: string }> {
	const res = await fetch('/api/Config', {
		method: 'PUT',
		headers: { 'Content-Type': 'application/json', ...authHeaders() },
		body: JSON.stringify(settings)
	});
	if (!res.ok) {
		const body = await res.json().catch(() => ({ message: res.statusText }));
		throw new Error(body.message ?? res.statusText);
	}
	return res.json();
}
