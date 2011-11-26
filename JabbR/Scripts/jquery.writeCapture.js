/**
* jquery.writeCapture.js
*
* Note that this file only provides the jQuery plugin functionality, you still
* need writeCapture.js. The compressed version will contain both as as single
* file.
*
* @author noah <noah.sloan@gmail.com>
*
*/
(function ($, wc, noop) {
    // methods that take HTML content (according to API)
    var methods = {
        html: html
    };
    // TODO wrap domManip instead?
    $.each(['append', 'prepend', 'after', 'before', 'wrap', 'wrapAll', 'replaceWith',
		'wrapInner'], function () { methods[this] = makeMethod(this); });

    function isString(s) {
        return Object.prototype.toString.call(s) == "[object String]";
    }

    function executeMethod(method, content, options, cb) {
        if (arguments.length == 0) return proxyMethods.call(this);

        var m = methods[method];
        if (method == 'load') {
            return load.call(this, content, options, cb);
        }
        if (!m) error(method);
        return doEach.call(this, content, options, m);
    }

    $.fn.writeCapture = executeMethod;

    var PROXIED = '__writeCaptureJsProxied-fghebd__';
    // inherit from the jQuery instance, proxying the HTML injection methods
    // so that the HTML is sanitized
    function proxyMethods() {
        if (this[PROXIED]) return this;

        var jq = this;
        function F() {
            var _this = this, sanitizing = false;
            this[PROXIED] = true;
            $.each(methods, function (method) {
                var _super = jq[method];
                if (!_super) return;
                _this[method] = function (content, options, cb) {
                    // if it's unsanitized HTML, proxy it
                    if (!sanitizing && isString(content)) {
                        try {
                            sanitizing = true;
                            return executeMethod.call(_this, method, content,
								options, cb);
                        } finally {
                            sanitizing = false;
                        }
                    }
                    return _super.apply(_this, arguments); // else delegate
                };
            });
            // wrap pushStack so that the new jQuery instance is also wrapped
            this.pushStack = function () {
                return proxyMethods.call(jq.pushStack.apply(_this, arguments));
            };
            this.endCapture = function () { return jq; };
        }
        F.prototype = jq;
        return new F();
    }

    function doEach(content, options, action) {
        var done, self = this;
        if (options && options.done) {
            done = options.done;
            delete options.done;
        } else if ($.isFunction(options)) {
            done = options;
            options = null;
        }
        wc.sanitizeSerial($.map(this, function (el) {
            return {
                html: content,
                options: options,
                action: function (text) {
                    action.call(el, text);
                }
            };
        }), done && function () { done.call(self); } || done);
        return this;
    }


    function html(safe) {
        $(this).html(safe);
    }

    function makeMethod(method) {
        return function (safe) {
            $(this)[method](safe);
        };
    }

    function load(url, options, callback) {
        var self = this, selector, off = url.indexOf(' ');
        if (off >= 0) {
            selector = url.slice(off, url.length);
            url = url.slice(0, off);
        }
        if ($.isFunction(callback)) {
            options = options || {};
            options.done = callback;
        }
        return $.ajax({
            url: url,
            type: options && options.type || "GET",
            dataType: "html",
            data: options && options.params,
            complete: loadCallback(self, options, selector)
        });
    }

    function loadCallback(self, options, selector) {
        return function (res, status) {
            if (status == "success" || status == "notmodified") {
                var text = getText(res.responseText, selector);
                doEach.call(self, text, options, html);
            }
        };
    }

    var PLACEHOLDER = /jquery-writeCapture-script-placeholder-(\d+)-wc/g;
    function getText(text, selector) {
        if (!selector || !text) return text;

        var id = 0, scripts = {};
        return $('<div/>').append(
			text.replace(/<script(.|\s)*?\/script>/g, function (s) {
			    scripts[id] = s;
			    return "jquery-writeCapture-script-placeholder-" + (id++) + '-wc';
			})
		).find(selector).html().replace(PLACEHOLDER, function (all, id) {
		    return scripts[id];
		});
    }

    function error(method) {
        throw "invalid method parameter " + method;
    }

    // expose core
    $.writeCapture = wc;
})(jQuery, writeCapture.noConflict());