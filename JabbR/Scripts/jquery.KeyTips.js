/*!
* jQuery KeyTips Plugin 1.0.6
* Copyright 2011, Damian Edwards http://damianedwards.com
* Licensed under Ms-PL
* http://www.opensource.org/licenses/MS-PL
*/

(function ($) {
    /// <param name="$" type="jQuery" />

    var requiresHighlighting = !/opera/.test(navigator.userAgent),
        requiresShiftAlt = $.browser.mozilla,
        keyTipsShowing = false,
        keyTipPopups = [],
        keyTipPopupFields = [];

    function trace(msg, isDebug) {
        if (!isDebug) {
            return;
        }

        if ((typeof Debug !== "undefined") && Debug.writeln) {
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
    }

    function escapeId(id) {
        var result = id.replace(/(:|\.)/g, '\\$1');
        return result === "#" ? "" : result;
    }

    function getFieldAccessKey(label) {
        var relatedFieldId = escapeId("#" + $(label).attr("for")),

            accessKey = $.trim(
                relatedFieldId !== ""
                    ? $(relatedFieldId + "[accesskey]").attr("accesskey") || $(label).attr("accesskey")
                    : $(label).attr("accesskey")
            );

        return accessKey;
    }

    function getOffset(element, settings) {
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
        }
    }

    function getPopupLocation(element, settings) {
        var $el = $(element),
            popupLocation,
            offset;

        if ($el.is(":hidden") || $el.css("visibility") === "hidden") {
            return false;
        }

        popupLocation = $el.offset();
        offset = getOffset(element, settings);

        return {
            left: popupLocation.left + offset.left,
            top: popupLocation.top + offset.top
        };
    }

    function createPopup(field, accessKey, settings) {
        /// <summary>Creates an access key popup and adds it to the global array</summary>
        var popupLocation = getPopupLocation(field, settings),
            popup;

        if (!popupLocation) {
            return;
        }

        // Create popup element, set its location and classs and add to the global array
        popup = $("<div/>")
            .text(accessKey)
            .css("left", popupLocation.left)
            .css("top", popupLocation.top)
            .addClass(settings.popupClass)
            .appendTo("body")
            .get(0);

        keyTipPopups.push(popup);
        keyTipPopupFields.push(field);

        return popup;
    }

    function clearPopups(els) {
        /// <summary>Clears all access key popups from the form</summary>
        $(keyTipPopups).remove();
        keyTipPopups = [];
        keyTipPopupFields = [];
    }

    function insertAccessKeyTags(els, tag) {
        /// <summary>Find access key text in all labels and surround with a tag</summary>
        els.find("label[for], label[accesskey]").each(function () {
            // Get accesskey from corresponding form field, otherwise from the label
            var accessKey = getFieldAccessKey(this),
                labelHtml,
                accessKeyIndex,
                accessKeyMarkup,
                newLabelHtmlLeft,
                newLabelHtmlRight,
                newLabelHtml;

            if (typeof (accessKey) === "undefined" || accessKey === "") {
                return true;
            }

            labelHtml = $(this).html();
            accessKeyIndex = labelHtml.toUpperCase().indexOf(accessKey.toUpperCase());

            if (accessKeyIndex < 0) {
                return true;
            }

            // <tagName>accessKeyFromLabel</tagName>
            accessKeyMarkup = "<" + tag + ">" + labelHtml.substr(accessKeyIndex, 1) + "</" + tag + ">";
            newLabelHtmlLeft = labelHtml.substring(0, accessKeyIndex);
            newLabelHtmlRight = labelHtml.substr(accessKeyIndex + 1);
            newLabelHtml = newLabelHtmlLeft + accessKeyMarkup + newLabelHtmlRight;

            if (labelHtml.indexOf(accessKeyMarkup) < 0) {
                $(this).html(newLabelHtml);
            }
        });
    }

    function createPopups(els, settings) {
        /// <summary>Creates popups for access keys on the form</summary>
        var accessKeyPopupFormFields;

        if (settings.highlightMode === "popup") {
            clearPopups(els);
            accessKeyPopupFormFields = [];

            // Create popups for labels
            els.find("label > " + settings.accessKeyTag).each(function () {
                var text = $(this).text(),
                    label = $(this).parent(),
                    relatedFieldId = escapeId("#" + label.attr("for")),
                    formField = relatedFieldId !== "#" ? $(relatedFieldId) : null,
                    accessKey = getFieldAccessKey(label);

                if (accessKey && text.toUpperCase() === accessKey.toUpperCase()) {
                    createPopup(label[0], accessKey, settings);
                }
                accessKeyPopupFormFields.push(formField[0]);
            });

            // Create popups for anchors and form fields
            els.find("a[href][accesskey], textarea[accesskey], input[accesskey], button[accesskey]").each(function () {
                if ($.inArray(this, accessKeyPopupFormFields) === -1) {
                    createPopup(this, this.accessKey, settings);
                }
            });
        }
    }

    function doShowKeyTips(highlight, els, settings) {
        /// <summary>Toggles the highlighting of access keys on the page</summary>
        /// <param name="els" type="jQuery" />
        var i = 0;
        if (settings.highlightMode === "popup") {
            // Popups
            $.each(keyTipPopups, function () {
                var field = keyTipPopupFields[i],
                    popupLocation = getPopupLocation(field, settings);
                i++;
                $(this).css("left", popupLocation.left)
                    .css("top", popupLocation.top)
                    .toggle(highlight);
            });
        } else if (settings.highlightMode === "toggleClass") {
            // Toggle label class
            els.find("label > " + settings.accessKeyTag).each(function () {
                var text = $(this).text(),
                    label = $(this).parent(),
                    relatedFieldId = escapeId("#" + label.attr("for")),
                    accessKey = getFieldAccessKey(label);

                if (!accessKey || text.toUpperCase() === accessKey.toUpperCase()) {
                    $(this).toggleClass(settings.highlightClass, highlight);
                }
            });
        }
        keyTipsShowing = highlight;
    }

    function showKeyTips(els, settings) {
        doShowKeyTips(true, els, settings);
    }

    function hideKeyTips(els, settings) {
        doShowKeyTips(false, els, settings);
    }

    $.fn.keyTips = function (options) {
        /// <summary>Shows KeyTips popups for elements' access keys</summary>
        /// <param name="options" type="Object">An object literal map:
        ///     &#10; debug: A boolean indicating whether to write debug information out the browser console
        ///     &#10; highlightMode: The display mode, choices are 'popup' (the default), or 'toggleClass'
        ///     &#10; highlightClass: CSS class name (default 'KeyTips__highlighted') to highlight accesskeys with when highlightMode is 'toggleClass'
        ///     &#10; popupClass: CSS class name (default 'KeyTips_popup') to use on popups when highlightMode is 'popup'
        ///     &#10; accessKeyTag: Inline HTML tag name to use (default 'em') to surround accesskeys in labels with
        ///     &#10; offsets: Offset values for popups, settable by tag type. Defaults:
        ///     &#10;     {
        ///     &#10;         label: { left: -20, top: 2 },
        ///     &#10;         button: { left: -3, top: -3 },
        ///     &#10;         anchor: { left: 2, top: 9 },
        ///     &#10;         text: { left: -3, top: -3 },
        ///     &#10;         other: { left: -3, top: -3 }
        ///     &#10;     }
        /// </param>

        var els = this,
            settings;

        if (!requiresHighlighting) {
            // exit if opera as it has its own dedicated access key selection interface
            return;
        }

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

        // bind handlers
        $(document)
            .bind("keydown.keytips", function (e) {
                trace("KeyTips document.keyDown: keyCode=" + e.keyCode +
                        ", altKey=" + e.altKey +
                        ", shiftKey=" + e.shiftKey, settings.debug);

                if (!keyTipsShowing && (
                        (e.keyCode == 18 && !requiresShiftAlt) ||
                        (e.keyCode == 16 && e.altKey && requiresShiftAlt) ||
                        (e.keyCode == 18 && e.shiftKey && requiresShiftAlt))) {
                    // Highlight all the access keys
                    showKeyTips(els, settings);
                }
            })
            .bind("keyup.keytips", function (e) {
                trace("KeyTips document.keyUp: keyCode=" + e.keyCode +
                        ", altKey=" + e.altKey +
                        ", shiftKey=" + e.shiftKey, settings.debug);

                // Un-highlight access keys
                if (keyTipsShowing) {
                    hideKeyTips(els, settings);
                }
            });

        $(window)
            .bind("resize.keytips", function (e) {
                trace("resize event", settings.debug);
                // Hide the popups
                if (keyTipsShowing && settings.highlightMode == "popup") {
                    hideKeyTips(els, settings);
                }
            })
            .bind("blur.keytips", function (e) {
                trace("blur event", settings.debug);
                // Un-highlight access keys
                if (keyTipsShowing) {
                    hideKeyTips(els, settings);
                }
            })
            .bind("focus.keytips", function (e) {
                trace("focus event", settings.debug);
                // Un-highlight access keys
                if (keyTipsShowing) {
                    hideKeyTips(els, settings);
                }
            });

        // Create the access key popups
        insertAccessKeyTags(els, settings.accessKeyTag);
        createPopups(els, settings);

        // unload
        $(window).unload(function () {
            $(document).unbind(".keytips");
            $(window).unbind(".keytips");
        });
    };

    $.keyTips = function (options) {
        /// <summary>DEPRECATED: Use $('selector').keyTips() instead.
        ///     &#10; Shows KeyTips popups for all elements in the page
        /// </summary>
        /// <param name="options" type="Object">An object literal map:
        ///     &#10; debug: A boolean indicating whether to write debug information out the browser console
        ///     &#10; highlightMode: The display mode, choices are 'popup' (the default), or 'toggleClass'
        ///     &#10; highlightClass: CSS class name (default 'KeyTips__highlighted') to highlight accesskeys with when highlightMode is 'toggleClass'
        ///     &#10; popupClass: CSS class name (default 'KeyTips_popup') to use on popups when highlightMode is 'popup'
        ///     &#10; accessKeyTag: Inline HTML tag name to use (default 'em') to surround accesskeys in labels with
        ///     &#10; offsets: Offset values for popups, settable by tag type. Defaults:
        ///     &#10;     {
        ///     &#10;         label: { left: -20, top: 2 },
        ///     &#10;         button: { left: -3, top: -3 },
        ///     &#10;         anchor: { left: 2, top: 9 },
        ///     &#10;         text: { left: -3, top: -3 },
        ///     &#10;         other: { left: -3, top: -3 }
        ///     &#10;     }
        /// </param>
        $("body").keyTips(options);
    };

} (jQuery));