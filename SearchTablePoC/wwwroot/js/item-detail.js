(function () {
    const table = document.getElementById('size-entry-table');
    const dataElement = document.getElementById('color-data');
    const MAX_SIZE_COLUMNS = 6;
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
    const fabricColorInput = document.getElementById('fabric-color-input');
    const colorNumberInput = document.getElementById('color-number-input');
    const colorNameInput = document.getElementById('color-name-input');
    const addColorButton = document.getElementById('add-color-button');

    let sizeOrder = Array.from(
        new Set(
            colors.flatMap(color => (color.sizes || []).map(size => size.size))
        )
    ).slice(0, MAX_SIZE_COLUMNS);

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

        const quantityWrapper = document.createElement('div');
        quantityWrapper.className = 'quantity-row';
        const qtyTitle = document.createElement('span');
        qtyTitle.textContent = '数量';
        quantityWrapper.appendChild(qtyTitle);
        quantityWrapper.appendChild(quantityInput);

        cell.appendChild(quantityLabel);
        cell.appendChild(quantityWrapper);

        return cell;
    }

    function createDueDateCell(color, index) {
        const cell = document.createElement('td');
        cell.className = 'due-cell fixed-column';

        const dueLabel = document.createElement('label');
        dueLabel.className = 'visually-hidden';
        dueLabel.htmlFor = `due-${index}`;
        dueLabel.textContent = `${color.colorName || 'カラー'}の納期`;

        const dueInput = document.createElement('input');
        dueInput.type = 'date';
        dueInput.value = color.dueDate || '';
        dueInput.id = `due-${index}`;
        dueInput.addEventListener('change', () => {
            color.dueDate = dueInput.value;
        });

        cell.appendChild(dueLabel);
        cell.appendChild(dueInput);
        return cell;
    }

    function renderTable() {
        table.innerHTML = '';

        const thead = table.createTHead();
        const headerRow = thead.insertRow();
        [
            { text: '生地カラー', className: 'fabric-color-column fixed-column' },
            { text: 'カラーNo', className: 'color-number-column fixed-column' },
            { text: 'カラー名', className: 'color-name-column fixed-column' }
        ].forEach(definition => {
            const th = document.createElement('th');
            th.scope = 'col';
            th.textContent = definition.text;
            th.className = definition.className;
            headerRow.appendChild(th);
        });

        sizeOrder.forEach(size => {
            const th = document.createElement('th');
            th.scope = 'col';
            th.className = 'size-column';
            const wrapper = document.createElement('div');
            wrapper.className = 'size-column-header';

            const label = document.createElement('span');
            label.textContent = size;
            wrapper.appendChild(label);

            const removeButton = document.createElement('button');
            removeButton.type = 'button';
            removeButton.className = 'icon-button';
            removeButton.setAttribute('aria-label', `${size} 列を削除`);
            removeButton.textContent = '×';
            removeButton.addEventListener('click', () => removeSizeColumn(size));

            wrapper.appendChild(removeButton);
            th.appendChild(wrapper);
            headerRow.appendChild(th);
        });

        const dueTh = document.createElement('th');
        dueTh.scope = 'col';
        dueTh.textContent = '納期';
        dueTh.className = 'due-column fixed-column';
        headerRow.appendChild(dueTh);

        const totalTh = document.createElement('th');
        totalTh.scope = 'col';
        totalTh.textContent = '合計数';
        totalTh.className = 'total-column fixed-column';
        headerRow.appendChild(totalTh);

        const tbody = table.createTBody();
        if (colors.length === 0) {
            const emptyRow = tbody.insertRow();
            const emptyCell = emptyRow.insertCell();
            emptyCell.colSpan = 3 + sizeOrder.length + 2;
            emptyCell.textContent = '生地カラー・カラーNo・カラー名を入力して行を追加してください。';
            emptyCell.className = 'empty-row';
        } else {
            colors.forEach((color, index) => {
                const row = tbody.insertRow();

                const fabricCell = row.insertCell();
                fabricCell.textContent = color.fabricColor;
                fabricCell.className = 'fabric-color-column fixed-column';

                const numberCell = row.insertCell();
                numberCell.textContent = color.colorNumber;
                numberCell.className = 'color-number-column fixed-column';

                const nameCell = row.insertCell();
                nameCell.textContent = color.colorName;
                nameCell.className = 'color-name-column fixed-column';

                sizeOrder.forEach(size => {
                    row.appendChild(createSizeCell(color, size));
                });

                row.appendChild(createDueDateCell(color, index));

                const totalCell = row.insertCell();
                totalCell.className = 'total-cell total-column fixed-column';
                const totalValue = document.createElement('span');
                totalValue.dataset.totalFor = String(index);
                totalValue.textContent = '0 枚';
                totalCell.appendChild(totalValue);
            });
        }

        updateTotals();
    }

    function addSizeColumn(label) {
        const trimmed = (label || '').trim();
        if (!trimmed) {
            sizeInput?.focus();
            return;
        }

        if (sizeOrder.length >= MAX_SIZE_COLUMNS) {
            if (sizeInput) {
                sizeInput.value = '';
                sizeInput.placeholder = `サイズは最大${MAX_SIZE_COLUMNS}件まで追加できます`;
                sizeInput.focus();
            }
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

    function removeSizeColumn(label) {
        sizeOrder = sizeOrder.filter(entry => entry.toLowerCase() !== label.toLowerCase());
        colors.forEach(color => {
            if (Array.isArray(color.sizes)) {
                color.sizes = color.sizes.filter(entry => entry.size.toLowerCase() !== label.toLowerCase());
            }
        });
        renderTable();
    }

    function addColorRow() {
        const fabricColor = (fabricColorInput?.value || '').trim();
        const colorNumber = (colorNumberInput?.value || '').trim();
        const colorName = (colorNameInput?.value || '').trim();

        if (!fabricColor || !colorNumber || !colorName) {
            fabricColorInput?.focus();
            return;
        }

        const newColor = {
            fabricColor,
            colorNumber,
            colorName,
            dueDate: '',
            sizes: []
        };

        sizeOrder.forEach(size => ensureSizeEntry(newColor, size));
        colors.push(newColor);
        renderTable();

        if (fabricColorInput) {
            fabricColorInput.value = '';
        }

        if (colorNumberInput) {
            colorNumberInput.value = '';
        }

        if (colorNameInput) {
            colorNameInput.value = '';
        }

        fabricColorInput?.focus();
    }

    renderTable();

    addButton?.addEventListener('click', () => addSizeColumn(sizeInput?.value));
    sizeInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            addSizeColumn(sizeInput.value);
        }
    });

    addColorButton?.addEventListener('click', addColorRow);
    fabricColorInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            addColorRow();
        }
    });
    colorNumberInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            addColorRow();
        }
    });
    colorNameInput?.addEventListener('keydown', event => {
        if (event.key === 'Enter') {
            event.preventDefault();
            addColorRow();
        }
    });
})();
