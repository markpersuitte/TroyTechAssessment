// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

document.addEventListener("DOMContentLoaded", () => {
    document.querySelectorAll(".modal[data-open='true']").forEach(modal => {
        bootstrap.Modal.getOrCreateInstance(modal).show();
    });
});
