import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import test from "node:test";
import vm from "node:vm";

const scriptUrl = new URL(
    "../src/EmpPortal.Web/wwwroot/js/auth-token-store.js",
    import.meta.url);
const scriptSource = await readFile(scriptUrl, "utf8");

function createBrowser(pathname = "/") {
    const values = new Map();
    const requests = [];
    const localStorage = {
        getItem: key => values.has(key) ? values.get(key) : null,
        removeItem: key => values.delete(key),
        setItem: (key, value) => values.set(key, String(value))
    };

    const fetch = async (input, init = {}) => {
        requests.push({ input, init });
        if (input === "/api/auth/antiforgery") {
            return new Response(JSON.stringify({ requestToken: "anti-forgery-token" }), {
                status: 200,
                headers: { "Content-Type": "application/json" }
            });
        }

        if (input === "/api/auth/token") {
            return new Response(JSON.stringify({
                accessToken: "signed.jwt.value",
                tokenType: "Bearer",
                expiresIn: 300
            }), {
                status: 200,
                headers: { "Content-Type": "application/json" }
            });
        }

        return new Response(null, { status: 204 });
    };

    const window = { localStorage, location: { pathname } };
    vm.runInNewContext(scriptSource, {
        Date,
        Error,
        fetch,
        Headers,
        Number,
        Object,
        Response,
        String,
        window
    });

    return { requests, values, window };
}

test("JWT is persisted in localStorage and sent without browser cookies", async () => {
    const browser = createBrowser();

    const accessToken = await browser.window.empPortalAuth.ensureAccessToken();
    assert.equal(accessToken, "signed.jwt.value");
    assert.equal(
        browser.values.get("empportal.auth.access-token"),
        "signed.jwt.value");

    await browser.window.empPortalAuth.authorizedFetch("/api/me");
    const apiRequest = browser.requests.at(-1);
    assert.equal(apiRequest.input, "/api/me");
    assert.equal(apiRequest.init.credentials, "omit");
    assert.equal(
        apiRequest.init.headers.get("Authorization"),
        "Bearer signed.jwt.value");

    const tokenRequest = browser.requests.find(request => request.input === "/api/auth/token");
    assert.equal(tokenRequest.init.credentials, "same-origin");
    assert.equal(
        tokenRequest.init.headers.RequestVerificationToken,
        "anti-forgery-token");
    assert.doesNotMatch(scriptSource, /document\.cookie/i);
});

test("opening the login page clears a previously stored JWT", () => {
    const browser = createBrowser("/account/login");
    browser.values.set("empportal.auth.access-token", "stale.jwt.value");
    browser.values.set("empportal.auth.access-token-expires-at", "9999999999999");

    vm.runInNewContext(scriptSource, {
        Date,
        Error,
        fetch: async () => new Response(null, { status: 500 }),
        Headers,
        Number,
        Object,
        Response,
        String,
        window: browser.window
    });

    assert.equal(browser.values.has("empportal.auth.access-token"), false);
    assert.equal(browser.values.has("empportal.auth.access-token-expires-at"), false);
});
