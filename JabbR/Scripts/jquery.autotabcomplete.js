; (function ($, window, document) {
    var pluginName = 'autoTabComplete',
        defaults = {
            prefixMatch: '[@]',
            values: [],
            get: function() {
                return this.values;
            }
        };

    function AutoTabComplete(element, options) {
        var element = element,
            options = $.extend({}, defaults, options),
            _defaults = defaults,
            _name = pluginName,
            _inAutoComplete,
            _text,
            _initial,
            _index,
            _caret,
            _prefix,
            _matchRegExp = new RegExp(options.prefixMatch + "\\S*$", "i"),
            Keys = { Tab: 9, Shift: 16 };

        reset();

        $(element).keydown(function (event) {
            if (event.which === Keys.Tab) {
                event.preventDefault();

                var offset = event.shiftKey ? -1 : 1;

                if (!_inAutoComplete) {
                    _text = $(this).val();
                    var sel = getSelection(this);
                    _caret = sel.end;

                    // find initial text (starts with prefix char)
                    var match = _text.substr(0, _caret).match(_matchRegExp);
                    if (!match) return;

                    _initial = match.toString().substr(1).toLowerCase();

                    // get prefix character to pass to get()
                    _prefix = match.toString().substring(0, 1);

                    _inAutoComplete = true;
                }

                // call get() to retrieve current list of values
                var values = options.get(_prefix);
                if (values.length === 0) {
                    reset();
                    return;
                }

                _index = getNextIndex(_index, offset, values.length);

                var initialLen = _initial.length;

                // loop through values (ring buffer) for match 
                var i = _index;
                while (true) {
                    var value = values[i];
                    if (value.substr(0, initialLen).toLowerCase() === _initial) {
                        var newText = _text.substr(0, _caret - initialLen) + value + _text.substr(_caret);
                        $(this).val(newText);

                        if (_caret < _text.length) {
                            setSelection(this, _caret, _caret);
                        }
                        break;
                    }
                    i = getNextIndex(i , offset, values.length);
                    if (i === _index) break;
                }
                _index = i;
                return;
            }
            else {
                if (event.which === Keys.Shift) return; // ignore shift key press
                reset();
            }
        });

        // create helper to get current caret position
        // only works if support selectionStart/End properties
        if (typeof(element["selectionStart"]) !== "undefined") {
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
            _initial = '';
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