
document.addEventListener('DOMContentLoaded', function () {
    const openModalBtn = document.getElementById('openConfirmModalBtn');
    if(openModalBtn) {
        const confirmSaveBtn = document.getElementById('confirmSaveBtn');
        const categorizeForm = document.getElementById('categorizeForm');
        const confirmationModal = new bootstrap.Modal(document.getElementById('confirmationModal'));
        const confirmationTableContainer = document.getElementById('confirmationTableContainer');

        openModalBtn.addEventListener('click', function () {
            const selections = {};
            const selectionTable = document.getElementById('selectionTable');
            const headers = Array.from(selectionTable.querySelectorAll('thead th')).map(th => th.textContent.trim());
            const checkedBoxes = categorizeForm.querySelectorAll('input[type="checkbox"]:checked');
            
            if(checkedBoxes.length === 0) {
                alert("No changes selected. Please check at least one box to save.");
                return;
            }

            checkedBoxes.forEach(box => {
                const row = box.closest('tr');
                const noteTitle = row.querySelector('td:first-child').textContent.trim();
                const cellIndex = box.closest('td').cellIndex;
                const categoryName = headers[cellIndex];

                if (!selections[categoryName]) {
                    selections[categoryName] = [];
                }
                selections[categoryName].push(noteTitle);
            });

            let tableHtml = `<table class="table futuristic-table table-bordered mt-2">
                                <thead>
                                    <tr><th>Subject (Category)</th><th>Documents (Notes)</th></tr>
                                </thead>
                                <tbody>`;
            for (const category in selections) {
                tableHtml += `<tr>
                                <td>${category}</td>
                                <td><ul>`;
                selections[category].forEach(note => {
                    tableHtml += `<li>${note}</li>`;
                });
                tableHtml += `</ul></td>
                            </tr>`;
            }
            tableHtml += `</tbody></table>`;
            
            confirmationTableContainer.innerHTML = tableHtml;
            confirmationModal.show();
        });

        confirmSaveBtn.addEventListener('click', function () {
            categorizeForm.submit();
        });
    }
});