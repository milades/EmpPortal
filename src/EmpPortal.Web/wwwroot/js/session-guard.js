window.empPortalSession = Object.freeze({
    async hasActiveSession() {
        try {
            const response = await fetch("/api/auth/session-state", {
                method: "GET",
                credentials: "same-origin",
                cache: "no-store",
                headers: {
                    "Accept": "application/json",
                    "X-EmpPortal-Session-Probe": "1"
                }
            });

            return response.status === 204;
        } catch {
            // A transient network failure must not silently sign the employee out.
            // The open circuit's periodic server-side revalidation remains active.
            return true;
        }
    }
});
