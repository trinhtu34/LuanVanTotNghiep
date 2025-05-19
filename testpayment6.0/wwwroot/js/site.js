// Định dạng số tiền khi người dùng nhập
$(document).ready(function () {
    const formatCurrency = function (number) {
        return new Intl.NumberFormat('vi-VN').format(number);
    };

    const amountInput = $('[name="Amount"]');

    // Format khi người dùng rời khỏi input
    amountInput.on('blur', function () {
        const value = $(this).val().replace(/[^\d]/g, '');
        if (value && !isNaN(value)) {
            $(this).val(value);
        }
    });

    // Hiển thị số tiền đã định dạng ở nơi khác nếu cần
    $('.formatted-amount').each(function () {
        const value = $(this).text();
        if (value && !isNaN(value)) {
            $(this).text(formatCurrency(value));
        }
    });
});