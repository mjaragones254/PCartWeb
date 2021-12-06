$(document).ready(function () {

    $.post('../Home/LoadCart', {
    }, function (data) {
        var tr = "";
        $('#vwCartItems tr').remove();
        for (var rec in data) {
            tr += "<tr>";
            tr += "<td style='text-align:center' >";
            tr += '<input type="checkbox" class="get_value" id="cart" value="' + data[rec].ProdCartId + '">';
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += data[rec].Firstname + " " + data[rec].Lastname;
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += data[rec].Created_at;
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += data[rec].Updated_at;
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += data[rec].Email;
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += "";
            tr += "</td>";
            tr += "</tr>";
        }
        $("#vwCartItems").html(tr);
    });

    $('.get_value').change(function () {
        if ($(this).prop('checked')) {
            alert("Checked Box Selected");
        } else {
            alert("Checked Box deselect");
        }
    });
    $('#btnDelete').click(function () {
        var itemsSelected = [];

        $('.get_value').each(function () {
            if ($(this).is(":checked")) {
                itemsSelected.push($(this).val());
            }
        });

        itemsSelected = itemsSelected.toString();

        $.post('../Home/DeleteCartItem', {
            itemsSelected: itemsSelected,
        }, function (data) {
            if (data[0].mess == 1) {
                alert('Deleted Successfully!');
                window.location.href = "/Home/DisplayCart";
            }
        });
    });

    $('#btnCheckout').click(function () {
        var itemsSelected = [];

        $('.get_value').each(function () {
            if ($(this).is(":checked")) {
                itemsSelected.push($(this).val());
            }
        });

        itemsSelected = itemsSelected.toString();

        $.post('../Home/SaveCheckouts', {
            itemsSelected: itemsSelected,
        }, function (data) {
            window.location.href = "/Home/CheckoutPage";
        });
    });


});