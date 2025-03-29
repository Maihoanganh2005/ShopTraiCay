<script>
    document.addEventListener("DOMContentLoaded", function () {
        const elements = document.querySelectorAll(".fade-in");

    function checkVisibility() {
        elements.forEach(el => {
            const rect = el.getBoundingClientRect();
            if (rect.top < window.innerHeight * 0.85) {
                el.classList.add("visible");
            }
        });
        }

    checkVisibility();
    window.addEventListener("scroll", checkVisibility);
    });
</script>
