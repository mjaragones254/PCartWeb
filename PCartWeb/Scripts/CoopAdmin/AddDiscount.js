$(document).ready(function () {
    $.post('../Coopadmin/LoadProduct', {
    }, function (data) {
        var tr = "";
        $('#discountProduct tr').remove();
        for (var rec in data) {
            tr += "<tr>";
            tr += "<td style='text-align:center' >";
            tr += '<input type="checkbox" class="get_value" onclick="onClickHandler()" id="cart" value="' + data[rec].ProdID + '">';
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += '<img src="' + data[rec].Image + '" width="50" height="50" />';
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += data[rec].ProductName;
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += "PHP  " + data[rec].ProductPrice;
            tr += "</td>";
            tr += "<td style='text-align:center'>";
            tr += "PHP  " + data[rec].ProductQty;
            tr += "</td>";
            tr += "</tr>";
        }

        $("#discountProduct").html(tr);
    });

    $('#btnDiscount').click(function () {
        var name = $('#discountName').val();
        var percent = $('#discountPercent').val();
        var dateStart = $('#discountDateStart').val();
        var dateEnd = $('#discountDateEnd').val();
        var itemsSelected = [];
        var date1 = new Date(dateStart);
        var date2 = new Date(dateEnd);
        var currDate = new Date();
        date1.setHours(0, 0, 0, 0);
        date2.setHours(0, 0, 0, 0);
        currDate.setHours(0, 0, 0, 0)

        $('.get_value').each(function () {
            if ($(this).is(":checked")) {
                itemsSelected.push($(this).val());
            }
        });

        itemsSelected = itemsSelected.toString();

        if (name == '' || percent == '' || dateStart == '' || dateEnd == '' || itemsSelected == '') {
            $('#discountName, #discountPercent, #discountDateStart, #discountDateEnd').addClass('error');
            alert('Kindly fill all fields.')
        }
        else if (date1 < currDate) {
            alert('Kindly input a valid start date.')
        }
        else if (date2 < date1) {
            alert('Kindly input a valid end date.')
        }
        else {
            $.post('../Coopadmin/SaveDiscount', {
                name: name,
                percent: percent,
                dateStart: dateStart,
                dateEnd: dateEnd,
                itemsSelected: itemsSelected,
            }, function (data) {
                if (data[0].mess == 1) {
                    alert('Save Successfully!');
                    $('#discountName, #discountPercent, #discountDateStart, #discountDateEnd').val("");
                    $('.get_value').each(function () {
                        if ($(this).is(":checked")) {
                            $(this).prop("checked", false);
                        }
                    });
                }
                else {
                    alert('Product/s are already discounted.');
                }
            });
        }
    });
});