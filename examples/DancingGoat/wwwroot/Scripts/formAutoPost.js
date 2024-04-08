(function () {
    'use strict';

    function formAutoPost(options) {
        // Default settings
        var settings = Object.assign({
            targetContainerSelector: "#target-list",
            url: ""
        }, options);

        function onSuccess(data) {
            document.querySelector(settings.targetContainerSelector).innerHTML = data;
        }

        function getAjaxParameters(htmlControl) {
            var form = htmlControl.closest("form");
            return {
                url: form.getAttribute("action"),
                method: form.getAttribute("method"),
                data: new URLSearchParams(new FormData(form)).toString()
            };
        }

        function ajaxCall() {
            var ajaxParams = getAjaxParameters(this);
            var xhr = new XMLHttpRequest();
            xhr.open(ajaxParams.method, ajaxParams.url, true);
            xhr.setRequestHeader("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            xhr.onreadystatechange = function () {
                if (xhr.readyState === 4 && xhr.status === 200) {
                    onSuccess(xhr.responseText);
                }
            };
            xhr.send(ajaxParams.data);
        }

        var elements = document.querySelectorAll(".js-postback input[type=checkbox]");
        elements.forEach(function (element) {
            element.addEventListener("click", ajaxCall);
        });
    }

    window.formAutoPost = formAutoPost;
})();