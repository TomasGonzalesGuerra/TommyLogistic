window.tourGuide = {

    getElementPosition: function (elementId) {
        const el = document.getElementById(elementId);
        if (!el) return null;

        const rect = el.getBoundingClientRect();
        return {
            top: rect.top + window.scrollY,
            left: rect.left + window.scrollX,
            width: rect.width,
            height: rect.height,
            viewportTop: rect.top
        };
    },

    scrollToElement: function (elementId, offset) {
        const el = document.getElementById(elementId);
        if (!el) return;

        const rect = el.getBoundingClientRect();
        const targetY = rect.top + window.scrollY - (offset ?? 120);
        window.scrollTo({ top: targetY, behavior: 'smooth' });
    },

    getScrollY: function () {
        return window.scrollY;
    }

};