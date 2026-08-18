(() => {
    "use strict";

    const accessTokenKey = "empportal.auth.access-token";
    const expiresAtKey = "empportal.auth.access-token-expires-at";
    const refreshSkewMilliseconds = 30_000;

    function storage() {
        try {
            return window.localStorage;
        } catch {
            return null;
        }
    }

    function clear() {
        const localStorage = storage();
        if (!localStorage) {
            return;
        }

        localStorage.removeItem(accessTokenKey);
        localStorage.removeItem(expiresAtKey);
    }

    function readValidToken() {
        const localStorage = storage();
        if (!localStorage) {
            return null;
        }

        const accessToken = localStorage.getItem(accessTokenKey);
        const expiresAt = Number(localStorage.getItem(expiresAtKey));
        if (!accessToken || !Number.isFinite(expiresAt) ||
            expiresAt <= Date.now() + refreshSkewMilliseconds) {
            clear();
            return null;
        }

        return accessToken;
    }

    function persist(tokenResponse) {
        if (!tokenResponse || typeof tokenResponse.accessToken !== "string" ||
            tokenResponse.accessToken.length === 0 ||
            !Number.isFinite(tokenResponse.expiresIn) || tokenResponse.expiresIn <= 0) {
            throw new Error("The access-token response is invalid.");
        }

        const localStorage = storage();
        if (!localStorage) {
            throw new Error("Browser localStorage is unavailable.");
        }

        localStorage.setItem(accessTokenKey, tokenResponse.accessToken);
        localStorage.setItem(
            expiresAtKey,
            String(Date.now() + (tokenResponse.expiresIn * 1000)));
        return tokenResponse.accessToken;
    }

    async function issueAccessToken() {
        const antiforgeryResponse = await fetch("/api/auth/antiforgery", {
            method: "GET",
            credentials: "same-origin",
            cache: "no-store",
            headers: { "Accept": "application/json" }
        });
        if (!antiforgeryResponse.ok) {
            clear();
            throw new Error(`Unable to acquire an antiforgery token (${antiforgeryResponse.status}).`);
        }

        const antiforgery = await antiforgeryResponse.json();
        if (!antiforgery || typeof antiforgery.requestToken !== "string" ||
            antiforgery.requestToken.length === 0) {
            clear();
            throw new Error("The antiforgery-token response is invalid.");
        }

        const tokenResponse = await fetch("/api/auth/token", {
            method: "POST",
            credentials: "same-origin",
            cache: "no-store",
            headers: {
                "Accept": "application/json",
                "RequestVerificationToken": antiforgery.requestToken
            }
        });
        if (!tokenResponse.ok) {
            clear();
            throw new Error(`Unable to issue an access token (${tokenResponse.status}).`);
        }

        return persist(await tokenResponse.json());
    }

    async function ensureAccessToken(forceRefresh = false) {
        if (!forceRefresh) {
            const existingToken = readValidToken();
            if (existingToken) {
                return existingToken;
            }
        }

        clear();
        return issueAccessToken();
    }

    async function authorizedFetch(input, init = {}) {
        const accessToken = await ensureAccessToken();
        const headers = new Headers(init.headers || {});
        headers.set("Authorization", `Bearer ${accessToken}`);

        const response = await fetch(input, {
            ...init,
            headers,
            credentials: "omit"
        });
        if (response.status === 401) {
            clear();
        }

        return response;
    }

    window.empPortalAuth = Object.freeze({
        authorizedFetch,
        clear,
        ensureAccessToken,
        getAccessToken: readValidToken
    });

    const accountPath = window.location.pathname.toLowerCase();
    if (accountPath.startsWith("/account/login") ||
        accountPath.startsWith("/account/logout")) {
        clear();
    }
})();
