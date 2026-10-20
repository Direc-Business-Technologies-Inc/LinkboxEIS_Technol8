$("#addon-code").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-addon-code-formgroup').hasClass('error'))) {
        $('#create-addon-code-formgroup').append('<span class="help-block text-red">Addon Code is Required</span>');
    }
    $('#create-addon-code-formgroup').addClass('error');
});

$("#addon-server").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-addon-server-formgroup').hasClass('error'))) {
        $('#create-addon-server-formgroup').append('<span class="help-block text-red">Server Name is Required</span>');
    }
    $('#create-addon-server-formgroup').addClass('error');
});

$("#addon-ip").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-addon-ip-formgroup').hasClass('error'))) {
        $('#create-addon-ip-formgroup').append('<span class="help-block text-red">IP Address is Required</span>');
    }
    $('#create-addon-ip-formgroup').addClass('error');
});

$("#addon-dbname").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-addon-dbname-formgroup').hasClass('error'))) {
        $('#create-addon-dbname-formgroup').append('<span class="help-block text-red">Database Name is Required</span>');
    }
    $('#create-addon-dbname-formgroup').addClass('error');
});

$("#addon-userid").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-addon-userid-formgroup').hasClass('error'))) {
        $('#create-addon-userid-formgroup').append('<span class="help-block text-red">DB User ID is Required</span>');
    }
    $('#create-addon-userid-formgroup').addClass('error');
});

$("#addon-password").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-addon-password-formgroup').hasClass('error'))) {
        $('#create-addon-password-formgroup').append('<span class="help-block text-red">DB Password is Required</span>');
    }
    $('#create-addon-password-formgroup').addClass('error');
});

$("#edit-addon-server").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-addon-server-formgroup').hasClass('error'))) {
        $('#edit-addon-server-formgroup').append('<span class="help-block text-red">Server Name is Required</span>');
    }
    $('#edit-addon-server-formgroup').addClass('error');
});

$("#edit-addon-ip").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-addon-ip-formgroup').hasClass('error'))) {
        $('#edit-addon-ip-formgroup').append('<span class="help-block text-red">IP Address is Required</span>');
    }
    $('#edit-addon-ip-formgroup').addClass('error');
});
$("#edit-addon-dbname").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-addon-dbname-formgroup').hasClass('error'))) {
        $('#edit-addon-dbname-formgroup').append('<span class="help-block text-red">Database Name is Required</span>');
    }
    $('#edit-addon-dbname-formgroup').addClass('error');
});

$("#edit-addon-user").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-addon-user-formgroup').hasClass('error'))) {
        $('#edit-addon-user-formgroup').append('<span class="help-block text-red">DB User ID is Required</span>');
    }
    $('#edit-addon-user-formgroup').addClass('error');
});
$("#edit-addon-password").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-addon-password-formgroup').hasClass('error'))) {
        $('#edit-addon-password-formgroup').append('<span class="help-block text-red">DB Password is Required</span>');
    }
    $('#edit-addon-password-formgroup').addClass('error');
});
$("#sap-code").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-code-formgroup').hasClass('error'))) {
        $('#create-sap-code-formgroup').append('<span class="help-block text-red">SAP Code is Required</span>');
    }
    $('#create-sap-code-formgroup').addClass('error');
});
$("#sap-dbversion").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-dbversion-formgroup').hasClass('error'))) {
        $('#create-sap-dbversion-formgroup').append('<span class="help-block text-red">DB Version is Required</span>');
    }
    $('#create-sap-dbversion-formgroup').addClass('error');
});
$("#sap-server").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-server-formgroup').hasClass('error'))) {
        $('#create-sap-server-formgroup').append('<span class="help-block text-red">Server Name is Required</span>');
    }
    $('#create-sap-server-formgroup').addClass('error');
});
$("#sap-ip").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-ip-formgroup').hasClass('error'))) {
        $('#create-sap-ip-formgroup').append('<span class="help-block text-red">IP Address is Required</span>');
    }
    $('#create-sap-ip-formgroup').addClass('error');
});
$("#sap-port").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-port-formgroup').hasClass('error'))) {
        $('#create-sap-port-formgroup').append('<span class="help-block text-red">Port is Required</span>');
    }
    $('#create-sap-port-formgroup').addClass('error');
});
$("#sap-dbname").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-dbname-formgroup').hasClass('error'))) {
        $('#create-sap-dbname-formgroup').append('<span class="help-block text-red">Database Name is Required</span>');
    }
    $('#create-sap-dbname-formgroup').addClass('error');
});
$("#sap-dbuser").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-dbuser-formgroup').hasClass('error'))) {
        $('#create-sap-dbuser-formgroup').append('<span class="help-block text-red">DB User ID is Required</span>');
    }
    $('#create-sap-dbuser-formgroup').addClass('error');
});
$("#sap-dbpass").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-dbpass-formgroup').hasClass('error'))) {
        $('#create-sap-dbpass-formgroup').append('<span class="help-block text-red">DB Password is Required</span>');
    }
    $('#create-sap-dbpass-formgroup').addClass('error');
});
$("#sap-user").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-user-formgroup').hasClass('error'))) {
        $('#create-sap-user-formgroup').append('<span class="help-block text-red">SAP User ID is Required</span>');
    }
    $('#create-sap-user-formgroup').addClass('error');
});
$("#sap-pass").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-sap-pass-formgroup').hasClass('error'))) {
        $('#create-sap-pass-formgroup').append('<span class="help-block text-red">SAP Password is Required</span>');
    }
    $('#create-sap-pass-formgroup').addClass('error');
});
$("#edit-sap-dbversion").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-dbversion-formgroup').hasClass('error'))) {
        $('#edit-sap-dbversion-formgroup').append('<span class="help-block text-red">DB Version is Required</span>');
    }
    $('#edit-sap-dbversion-formgroup').addClass('error');
});
$("#edit-sap-server").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-server-formgroup').hasClass('error'))) {
        $('#edit-sap-server-formgroup').append('<span class="help-block text-red">Server Name is Required</span>');
    }
    $('#edit-sap-server-formgroup').addClass('error');
});
$("#edit-sap-ip").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-ip-formgroup').hasClass('error'))) {
        $('#edit-sap-ip-formgroup').append('<span class="help-block text-red">IP Address is Required</span>');
    }
    $('#edit-sap-ip-formgroup').addClass('error');
});
$("#edit-sap-port").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-port-formgroup').hasClass('error'))) {
        $('#edit-sap-port-formgroup').append('<span class="help-block text-red">Port is Required</span>');
    }
    $('#edit-sap-port-formgroup').addClass('error');
});
$("#edit-sap-dbname").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-dbname-formgroup').hasClass('error'))) {
        $('#edit-sap-dbname-formgroup').append('<span class="help-block text-red">DB Name is Required</span>');
    }
    $('#edit-sap-dbname-formgroup').addClass('error');
});
$("#edit-sap-dbuser").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-dbuser-formgroup').hasClass('error'))) {
        $('#edit-sap-dbuser-formgroup').append('<span class="help-block text-red">DB User ID is Required</span>');
    }
    $('#edit-sap-dbuser-formgroup').addClass('error');
});
$("#edit-sap-dbpass").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-dbpass-formgroup').hasClass('error'))) {
        $('#edit-sap-dbpass-formgroup').append('<span class="help-block text-red">DB Password is Required</span>');
    }
    $('#edit-sap-dbpass-formgroup').addClass('error');
});
$("#edit-sap-user").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-user-formgroup').hasClass('error'))) {
        $('#edit-sap-user-formgroup').append('<span class="help-block text-red">SAP User ID is Required</span>');
    }
    $('#edit-sap-user-formgroup').addClass('error');
});
$("#edit-sap-pass").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-sap-pass-formgroup').hasClass('error'))) {
        $('#edit-sap-pass-formgroup').append('<span class="help-block text-red">SAP Password is Required</span>');
    }
    $('#edit-sap-pass-formgroup').addClass('error');
});
$("#path-code").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-path-code-formgroup').hasClass('error'))) {
        $('#create-path-code-formgroup').append('<span class="help-block text-red">Path Code is Required</span>');
    }
    $('#create-path-code-formgroup').addClass('error');
});
$("#path-localpath").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-path-localpath-formgroup').hasClass('error'))) {
        $('#create-path-localpath-formgroup').append('<span class="help-block text-red">Local Path is Required</span>');
    }
    $('#create-path-localpath-formgroup').addClass('error');
});
$("#path-filetype").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#create-path-filetype-formgroup').hasClass('error'))) {
        $('#create-path-filetype-formgroup').append('<span class="help-block text-red">File Type is Required</span>');
    }
    $('#create-path-filetype-formgroup').addClass('error');
});
$("#edit-path-localpath").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-path-localpath-formgroup').hasClass('error'))) {
        $('#edit-path-localpath-formgroup').append('<span class="help-block text-red">Local Path is Required</span>');
    }
    $('#edit-path-localpath-formgroup').addClass('error');
});
$("#edit-path-filetype").on("invalid", function (e) {
    e.preventDefault();
    if (!($('#edit-path-filetype-formgroup').hasClass('error'))) {
        $('#edit-path-filetype-formgroup').append('<span class="help-block text-red">File Type is Required</span>');
    }
    $('#edit-path-filetype-formgroup').addClass('error');
});

$('.modal').on('hidden.bs.modal', function (e) {
    //addon-submit
    $('.form-group').removeClass('error');
    $('.help-block').remove();
});