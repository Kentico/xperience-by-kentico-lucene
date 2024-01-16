(function () {
    const navToggle = document.querySelector('.nav-toggle');
    const menu = document.querySelector('.nav-menu');

    // Toggles mobile menu on toggle button click
    navToggle.addEventListener('click', function (e) {
        e.preventDefault();
        navToggle.classList.toggle('active');
        menu.classList.toggle('active');
    });

    // Close mobile menu on click away
    document.addEventListener('mouseup', function (e) {
        // If menu is active and if target is not menu and its children
        // nor menu toggle button and its children
        if (menu.classList.contains('active') && !menu.contains(e.target) &&
            e.target !== navToggle && !navToggle.contains(e.target)) {
            e.stopPropagation();
            navToggle.classList.remove('active');
            menu.classList.remove('active');
        }
    });
})();