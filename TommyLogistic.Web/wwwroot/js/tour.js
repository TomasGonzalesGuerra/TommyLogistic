window.tourGetElementRect = (selector) => {
    const el = document.querySelector(selector);
    if (!el) return null;
    const r = el.getBoundingClientRect();
    el.scrollIntoView({ behavior: 'smooth', block: 'center' });
    return { left: r.left, top: r.top, width: r.width, height: r.height };
};

window.tourSetSpotlight = (x, y, w, h) => {
    const s = document.getElementById('tour-spotlight');
    if (!s) return;
    s.style.left = x + 'px';
    s.style.top = y + 'px';
    s.style.width = w + 'px';
    s.style.height = h + 'px';
};

window.tourGetViewportWidth = () => window.innerWidth;
window.tourGetViewportHeight = () => window.innerHeight;