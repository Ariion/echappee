// Petit pont JS : sauvegarde locale, partage, copie. Tout est protégé : le stockage peut être refusé (navigation privée).
window.ech = {
  load: function (k) { try { return localStorage.getItem(k); } catch (e) { return null; } },
  save: function (k, v) { try { localStorage.setItem(k, v); return true; } catch (e) { return false; } },
  remove: function (k) { try { localStorage.removeItem(k); } catch (e) { } },
  now: function () { return Math.floor(Date.now() / 1000); },
  region: function () { try { return (navigator.language || '').split('-')[1] || ''; } catch (e) { return ''; } },
  lang: function () { try { return (navigator.language || 'fr').split('-')[0]; } catch (e) { return 'fr'; } },
  vibrate: function (ms) { try { navigator.vibrate && navigator.vibrate(ms); } catch (e) { } },
  svgToPng: function (svg, w, h) {
    return new Promise(function (res) {
      var img = new Image(), url = 'data:image/svg+xml;charset=utf-8,' + encodeURIComponent(svg);
      img.onload = function () { var c = document.createElement('canvas'); c.width = w; c.height = h; c.getContext('2d').drawImage(img, 0, 0, w, h); res(c.toDataURL('image/png')); };
      img.onerror = function () { res(null); }; img.src = url;
    });
  },
  sharePoster: async function (svg, text) {
    var data = await window.ech.svgToPng(svg, 800, 1000); if (!data) return 'error';
    var blob = await (await fetch(data)).blob(), file = new File([blob], 'affiche-echappee.png', { type: 'image/png' });
    if (navigator.canShare && navigator.canShare({ files: [file] })) { try { await navigator.share({ files: [file], text: text }); return 'shared'; } catch (e) { return 'cancel'; } }
    var a = document.createElement('a'); a.href = data; a.download = 'affiche-echappee.png'; document.body.appendChild(a); a.click(); a.remove(); return 'downloaded';
  },
  download: function (name, text) {
    var a = document.createElement('a'); a.href = URL.createObjectURL(new Blob([text], { type: 'application/json' })); a.download = name; document.body.appendChild(a); a.click(); a.remove();
  }
};
