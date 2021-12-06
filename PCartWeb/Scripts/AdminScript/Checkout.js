$(document).ready(function () {
    $.post('../Home/CheckoutItems', {
    }, function (data) {
        var tr = "";
        var div = "";
        var totalall = 0;
        var tr2 = "";
        var deliverfee = 0;
        $('#checkoutTable tr').remove();
        for (var rec in data) {
            deliverfee = data[rec].Delivery_fee;
            totalall = totalall + parseFloat(data[rec].Subtotal);
        }

        div += "<p> Name: "+ data[0].Name +"";
        div += "<p> Address: "+data[0].Address+" </p>";
        $('#addressDetails').html(div);
    });
});