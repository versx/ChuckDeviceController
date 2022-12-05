const form = $('body').find('form');
const actionUrl = form.attr('action');
//console.log('form:', form, 'action:', actionUrl);

if (form && actionUrl) {
    // TODO: Notifications endpoint
    $.get('/Device/Notifications').done(function (notifications) {
        $('#notifications').html(notifications);
    });
}