/* jQuery KeyTips Plugin 1.0.3 - http://damianedwards.com */
(function ($) {
    /// <param name="$" type="jQuery" />

    jQuery.extend({
        trace: function (msg, isDebug) {
            if (isDebug === false) return;
            if ((typeof (Debug) !== "undefined") && Debug.writeln) {
                Debug.writeln(msg);
            }
            if (window.console && window.console.log) {
                window.console.log(msg);
            }
            if (window.opera) {
                window.opera.postError(msg);
            }
            if (window.debugService) {
                window.debugService.trace(msg);
            }
        },
        keyTips: function (options) {
            // ready
            $(function () {
                var requiresHighlighting, requiresShiftAlt, accessKeysHighlighted,
                    accessKeyPopups, accessKeyPopupFields, settings;

                requiresHighlighting = !/opera/.test(navigator.userAgent); // exit if opera as it has its own dedicated access key selection interface

                if (!requiresHighlighting) return;
                requiresShiftAlt = $.browser.mozilla;
                accessKeysHighlighted = false;
                accessKeyPopups = [];
                accessKeyPopupFields = [];

                // define settings
                settings = $.extend({
                    debug: false,
                    highlightClass: "KeyTips__highlighted",
                    popupClass: "KeyTips__popup",
                    highlightMode: "popup", // alternative is "toggleClass"
                    accessKeyTag: "em", // could be any inline tag, e.g. em, strong, span
                    offsets: {
                        label: {
                            left: -20,
                            top: 2
                        },
                        button: {
                            left: -3,
                            top: -3
                        },
                        anchor: {
                            left: 2,
                            top: 9
                        },
                        text: {
                            left: -3,
                            top: -3
                        },
                        other: {
                            left: -3,
                            top: -3
                        }
                    }
                }, options);

                // functions
                var escapeId = function (id) {
                    var result = id.replace(/(:|\.)/g, '\\$1');
                    if (result === "#")
                        result = "";
                    return result;
                },

                getFieldAccessKey = function (label) {
                    var relatedFieldId = escapeId("#" + $(label).attr("htmlFor")),

                        accessKey = $.trim(
                            relatedFieldId !== ""
                            ? $(relatedFieldId + "[accesskey]").attr("accesskey") || $(label).attr("accesskey")
                            : $(label).attr("accesskey")
                        );

                    return accessKey;
                },

                getOffset = function (element) {
                    var $el = $(element);
                    if ($el.is("label")) {
                        return settings.offsets.label;
                    } else if ($el.is(":button, :submit, :reset, :image")) {
                        return settings.offsets.button;
                    } else if ($el.is("a")) {
                        return settings.offsets.anchor;
                    } else if ($el.is(":text, textarea")) {
                        return settings.offsets.text;
                    } else {
                        return settings.offsets.other;
                    };
                },

                getPopupLocation = function (element) {
                    var $el = $(element);

                    if ($el.is(":hidden") || $el.css("visibility") === "hidden")
                        return false;

                    var popupLocation = $el.offset(),
                        offset = getOffset(element);

                    popupLocation.left = popupLocation.left + offset.left;
                    popupLocation.top = popupLocation.top + offset.top;
                    return popupLocation;
                },

                createPopup = function (field, accessKey) {
                    /// <summary>Creates an access key popup and adds it to the global array</summary>
                    var popupLocation = getPopupLocation(field);

                    if (!popupLocation) return;

                    // Create popup element, set its location and classs and add to the global array
                    var popup = $("<div/>")
                        .text(accessKey)
                        .css("left", popupLocation.left)
                        .css("top", popupLocation.top)
                        .addClass(settings.popupClass)
                        .appendTo("body")
                        .get(0);

                    accessKeyPopups.push(popup);
                    accessKeyPopupFields.push(field);
                    return popup;
                },

                clearPopups = function () {
                    /// <summary>Clears all access key popups from the form</summary>
                    $(accessKeyPopups).remove();
                    accessKeyPopups = [];
                    accessKeyPopupFields = [];
                },

                insertAccessKeyTags = function () {
                    /// <summary>Find access key text in all labels and surround with a tag</summary>
                    $("label[for], label[accesskey]").each(function () {
                        // Get accesskey from corresponding form field, otherwise from the label
                        var accessKey = getFieldAccessKey(this);
                        if (typeof (accessKey) === "undefined" || accessKey === "") return true;
                        var labelHtml = $(this).html(),
                            accessKeyIndex = labelHtml.toUpperCase().indexOf(accessKey.toUpperCase());
                        if (accessKeyIndex < 0) return true;
                        // <tagName>accessKeyFromLabel</tagName>
                        var accessKeyMarkup = "<" + settings.accessKeyTag + ">" + labelHtml.substr(accessKeyIndex, 1) + "</" + settings.accessKeyTag + ">",
                            newLabelHtmlLeft = labelHtml.substring(0, accessKeyIndex),
                            newLabelHtmlRight = labelHtml.substr(accessKeyIndex + 1),
                            newLabelHtml = newLabelHtmlLeft + accessKeyMarkup + newLabelHtmlRight;
                        if (labelHtml.indexOf(accessKeyMarkup) < 0)
                            $(this).html(newLabelHtml);
                    });
                },

                createPopups = function () {
                    /// <summary>Creates popups for access keys on the form</summary>
                    if (settings.highlightMode == "popup") {
                        clearPopups();
                        var accessKeyPopupFormFields = [];

                        // Create popups for labels
                        $("label > " + settings.accessKeyTag).each(function () {
                            var text = $(this).text(),
                                label = $(this).parent(),
                                relatedFieldId = escapeId("#" + label.attr("htmlFor")),
                                formField = relatedFieldId !== "#" ? $(relatedFieldId) : null,
                                accessKey = getFieldAccessKey(label);

                            if (accessKey && text.toUpperCase() == accessKey.toUpperCase()) {
                                createPopup(label[0], accessKey);
                            }
                            accessKeyPopupFormFields.push(formField[0]);
                        });

                        $("a[href][accesskey], textarea[accesskey], input[accesskey]").each(function () {
                            if ($.inArray(this, accessKeyPopupFormFields) === -1) {
                                createPopup(this, this.accessKey);
                            }
                        });
                    }
                },

                refreshPopups = function () {
                    /// <summary>Clears and then re-creates access key popups for the form</summary>
                    clearPopups();
                    createPopups();
                },

                doHighlightAccessKeys = function (highlight) {
                    /// <summary>Toggles the highlighting of access keys on the page</summary>
                    if (settings.highlightMode === "popup") {
                        // Popups
                        var i = 0;
                        $.each(accessKeyPopups, function () {
                            var field = accessKeyPopupFields[i];
                            i++;
                            var popupLocation = getPopupLocation(field);
                            $(this).css("left", popupLocation.left)
                                   .css("top", popupLocation.top)
                                   .toggle(highlight);
                        });
                    } else if (settings.highlightMode === "toggleClass") {
                        // Toggle label class
                        $("label > " + settings.accessKeyTag).each(function () {
                            var text = $(this).text(),
                                label = $(this).parent(),
                                relatedFieldId = escapeId("#" + label.attr("htmlFor")),
                                accessKey = getFieldAccessKey(label);

                            if (!accessKey || text.toUpperCase() === accessKey.toUpperCase()) {
                                $(this).toggleClass(settings.highlightClass, highlight);
                            }
                        });
                    }
                    accessKeysHighlighted = highlight;
                },

                highlightAccessKeys = function () {
                    doHighlightAccessKeys(true);
                },

                unhighlightAccessKeys = function () {
                    doHighlightAccessKeys(false);
                };

                // bind handlers
                $(document)
                    .bind("keydown.keytips", function (e) {
                        $.trace("KeyTips document.keyDown: keyCode=" + e.keyCode +
                                ", altKey=" + e.altKey +
                                ", shiftKey=" + e.shiftKey, settings.debug);
                        if (!accessKeysHighlighted && (
                                (e.keyCode == 18 && !requiresShiftAlt) ||
                                (e.keyCode == 16 && e.altKey && requiresShiftAlt) ||
                                (e.keyCode == 18 && e.shiftKey && requiresShiftAlt))) {
                            // Highlight all the access keys
                            highlightAccessKeys();
                            //accessKeysHighlighted = true;
                        }
                    })
                    .bind("keyup.keytips", function (e) {
                        $.trace("KeyTips document.keyUp: keyCode=" + e.keyCode +
                                ", altKey=" + e.altKey +
                                ", shiftKey=" + e.shiftKey, settings.debug);
                        // Un-highlight access keys
                        if (accessKeysHighlighted) {
                            unhighlightAccessKeys();
                            //accessKeysHighlighted = false;
                        }
                    });

                $(window)
                    .bind("resize.keytips", function (e) {
                        $.trace("resize event", settings.debug);
                        // Hide the popups
                        if (accessKeysHighlighted && settings.highlightMode == "popup") {
                            unhighlightAccessKeys();
                            //accessKeysHighlighted = false;
                        }
                    })
                    .bind("blur.keytips", function (e) {
                        $.trace("blur event", settings.debug);
                        // Un-highlight access keys
                        if (accessKeysHighlighted) {
                            unhighlightAccessKeys();
                            //accessKeysHighlighted = false;
                        }
                    })
                    .bind("focus.keytips", function (e) {
                        $.trace("focus event", settings.debug);
                        // Un-highlight access keys
                        if (accessKeysHighlighted) {
                            unhighlightAccessKeys();
                            //accessKeysHighlighted = false;
                        }
                    });

                // Create the access key popups
                insertAccessKeyTags();
                createPopups();
            });

            // unload
            $(window).unload(function () {
                $(document).unbind(".keytips");
                $(window).unbind(".keytips");
            });
        }
    });
})(jQuery);