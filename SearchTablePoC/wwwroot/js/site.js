(function () {
    const stateElement = document.getElementById('records-state');
    const table = document.getElementById('records-table');
    if (!stateElement || !table) {
        return;
    }

    const searchToggle = document.getElementById('search-toggle');
    const searchPanel = document.getElementById('search-panel');
    const columnToggle = document.getElementById('column-toggle');
    const columnPanel = document.getElementById('column-panel');
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
    const columnSettingsList = document.getElementById('column-settings-list');
    const columnToggleInputs = columnSettingsList ? Array.from(columnSettingsList.querySelectorAll('[data-column-toggle]')) : [];
    const autocompleteInputs = Array.from(document.querySelectorAll('[data-autocomplete]'));
    let columnState = columns.map(column => ({ ...column, visible: true }));
    let currentRowsData = [];

    if (columnToggleInputs.length) {
        const visibilityMap = new Map();
        columnToggleInputs.forEach(input => {
            const columnName = input.dataset.columnToggle;
            if (columnName) {
                visibilityMap.set(columnName, input.checked);
            }
        });

        columnState = columnState.map(column => ({
            ...column,
            visible: visibilityMap.has(column.PropertyName) ? visibilityMap.get(column.PropertyName) : column.visible
        }));
    }

    const headerRow = table.tHead && table.tHead.rows.length > 0 ? table.tHead.rows[0] : null;
    const filterRow = table.tHead && table.tHead.rows.length > 1 ? table.tHead.rows[1] : null;
    const headerCellsByColumn = new Map();
    const filterCellsByColumn = new Map();

    if (headerRow) {
        Array.from(headerRow.cells).forEach(cell => {
            const button = cell.querySelector('.sort-button');
            const columnName = button && button.dataset.column;
            if (columnName) {
                headerCellsByColumn.set(columnName, cell);
                cell.dataset.column = columnName;
            }
        });
    }

    if (filterRow) {
        Array.from(filterRow.cells).forEach(cell => {
            const input = cell.querySelector('.column-filter');
            const columnName = input && input.dataset.columnFilter;
            if (columnName) {
                filterCellsByColumn.set(columnName, cell);
                cell.dataset.column = columnName;
            }
        });
    }

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

    function attachSectionToggle(button, panel) {
        if (!button || !panel) {
            return;
        }

        button.addEventListener('click', () => {
            const expanded = button.getAttribute('aria-expanded') === 'true';
            button.setAttribute('aria-expanded', (!expanded).toString());
            if (expanded) {
                panel.setAttribute('hidden', '');
            } else {
                panel.removeAttribute('hidden');
            }
        });
    }

    attachSectionToggle(searchToggle, searchPanel);
    attachSectionToggle(columnToggle, columnPanel);

    function debounce(fn, wait) {
        let timeoutId;
        return function (...args) {
            clearTimeout(timeoutId);
            timeoutId = window.setTimeout(() => fn.apply(this, args), wait);
        };
    }

    function getVisibleColumns() {
        return columnState.filter(column => column.visible);
    }

    function renderTableHead() {
        if (!headerRow && !filterRow) {
            return;
        }

        if (headerRow) {
            const headerFragment = document.createDocumentFragment();
            columnState.forEach(column => {
                const headerCell = headerCellsByColumn.get(column.PropertyName);
                if (headerCell) {
                    headerCell.hidden = !column.visible;
                    if (column.visible) {
                        headerCell.removeAttribute('aria-hidden');
                    } else {
                        headerCell.setAttribute('aria-hidden', 'true');
                    }
                    headerFragment.appendChild(headerCell);
                }
            });
            headerRow.appendChild(headerFragment);
        }

        if (filterRow) {
            const filterFragment = document.createDocumentFragment();
            columnState.forEach(column => {
                const filterCell = filterCellsByColumn.get(column.PropertyName);
                if (filterCell) {
                    filterCell.hidden = !column.visible;
                    if (column.visible) {
                        filterCell.removeAttribute('aria-hidden');
                    } else {
                        filterCell.setAttribute('aria-hidden', 'true');
                    }
                    const input = filterCell.querySelector('.column-filter');
                    if (input) {
                        input.disabled = !column.visible;
                    }
                    filterFragment.appendChild(filterCell);
                }
            });
            filterRow.appendChild(filterFragment);
        }
    }

    function renderTableBody() {
        const tbody = table.tBodies && table.tBodies.length > 0 ? table.tBodies[0] : null;
        if (!tbody) {
            return;
        }

        tbody.innerHTML = '';
        const visibleColumns = getVisibleColumns();

        if (visibleColumns.length === 0) {
            const row = document.createElement('tr');
            row.className = 'empty';
            const cell = document.createElement('td');
            cell.colSpan = columnState.length > 0 ? columnState.length : 1;
            cell.textContent = '表示する項目が選択されていません。';
            row.appendChild(cell);
            tbody.appendChild(row);
            return;
        }

        if (!currentRowsData.length) {
            const row = document.createElement('tr');
            row.className = 'empty';
            const cell = document.createElement('td');
            cell.colSpan = visibleColumns.length;
            cell.textContent = '該当するデータがありません。';
            row.appendChild(cell);
            tbody.appendChild(row);
            return;
        }

        currentRowsData.forEach(dataRow => {
            const row = document.createElement('tr');
            visibleColumns.forEach(column => {
                const cell = document.createElement('td');
                cell.dataset.column = column.PropertyName;

                if (column.PropertyName === 'Field02') {
                    const code = dataRow[column.PropertyName] ?? '';
                    if (code) {
                        const link = document.createElement('a');
                        link.href = `/Items/Detail?itemCode=${encodeURIComponent(code)}`;
                        link.target = '_blank';
                        link.rel = 'noopener';
                        link.textContent = code;
                        cell.appendChild(link);
                    }
                } else {
                    cell.textContent = dataRow[column.PropertyName] ?? '';
                }

                row.appendChild(cell);
            });
            tbody.appendChild(row);
        });
    }

    function applyColumnState() {
        renderTableHead();
        renderTableBody();

        if (columnToggleInputs.length) {
            columnToggleInputs.forEach(input => {
                const columnName = input.dataset.columnToggle;
                const entry = columnState.find(column => column.PropertyName === columnName);
                if (entry) {
                    input.checked = entry.visible;
                }
            });
        }

        updateSortIndicators();
    }

    function extractRowsFromTable() {
        const tbody = table.tBodies && table.tBodies.length > 0 ? table.tBodies[0] : null;
        if (!tbody) {
            return [];
        }

        const result = [];
        Array.from(tbody.rows).forEach(row => {
            if (row.classList.contains('empty')) {
                return;
            }

            const rowData = {};
            Array.from(row.cells).forEach(cell => {
                const columnName = cell.dataset.column;
                if (columnName) {
                    rowData[columnName] = cell.textContent ?? '';
                }
            });

            if (Object.keys(rowData).length > 0) {
                result.push(rowData);
            }
        });

        return result;
    }

    function syncColumnStateFromListOrder() {
        if (!columnSettingsList) {
            return;
        }

        const items = Array.from(columnSettingsList.querySelectorAll('[data-column-item]'));
        const order = items
            .map(item => item.dataset.columnItem)
            .filter(Boolean);

        columnState.sort((a, b) => {
            const aIndex = order.indexOf(a.PropertyName);
            const bIndex = order.indexOf(b.PropertyName);
            const safeA = aIndex === -1 ? Number.MAX_SAFE_INTEGER : aIndex;
            const safeB = bIndex === -1 ? Number.MAX_SAFE_INTEGER : bIndex;
            return safeA - safeB;
        });
    }

    function setColumnVisibility(columnName, isVisible) {
        const entry = columnState.find(column => column.PropertyName === columnName);
        if (!entry) {
            return;
        }

        entry.visible = isVisible;
    }

    currentRowsData = extractRowsFromTable();
    applyColumnState();

    if (columnToggleInputs.length) {
        columnToggleInputs.forEach(input => {
            input.addEventListener('change', () => {
                const columnName = input.dataset.columnToggle;
                if (!columnName) {
                    return;
                }

                setColumnVisibility(columnName, input.checked);
                applyColumnState();
            });
        });
    }

    let draggedItem = null;

    if (columnSettingsList) {
        columnSettingsList.addEventListener('dragstart', event => {
            const target = event.target instanceof HTMLElement ? event.target.closest('[data-column-item]') : null;
            if (!target) {
                return;
            }

            draggedItem = target;
            target.classList.add('is-dragging');
            if (event.dataTransfer) {
                event.dataTransfer.effectAllowed = 'move';
                event.dataTransfer.setData('text/plain', target.dataset.columnItem || '');
            }
        });

        columnSettingsList.addEventListener('dragover', event => {
            if (!draggedItem) {
                return;
            }

            event.preventDefault();
            const target = event.target instanceof HTMLElement ? event.target.closest('[data-column-item]') : null;
            if (!target || target === draggedItem) {
                return;
            }

            const rect = target.getBoundingClientRect();
            const shouldInsertBefore = event.clientY - rect.top < rect.height / 2;
            columnSettingsList.insertBefore(draggedItem, shouldInsertBefore ? target : target.nextSibling);
        });

        columnSettingsList.addEventListener('drop', event => {
            event.preventDefault();
        });

        columnSettingsList.addEventListener('dragend', () => {
            if (draggedItem) {
                draggedItem.classList.remove('is-dragging');
                draggedItem = null;
            }

            syncColumnStateFromListOrder();
            applyColumnState();
        });
    }

    function gatherSearchParameters(includePage = true) {
        const params = new URLSearchParams();
        const keyword = (document.getElementById('Keyword')?.value || '').trim();
        const idValue = (document.getElementById('Id')?.value || '').trim();
        const field01 = (document.getElementById('Field01')?.value || '').trim();
        const category = (document.getElementById('Category')?.value || '').trim();
        const status = (document.getElementById('Status')?.value || '').trim();
        const name = (document.getElementById('Name')?.value || '').trim();
        const updatedFrom = (document.getElementById('UpdatedFrom')?.value || '').trim();
        const updatedTo = (document.getElementById('UpdatedTo')?.value || '').trim();

        if (keyword) params.set('Keyword', keyword);
        if (idValue) params.set('Id', idValue);
        if (field01) params.set('Field01', field01);
        if (category) params.set('Category', category);
        if (status) params.set('Status', status);
        if (name) params.set('Name', name);
        if (updatedFrom) params.set('UpdatedFrom', updatedFrom);
        if (updatedTo) params.set('UpdatedTo', updatedTo);

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
            if (input.disabled) {
                return;
            }

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
        if (!Array.isArray(rows)) {
            currentRowsData = [];
        } else {
            currentRowsData = rows.map(row => ({ ...row }));
        }

        renderTableBody();
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

    function requestFilterUpdate() {
        state.page = 1;
        const params = gatherSearchParameters(true);
        params.set('Page', '1');
        fetchList(params);
    }

    columnFilterInputs.forEach(input => {
        input.addEventListener('change', () => {
            requestFilterUpdate();
        });

        input.addEventListener('keydown', (event) => {
            if (event.key === 'Enter') {
                event.preventDefault();
                requestFilterUpdate();
            }
        });
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
        const panel = document.createElement('div');
        panel.className = 'autocomplete-panel';
        panel.setAttribute('role', 'listbox');
        panel.hidden = true;
        input.insertAdjacentElement('afterend', panel);

        let activeRequest = 0;
        let suggestions = [];
        let highlightedIndex = -1;

        function hidePanel() {
            panel.hidden = true;
            panel.classList.remove('is-visible');
            highlightedIndex = -1;
        }

        function showPanel() {
            if (!suggestions.length) {
                hidePanel();
                return;
            }

            panel.hidden = false;
            panel.classList.add('is-visible');
        }

        function highlightOption(index) {
            const options = Array.from(panel.querySelectorAll('.autocomplete-option'));
            options.forEach(option => option.classList.remove('is-active'));
            if (index < 0 || index >= options.length) {
                highlightedIndex = -1;
                return;
            }

            highlightedIndex = index;
            const option = options[index];
            option.classList.add('is-active');
            option.scrollIntoView({ block: 'nearest' });
        }

        function selectOption(index) {
            if (index < 0 || index >= suggestions.length) {
                return;
            }

            const value = suggestions[index];
            input.value = value;
            hidePanel();
            if (scope === 'column') {
                requestFilterUpdate();
            } else {
                input.dispatchEvent(new Event('input', { bubbles: true }));
            }
            input.focus({ preventScroll: true });
        }

        function renderSuggestions(values) {
            suggestions = values;
            panel.innerHTML = '';

            if (!values.length) {
                hidePanel();
                return;
            }

            const fragment = document.createDocumentFragment();
            values.forEach((value, index) => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'autocomplete-option';
                button.setAttribute('role', 'option');
                button.textContent = value;
                button.addEventListener('mousedown', (event) => event.preventDefault());
                button.addEventListener('click', () => {
                    selectOption(index);
                });
                fragment.appendChild(button);
            });

            panel.appendChild(fragment);
            highlightedIndex = -1;
            showPanel();
        }

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
                renderSuggestions(values);
            } catch (error) {
                console.error(error);
            }
        }

        const debouncedFetch = debounce(performFetch, 200);

        input.addEventListener('focus', () => {
            if (suggestions.length) {
                showPanel();
            }
            performFetch(input.value);
        });

        input.addEventListener('input', () => {
            debouncedFetch(input.value);
        });

        input.addEventListener('keydown', (event) => {
            if (event.key === 'Escape') {
                hidePanel();
                return;
            }

            if (suggestions.length === 0) {
                return;
            }

            if (event.key === 'ArrowDown') {
                event.preventDefault();
                showPanel();
                const nextIndex = highlightedIndex + 1 >= suggestions.length ? 0 : highlightedIndex + 1;
                highlightOption(nextIndex);
            } else if (event.key === 'ArrowUp') {
                event.preventDefault();
                showPanel();
                const nextIndex = highlightedIndex - 1 < 0 ? suggestions.length - 1 : highlightedIndex - 1;
                highlightOption(nextIndex);
            } else if (event.key === 'Enter' && !panel.hidden && highlightedIndex >= 0) {
                event.preventDefault();
                selectOption(highlightedIndex);
            }
        });

        input.addEventListener('blur', () => {
            window.setTimeout(() => {
                if (!input.matches(':focus')) {
                    hidePanel();
                }
            }, 150);
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
            const searchFieldIds = ['Keyword', 'Id', 'Field01', 'Category', 'Status', 'Name', 'UpdatedFrom', 'UpdatedTo'];
            searchFieldIds.forEach(fieldId => {
                const input = document.getElementById(fieldId);
                if (input instanceof HTMLInputElement) {
                    input.value = '';
                }
            });
            columnFilterInputs.forEach(input => input.value = '');
            const params = gatherSearchParameters(true);
            params.set('Page', '1');
            fetchList(params);
        });
    }

    updateSortIndicators();
})();
