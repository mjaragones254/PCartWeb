$(document).ready(function () {
    $.post('../Admin/RetrieveUser', {},
        function (data) {
            var tr = "";
            var i = 0;
            $('#retrieveData tr').remove();
            for (var rec in data) {
                tr += "<tr style='text-align: center;'>";

                tr += "<td scope='col'>";
                tr += data[rec].user_email;
                tr += "</td>";

                tr += "<td scope='col'>";
                tr += data[rec].role;
                tr += "</td>";

                tr += "<td scope='col'>";
                tr += "<button type='button' /*data-toggle='modal' data-target='#confirmup' onclick='UpdateProd(" + data[rec].prod_id + ")'*/ class='btn btn-info'>Edit</button>";
                tr += "  ";
                tr += "<button type='button' /*data-toggle='modal' data-target='#deletecon' onclick='DeleteCon(" + data[rec].prod_id + ")'*/ class='btn btn-dark'>Remove</button>";
                tr += "</td>";

                tr += "</tr>";
            }
            $('#retrieveData').html(tr);
        });
});