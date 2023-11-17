function replaceForm(event) {
    event.preventDefault();
    var form = event.target;
    
    $.ajax({
        method: "POST",
        url: form.action,
        data: $(form).serialize(),
        success: function (data) {
            $(form).replaceWith(data);
        }
    });
}