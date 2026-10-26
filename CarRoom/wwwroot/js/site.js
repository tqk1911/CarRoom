// Form Thêm / Sửa xe (Admin): trình soạn thảo cho Mô tả và thêm / xóa dòng màu.
// Chỉ chạy khi trang có các phần tử của form xe, các trang khác tự bỏ qua.
document.addEventListener('DOMContentLoaded', function () {
    'use strict';

    // 1. CKEditor cho ô Mô tả
    var moTa = document.getElementById('MoTa');
    if (moTa && window.ClassicEditor) {
        ClassicEditor.create(moTa, {
            toolbar: ['heading', '|', 'bold', 'italic', 'link', 'bulletedList', 'numberedList', 'blockQuote', 'undo', 'redo']
        }).catch(function (err) {
            console.error('Không khởi tạo được CKEditor:', err);
        });
    }

    // 2. Thêm / xóa màu
    var list = document.getElementById('color-list');
    var template = document.getElementById('color-template');
    var addBtn = document.getElementById('add-color');
    if (!list || !template || !addBtn) return;

    // Chỉ số tiếp theo = lớn nhất hiện có + 1. Dùng hidden "Colors.Index" nên chỉ số không cần liên tục.
    var nextIndex = 0;
    list.querySelectorAll('input[name="Colors.Index"]').forEach(function (el) {
        nextIndex = Math.max(nextIndex, parseInt(el.value, 10) + 1);
    });

    addBtn.addEventListener('click', function () {
        var html = template.innerHTML.replace(/__i__/g, String(nextIndex++));
        list.insertAdjacentHTML('beforeend', html);
        var rows = list.querySelectorAll('.color-row');
        var input = rows[rows.length - 1].querySelector('input[type="text"], input:not([type])');
        if (input) input.focus();
    });

    list.addEventListener('click', function (e) {
        var btn = e.target.closest('.remove-color');
        if (!btn) return;

        var rows = list.querySelectorAll('.color-row');
        if (rows.length <= 1) {
            alert('Xe phải có ít nhất 1 màu.');
            return;
        }

        var row = btn.closest('.color-row');
        var hasImages = row.querySelector('input[name$=".RemoveImageIds"]') !== null;
        var message = hasImages
            ? 'Xóa màu này sẽ xóa luôn các ảnh của màu. Tiếp tục?'
            : 'Xóa màu này?';
        if (confirm(message)) row.remove();
    });
});