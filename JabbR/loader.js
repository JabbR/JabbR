/*
 * Windows Azure Loading Style with Canvas
 * Taken from: http://codepen.io/btipling/pen/frkzg
 * Created by: Bjorn Tipling (http://codepen.io/btipling)
 */

window.requestAnimFrame = (function (callback) {
    return window.requestAnimationFrame || window.webkitRequestAnimationFrame || window.mozRequestAnimationFrame || window.oRequestAnimationFrame || window.msRequestAnimationFrame || function (callback) {
        window.setTimeout(callback, 1000 / 60);
    };
})();


function colorHslToRgb(color) {
    var r, g, b, h, s, l;
    h = color.h;
    s = color.s;
    l = color.l;
    if (s === 0) {
        r = g = b = l; // achromatic
    } else {
        var hue2rgb = function (p, q, t) {
            if (t < 1 / 6) return p; // + (q - p) * 6 * t;
            if (t < 1 / 2) return q;
            if (t < 2 / 3) return p; // + (q - p) * (2 / 3 - t) * 6;
            return p;
        };

        var q = l < 0.5 ? l * (1 + s) : l + s - l * s;
        var p = 2 * l - q;
        r = hue2rgb(p, q, h + 1 / 3);
        g = hue2rgb(p, q, h);
        b = hue2rgb(p, q, h - 1 / 3);
    }

    color.r = parseInt(r * 255, 10);
    color.g = parseInt(g * 255, 10);
    color.b = parseInt(b * 255, 10);
}


function colorRgbToHsl(color) {
    var r, g, b;
    r = color.r;
    g = color.g;
    b = color.b;
    r /= 255;
    g /= 255;
    b /= 255;
    var max = Math.max(r, g, b),
        min = Math.min(r, g, b);
    var h, s, l = (max + min) / 2;

    if (max == min) {
        h = s = 0; // achromatic
    } else {
        var d = max - min;
        s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
        switch (max) {
            case r:
                h = (g - b) / d + (g < b ? 6 : 0);
                break;
            case g:
                h = (b - r) / d + 2;
                break;
            case b:
                h = (r - g) / d + 4;
                break;
        }
        h /= 6;
    }
    color.h = h;
    color.s = s;
    color.l = l;
}


function main() {
    var colors, arcs, w, h, canvas, ctx, x, y, borderWidth, i, r, step,
    startColor, addColor, numStripes;

    canvas = document.getElementById('canvas');
    ctx = canvas.getContext('2d');
    w = canvas.width;
    h = canvas.height;
    x = w / 2;
    y = w / 2;
    borderWidth = 5;
    r = 1;
    step = 10;
    numStripes = 4;
    startColor = {
        r: 0,
        g: 16,
        b: 63
    };

    arcs = [];
    for (i = 0; i < numStripes; i++) {
        addColor = {
            r: startColor.r,
            g: startColor.g,
            b: startColor.b
        };
        arcs.push({
            width: borderWidth,
            color: addColor,
            radius: r,
            x: x,
            y: y,
            offset: 1,
            frameStart: step * i
        });
        r -= 0.1;
        luminateColor(startColor);
    }
    run(arcs, ctx, canvas, 0, step);
}

function luminateColor(color) {
    colorRgbToHsl(color);
    color.l += 0.15;
    colorHslToRgb(color);
}

function clear(ctx, canvas) {
    ctx.clearRect(0, 0, canvas.width, canvas.height);
}

function run(arcs, ctx, canvas, frame, step) {
    var i, speed, pause, arc, c, newColor;
    speed = 75;
    pause = arcs.length * step + step;
    if (frame === speed + pause) {
        frame = 0;
    } else {
        for (i = 0; i < arcs.length; i++) {
            arc = arcs[i];
            if (!c) {
                c = {
                    r: arc.color.r,
                    g: arc.color.g,
                    b: arc.color.b

                };
                colorRgbToHsl(c);
                c.h += 0.001;
                if (c.h >= 1) {
                    c.h = 0.001;
                }
                colorHslToRgb(c);
            }
            arc.color = {
                r: c.r,
                g: c.g,
                b: c.b
            };
            luminateColor(c);
        }
    }
    clear(ctx, canvas);
    for (i = 0; i < arcs.length; i++) {
        arc = arcs[i];
        setupArc(arc, ctx, frame, speed);
    }
    requestAnimFrame(function () {
        frame++;
        run(arcs, ctx, canvas, frame, step);
    });
}

function getPos(pos, arc, frame, speed) {

    var animPos, newPos;

    if (frame < arc.frameStart) {
        frame = 0;
    } else {
        frame -= arc.frameStart;
        if (frame > speed) {
            frame = 0;
        }
    }
    animPos = ((frame / speed) * (Math.PI * 2));
    newPos = pos + animPos;
    if (newPos > (Math.PI * 2)) {
        newPos -= Math.PI * 2;
    }
    return newPos;
}

function setupArc(arc, ctx, frame, speed) {
    var arcWidth, x, y, radius, color, start, end;
    arcWidth = arc.width;
    color = arc.color;
    x = arc.x;
    y = arc.y;
    radius = (x - arcWidth) * arc.radius;
    start = getPos(arc.offset * Math.PI, arc, frame, speed);
    end = getPos(arc.offset * Math.PI * 2, arc, frame, speed);
    drawArc(ctx, arcWidth, x, y, radius, start, end, color);
}

function drawArc(ctx, arcWidth, x, y, radius, start, end, color) {
    ctx.closePath();
    ctx.beginPath();
    ctx.lineWidth = arcWidth;
    ctx.strokeStyle = 'rgb(' + color.r + ',' + color.g + ',' + color.b + ')';
    ctx.arc(x, y, radius, start, end, false);
    ctx.stroke();
}

main();