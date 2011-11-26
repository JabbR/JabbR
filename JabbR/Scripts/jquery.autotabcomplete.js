; (function ($, window, document, undefined) {
    var pluginName = 'autoTabComplete',
        defaults = {
            values: [],
            get: function() {
                return this.values;
            }
        };
    var UNDEF = undefined;

    function AutoTabComplete(element, options) {
        var element = element;
        var options = $.extend({}, defaults, options);

        var _defaults = defaults;
        var _name = pluginName;

        var _inAutoComplete;
        var _text;
        var _prefix;
        var _index;
        var _caret;

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
                    var sel = getSelection(this);
                    _caret = sel.end;

                    // find prefix (starts with @)
                    var match = _text.substr(0, _caret).match(/@\S*$/i);
                    if (!match) return;

                    _prefix = match.toString().substr(1).toLowerCase();

                    _inAutoComplete = true;
                }
                _index = getNextIndex(_index, offset, values.length);

                var prefixLen = _prefix.length;
                // sort values if there's a prefix
                if (prefixLen > 0) {
                    values = values.sort(sortInsensitive);
                }

                // loop through values (ring buffer) for match 
                var i = _index;
                while (true) {
                    var value = values[i];
                    if (value.substr(0, prefixLen).toLowerCase() == _prefix) {
                        var newText = _text.substr(0, _caret - prefixLen) + value + _text.substr(_caret);
                        $(this).val(newText);

                        if (_caret < _text.length) {
                            setSelection(this, _caret, _caret);
                        }
                        break;
                    }
                    i = getNextIndex(i , offset, values.length);
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

        // create helper to get current caret position
        // only works if support selectionStart/End properties
        if (typeof(element["selectionStart"]) != UNDEF) {
            getSelection = function (el) {
                return { start: el.selectionStart, end: el.selectionEnd };
            };

            setSelection = function(el, start, end) {
                el.selectionStart = start;
                el.selectionEnd = end;
            };
        }
        else {
            getSelection = function (el) {
                var len = $(el).val().length;
                return { start: len, end: len };
            };
            setSelection = function(el, start, end) {
            };
        }

        function sortInsensitive(a, b) {
            return a.toLowerCase() > b.toLowerCase() ? 1 : -1;
        }

        function reset() {
            _inAutoComplete = false;
            _index = -1;
            _text = '';
            _prefix = '';
            _caret = 0;
        }

        function getNextIndex(index, offset, length) {
            var i = (index + offset) % length;
            if (i < 0) i = length - 1;
            return i;
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