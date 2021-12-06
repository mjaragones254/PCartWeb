$(document).ready(function () {
    $.post('../Home/DisplayAllProducts', {},
        function (data) {
            var tr = "";
            var i = 0;
            $('#displayproducts tr').remove();
            for (var rec in data) {
                tr += "<tr style='text-align: center;'>";
                i++;
                tr += "<td>";
                tr += i;
                tr += "</td>";

                tr += "<td>";
                tr += data[rec].brand;
                tr += "</td>";

                tr += "<td>";
                tr += data[rec].desc;
                tr += "</td>";

                tr += "<td>";
                tr += data[rec].qty;
                tr += "</td>";

                tr += "<td>";
                tr += data[rec].price;
                tr += "</td>";

                tr += "<td>";
                tr += data[rec].category;
                tr += "</td>";

                tr += "<td>";
                tr += data[rec].minqty;
                tr += "</td>";

                tr += "<td>";
                tr += "<button type='button' data-toggle='modal' data-target='#confirmup' onclick='UpdateProd(" + data[rec].prod_id + ")' class='btn btn-info'>Edit</button>";
                tr += "  ";
                tr += "<button type='button' data-toggle='modal' data-target='#deletecon' onclick='DeleteCon(" + data[rec].prod_id + ")' class='btn btn-dark'>Remove</button>";
                tr += "</td>";

                tr += "</tr>";
            }
            $('#displayproducts').html(tr);
        });

});