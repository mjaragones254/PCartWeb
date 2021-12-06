$(document).ready(function () {
    $.post('../Home/DisplayCategories', {},
        function (data) {
            var tr = "";
            for (var rec in data) {
                tr += '<li class="nav-item"';

                tr += '<a class="nav-link" onclick="Show('+data[rec].Id+')" href="#" style="cursor:pointer">';
                tr += data[rec].Name;
                tr += '</a>';

                tr += '</li>';
            }
            $('#showCat').html(tr);
        });
});

function Show(id) {
    $.post('../Home/ShowCatProds', {
        id: id,
    }, function (data) {
        var div = "";
        $('#showProds').remove();
        for (var rec in data) {
            div += '<div class="col-md-3">';
            div += '<div class="card card-product-grid">';
            div += '<div class="card-body"';
            div += '<div href="#" onclick="ShowDetail(' + data[rec].Id + ')">';
            div += '<a href="#" class="img-wrap">';
            div += '<img src="~/Images/' + data[rec].Image + '"></a>';
            div += '<figcaption class="info-wrap">';
            div += '<a href="#" class="title">';
            div += data[rec].ProdName + '</a>';
            if (data[rec].DiscountPrice != 0) {
                var total = 0;
                div += '<div class="price mt-1">';
                div += '<span class="origprice">';
                div += '&#8369; ' + data[rec].ProdPrice + '</span>';
                div += '&#8369;';
                total = parseFloat(data[rec].ProdPrice) / (parseFloat(data[rec].ProdPrice) - parseFloat(data[rec].DiscountPrice));
                div += total;
                div += ' &percnt; OFF</div>';
            }
            else {
                div += '<div class="price mt-1">&#8369;';
                div += data[rec].ProdPrice;
                div += '</div>';
            }
            div += '</figcaption>';
            div += '<div class="price mt-1">';
            div += '<a href="#" class="btn btn-info">Add To Cart</a>';
            if (data[rec].Wish == "true") {
                div += '<img src="~/Images/heartred.png" style="cursor:pointer;" height="25" width="25" />';
            }
            else {
                div += '<img src="~/Images/heart.png" style="cursor:pointer;" height="25" width="25" />';
            }
            div += "</div>";
        }
        $('#showProds').html(div);
    });
}