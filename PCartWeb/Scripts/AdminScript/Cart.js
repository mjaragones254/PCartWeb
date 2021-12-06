$(document).ready(function () {

    $('.get_value').change(function () {
        if ($(this).prop('checked')) {
            alert("Checked Box Selected");
        } else {
            alert("Checked Box deselect");
        }
    });
});

function onClickHandler() {
    var itemSelected = [];
    $('.get_value').each(function () {
        if ($(this).is(":checked")) {
            itemSelected.push($(this).val());
        }
    });

    itemSelected = itemSelected.toString();

    $.post('../Home/CalculateItems', {
        itemSelected: itemSelected,
    }, function (data) {
            document.getElementById("total").value = data[0].total;
    });
}
