(function ($) {
    var defaultOptions = {};
    var _loadScript = function (url, options) {
        ///<summary>
        /// Hijacking the document.write method and retrieves the script
        /// on the target location. If the options parameter contains a
        /// target element, the elements html will be replaced by the
        /// content written into the document.write method. If the options
        /// parameter contains a callback, the callback will be called once
        /// everything is done.
        ///
        /// Restores the original document.write once completed. Returns
        /// the jQuery ajax request object to allow deferring other pasties.
        ///</summary>
        var content = [];
        var oldWriter = document.write;
        document.write = function (str) {
            content.push(str);
        };
        return $.getScript(url,
            function () {
                document.write = oldWriter;
                var joinedContent = content.join('');
                if (options.targetElement && $.isFunction(options.targetElement.html)){
                    options.targetElement.append(joinedContent);
                    //Internet explorer doesn't load the css links when just added to the
                    //target element, therefore we move those to the header.
                    var links = $('link', options.targetElement);
                    links.remove();
                    $('head').append(links);
                }
                if (options.callback !== null && $.isFunction(options.callback))
                    //Do the callback with a timeout to allow the browser to enforce css
                    //rules on potential new html before calling the callback. Firefox
                    //would not enforce the stylesheets in advance otherwise.
                    setTimeout(function(){options.callback(joinedContent);},1);
            }
        );
    };
    var lastScriptLoader = null;
    var _deferringScriptLoader = function (url, options) {
        ///<summary>
        /// Makes sure that we do not load multiple scripts at the same this. This
        /// is important since we hijack the document.write method.
        ///</summary>
        if (lastScriptLoader == null)
            lastScriptLoader = _loadScript(url, options);
        else
            lastScriptLoader = lastScriptLoader.pipe(function(){return _loadScript(url, options);});
    };
    var _getSettings = function(options){
        ///<summary>
        /// Loads the default settings and extends them with the user-provided
        /// settings or the user-provided function object.
        ///</summary>
        var settings = null;
        if (options && !$.isFunction(options))
            settings = $.extend({}, defaultOptions, options);
        else
            settings = $.extend({}, defaultOptions);
        if ($.isFunction(options))
            settings.callback = options;
        return settings;
    };
    var _captureWriteExternal = function (url, options) {
        ///<summary>
        /// Loads the script and enters any content written to the document.write
        /// method as the HTML of the elements this methods is called on (if any).
        /// If a callback is supplied, either as a function for options or given
        /// options as an object with a callback property, the callback will be
        /// called once the script loading is completed.
        ///</summary>
        if (this && this.length > 0){
            return $(this).each(function(){
                var settings = _getSettings(options);
                settings.targetElement = $(this);
                _deferringScriptLoader(url, settings);
            });
        }
        else {
            _deferringScriptLoader(url, _getSettings(options));
        }
    };
    $.fn.captureDocumentWrite = _captureWriteExternal;
})(jQuery);
