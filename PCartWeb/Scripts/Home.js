$(document).ready(function () {
    $.post('../Home/DisplayAllProd', {},
        function (data) {
            var div = "";
            var i = 0;
            for (var rec in data) {
                div += "<div class='col - xl - 3 col - md - 10'>";
                div += "<div class='card'>";
                div += "<img src='" + data[rec].Image + "' class='card - img - top' height = '230' alt = '...'> ";

                div += "<button class='card - body btn btn - light'>";
                div += "<h5 class='card - title'>" + data[rec].ProductName + "</h5>";
                div += "<p class='card - text'>" + data[rec].desc + "</p>";
                div += "</button>";

                div += "<ul class='list - group list - group - flush'>";
                div += "<li class='list - group - item'>PHP " + data[rec].price + "</li></ul>"; 

                div += "<div class='card - footer'>";
                div += "<p class='card - text'>";
                div += "<a href='#' class='btn btn - primary'>Buy</a> <a href='#' class='btn btn - info'>Add to Cart</a>";
                if (data[rec].Favorite == true) {
                    div += "<img src='~/Images/heartred.png' onclick='' style='cursor: pointer; ' height='25' width='25' />";
                }
                else {
                    div += "<img src='~/Images/heart.png' onclick='' style='cursor: pointer; ' height='25' width='25' />";
                }
                div += "</p>";
                div += "</div>";
                div += "</div>";
                div += "<br />";
                div += "</div>";
            }
            $('#dispprod').html(div);
        });
});