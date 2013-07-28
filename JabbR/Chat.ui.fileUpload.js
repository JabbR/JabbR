(function ($, ui, utility) {
    "use strict";
    
    var $ui = $(ui),
        $hiddenFile = $('#hidden-file'),
        $previewUpload = $('#jabbr-upload-preview'),
        $imageUploadPreview = $('#jabbr-upload-preview #image-upload-preview'),
        $unknownUploadPreview = $('#jabbr-upload-preview #unknown-upload-preview'),
        $previewUploadButton = $('#jabbr-upload-preview #upload-preview-upload'),
        $previewCancelButton = $('#jabbr-upload-preview #upload-preview-cancel'),
        $fileRoom = $('#file-room'),
        $fileConnectionId = $('#file-connection-id'),
        $uploadCallback = null;

    function showUploadPreview(file, type, uploader) {
        $uploadCallback = uploader;
        $imageUploadPreview.show();
        $unknownUploadPreview.hide();
        //set image url
        if (file.dataURL.indexOf('data:image') === 0) {
            $previewUpload.find('h3').text(utility.getLanguageResource('Client_Uploading', type));
            $imageUploadPreview.attr('src', file.dataURL);
        } else {
            $previewUpload.find('h3').text(utility.getLanguageResource('Client_Uploading', file.name));
            if (type === 'image') {
                //uploading an actual file
                $imageUploadPreview.attr('src', file.data.result);
            } else {
                //nothing just yet
                $imageUploadPreview.hide();
                $unknownUploadPreview.show();
            }
        }

        $previewUpload.modal();
    }

    function uploadFile(file) {
        var uploader = {
            submitFile: function (connectionId, room) {
                $fileConnectionId.val(connectionId);

                $fileRoom.val(room);
                $.ajax({
                    url: $("#upload").attr("action"),
                    type: 'POST',
                    xhr: function () {
                        var h = $.ajaxSettings.xhr();
                        if (h.upload) {
                            h.upload.addEventListener("progress", function () {
                                //empty handler for future progress bar for upload
                            });
                        }

                        return h;
                    },
                    data: {
                        file: file.data,
                        room: room,
                        connectionId: connectionId,
                        filename: file.name,
                        size: file.length,
                        type: file.type
                    }
                }).done(function () {
                    //remove image from preview
                    $imageUploadPreview.attr('src', '');
                });

                $hiddenFile.val(''); //hide upload dialog
            }
        };

        ui.addMessage(utility.getLanguageResource('Client_Uploading', file.name), 'broadcast');

        $ui.trigger(ui.events.fileUploaded, [uploader]);
    }

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

    (function initializeFilePaste() {
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
                if (browser.webkit) {
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
    })();

    // Crazy browser hack
    $hiddenFile[0].style.left = '-800px';

    $.imagePaste(function (file) {
        showUploadPreview(file, 'clipboard', function () {
            file.name = 'clipboard.png';
            file.data = $imageUploadPreview.attr('src');
            file.filename = null;
            file.type = 'image/png';
            file.length = 0;
            uploadFile(file);
        });
    });

    $previewUploadButton.on('click', function () {
        // Callback is initialized when previewUpload is
        // created. This button is only available when
        // modal is being shown. Hence should never be
        // stale. 
        $uploadCallback();
        $previewUpload.modal('hide');
    });

    $(document).on('dragenter dragover', '.messages.current', function () {
        //show drag target
        //get css position
        //width,height
        var position = $(this).offset();
        var size = { width: $(this).width(), height: $(this).height() };
        $('#drop-file-target').css({
            top: position.top + 1,
            left: position.left + 1,
            width: size.width,
            height: size.height,
            position: 'absolute',
            background: '#000',
            opacity: '.25'
        });

        $('#drop-file-target').fadeIn(500);
    });

    $('.drop-file-text').on('dragexit dragleave', function (e) {
        e.preventDefault();
        e.stopPropagation();
    });

    $('#drop-file-target').on('dragexit dragleave', function (e) {
        e.preventDefault();
        e.stopPropagation();
        if (e.currentTarget.id !== 'drop-file-text') {
            $('#drop-file-target').fadeOut(500);
        }
    });

    //change the drop target from the one that initiated it
    //in this case .messages.current
    $('#drop-file-target').on('dragenter dragover', function (e) {
        e.preventDefault();
        e.stopPropagation();
        return false; //required for IE
    });

    $('#drop-file-target').on('click', function () {
        $('#drop-file-target').fadeOut(500);
    });

    $('#drop-file-target').on('drop', function (e) {
        e = e || window.event;
        e.stopPropagation();
        e.preventDefault();
        if (e.originalEvent.dataTransfer) {
            if (e.originalEvent.dataTransfer.files.length) {
                var file = e.originalEvent.dataTransfer.files[0];

                var reader = new FileReader();
                reader.onload = (function () {
                    return function (loadEvent) {
                        showUploadPreview({ dataURL: loadEvent.target.result, name: file.name }, file.name, function () {
                            file.data = loadEvent.target.result;
                            uploadFile(file);
                        });
                    };
                })(file);

                reader.readAsDataURL(file);
            }
        }

        $('#drop-file-target').fadeOut(500);
        return false;
    });

    $previewCancelButton.on('click', function () {
        $previewUpload.modal('hide');
    });

    $hiddenFile.change(function (e) {
        var file = e.target.files[0];

        var reader = new FileReader();
        reader.onload = (function () {
            return function (e) {
                showUploadPreview({ dataURL: e.target.result, name: file.name }, file.name, function () {
                    file.data = e.target.result;
                    uploadFile(file);
                });

            };
        })(file);

        reader.readAsDataURL(file);
    });

})(window.jQuery, window.chat.ui, window.chat.utility);