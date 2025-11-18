(function () {
    const table = document.getElementById('size-entry-table');
    const dataElement = document.getElementById('color-data');
    if (!table || !dataElement) {
        return;
    }

    let colors = [];
    try {
        colors = JSON.parse(dataElement.textContent || '[]') || [];
    } catch (error) {
        console.error('Failed to parse color data', error);
        return;
    }

    const sizeInput = document.getElementById('size-label-input');
    const addButton = document.getElementById('add-size-button');
    const summaryGrid = document.getElementById('size-summary-grid');

    let sizeOrder = Array.from(
        new Set(
            colors.flatMap(color => (color.sizes || []).map(size => size.size))
        )
    );

    function ensureSizeEntry(color, sizeLabel) {
        if (!Array.isArray(color.sizes)) {
            color.sizes = [];
        }

        let entry = color.sizes.find(entry => entry.size.toLowerCase() === sizeLabel.toLowerCase());
        if (!entry) {
            entry = { size: sizeLabel, quantity: 0, dueDate: '' };
            color.sizes.push(entry);
        }

        return entry;
    }

    function updateTotals() {
        colors.forEach((color, index) => {
            const total = (color.sizes || []).reduce((sum, entry) => sum + (parseInt(entry.quantity, 10) || 0), 0);
            const totalElement = table.querySelector(`[data-total-for="${index}"]`);
            if (totalElement) {
                totalElement.textContent = `${total.toLocaleString()} 枚`;
            }
        });

        if (!summaryGrid) {
            return;
        }

        const sizeTotals = new Map();
        sizeOrder.forEach(size => sizeTotals.set(size, 0));

        colors.forEach(color => {
            sizeOrder.forEach(size => {
                const entry = ensureSizeEntry(color, size);
                const current = sizeTotals.get(size) || 0;
                sizeTotals.set(size, current + (parseInt(entry.quantity, 10) || 0));
            });
        });

        summaryGrid.innerHTML = '';
        let grandTotal = 0;
        sizeTotals.forEach((qty, size) => {
            const item = document.createElement('div');
            item.className = 'summary-item';
            item.innerHTML = `<span class="summary-label">${size}</span><span class="summary-value">${qty.toLocaleString()} 枚</span>`;
            summaryGrid.appendChild(item);
            grandTotal += qty;
        });

        const totalItem = document.createElement('div');
        totalItem.className = 'summary-item grand-total';
        totalItem.innerHTML = `<span class="summary-label">合計</span><span class="summary-value">${grandTotal.toLocaleString()} 枚</span>`;
        summaryGrid.appendChild(totalItem);
    }

    function createSizeCell(color, sizeLabel) {
        const entry = ensureSizeEntry(color, sizeLabel);
        const cell = document.createElement('td');
        cell.className = 'size-entry-cell';

        const quantityLabel = document.createElement('label');
        quantityLabel.textContent = '数量';
        quantityLabel.className = 'visually-hidden';
        quantityLabel.htmlFor = `qty-${color.colorNumber}-${sizeLabel}`;

        const quantityInput = document.createElement('input');
        quantityInput.type = 'number';
        quantityInput.min = '0';
        quantityInput.value = entry.quantity ?? 0;
        quantityInput.id = `qty-${color.colorNumber}-${sizeLabel}`;
        quantityInput.setAttribute('aria-label', `${color.colorName} ${sizeLabel}の数量`);
        quantityInput.addEventListener('input', () => {
            const value = parseInt(quantityInput.value, 10);
            entry.quantity = Number.isFinite(value) && value >= 0 ? value : 0;
            updateTotals();
        });

        const dueLabel = document.createElement('label');
        dueLabel.textContent = '納期';
        dueLabel.className = 'visually-hidden';
        dueLabel.htmlFor = `due-${color.colorNumber}-${sizeLabel}`;

        const dueInput = document.createElement('input');
        dueInput.type = 'date';
        dueInput.value = entry.dueDate || '';
        dueInput.id = `due-${color.colorNumber}-${sizeLabel}`;
        dueInput.setAttribute('aria-label', `${color.colorName} ${sizeLabel}の納期`);
        dueInput.addEventListener('change', () => {
            entry.dueDate = dueInput.value;
        });

        const dueWrapper = document.createElement('div');
        dueWrapper.className = 'due-date-row';
        const dueTitle = document.createElement('span');
        dueTitle.textContent = '納期';
        dueWrapper.appendChild(dueTitle);
        dueWrapper.appendChild(dueInput);

        const quantityWrapper = document.createElement('div');
        quantityWrapper.className = 'quantity-row';
        const qtyTitle = document.createElement('span');
        qtyTitle.textContent = '数量';
        quantityWrapper.appendChild(qtyTitle);
        quantityWrapper.appendChild(quantityInput);

        cell.appendChild(quantityLabel);
        cell.appendChild(quantityWrapper);
        cell.appendChild(dueLabel);
        cell.appendChild(dueWrapper);

        return cell;
    }

    function renderTable() {
        table.innerHTML = '';

        const thead = table.createTHead();
        const headerRow = thead.insertRow();
        ['生地カラー', 'カラーNo', 'カラー名'].forEach(text => {
            const th = document.createElement('th');
            th.scope = 'col';
            th.textContent = text;
            headerRow.appendChild(th);
        });

        sizeOrder.forEach(size => {
            const th = document.createElement('th');
            th.scope = 'col';
            th.textContent = `${size} (数量/納期)`;
            th.className = 'size-column';
            headerRow.appendChild(th);
        });

        const totalTh = document.createElement('th');
        totalTh.scope = 'col';
        totalTh.textContent = '合計数';
        headerRow.appendChild(totalTh);

        const tbody = table.createTBody();
        colors.forEach((color, index) => {
            const row = tbody.insertRow();
            row.insertCell().textContent = color.fabricColor;
            row.insertCell().textContent = color.colorNumber;
            row.insertCell().textContent = color.colorName;

            sizeOrder.forEach(size => {
                row.appendChild(createSizeCell(color, size));
            });

            const totalCell = row.insertCell();
            totalCell.className = 'total-cell';
            const totalValue = document.createElement('span');
            totalValue.dataset.totalFor = String(index);
            totalValue.textContent = '0 枚';
            totalCell.appendChild(totalValue);
        });

        updateTotals();
    }

    function addSizeColumn(label) {
        const trimmed = (label || '').trim();
        if (!trimmed) {
            sizeInput?.focus();
            return;
        }

        const exists = sizeOrder.some(entry => entry.toLowerCase() === trimmed.toLowerCase());
        if (exists) {
            if (sizeInput) {
                sizeInput.value = '';
                sizeInput.placeholder = `${trimmed} は既に追加されています`;
                sizeInput.focus();
            }
            return;
        }

        sizeOrder.push(trimmed);
        colors.forEach(color => ensureSizeEntry(color, trimmed));
        renderTable();
        if (sizeInput) {
            sizeInput.value = '';
            sizeInput.placeholder = '例: 2XL';
        }
    }

    renderTable();

    addButton?.addEventListener('click', () => addSizeColumn(sizeInput?.value));
    sizeInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            addSizeColumn(sizeInput.value);
        }
    });
})();
