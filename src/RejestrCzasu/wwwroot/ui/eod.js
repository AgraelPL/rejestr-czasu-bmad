// MOJE EOD – wzorzec UI: funkcje pomocnicze dla komponentów Eod* (wywoływane przez IJSRuntime jako „EodUI.*”).
(function () {
  'use strict';

  // Klucz i wartości skali muszą być zgodne ze skryptem skali w EodHead.razor (ustawia skalę przed pierwszym renderowaniem)
  const SCALE_KEY = 'eod.scale';
  const THEME_KEY = 'eod.theme';
  const SCALES = ['90', '100', '110', '125'];
  const content = () => document.querySelector('.fluent-layout-item[area="content"]');
  const read = (key, fallback) => { try { return localStorage.getItem(key) || fallback; } catch { return fallback; } };
  const write = (key, value) => { try { localStorage.setItem(key, value); } catch { /* ustawienie tylko na czas sesji */ } };

  // Przyklejona sekcja główna formularza sprawy: jej wysokość trafia do zmiennej --eod-casebar-h,
  // żeby spis sekcji i przewijanie do sekcji nie chowały się pod nią.
  let observer = null;
  function observeMainSection(element) {
    observer?.disconnect();
    if (!element || !window.ResizeObserver) return;
    const update = () => {
      const sticky = getComputedStyle(element).position === 'sticky';
      content()?.style.setProperty('--eod-casebar-h', (sticky ? element.offsetHeight : 0) + 'px');
    };
    observer = new ResizeObserver(update);
    observer.observe(element);
    update();
  }

  // ── Komentarze: lista osób pod znakiem @ (EodComments) ─────────────────────────────────────────
  // Porównanie bez wielkości liter i polskich znaków: „gor” znajduje „Górski”.
  const normalize = (s) => s.toLowerCase().normalize('NFD').replace(/[̀-ͯ]/g, '').replace(/ł/g, 'l');
  const escapeHtml = (s) => String(s).replace(/[&<>"]/g, (c) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c]));

  // Fragment „@tekst” tuż przed kursorem (imię i nazwisko = najwyżej jedna spacja)
  function mentionToken(field) {
    const beforeCursor = field.value.slice(0, field.selectionStart);
    const match = /(^|[\s(])@([^\s@]*(?: [^\s@]*)?)$/.exec(beforeCursor);
    return match ? { start: beforeCursor.length - match[2].length - 1, phrase: match[2] } : null;
  }

  // Pozycja znaku w polu tekstowym (element lustrzany o tych samych stylach) – lista otwiera się pod znakiem @
  function caretPosition(field, index) {
    const mirror = document.createElement('div');
    const computed = getComputedStyle(field);
    ['fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'letterSpacing', 'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
      'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth', 'boxSizing', 'tabSize'].forEach((prop) => { mirror.style[prop] = computed[prop]; });
    Object.assign(mirror.style, { position: 'absolute', top: '0', left: '0', visibility: 'hidden', whiteSpace: 'pre-wrap', overflowWrap: 'break-word', width: field.offsetWidth + 'px', borderStyle: 'solid' });
    mirror.textContent = field.value.slice(0, index);
    const marker = document.createElement('span');
    marker.textContent = '@';
    mirror.appendChild(marker);
    field.parentElement.appendChild(mirror);
    const position = { x: marker.offsetLeft, y: marker.offsetTop + marker.offsetHeight - field.scrollTop };
    mirror.remove();
    return position;
  }

  let anchorCount = 0;
  function show(list, anchor) {
    if (!anchor.style.anchorName) anchor.style.anchorName = `--eod-anchor-${++anchorCount}`;
    list.style.positionAnchor = anchor.style.anchorName;
    if (!list.matches(':popover-open')) list.showPopover();
  }
  const hide = (list) => { if (list.matches(':popover-open')) list.hidePopover(); };
  const notifyInput = (field) => field.dispatchEvent(new Event('input', { bubbles: true }));

  // people: [{ name, jobTitle }] – osoby do oznaczenia (EodComments); dotnet: referencja komponentu (AddFromKeyboard)
  const comments = {
    init(field, list, anchor, buttonId, people, dotnet) {
      if (!field || field._eod) return;
      const state = { index: 0, dismissed: -1, timer: 0 };
      field._eod = state;

      const suggest = () => {
        const token = mentionToken(field);
        if (!token) state.dismissed = -1;
        const query = token ? normalize(token.phrase) : '';
        const matches = token && token.start !== state.dismissed
          ? people.filter((person) => { const name = normalize(person.name); return name !== query && (name.startsWith(query) || (!query.includes(' ') && name.split(' ').some((part) => part.startsWith(query)))); })
          : [];
        if (!matches.length) { hide(list); field.removeAttribute('aria-activedescendant'); return; }
        state.index = Math.min(state.index, matches.length - 1);
        list.innerHTML = matches.map((person, i) => `<button type="button" class="eod-ac-option" role="option" id="${list.id}-${i}" aria-selected="${i === state.index}" data-value="${escapeHtml(person.name)}"><fluent-avatar name="${escapeHtml(person.name)}" size="24" color="colorful"></fluent-avatar><span><span class="eod-block">${escapeHtml(person.name)}</span><span class="eod-caption">${escapeHtml(person.jobTitle)}</span></span></button>`).join('');
        field.setAttribute('aria-activedescendant', `${list.id}-${state.index}`);
        const position = caretPosition(field, token.start);
        anchor.style.left = Math.max(0, Math.min(position.x, field.offsetWidth - 280)) + 'px'; // lista (280 px) nie wychodzi poza pole
        anchor.style.top = Math.min(position.y, field.offsetHeight) + 'px';
        show(list, anchor);
        list.querySelector('[aria-selected="true"]')?.scrollIntoView({ block: 'nearest' });
      };

      const select = (name) => {
        const token = mentionToken(field);
        if (!token) return;
        const before = field.value.slice(0, token.start);
        const after = field.value.slice(field.selectionStart).replace(/^ /, '');
        field.value = `${before}@${name} ${after}`;
        field.selectionStart = field.selectionEnd = before.length + name.length + 2;
        state.index = 0;
        hide(list);
        notifyInput(field);
      };

      field.addEventListener('input', (e) => { if (e.isTrusted) { state.index = 0; suggest(); } });
      field.addEventListener('click', suggest);
      field.addEventListener('keyup', (e) => { if (['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(e.key)) suggest(); });
      field.addEventListener('keydown', (e) => {
        if (list.matches(':popover-open')) {
          const count = list.children.length;
          if (e.key === 'ArrowDown' || e.key === 'ArrowUp') { e.preventDefault(); state.index = (state.index + (e.key === 'ArrowDown' ? 1 : count - 1)) % count; suggest(); return; }
          if (e.key === 'Enter' || e.key === 'Tab') { e.preventDefault(); select(list.children[state.index].dataset.value); return; }
          if (e.key === 'Escape') { e.preventDefault(); state.dismissed = mentionToken(field)?.start ?? -1; hide(list); return; }
        }
        if (e.key === 'Enter' && (e.ctrlKey || e.metaKey)) { e.preventDefault(); dotnet.invokeMethodAsync('AddFromKeyboard'); }
      });
      // Lista zamyka się po wyjściu z pola; klik w listę lub w „Oznacz osobę” nie zabiera polu fokusu
      field.addEventListener('blur', () => { state.timer = setTimeout(() => hide(list), 150); });
      field.addEventListener('focus', () => clearTimeout(state.timer));
      list.addEventListener('mousedown', (e) => e.preventDefault());
      list.addEventListener('click', (e) => { const option = e.target.closest('.eod-ac-option'); if (option) select(option.dataset.value); });

      // „Oznacz osobę”: wstawia @ w miejscu kursora i otwiera listę (dla osób, które nie znają skrótu)
      const button = document.getElementById(buttonId);
      if (button) {
        button.addEventListener('mousedown', (e) => e.preventDefault());
        button.addEventListener('click', () => {
          const at = field.selectionStart ?? field.value.length;
          const before = field.value.slice(0, at);
          const insert = (before && !/\s$/.test(before) ? ' ' : '') + '@';
          field.value = before + insert + field.value.slice(field.selectionEnd ?? at);
          field.focus();
          field.selectionStart = field.selectionEnd = at + insert.length;
          state.index = 0;
          state.dismissed = -1;
          notifyInput(field);
          suggest();
        });
      }
    },
    clear(field) { if (!field) return; field.value = ''; notifyInput(field); },
    reply(field, name) {
      if (!field) return;
      field.value = `${field.value.replace(/\s+$/, '')}${field.value.trim() ? ' ' : ''}@${name} `;
      field.focus();
      field.selectionStart = field.selectionEnd = field.value.length;
      field.scrollIntoView({ block: 'center', behavior: 'smooth' });
      notifyInput(field);
    },
    dispose(field) { if (field) delete field._eod; },
  };

  window.EodUI = {
    comments,

    // ── Skala interfejsu (CSS zoom na <body>, atrybut data-scale na <html>) ──
    getScale: () => read(SCALE_KEY, 'auto'),
    setScale(scale) {
      const root = document.documentElement;
      if (SCALES.includes(scale)) root.setAttribute('data-scale', scale); else root.removeAttribute('data-scale');
      write(SCALE_KEY, scale);
    },
    // Kształt zgodny z rekordem C# AppearanceSettings.BrowserWindow(Width, Height, Scale, Manual)
    describeWindow() {
      const root = document.documentElement;
      const zoom = parseFloat(getComputedStyle(root).getPropertyValue('--eod-zoom')) || 1;
      return { width: innerWidth, height: innerHeight, scale: Math.round(zoom * 100), manual: root.hasAttribute('data-scale') };
    },

    // ── Motyw (stosuje go IThemeService biblioteki; tu tylko pamiętamy wybór użytkownika) ──
    getTheme: () => read(THEME_KEY, 'system'),
    saveTheme: (theme) => write(THEME_KEY, theme),

    // ── Przewijanie i fokus ──
    scrollToTop() { content()?.scrollTo({ top: 0, behavior: 'instant' }); },
    scrollToElement(selector, block) { document.querySelector(selector)?.scrollIntoView({ block: block || 'start' }); },
    focusElement(selector) { const element = document.querySelector(selector); element?.focus?.(); },
    observeMainSection,

    // ── Pobranie pliku wygenerowanego po stronie serwera (eksport CSV: UTF-8 z BOM, otwiera się w Excelu) ──
    downloadFile(fileName, fileContent, type) {
      const blob = new Blob(['﻿' + fileContent], { type: type || 'text/csv;charset=utf-8' });
      const link = document.createElement('a');
      link.href = URL.createObjectURL(blob);
      link.download = fileName;
      document.body.appendChild(link);
      link.click();
      link.remove();
      setTimeout(() => URL.revokeObjectURL(link.href), 2000);
    },
  };
})();
