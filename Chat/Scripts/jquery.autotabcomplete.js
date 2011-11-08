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

        var _inAutoComplete;
        var _text;
        var _prefix;
        var _index;

        var KEY = {
		    TAB: 9,
		    SHIFT: 16
	    };

        reset();

        $(element).keydown(function (event) {
            if (event.which == KEY.TAB) {
                event.preventDefault();

                // call get() to retrieve current list of values
                var values = options.get();
                if (values.length == 0) {
                    reset();
                    return;
                }

                var offset = event.shiftKey ? -1 : 1;

                if (!_inAutoComplete) {
                    _text = $(this).val();

                    // find prefix (starts with @)
                    var match = _text.match(/@\S*$/i);
                    if (!match) return;

                    _prefix = match.toString().substr(1).toLowerCase();

                    _inAutoComplete = true;
                }
                _index = (_index + offset) % values.length;
                if (_index < 0) _index = values.length - 1;

                var prefixLen = _prefix.length;
                // sort values if there's a prefix
                if (_prefix.length > 0) {
                    values = values.sort(sortInsensitive);
                }

                // loop through values (ring buffer) for match 
                var i = _index;
                while (true) {
                    var value = values[i];
                    if (value.substr(0, prefixLen).toLowerCase() == _prefix) {
                        var newText = _text.substr(0, _text.length - prefixLen) + value;
                        $(this).val(newText);
                        break;
                    }
                    i = (i + offset) % values.length;
                    if (i == _index) break;
                }
                _index = i;
                return;
            }
            else {
                if (event.which == KEY.SHIFT) return; // ignore shift key press
                reset();
            }
        });

        function sortInsensitive(a, b) {
            return a.toLowerCase() > b.toLowerCase() ? 1 : -1;
        }

        function reset() {
            _inAutoComplete = false;
            _index = -1;
            _text = '';
            _prefix = '';
        }


    }

    $.fn[pluginName] = function (options) {
        return this.each(function () {
            if (!$.data(this, 'plugin_' + pluginName)) {
                $.data(this, 'plugin_' + pluginName, new AutoTabComplete(this, options));
            }
        });
    }

})(jQuery, window, document);