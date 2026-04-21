// Admin table search — filters rows of any table with data-searchable="true"
(function () {
    document.addEventListener('DOMContentLoaded', function () {
        var input = document.getElementById('adminSearchInput');
        var table = document.querySelector('table[data-searchable="true"]');

        if (!input || !table) return;

        var tbody = table.querySelector('tbody');

        input.addEventListener('input', function () {
            var term = this.value.trim().toLowerCase();
            var rows = tbody.querySelectorAll('tr');
            var anyVisible = false;

            rows.forEach(function (row) {
                // skip the "empty state" row (single cell spanning all columns)
                if (row.cells.length === 1 && row.cells[0].colSpan > 1) return;

                var text = row.textContent.toLowerCase();
                var match = term === '' || text.includes(term);
                row.style.display = match ? '' : 'none';
                if (match) anyVisible = true;
            });

            // show/hide "no results" row
            var noResults = tbody.querySelector('tr.no-search-results');
            if (!noResults) {
                noResults = document.createElement('tr');
                noResults.className = 'no-search-results';
                noResults.innerHTML = '<td colspan="99" class="text-center py-4 text-muted fst-italic">No results match your search.</td>';
                tbody.appendChild(noResults);
            }
            noResults.style.display = (!anyVisible && term !== '') ? '' : 'none';
        });
    });
})();
