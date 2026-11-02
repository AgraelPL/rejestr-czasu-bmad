// Rejestr czasu (kopia wzorca eod-ui): skala interfejsu zapisana przez użytkownika, ustawiana przed pierwszym renderowaniem
// (bez „skoku” rozmiaru). Plik zamiast skryptu inline w EodHead, bo CSP ma script-src 'self' (AD-12, AD-23 poz. 8).
// Klucz i wartości muszą być zgodne z SCALE_KEY / SCALES w eod.js (atrybut data-scale na <html>).
(function () {
  'use strict';
  try {
    var scale = localStorage.getItem('eod.scale');
    if (['90', '100', '110', '125'].indexOf(scale) >= 0) {
      document.documentElement.setAttribute('data-scale', scale);
    }
  } catch (e) {
    // localStorage niedostępny – skala automatyczna
  }
})();
