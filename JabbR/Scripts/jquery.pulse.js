/*global jQuery*/
/*jshint curly:false*/

;(function ( $, window) {
  "use strict";

  var defaults = {
      pulses   : 1,
      interval : 0,
      returnDelay : 0,
      duration : 500
    };

  $.fn.pulse = function(properties, options, callback) {
    // $(...).pulse('destroy');
    var stop = properties === 'destroy';

    if (typeof options === 'function') {
      callback = options;
      options = {};
    }

    options = $.extend({}, defaults, options);

    if (!(options.interval >= 0))    options.interval = 0;
    if (!(options.returnDelay >= 0)) options.returnDelay = 0;
    if (!(options.duration >= 0))    options.duration = 500;
    if (!(options.pulses >= -1))     options.pulses = 1;
    if (typeof callback !== 'function') callback = function(){};

    return this.each(function () {
      var el = $(this),
          property,
          original = {};

      var data = el.data('pulse') || {};
      data.stop = stop;
      el.data('pulse', data);

      for (property in properties) {
        if (properties.hasOwnProperty(property)) original[property] = el.css(property);
      }

      var timesPulsed = 0;

      function animate() {
        if (el.data('pulse').stop) return;
        if (options.pulses > -1 && ++timesPulsed > options.pulses) return callback.apply(el);
        el.animate(
          properties,
          {
            duration : options.duration / 2,
            complete : function(){
              window.setTimeout(function(){
                el.animate(original, {
                  duration : options.duration / 2,
                  complete : function() {
                    window.setTimeout(animate, options.interval);
                  }
                });
              },options.returnDelay);
            }
          }
        );
      }

      animate();
    });
  };

})( jQuery, window, document );
