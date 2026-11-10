
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
    var keywordEl = document.getElementById('f-keyword');
    var sortPrice = document.getElementById('f-sort-price');
    var sortYear = document.getElementById('f-sort-year');
    var tokenEl = form.querySelector('input[name="__RequestVerificationToken"]');

    var PAGE_SIZE = 12;
    var controller = null;
    var favoriteIds = new Set();
    var pendingFavorites = new Set();

    var PLACEHOLDER = 'data:image/svg+xml;utf8,' + encodeURIComponent(
        '<svg xmlns="http://www.w3.org/2000/svg" width="600" height="400">' +
        '<rect width="100%" height="100%" fill="#e9ecef"/>' +
        '<text x="50%" y="50%" fill="#6c757d" font-family="sans-serif" font-size="28" ' +
        'text-anchor="middle" dominant-baseline="middle">Chưa có ảnh</text></svg>'
    );

    var HEART = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" fill="currentColor" ' +
        'viewBox="0 0 16 16" aria-hidden="true"><path d="M8 2.748l-.717-.737C5.6.281 2.514.878 ' +
        '1.4 3.053c-.523 1.023-.641 2.5.314 4.385.92 1.815 2.834 3.989 6.286 6.357 3.452-2.368 ' +
        '5.365-4.542 6.286-6.357.955-1.886.838-3.362.314-4.385C13.486.878 10.4.28 8.717 2.01L8 ' +
        '2.748zM8 15C-7.333 4.868 3.279-3.04 7.824 1.143c.06.055.119.112.176.171a3.12 3.12 0 0 1 ' +
        '.176-.17C12.72-3.042 23.333 4.867 8 15z"/></svg>';

    function esc(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return {
                '&': '&amp;',
                '<': '&lt;',
                '>': '&gt;',
                '"': '&quot;',
                "'": '&#39;'
            }[c];
        });
    }

    function formatPrice(n) {
        return Number(n).toLocaleString('vi-VN') + ' ₫';
    }

    function checkedValues(name) {
        return Array.prototype.map.call(
            form.querySelectorAll('input[name="' + name + '"]:checked'),
            function (el) {
                return el.value;
            }
        );
    }

    function buildQuery(page) {
        var params = new URLSearchParams();

        checkedValues('brandIds').forEach(function (v) {
            params.append('brandIds', v);
        });

        checkedValues('bodyTypes').forEach(function (v) {
            params.append('bodyTypes', v);
        });

        checkedValues('fuels').forEach(function (v) {
            params.append('fuels', v);
        });
        if (keywordEl && keywordEl.value.trim()) {
            params.set('keyword', keywordEl.value.trim());
        }

        if (yearEl.value) {
            params.set('year', yearEl.value);
        }

        if (priceEl.value) {
            var parts = priceEl.value.split('-');

            if (parts[0]) {
                params.set('minPrice', parts[0]);
            }

            if (parts[1]) {
                params.set('maxPrice', parts[1]);
            }
        }

        if (sortPrice.value) {
            params.set('priceSort', sortPrice.value);
        }

        params.set('yearSort', sortYear.value || 'newest');
        params.set('page', page);
        params.set('pageSize', PAGE_SIZE);

        return params.toString();
    }

    function favoriteButtonHtml(carId) {
        var isFavorite = favoriteIds.has(Number(carId));
        var isPending = pendingFavorites.has(Number(carId));

        var buttonClass = isFavorite
            ? 'btn btn-danger btn-sm w-100 mt-auto position-relative btn-favorite'
            : 'btn btn-primary btn-sm w-100 mt-auto position-relative btn-favorite';

        var buttonText = isFavorite
            ? 'Đã yêu thích'
            : 'Yêu thích';

        return '<button type="button" class="' + buttonClass + '" ' +
            'data-car-id="' + carId + '" ' +
            'aria-pressed="' + (isFavorite ? 'true' : 'false') + '" ' +
            (isPending ? 'disabled ' : '') + '>' +
            HEART + ' ' + buttonText +
            '</button>';
    }

    // Tạo thẻ xe
    function cardHtml(car) {
        var url = '/Cars/Detail/' + encodeURIComponent(car.id);

        var soldOut = car.inStock
            ? ''
            : '<span class="badge text-bg-danger position-absolute top-0 start-0 m-2">Hết hàng</span>';

        return '' +
            '<div class="col">' +
            '<div class="card h-100 position-relative car-clickable" ' +
            'data-detail-url="' + url + '" role="link" tabindex="0" ' +
            'aria-label="Xem chi tiết ' + esc(car.name) + '" style="cursor:pointer">' +
            soldOut +
            '<img src="' + esc(car.image || PLACEHOLDER) + '" ' +
            'class="card-img-top" alt="' + esc(car.name) + '" ' +
            'style="aspect-ratio:3/2;object-fit:cover" loading="lazy">' +
            '<div class="card-body d-flex flex-column p-3">' +
            '<h2 class="h6 card-title mb-1">' +
            '<a class="text-decoration-none text-reset" href="' + url + '">' +
            esc(car.name) +
            '</a></h2>' +
            '<div class="fw-bold mb-1">' + formatPrice(car.price) + '</div>' +
            '<div class="small text-muted mb-3">' +
            esc(car.year) + ', ' +
            esc(car.gearbox) + ', ' +
            esc(car.fuel) +
            '</div>' +
            favoriteButtonHtml(car.id) +
            '</div></div></div>';
    }

    function pagerHtml(page, totalPages) {
        if (totalPages <= 1) return '';

        var start = Math.max(1, page - 2);
        var end = Math.min(totalPages, start + 4);
        start = Math.max(1, end - 4);

        var item = function (label, target, disabled, active) {
            return '<li class="page-item' +
                (disabled ? ' disabled' : '') +
                (active ? ' active' : '') +
                '">' +
                '<a class="page-link" href="#" data-page="' + target + '">' +
                label +
                '</a></li>';
        };

        var html = '<ul class="pagination justify-content-center">';

        html += item('Trước', page - 1, page === 1, false);

        for (var i = start; i <= end; i++) {
            html += item(i, i, false, i === page);
        }

        html += item('Sau', page + 1, page === totalPages, false);

        return html + '</ul>';
    }

    function updateFavoriteButtons(carId) {
        listEl.querySelectorAll('.btn-favorite').forEach(function (button) {
            if (Number(button.dataset.carId) !== carId) return;

            var isFavorite = favoriteIds.has(carId);
            var isPending = pendingFavorites.has(carId);

            button.classList.toggle('btn-danger', isFavorite);
            button.classList.toggle('btn-primary', !isFavorite);

            button.innerHTML = HEART + ' ' +
                (isFavorite ? 'Đã yêu thích' : 'Yêu thích');

            button.setAttribute('aria-pressed', String(isFavorite));
            button.disabled = isPending;
        });
    }

    async function loadFavoriteStatus() {
        var response = await fetch('/Favorite/Status', {
            method: 'GET',
            credentials: 'same-origin',
            cache: 'no-store'
        });

        if (!response.ok) {
            throw new Error('Không tải được trạng thái yêu thích: ' + response.status);
        }

        var data = await response.json();

        favoriteIds = new Set(
            (data.carIds || []).map(function (id) {
                return Number(id);
            })
        );
    }

    async function toggleFavorite(carId) {
        if (pendingFavorites.has(carId)) return;

        if (!tokenEl || !tokenEl.value) {
            alert('Không tìm thấy mã xác thực yêu cầu. Vui lòng tải lại trang.');
            return;
        }

        pendingFavorites.add(carId);
        updateFavoriteButtons(carId);

        try {
            var body = new URLSearchParams();
            body.set('carId', String(carId));
            body.set('__RequestVerificationToken', tokenEl.value);

            var response = await fetch('/Favorite/ToggleAjax', {
                method: 'POST',
                credentials: 'same-origin',
                headers: {
                    'Content-Type': 'application/x-www-form-urlencoded;charset=UTF-8'
                },
                body: body.toString()
            });

            if (!response.ok) {
                throw new Error('HTTP ' + response.status);
            }

            var data = await response.json();

            if (!data.success) {
                throw new Error(data.message || 'Không thể cập nhật yêu thích.');
            }

            if (data.isFavorite) {
                favoriteIds.add(carId);
            } else {
                favoriteIds.delete(carId);
            }
        } catch (err) {
            console.error('Lỗi cập nhật yêu thích:', err);
            alert('Không thể cập nhật yêu thích. Vui lòng thử lại.');
        } finally {
            pendingFavorites.delete(carId);
            updateFavoriteButtons(carId);
        }
    }

    function load(page) {
        if (controller) {
            controller.abort();
        }

        controller = new AbortController();
        var signal = controller.signal;

        loadingEl.classList.remove('d-none');
        emptyEl.classList.add('d-none');

        fetch('/api/cars?' + buildQuery(page), {
            signal: signal
        })
            .then(function (res) {
                if (!res.ok) {
                    throw new Error('HTTP ' + res.status);
                }

                return res.json();
            })
            .then(function (data) {
                if (signal.aborted) return;

                listEl.innerHTML = data.items.map(cardHtml).join('');
                pagerEl.innerHTML = pagerHtml(data.page, data.totalPages);

                if (countEl) {
                    countEl.textContent = data.totalItems + ' xe';
                }

                emptyEl.textContent =
                    'Không có xe nào khớp bộ lọc. Hãy nới khoảng giá hoặc bấm "Xóa bộ lọc".';

                emptyEl.classList.toggle('d-none', data.items.length > 0);
                loadingEl.classList.add('d-none');
            })
            .catch(function (err) {
                if (err.name === 'AbortError' || signal.aborted) return;

                loadingEl.classList.add('d-none');
                listEl.innerHTML = '';
                pagerEl.innerHTML = '';

                if (countEl) {
                    countEl.textContent = '';
                }

                emptyEl.textContent =
                    'Không tải được danh sách xe. Vui lòng thử lại sau.';

                emptyEl.classList.remove('d-none');

                console.error(err);
            });
    }

    form.addEventListener('change', function () {
        load(1);
    });

    form.addEventListener('submit', function (e) {
        e.preventDefault();
        load(1);
    });

    sortPrice.addEventListener('change', function () {
        load(1);
    });

    sortYear.addEventListener('change', function () {
        load(1);
    });

    document.getElementById('filter-reset').addEventListener('click', function () {
        form.querySelectorAll('input[type="checkbox"]').forEach(function (c) {
            c.checked = false;
        });

        yearEl.value = '';
        priceEl.value = '';
        sortPrice.value = '';
        sortYear.value = 'newest';
        if (keywordEl) keywordEl.value = '';
        var navInput = document.getElementById('nav-search-input');
        if (navInput) navInput.value = '';
        history.replaceState(null, '', window.location.pathname);

        load(1);
    });

    pagerEl.addEventListener('click', function (e) {
        var link = e.target.closest('a[data-page]');

        if (!link) return;

        e.preventDefault();

        var li = link.parentElement;

        if (li.classList.contains('disabled') ||
            li.classList.contains('active')) {
            return;
        }

        load(parseInt(link.dataset.page, 10));

        listEl.scrollIntoView({
            behavior: 'smooth',
            block: 'start'
        });
    });

    // Bấm vào bất kỳ vị trí nào trên thẻ xe cũng mở trang chi tiết,
    // ngoại trừ nút yêu thích.
    listEl.addEventListener('click', function (e) {
        var button = e.target.closest('.btn-favorite');

        if (button && listEl.contains(button)) {
            e.preventDefault();
            e.stopPropagation();

            var carId = Number(button.dataset.carId);
            if (!Number.isInteger(carId) || carId <= 0) return;

            toggleFavorite(carId);
            return;
        }

        // Giữ hành vi mặc định của liên kết tên xe.
        if (e.target.closest('a')) return;

        var card = e.target.closest('.car-clickable');
        if (!card || !listEl.contains(card)) return;

        var detailUrl = card.dataset.detailUrl;
        if (detailUrl) {
            window.location.href = detailUrl;
        }
    });

    // Hỗ trợ mở chi tiết bằng Enter hoặc Space.
    listEl.addEventListener('keydown', function (e) {
        if (e.key !== 'Enter' && e.key !== ' ') return;

        var card = e.target.closest('.car-clickable');
        if (!card || !listEl.contains(card)) return;

        // Không chuyển trang khi thao tác với nút hoặc liên kết.
        if (e.target.closest('.btn-favorite, a, input, select, textarea')) return;

        e.preventDefault();

        if (card.dataset.detailUrl) {
            window.location.href = card.dataset.detailUrl;
        }
    });

    loadFavoriteStatus()
        .then(function () {
            load(1);
        })
        .catch(function (err) {
            console.error(err);

            loadingEl.classList.add('d-none');
            listEl.innerHTML = '';
            pagerEl.innerHTML = '';

            if (countEl) {
                countEl.textContent = '';
            }

            emptyEl.textContent =
                'Không tải được trạng thái yêu thích. Vui lòng tải lại trang.';

            emptyEl.classList.remove('d-none');
        });
})();
