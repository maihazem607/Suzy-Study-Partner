// Theme toggling logic (works on all pages)
document.addEventListener("DOMContentLoaded", () => {
    const body = document.body;
    const toggleBtn = document.getElementById("themeToggle");

    // Apply saved theme from localStorage
    if (localStorage.getItem("theme") === "dark") {
        body.classList.add("theme-dark");
    }

    // Toggle button click
    toggleBtn?.addEventListener("click", () => {
        body.classList.toggle("theme-dark");
        const isDark = body.classList.contains("theme-dark");
        localStorage.setItem("theme", isDark ? "dark" : "light");
    });
});
