document.addEventListener("click", async event => {
    const modalButton = event.target.closest("[data-modal-url]");
    if (modalButton) {
        const response = await fetch(modalButton.dataset.modalUrl, { headers: { "X-Requested-With": "XMLHttpRequest" } });
        if (!response.ok) return;
        document.getElementById("server-modal-host").innerHTML = await response.text();
        const modal = document.getElementById("serverModal");
        bootstrap.Modal.getOrCreateInstance(modal).show();
        return;
    }

    const deleteButton = event.target.closest("[data-delete-url]");
    if (deleteButton) {
        if (!confirm("Delete this item?")) return;
        const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
        const response = await fetch(deleteButton.dataset.deleteUrl, {
            method: "POST",
            headers: {
                "RequestVerificationToken": token ?? "",
                "X-Concurrency-Token": deleteButton.dataset.concurrencyToken ?? ""
            }
        });
        if (response.ok) window.location.reload();
        else alert(await response.text());
    }
});

document.addEventListener("submit", async event => {
    const form = event.target.closest("[data-server-modal-form]");
    if (!form) return;
    event.preventDefault();
    const response = await fetch(form.action, {
        method: "POST",
        body: new FormData(form),
        headers: { "X-Requested-With": "XMLHttpRequest" }
    });
    if (response.ok && response.headers.get("content-type")?.includes("application/json")) {
        const result = await response.json();
        bootstrap.Modal.getInstance(document.getElementById("serverModal"))?.hide();
        window.location.href = result.url ?? window.location.href;
        return;
    }
    document.getElementById("server-modal-host").innerHTML = await response.text();
    bootstrap.Modal.getOrCreateInstance(document.getElementById("serverModal")).show();
});
