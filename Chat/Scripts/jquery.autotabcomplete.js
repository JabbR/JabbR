; (function ($, window, document, undefined) {
    var pluginName = 'autoTabComplete',
        defaults = {
            values: [],
            get: function() {
                return this.values;
            }
        };

    function AutoTabComplete(element, options) {
        var element = element;
        var options = $.extend({}, defaults, options);

        var _defaults = defaults;
        var _name = pluginName;

        var _inAutoComplete = false;
        var _text = '';
        var _prefix = '';
        var _index = 0;

        $(element).keydown(function (event) {
            if (event.which == 9) { // tab
                event.preventDefault();

                // call get() to retrieve current list of values
                var values = options.get();
                if (values.length == 0) {
                    _inAutoComplete = false;
                    _index = 0;
                    return;
                }

                if (!_inAutoComplete) {
                    _inAutoComplete = true;
                    _text = $(this).val();

                    // find prefix
                    var match = _text.match(/\S+$/i);
                    if (!match) match = '';
                    _prefix = match.toString().toLowerCase();
                    _index = 0;
                }

                var prefixLen = _prefix.length;

                // loop through values (ring buffer) for match 
                var i = _index;
                while (true) {
                    var value = values[i];
                    i = (i + 1) % values.length;
                    if (value.substr(0, prefixLen).toLowerCase() == _prefix) {
                        var newText = _text.substr(0, _text.length - prefixLen) + value;
                        $(this).val(newText);
                        break;
                    }
                    if (i == _index) break;
                }
                _index = i;
                return;
            }
            else {
                _inAutoComplete = false;
                _index = 0;
            }
        });

    }

    $.fn[pluginName] = function (options) {
        return this.each(function () {
            if (!$.data(this, 'plugin_' + pluginName)) {
                $.data(this, 'plugin_' + pluginName, new AutoTabComplete(this, options));
            }
        });
    }

})(jQuery, window, document);