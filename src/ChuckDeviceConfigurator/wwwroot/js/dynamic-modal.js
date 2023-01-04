$('body').on('click', '.modal-link', function (e) {
    e.preventDefault();

    // Remove previous modal content
    $('#modal-content').remove();

    // Ajax call to controller action for partial view
    // using url from source button.
    const url = $(this).data('targeturl');
    $.get(url, function (data) {
        $('.modal .modal-dialog').html(data);
        $('.modal').modal('show');
    });
});