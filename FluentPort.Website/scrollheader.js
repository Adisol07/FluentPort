window.addEventListener("scroll", function() {
    const box = document.querySelector("header");
    if (window.scrollY > 50) {
        box.classList.add("scrolled");
    } else {
        box.classList.remove("scrolled");
    }
});
