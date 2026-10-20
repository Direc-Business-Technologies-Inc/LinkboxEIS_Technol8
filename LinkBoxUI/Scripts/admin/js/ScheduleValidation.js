
$("#sched-code").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#sched-code-formgroup').hasClass('error'))) {
        $('#sched-code-formgroup').append('<span class="help-block text-red">Schedule Code is Required</span>');
    }
    $('#sched-code-formgroup').addClass('error');
});
$("#sched-date").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#sched-date-formgroup').hasClass('error'))) {
        $('#sched-date-formgroup').append('<span class="help-block text-red">Start Date is Required</span>');
    }
    $('#sched-date-formgroup').addClass('error');
});
$("#sched-time").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#sched-time-formgroup').hasClass('error'))) {
        $('#sched-time-formgroup').append('<span class="help-block text-red">Start Time is Required</span>');
    }
    $('#sched-time-formgroup').addClass('error');
});
$("#sched-module").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#sched-module-formgroup').hasClass('error'))) {
        $('#sched-module-formgroup').append('<span class="help-block text-red">Process Code is Required</span>');
    }
    $('#sched-module-formgroup').addClass('error');
});
$("#edit-sched-date").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sched-date-formgroup').hasClass('error'))) {
        $('#edit-sched-date-formgroup').append('<span class="help-block text-red">Start Date is Required</span>');
    }
    $('#edit-sched-date-formgroup').addClass('error');
});
$("#edit-sched-time").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sched-time-formgroup').hasClass('error'))) {
        $('#edit-sched-time-formgroup').append('<span class="help-block text-red">Start Time is Required</span>');
    }
    $('#edit-sched-time-formgroup').addClass('error');
});
$("#edit-sched-module").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sched-module-formgroup').hasClass('error'))) {
        $('#edit-sched-module-formgroup').append('<span class="help-block text-red">Process Code is Required</span>');
    }
    $('#edit-sched-module-formgroup').addClass('error');
});


$("#schedemail-code").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedemail-code-formgroup').hasClass('error'))) {
        $('#schedemail-code-formgroup').append('<span class="help-block text-red">Schedule Code is Required</span>');
    }
    $('#schedemail-code-formgroup').addClass('error');
});
$("#schedemail-date").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedemail-date-formgroup').hasClass('error'))) {
        $('#schedemail-date-formgroup').append('<span class="help-block text-red">Start Date is Required</span>');
    }
    $('#schedemail-date-formgroup').addClass('error');
});
$("#schedemail-time").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedemail-time-formgroup').hasClass('error'))) {
        $('#schedemail-time-formgroup').append('<span class="help-block text-red">Start Time is Required</span>');
    }
    $('#schedemail-time-formgroup').addClass('error');
});
$("#schedemail-module").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedemail-module-formgroup').hasClass('error'))) {
        $('#schedemail-module-formgroup').append('<span class="help-block text-red">Process Code is Required</span>');
    }
    $('#schedemail-module-formgroup').addClass('error');
});
$("#edit-schedemail-date").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-schedemail-date-formgroup').hasClass('error'))) {
        $('#edit-schedemail-date-formgroup').append('<span class="help-block text-red">Start Date is Required</span>');
    }
    $('#edit-schedemail-date-formgroup').addClass('error');
});
$("#edit-schedemail-time").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-schedemail-time-formgroup').hasClass('error'))) {
        $('#edit-schedemail-time-formgroup').append('<span class="help-block text-red">Start Time is Required</span>');
    }
    $('#edit-schedemail-time-formgroup').addClass('error');
});
$("#edit-schedemail-module").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-schedemail-module-formgroup').hasClass('error'))) {
        $('#edit-schedemail-module-formgroup').append('<span class="help-block text-red">Email Code is Required</span>');
    }
    $('#edit-schedemail-module-formgroup').addClass('error');
});


$("#schedsync-code").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedsync-code-formgroup').hasClass('error'))) {
        $('#schedsync-code-formgroup').append('<span class="help-block text-red">Schedule Code is Required</span>');
    }
    $('#schedsync-code-formgroup').addClass('error');
});
$("#schedsync-date").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedsync-date-formgroup').hasClass('error'))) {
        $('#schedsync-date-formgroup').append('<span class="help-block text-red">Start Date is Required</span>');
    }
    $('#schedsync-date-formgroup').addClass('error');
});
$("#schedsync-time").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedsync-time-formgroup').hasClass('error'))) {
        $('#schedsync-time-formgroup').append('<span class="help-block text-red">Start Time is Required</span>');
    }
    $('#schedsync-time-formgroup').addClass('error');
});
$("#schedsync-module").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#schedsync-module-formgroup').hasClass('error'))) {
        $('#schedsync-module-formgroup').append('<span class="help-block text-red">Process Code is Required</span>');
    }
    $('#schedsync-module-formgroup').addClass('error');
});
$("#edit-schedsync-date").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-schedsync-date-formgroup').hasClass('error'))) {
        $('#edit-schedsync-date-formgroup').append('<span class="help-block text-red">Start Date is Required</span>');
    }
    $('#edit-schedsync-date-formgroup').addClass('error');
});
$("#edit-schedsync-time").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-schedsync-time-formgroup').hasClass('error'))) {
        $('#edit-schedsync-time-formgroup').append('<span class="help-block text-red">Start Time is Required</span>');
    }
    $('#edit-schedsync-time-formgroup').addClass('error');
});
$("#edit-schedsync-module").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-schedsync-module-formgroup').hasClass('error'))) {
        $('#edit-schedsync-module-formgroup').append('<span class="help-block text-red">Sync Query Code is Required</span>');
    }
    $('#edit-schedsync-module-formgroup').addClass('error');
});




$('.modal').on('hidden.bs.modal', function (e) {
    //addon-submit
    $('.form-group').removeClass('error');
    $('.help-block').remove();
});