(function ($) {
    const $navToggle = $('.nav-toggle');
    const $menu = $('.nav-menu');

    // toggles mobile menu on toggle button click
    $navToggle.click(function (e) {
        e.preventDefault();
        $navToggle.toggleClass('active');
        $menu.toggleClass('active');
    })

    // close mobile menu on click away
    $(document).mouseup(e => {
        // if menu is active and if target is not menu and its children nor menu toggle button and its children
        if ($menu.hasClass('active') && !$menu.is(e.target) && $menu.has(e.target).length === 0 && !$navToggle.is(e.target) && $navToggle.has(e.target).length === 0) {
            e.stopPropagation();
            $navToggle.removeClass('active');
            $menu.removeClass('active');
        }
    });
})(jQuery);