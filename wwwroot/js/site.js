$(document).ready(function () {
    // Thêm logic tùy chỉnh, ví dụ: xác nhận trước khi xóa
    $('form').on('submit', function (e) {
        if ($(this).attr('asp-action') === 'Delete') {
            return confirm('Are you sure you want to delete this event?');
        }
    });
});