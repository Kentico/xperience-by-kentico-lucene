(function () {
  window.kentico.pageBuilder.registerInlineEditor("color-picker-editor", {
    init: function (options) {
      var editor = options.editor;
      var buttons = [
        { button: editor.querySelector("#first-color-btn"), cssClass: "first-color" },
        { button: editor.querySelector("#second-color-btn"), cssClass: "second-color" },
        { button: editor.querySelector("#third-color-btn"), cssClass: "third-color" },
      ];

      var setColor = function (selectedButton) {
        for (var i = 0; i < buttons.length; ++i) {
          editor.parentElement.classList.remove(buttons[i].cssClass);
          buttons[i].button.children[0].classList.remove("icon-cb-check-sign");
        }
        editor.parentElement.classList.add(selectedButton.cssClass);
        selectedButton.button.children[0].classList.add("icon-cb-check-sign");

        var event = new CustomEvent("updateProperty", {
          detail: {
            value: selectedButton.cssClass,
            name: options.propertyName
          }
        });
        editor.dispatchEvent(event);
      }

      for (var i = 0; i < buttons.length; ++i) {
        if (options.propertyValue === buttons[i].cssClass) {
          buttons[i].button.children[0].classList.add("icon-cb-check-sign");
        }
        var click = function(index) { setColor(buttons[index]); }.bind(this, i);
        buttons[i].button.addEventListener("click", click);
      }
    }
  });
})();

(function () {
    window.kentico.pageBuilder.registerInlineEditor("text-editor", {
        init: function (options) {
            var editor = options.editor;

            editor.addEventListener("input", function () {
                if (!this.textContent) {
                    // Clear the element when text content is empty because Firefox always
                    // keeps a <br> element in the contenteditable even when it's empty
                    // which prevents the css placeholder text from appearing.
                    this.innerHTML = "";
                }

                var event = new CustomEvent("updateProperty", {
                    detail: {
                        name: options.propertyName,
                        value: this.textContent,
                        refreshMarkup: false
                    }
                });

                editor.dispatchEvent(event);
            });
        },
    });
})();
