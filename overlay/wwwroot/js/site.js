/* Tanglaw — site interactions: smooth scroll, reveal-on-scroll, counters, parallax, header state. */
(function () {
    "use strict";

    var reduceMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    var root = document.documentElement;

    /* ---------- Smooth scrolling (Lenis, desktop only) ---------- */
    var lenis = null;
    if (!reduceMotion && window.Lenis) {
        lenis = new window.Lenis({ duration: 1.15, easing: function (t) { return Math.min(1, 1.001 - Math.pow(2, -10 * t)); } });
        var raf = function (time) { lenis.raf(time); requestAnimationFrame(raf); };
        requestAnimationFrame(raf);

        // in-page anchors (e.g. scroll cue)
        document.addEventListener("click", function (e) {
            var a = e.target.closest('a[href^="#"]');
            if (!a || a.getAttribute("href").length < 2) return;
            var target = document.querySelector(a.getAttribute("href"));
            if (!target) return;
            e.preventDefault();
            lenis.scrollTo(target, { offset: -70 });
        });
    }

    var scrollToTop = function () {
        if (lenis) lenis.scrollTo(0); else window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
    };

    /* ---------- Elements ---------- */
    var header = document.querySelector(".site-header");
    var progress = document.querySelector(".scroll-progress span");
    var toTop = document.querySelector(".to-top");
    var parallax = Array.prototype.slice.call(document.querySelectorAll("[data-parallax]"));
    var process = document.querySelector(".process");
    var steps = process ? Array.prototype.slice.call(process.querySelectorAll(".step")) : [];
    var navToggle = document.getElementById("nav-toggle");
    var lastY = window.scrollY;
    var ticking = false;

    function onScroll() {
        var y = window.scrollY;
        var max = Math.max(1, root.scrollHeight - window.innerHeight);
        var ratio = Math.min(1, y / max);

        if (header) {
            header.classList.toggle("is-scrolled", y > 40);
            // hide header when scrolling down fast past the hero, show on scroll up
            var navOpen = navToggle && navToggle.checked;
            header.classList.toggle("is-hidden", !navOpen && y > 600 && y > lastY + 4);
            if (y < lastY - 4) header.classList.remove("is-hidden");
        }
        if (progress) progress.style.transform = "scaleX(" + ratio + ")";
        if (toTop) {
            toTop.classList.toggle("is-visible", y > 700);
            toTop.style.setProperty("--sp", ratio.toFixed(3));
        }

        if (!reduceMotion) {
            parallax.forEach(function (el) {
                var rect = el.parentElement.getBoundingClientRect();
                if (rect.bottom < 0 || rect.top > window.innerHeight) return;
                var speed = parseFloat(el.getAttribute("data-parallax")) || 0.2;
                el.style.transform = "translate3d(0," + (-rect.top * speed).toFixed(1) + "px,0)";
            });
        }

        if (process) {
            var r = process.getBoundingClientRect();
            var start = window.innerHeight * 0.75;
            var p = Math.max(0, Math.min(1, (start - r.top) / (r.height + window.innerHeight * 0.15)));
            process.style.setProperty("--p", p.toFixed(3));
            steps.forEach(function (s, i) { s.classList.toggle("is-active", p >= (i / steps.length) + 0.02 || p >= 0.98); });
        }

        lastY = y;
        ticking = false;
    }

    window.addEventListener("scroll", function () {
        if (!ticking) { requestAnimationFrame(onScroll); ticking = true; }
    }, { passive: true });
    window.addEventListener("resize", onScroll);
    onScroll();

    if (toTop) toTop.addEventListener("click", scrollToTop);

    /* ---------- Mobile nav: lock scroll, close on link click / Escape ---------- */
    if (navToggle) {
        var sync = function () {
            document.body.classList.toggle("nav-open", navToggle.checked);
            if (lenis) { if (navToggle.checked) lenis.stop(); else lenis.start(); }
        };
        navToggle.addEventListener("change", sync);
        document.querySelectorAll(".main-nav a").forEach(function (a) {
            a.addEventListener("click", function () { navToggle.checked = false; sync(); });
        });
        document.addEventListener("keydown", function (e) {
            if (e.key === "Escape" && navToggle.checked) { navToggle.checked = false; sync(); }
        });
    }

    /* ---------- Reveal on scroll ---------- */
    var revealEls = document.querySelectorAll(".reveal, .img-reveal");
    if ("IntersectionObserver" in window && !reduceMotion) {
        var io = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) {
                    entry.target.classList.add("is-visible");
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.12, rootMargin: "0px 0px -8% 0px" });
        revealEls.forEach(function (el) { io.observe(el); });
    } else {
        revealEls.forEach(function (el) { el.classList.add("is-visible"); });
    }

    /* ---------- Animated counters: "120+", "₱18B", "15" ---------- */
    var counters = document.querySelectorAll("[data-count]");
    var animateCount = function (el) {
        var raw = el.textContent.trim();
        var m = raw.match(/^([^\d]*)([\d.,]+)(.*)$/);
        if (!m) return;
        var prefix = m[1], suffix = m[3];
        var target = parseFloat(m[2].replace(/,/g, ""));
        var decimals = (m[2].split(".")[1] || "").length;
        var duration = 1800, start = null;
        var step = function (ts) {
            if (!start) start = ts;
            var t = Math.min(1, (ts - start) / duration);
            var eased = 1 - Math.pow(1 - t, 4);
            el.textContent = prefix + (target * eased).toLocaleString(undefined, { minimumFractionDigits: decimals, maximumFractionDigits: decimals }) + suffix;
            if (t < 1) requestAnimationFrame(step); else el.textContent = raw;
        };
        el.textContent = prefix + "0" + suffix;
        requestAnimationFrame(step);
    };
    if ("IntersectionObserver" in window && !reduceMotion) {
        var co = new IntersectionObserver(function (entries) {
            entries.forEach(function (entry) {
                if (entry.isIntersecting) { animateCount(entry.target); co.unobserve(entry.target); }
            });
        }, { threshold: 0.6 });
        counters.forEach(function (el) { co.observe(el); });
    }

    /* ---------- Fade images in when loaded ---------- */
    document.querySelectorAll("img.fade").forEach(function (img) {
        var done = function () { img.classList.add("loaded"); };
        if (img.complete && img.naturalWidth) done();
        else { img.addEventListener("load", done); img.addEventListener("error", done); }
    });
})();
