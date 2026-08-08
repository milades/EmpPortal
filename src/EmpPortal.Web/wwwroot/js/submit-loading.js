document.addEventListener("submit", event => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement) || form.dataset.staticSubmitLoading !== "true") {
        return;
    }

    if (form.dataset.loadingBound === "true") {
        event.preventDefault();
        event.stopImmediatePropagation();
        return;
    }

    if (!form.checkValidity()) {
        return;
    }

    const submitter = event.submitter instanceof HTMLButtonElement ? event.submitter : null;
    if (!submitter) {
        return;
    }

    form.dataset.loadingBound = "true";
    form.setAttribute("aria-busy", "true");
    submitter.disabled = true;
    submitter.classList.add("is-loading");
    const loadingText = submitter.dataset.loadingText || "در حال ارسال…";
    submitter.dataset.originalText = submitter.textContent || "";
    submitter.replaceChildren();

    const spinner = document.createElement("span");
    spinner.className = "button-loading-spinner";
    spinner.setAttribute("aria-hidden", "true");
    const label = document.createElement("span");
    label.textContent = loadingText;
    submitter.append(spinner, label);
}, true);

const submitConfirmedLogout = () => {
    const form = document.querySelector('form[data-auto-submit="true"]');
    if (!(form instanceof HTMLFormElement) || form.dataset.autoSubmitted === "true") {
        return;
    }

    const submitter = form.querySelector('button[type="submit"]');
    if (!(submitter instanceof HTMLButtonElement)) {
        return;
    }

    form.dataset.autoSubmitted = "true";
    form.requestSubmit(submitter);
};

if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", submitConfirmedLogout, { once: true });
} else {
    submitConfirmedLogout();
}
