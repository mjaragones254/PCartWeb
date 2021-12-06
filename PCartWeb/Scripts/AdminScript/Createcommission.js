$(document).ready(function () {

    $('#btnClick').click(function () {
        var rate = $('#rate').val();
        if (rate == null) {
            alert('Please enter value');
        }
        else {
            $.post('../Home/CreateCommissionRate', {
                rate: rate,
            }, function () {
            });
        }
    });


});
