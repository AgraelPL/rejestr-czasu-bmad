// Rejestr czasu (kopia wzorca eod-ui): funkcje pomocnicze dla komponentów Eod* (wywoływane przez IJSRuntime jako „EodUI.*”).
(function () {
  'use strict';

  // Klucz i wartości skali muszą być zgodne ze skryptem skali ui/eod-scale.js (ładuje go EodHead; ustawia skalę przed pierwszym renderowaniem)
  const SCALE_KEY = 'eod.scale';
  const THEME_KEY = 'eod.theme';
  const SCALES = ['90', '100', '110', '125'];
  const content = () => document.querySelector('.fluent-layout-item[area="content"]');
  const read = (key, fallback) => { try { return localStorage.getItem(key) || fallback; } catch { return fallback; } };
  const write = (key, value) => { try { localStorage.setItem(key, value); } catch { /* ustawienie tylko na czas sesji */ } };

  // Rejestr (AD-23 poz. 9): Fluent UI 5.0.1 wysyła tekst wartości comboboxa ustawionej przy pierwszym renderze tylko raz,
  // zaraz po nim, a komponent sieciowy tworzy swoje pole tekstowe asynchronicznie – tekst ginie i pole wygląda na puste,
  // choć wartość jest wybrana. Funkcja pokazuje tekst, gdy pole powstanie (najwyżej 30 klatek; potem nic nie robi).
  function showSelectedText(dropdown, text) {
    const show = (attempt) => {
      const control = dropdown?.control;
      if (control) {
        control.value = text;
      } else if (attempt < 30) {
        requestAnimationFrame(() => show(attempt + 1));
      }
    };
    show(0);
  }

  window.EodUI = {
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

    // ── Combobox (FluentCombobox / FluentSelect): tekst wartości ustawionej przy pierwszym renderze ──
    showSelectedText,

    // ── Pobranie pliku wygenerowanego po stronie serwera (eksport raportu: strumień obwodu → Blob, bez BOM) ──
    async downloadStream(fileName, streamRef, type) {
      const buffer = await streamRef.arrayBuffer();
      const blob = new Blob([buffer], { type: type || 'application/octet-stream' });
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
