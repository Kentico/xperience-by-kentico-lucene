(function () {
    window.kentico.pageBuilder.registerInlineEditor("spinner-editor", {
        init: function (options) {
            var editor = options.editor;
            editor.querySelector("#value-placeholder").innerText = options.propertyValue;

            var minusButton = editor.querySelector("#minus-btn");
            if (options.propertyValue < 2) {
                minusButton.disabled = true;
            } else {
                minusButton.addEventListener("click", function () {
                    var event = new CustomEvent("updateProperty", {
                        detail: {
                            value: options.propertyValue - 1,
                            name: options.propertyName
                        }
                    });
                    editor.dispatchEvent(event);
                });
            }

            editor.querySelector("#plus-btn").addEventListener("click", function () {
                var event = new CustomEvent("updateProperty", {
                    detail: {
                        value: options.propertyValue + 1,
                        name: options.propertyName
                    }
                });
                editor.dispatchEvent(event);
            });
        }
    });
})();
