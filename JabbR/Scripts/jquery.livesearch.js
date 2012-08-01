jQuery.fn.liveUpdate = function (list, childSelectorFilter) {
    list = jQuery(list);
    childSelectorFilter = childSelectorFilter || 'li';
    var rows = $([]);
    var cache = $([]);
    // Change from John Resig's original code to allow for updates without rebinding events
    this.update = function (updatedChildSelectorFilter) {
        if (list.length) {
            rows = list.children(updatedChildSelectorFilter || childSelectorFilter);
            cache = rows.map(function () {
                return $(this).text().toLowerCase();
            });
        }
    }

    this.keyup(filter).keyup()
		.parents('form').submit(function () {
			return false;
		});


    return this;

    function filter () {
        var term = jQuery.trim(jQuery(this).val().toLowerCase()), scores = [];
        list.scrollTop(0);
        if (!term) {
            $(rows).show();
        } else {
            $(rows).hide();

            cache.each(function (i) {
                var score = this.score(term);
                if (score > 0) { scores.push([score, i]); }
            });

            jQuery.each(scores.sort(function (a, b) { return b[0] - a[0]; }), function () {
                jQuery(rows[this[1]]).show();
            });
        }
    }
};
