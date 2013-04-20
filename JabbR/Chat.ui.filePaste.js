(function ($) {
    function match() {
        var ua = navigator.userAgent.toLowerCase();

        var result = /(chrome)[ \/]([\w.]+)/.exec(ua) ||
            /(webkit)[ \/]([\w.]+)/.exec(ua) ||
            /(opera)(?:.*version|)[ \/]([\w.]+)/.exec(ua) ||
            /(msie) ([\w.]+)/.exec(ua) ||
            ua.indexOf("compatible") < 0 && /(mozilla)(?:.*? rv:([\w.]+)|)/.exec(ua) || [];
        return {
            browser: result[1] || "",
            version: result[2] || "0"
        };
    }
    var browser = {};

    var matched = match();

    if (matched.browser) {
        browser[matched.browser] = true;
        browser.version = matched.version;
    }

    // Chrome is Webkit, but Webkit is also Safari.
    if (browser.chrome) {
        browser.webkit = true;
    } else if (browser.webkit) {
        browser.safari = true;
    }

    $.extend({
        //here's the actual call we're going to make
        imagePaste: function (callback) {
            if (browser.mozilla) {
                initializeFirefox(callback);
            } else if (browser.webkit) {
                initializeWebkit(callback);
            }
        }
    });

    function initializeWebkit(callback) {
        $.event.fix = (function (fix) {
            return function (event) {
                event = fix.apply(this, arguments);
                if (event.type.indexOf("copy") === 0 || event.type.indexOf("paste") === 0) {
                    event.clipboardData = event.originalEvent.clipboardData;
                }
                return event;
            };
        })($.event.fix);

        var defaults = {
            callback: $.noop,
            matchType: /image.*/
        };
        var pasteImageReader = function (selector, options) {
            if (typeof options === "function") {
                options = {
                    callback: options
                };
            }
            options = $.extend({}, defaults, options);

            return $(selector).each(function () {
                var local = this;
                return $(this).bind("paste", function (event) {
                    var haveData = false;
                    var clipboard = event.clipboardData;
                    return Array.prototype.forEach.call(clipboard.types, function (type, index) {
                        var file, stream;
                        if (haveData) {
                            return true;
                        }
                        if (type.match(options.matchType) || clipboard.items[index].type.match(options.matchType)) {
                            file = clipboard.items[index].getAsFile();
                            stream = new FileReader();
                            stream.onload = function (event) {
                                return options.callback.call(local, {
                                    dataURL: event.target.result,
                                    event: event,
                                    file: file,
                                    name: file.name
                                });
                            };
                            stream.readAsDataURL(file);
                            haveData = true;
                            return true;
                        }
                    });
                });
            });
        };

        pasteImageReader("html", callback);
    }

    function initializeFirefox(options) {
        var defaults = {
            contentEditableDiv: "#paste"
        };
        if (typeof options === "function") {
            options = {
                callback: options
            };
        }
        options = $.extend({}, defaults, options);

        //this portion of the source is
        //for mozilla only
        var pasteDiv = $(options.contentEditableDiv)[0];

        function waitForPasteData(pasteDiv) {
            if (pasteDiv.childNodes && pasteDiv.childNodes.length > 0) {
                processPaste(pasteDiv);
            } else {
                setTimeout(function () {
                    waitForPasteData(pasteDiv);
                }, 20);
            }
        }

        function processPaste(pasteDiv) {
            
            var innerHtml = pasteDiv.innerHTML;
            var data = {
                image: innerHtml.indexOf("<img") !== -1 && innerHtml.indexOf("src=") !== -1,
                base64: innerHtml.indexOf("base64,") !== -1,
                png: innerHtml.indexOf("iVBORw0K") !== -1
            };

            if (data.image && data.base64) {
                var tag = innerHtml.split('<img src="');
                tag = tag[1].split('" alt="">');
                var fileData = {
                    dataURL: tag[0],
                    file: {
                        size: 0 //file is in base 64 format, we need to get the filesize somehow
                    }
                };

                //we have an image so let's use it
                //what do you want to do with it?

                //upload using base64?
                
                options.callback(fileData);
            } else {
                
                pasteDiv.innerHTML = "";
            }
        }

        function handlePaste(event, pasteDiv) {
            if (event && event.clipboardData && event.clipboardData.getData) {
                waitForPasteData(pasteDiv);
                if (event.preventDefault) {
                    event.stopPropagation();
                    event.preventDefault();
                }
                return false;
            } else {
                waitForPasteData(pasteDiv);
                return true;
            }
        }

        document.onpaste = function(event) {
            pasteDiv.focus();
            handlePaste(event, pasteDiv);
        };
    }
})(jQuery);