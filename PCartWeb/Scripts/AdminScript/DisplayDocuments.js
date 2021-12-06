$(document).ready(function () {
    var id = $('#myid').val();
    $.post('../Admin/DisplayCoopDocuments', {
        id: id,
    }, function (data) {
        var div = "";
        for (var rec in data) {
            div += '<div class="col-md-3">';
            div += '<img src='+data[rec].Image+' width="200" height="200" style="margin:10px" />';
            div += '</div>';
        }
        $("#displayDocuments").html(div);
    });
});