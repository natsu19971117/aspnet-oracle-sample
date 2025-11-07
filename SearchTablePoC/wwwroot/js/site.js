(function () {
    const stateElement = document.getElementById('records-state');
    const table = document.getElementById('records-table');
    if (!stateElement || !table) {
        return;
    }

    const searchToggle = document.getElementById('search-toggle');
    const searchPanel = document.getElementById('search-panel');
    const searchForm = document.getElementById('search-form');
    const clearButton = document.getElementById('clear-search');
    const pageSizeSelect = document.getElementById('page-size');
    const paginationList = document.getElementById('pagination');
    const csvButton = document.getElementById('download-csv');
    const summaryElement = document.querySelector('[data-summary]');
    const validationElement = document.querySelector('[data-validation-message]');
    const columnFilterInputs = Array.from(document.querySelectorAll('.column-filter'));
    const sortButtons = Array.from(document.querySelectorAll('.sort-button'));
    const columns = window.recordColumns || JSON.parse(table.dataset.columns || '[]');
    const autocompleteInputs = Array.from(document.querySelectorAll('[data-autocomplete]'));

    const state = {
        page: parseInt(stateElement.dataset.currentPage || '1', 10),
        pageSize: parseInt(stateElement.dataset.pageSize || '20', 10),
        sortBy: stateElement.dataset.sortBy || 'Id',
        sortDir: stateElement.dataset.sortDir || 'asc'
    };

    function setValidationMessage(message) {
        if (!validationElement) {
            return;
        }

        if (message) {
            validationElement.textContent = message;
            validationElement.classList.remove('is-hidden');
        } else {
            validationElement.textContent = '';
            validationElement.classList.add('is-hidden');
        }
    }

    function toggleSearchPanel() {
        if (!searchToggle || !searchPanel) {
            return;
        }

        const expanded = searchToggle.getAttribute('aria-expanded') === 'true';
        searchToggle.setAttribute('aria-expanded', (!expanded).toString());
        if (expanded) {
            searchPanel.setAttribute('hidden', '');
        } else {
            searchPanel.removeAttribute('hidden');
        }
    }

    if (searchToggle && searchPanel) {
        searchToggle.addEventListener('click', toggleSearchPanel);
    }

    function debounce(fn, wait) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = window.setTimeout(() => fn.apply(this, args), wait);
        };
    }

    function gatherSearchParameters(includePage = true) {
        const params = new URLSearchParams();
        const keyword = (document.getElementById('Keyword')?.value || '').trim();
        const category = (document.getElementById('Category')?.value || '').trim();
        const status = (document.getElementById('Status')?.value || '').trim();
        const updatedFrom = (document.getElementById('UpdatedFrom')?.value || '').trim();
        const updatedTo = (document.getElementById('UpdatedTo')?.value || '').trim();
        const idValue = (document.getElementById('Id')?.value || '').trim();

        if (keyword) params.set('Keyword', keyword);
        if (category) params.set('Category', category);
        if (status) params.set('Status', status);
        if (updatedFrom) params.set('UpdatedFrom', updatedFrom);
        if (updatedTo) params.set('UpdatedTo', updatedTo);
        if (idValue) params.set('Id', idValue);

        if (includePage) {
            params.set('Page', String(state.page));
        }

        params.set('PageSize', String(state.pageSize));

        if (state.sortBy) {
            params.set('SortBy', state.sortBy);
        }

        if (state.sortDir) {
            params.set('SortDir', state.sortDir);
        }

        columnFilterInputs.forEach(input => {
            const value = input.value.trim();
            if (value) {
                params.set(`col_${input.dataset.columnFilter}`, value);
            }
        });

        return params;
    }

    function buildQueryString(includePage = true) {
        return gatherSearchParameters(includePage).toString();
    }

    function updateSortIndicators() {
        sortButtons.forEach(button => {
            const column = button.dataset.column;
            const isActive = column && column.toLowerCase() === state.sortBy.toLowerCase();
            button.classList.toggle('is-active', !!isActive);
            const indicator = button.querySelector('.sort-indicator');
            if (indicator) {
                indicator.textContent = isActive ? (state.sortDir === 'desc' ? '▼' : '▲') : '';
            }
        });
    }

    function updateSummary(summary) {
        if (summaryElement) {
            summaryElement.textContent = summary;
        }
    }

    function updateTableRows(rows) {
        const tbody = table.querySelector('tbody');
        if (!tbody) {
            return;
        }

        tbody.innerHTML = '';
        if (!rows || rows.length === 0) {
            const row = document.createElement('tr');
            row.className = 'empty';
            const cell = document.createElement('td');
            cell.colSpan = columns.length;
            cell.textContent = '該当するデータがありません。';
            row.appendChild(cell);
            tbody.appendChild(row);
            return;
        }

        rows.forEach(dataRow => {
            const row = document.createElement('tr');
            columns.forEach(column => {
                const cell = document.createElement('td');
                cell.dataset.column = column.PropertyName;
                cell.textContent = dataRow[column.PropertyName] ?? '';
                row.appendChild(cell);
            });
            tbody.appendChild(row);
        });
    }

    function updatePagination(pageCount, currentPage) {
        if (!paginationList) {
            return;
        }

        paginationList.innerHTML = '';
        if (pageCount <= 0) {
            pageCount = 1;
        }

        for (let page = 1; page <= pageCount; page++) {
            const item = document.createElement('li');
            if (page === currentPage) {
                const span = document.createElement('span');
                span.className = 'page-button is-current';
                span.textContent = String(page);
                item.appendChild(span);
            } else {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'page-button';
                button.dataset.page = String(page);
                button.textContent = String(page);
                item.appendChild(button);
            }
            paginationList.appendChild(item);
        }
    }

    async function fetchList(params) {
        const url = `/Records/ListJson?${params.toString()}`;
        try {
            const response = await fetch(url, {
                headers: { 'X-Requested-With': 'XMLHttpRequest' }
            });

            if (!response.ok) {
                const payload = await response.json().catch(() => ({}));
                setValidationMessage(payload.error || '検索条件を確認してください。');
                return;
            }

            const data = await response.json();
            setValidationMessage('');
            updateTableRows(data.rows);
            updatePagination(data.pageCount, data.page);
            updateSummary(data.summary);

            state.page = data.page;
            state.pageSize = data.pageSize;
            state.sortBy = data.sortBy || state.sortBy;
            state.sortDir = data.sortDir || state.sortDir;

            stateElement.dataset.currentPage = String(state.page);
            stateElement.dataset.pageSize = String(state.pageSize);
            stateElement.dataset.sortBy = state.sortBy;
            stateElement.dataset.sortDir = state.sortDir;

            updateSortIndicators();

            const queryString = params.toString();
            const nextUrl = `${window.location.pathname}?${queryString}`;
            window.history.replaceState(null, '', nextUrl);
        } catch (error) {
            console.error(error);
            setValidationMessage('データの取得に失敗しました。');
        }
    }

    const debouncedFilterRequest = debounce(() => {
        state.page = 1;
        const params = gatherSearchParameters(true);
        params.set('Page', '1');
        fetchList(params);
    }, 300);

    columnFilterInputs.forEach(input => {
        input.addEventListener('input', debouncedFilterRequest);
    });

    function buildSuggestionParams(field, scope, term) {
        const params = gatherSearchParameters(false);
        if (scope === 'column') {
            params.delete(`col_${field}`);
        } else {
            params.delete(field);
        }

        params.set('field', field);
        params.set('term', term ?? '');
        params.set('limit', '10');
        params.set('scope', scope);
        return params;
    }

    function setupAutocomplete(input) {
        const field = input.dataset.autocomplete;
        if (!field) {
            return;
        }

        const scope = input.dataset.autocompleteScope === 'query' ? 'query' : 'column';
        const datalist = document.createElement('datalist');
        const listId = `autocomplete-${field}-${Math.random().toString(36).slice(2)}`;
        datalist.id = listId;
        input.setAttribute('list', listId);
        input.insertAdjacentElement('afterend', datalist);

        let activeRequest = 0;

        async function performFetch(term) {
            const requestId = ++activeRequest;
            const params = buildSuggestionParams(field, scope, term?.trim() ?? '');
            const url = `/Records/Suggestions?${params.toString()}`;

            try {
                const response = await fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } });
                if (!response.ok) {
                    return;
                }

                const payload = await response.json();
                if (requestId !== activeRequest) {
                    return;
                }

                const values = Array.isArray(payload.values) ? payload.values : [];
                datalist.innerHTML = '';
                values.forEach(value => {
                    const option = document.createElement('option');
                    option.value = value;
                    datalist.appendChild(option);
                });
            } catch (error) {
                console.error(error);
            }
        }

        const debouncedFetch = debounce(performFetch, 200);

        input.addEventListener('focus', () => {
            performFetch(input.value);
        });

        input.addEventListener('input', () => {
            debouncedFetch(input.value);
        });
    }

    autocompleteInputs.forEach(setupAutocomplete);

    sortButtons.forEach(button => {
        button.addEventListener('click', () => {
            const column = button.dataset.column;
            if (!column) {
                return;
            }

            if (state.sortBy.toLowerCase() === column.toLowerCase()) {
                state.sortDir = state.sortDir === 'asc' ? 'desc' : 'asc';
            } else {
                state.sortBy = column;
                state.sortDir = 'asc';
            }

            state.page = 1;
            const params = gatherSearchParameters(true);
            params.set('Page', '1');
            fetchList(params);
        });
    });

    if (paginationList) {
        paginationList.addEventListener('click', (event) => {
            const target = event.target;
            if (!(target instanceof HTMLElement)) {
                return;
            }

            const page = target.dataset.page;
            if (!page) {
                return;
            }

            state.page = parseInt(page, 10);
            const params = gatherSearchParameters(true);
            fetchList(params);
        });
    }

    if (pageSizeSelect) {
        pageSizeSelect.addEventListener('change', () => {
            state.pageSize = parseInt(pageSizeSelect.value, 10) || 20;
            state.page = 1;
            const params = gatherSearchParameters(true);
            params.set('Page', '1');
            fetchList(params);
        });
    }

    if (csvButton) {
        csvButton.addEventListener('click', () => {
            const query = buildQueryString(false);
            const url = `/Records/Csv?${query}`;
            window.open(url, '_blank', 'noopener');
        });
    }

    if (searchForm) {
        searchForm.addEventListener('submit', (event) => {
            event.preventDefault();
            state.page = 1;
            const query = buildQueryString(true);
            window.location.href = `${searchForm.action}?${query}`;
        });
    }

    if (clearButton) {
        clearButton.addEventListener('click', () => {
            searchForm?.reset();
            state.page = 1;
            const keywordInput = document.getElementById('Keyword');
            if (keywordInput) keywordInput.value = '';
            columnFilterInputs.forEach(input => input.value = '');
            const params = gatherSearchParameters(true);
            params.set('Page', '1');
            fetchList(params);
        });
    }

    updateSortIndicators();
})();
