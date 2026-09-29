"use strict";
document.addEventListener("submit", function (event) {
    const prompt = event.target.dataset.confirm;
    if (prompt && !window.confirm(prompt)) event.preventDefault();
});
document.addEventListener("error", function (event) {
    if (event.target instanceof HTMLImageElement && !event.target.dataset.fallback) {
        event.target.dataset.fallback = "true";
        event.target.src = "/images/crests/fc.svg";
    }
}, true);
