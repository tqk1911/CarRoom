// Trang Danh sách xe: lọc, sắp xếp, phân trang bằng AJAX (không tải lại trang).
// Gọi GET /api/cars rồi vẽ lại danh sách và thanh phân trang.
(function () {
    'use strict';

    var form = document.getElementById('filter-form');
    var listEl = document.getElementById('car-list');
    if (!form || !listEl) return;

    var pagerEl = document.getElementById('car-pagination');
    var loadingEl = document.getElementById('car-loading');
    var emptyEl = document.getElementById('car-empty');
    var countEl = document.getElementById('result-count');
    var priceEl = document.getElementById('f-price');
    var yearEl = document.getElementById('f-year');
    var sortPrice = document.getElementById('f-sort-price');
    var sortYear = document.getElementById('f-sort-year');

    var PAGE_SIZE = 12;
    var controller = null; // hủy request cũ nếu người dùng đổi bộ lọc liên tục

    var PLACEHOLDER = 'data:image/svg+xml;utf8,' + encodeURIComponent(
        '<svg xmlns="http://www.w3.org/2000/svg" width="600" height="400">' +
        '<rect width="100%" height="100%" fill="#e9ecef"/>' +
        '<text x="50%" y="50%" fill="#6c757d" font-family="sans-serif" font-size="28" ' +
        'text-anchor="middle" dominant-baseline="middle">Chưa có ảnh</text></svg>');

    var HEART = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" ' +
        'viewBox="0 0 16 16" aria-hidden="true"><path d="M8 2.748l-.717-.737C5.6.281 2.514.878 ' +
        '1.4 3.053c-.523 1.023-.641 2.5.314 4.385.92 1.815 2.834 3.989 6.286 6.357 3.452-2.368 ' +
        '5.365-4.542 6.286-6.357.955-1.886.838-3.362.314-4.385C13.486.878 10.4.28 8.717 2.01L8 ' +
        '2.748zM8 15C-7.333 4.868 3.279-3.04 7.824 1.143c.06.055.119.112.176.171a3.12 3.12 0 0 1 ' +
        '.176-.17C12.72-3.042 23.333 4.867 8 15z"/></svg>';

    function esc(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function formatPrice(n) {
        return Number(n).toLocaleString('vi-VN') + ' ₫';
    }

    function checkedValues(name) {
        return Array.prototype.map.call(
            form.querySelectorAll('input[name="' + name + '"]:checked'),
            function (el) { return el.value; });
    }

    function buildQuery(page) {
        var params = new URLSearchParams();

        checkedValues('brandIds').forEach(function (v) { params.append('brandIds', v); });
        checkedValues('bodyTypes').forEach(function (v) { params.append('bodyTypes', v); });
        checkedValues('fuels').forEach(function (v) { params.append('fuels', v); });

        if (yearEl.value) params.set('year', yearEl.value);

        // Mức giá dạng "min-max", đầu nào trống là không giới hạn
        if (priceEl.value) {
            var parts = priceEl.value.split('-');
            if (parts[0]) params.set('minPrice', parts[0]);
            if (parts[1]) params.set('maxPrice', parts[1]);
        }

        // Hai kiểu sắp xếp gửi riêng, cùng có tác dụng
        if (sortPrice.value) params.set('priceSort', sortPrice.value);
        params.set('yearSort', sortYear.value || 'newest');
        params.set('page', page);
        params.set('pageSize', PAGE_SIZE);
        return params.toString();
    }

    function cardHtml(car) {
        var url = '/Cars/Detail/' + car.id;
        var soldOut = car.inStock ? ''
            : '<span class="badge text-bg-danger position-absolute top-0 start-0 m-2">Hết hàng</span>';

        return '' +
            '<div class="col">' +
            '<div class="card h-100 position-relative">' +
            soldOut +
            '<img src="' + esc(car.image || PLACEHOLDER) + '" class="card-img-top" alt="' + esc(car.name) + '" ' +
            'style="aspect-ratio:3/2;object-fit:cover" loading="lazy">' +
            '<div class="card-body d-flex flex-column p-3">' +
            '<h2 class="h6 card-title mb-1"><a class="text-decoration-none text-reset stretched-link" href="' + url + '">' +
            esc(car.name) + '</a></h2>' +
            '<div class="fw-bold mb-1">' + formatPrice(car.price) + '</div>' +
            '<div class="small text-muted mb-3">' + esc(car.year) + ', ' + esc(car.gearbox) + ', ' + esc(car.fuel) + '</div>' +
            // Người B gắn xử lý Yêu thích bằng class btn-favorite và data-car-id
            '<button type="button" class="btn btn-primary btn-sm w-100 mt-auto position-relative btn-favorite" ' +
            'data-car-id="' + car.id + '">' + HEART + ' Yêu thích</button>' +
            '</div></div></div>';
    }

    function pagerHtml(page, totalPages) {
        if (totalPages <= 1) return '';

        // Hiện tối đa 5 số trang quanh trang hiện tại
        var start = Math.max(1, page - 2);
        var end = Math.min(totalPages, start + 4);
        start = Math.max(1, end - 4);

        var item = function (label, target, disabled, active) {
            return '<li class="page-item' + (disabled ? ' disabled' : '') + (active ? ' active' : '') + '">' +
                '<a class="page-link" href="#" data-page="' + target + '">' + label + '</a></li>';
        };

        var html = '<ul class="pagination justify-content-center">';
        html += item('Trước', page - 1, page === 1, false);
        for (var i = start; i <= end; i++) html += item(i, i, false, i === page);
        html += item('Sau', page + 1, page === totalPages, false);
        return html + '</ul>';
    }

    function load(page) {
        if (controller) controller.abort();
        controller = new AbortController();

        loadingEl.classList.remove('d-none');
        emptyEl.classList.add('d-none');

        fetch('/api/cars?' + buildQuery(page), { signal: controller.signal })
            .then(function (res) {
                if (!res.ok) throw new Error('HTTP ' + res.status);
                return res.json();
            })
            .then(function (data) {
                listEl.innerHTML = data.items.map(cardHtml).join('');
                pagerEl.innerHTML = pagerHtml(data.page, data.totalPages);
                countEl.textContent = data.totalItems + ' xe';
                emptyEl.textContent = 'Không có xe nào khớp bộ lọc. Hãy nới khoảng giá hoặc bấm "Xóa bộ lọc".';
                emptyEl.classList.toggle('d-none', data.items.length > 0);
                loadingEl.classList.add('d-none');
            })
            .catch(function (err) {
                if (err.name === 'AbortError') return; // request cũ bị hủy, bỏ qua
                loadingEl.classList.add('d-none');
                listEl.innerHTML = '';
                pagerEl.innerHTML = '';
                countEl.textContent = '';
                emptyEl.textContent = 'Không tải được danh sách xe. Vui lòng thử lại sau.';
                emptyEl.classList.remove('d-none');
                console.error(err);
            });
    }

    // Checkbox, ô giá, ô năm: đổi là tải lại từ trang 1
    form.addEventListener('change', function () { load(1); });
    form.addEventListener('submit', function (e) { e.preventDefault(); load(1); });

    // Hai ô sắp xếp nằm ngoài <form> và hoạt động độc lập với nhau
    sortPrice.addEventListener('change', function () { load(1); });
    sortYear.addEventListener('change', function () { load(1); });

    document.getElementById('filter-reset').addEventListener('click', function () {
        form.querySelectorAll('input[type="checkbox"]').forEach(function (c) { c.checked = false; });
        yearEl.value = '';
        priceEl.value = '';
        sortPrice.value = '';
        sortYear.value = 'newest';
        load(1);
    });

    pagerEl.addEventListener('click', function (e) {
        var link = e.target.closest('a[data-page]');
        if (!link) return;
        e.preventDefault();
        var li = link.parentElement;
        if (li.classList.contains('disabled') || li.classList.contains('active')) return;
        load(parseInt(link.dataset.page, 10));
        listEl.scrollIntoView({ behavior: 'smooth', block: 'start' });
    });

    load(1);
})();
