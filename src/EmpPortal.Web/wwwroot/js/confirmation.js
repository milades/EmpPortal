window.empPortalDialogs = Object.freeze({
    async confirm(options) {
        const settings = {
            title: options?.title || "آیا مطمئن هستید؟",
            text: options?.text || "این عملیات نیاز به تأیید شما دارد.",
            icon: options?.icon || "warning",
            confirmButtonText: options?.confirmButtonText || "تأیید",
            cancelButtonText: options?.cancelButtonText || "انصراف",
            showCancelButton: true,
            reverseButtons: true,
            focusCancel: true,
            allowEscapeKey: true,
            allowOutsideClick: false,
            heightAuto: false,
            customClass: {
                popup: "empportal-confirm-popup",
                confirmButton: "empportal-confirm-button",
                cancelButton: "empportal-cancel-button"
            }
        };

        if (typeof window.Swal?.fire !== "function") {
            return window.confirm(`${settings.title}\n\n${settings.text}`);
        }

        const result = await window.Swal.fire(settings);
        return result.isConfirmed === true;
    }
});

document.addEventListener("click", async event => {
    const trigger = event.target instanceof Element
        ? event.target.closest("[data-confirm-submit]")
        : null;
    if (!(trigger instanceof HTMLButtonElement) || trigger.dataset.confirmed === "true") {
        return;
    }

    event.preventDefault();
    event.stopImmediatePropagation();
    const confirmed = await window.empPortalDialogs.confirm({
        title: trigger.dataset.confirmTitle,
        text: trigger.dataset.confirmText,
        confirmButtonText: trigger.dataset.confirmButtonText,
        icon: trigger.dataset.confirmIcon
    });
    if (!confirmed || !trigger.form) {
        return;
    }

    trigger.dataset.confirmed = "true";
    trigger.form.requestSubmit(trigger);
}, true);
