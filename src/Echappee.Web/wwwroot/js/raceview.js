// Dessin de la course sur un canvas, à 60 images par seconde. Le C# fournit la simulation (positions des équipes
// toutes les 0,5 s) ; ici on interpole, on place 5 coureurs par équipe en formation et on dessine.
// Aucune position n'est « étirée » par rapport au leader : un coureur ne peut donc jamais reculer.
(function () {
  if (typeof CanvasRenderingContext2D !== 'undefined' && !CanvasRenderingContext2D.prototype.roundRect) {
    CanvasRenderingContext2D.prototype.roundRect = function (x, y, w, h) { this.rect(x, y, w, h); };   // vieux navigateurs : coins droits
  }
  const TAU = Math.PI * 2;
  const W = 200, H = 150;
  const R = { cv: null, ctx: null, bg: null, scene: null, data: null, t: 0, speed: 1, state: 'idle', last: 0, raf: 0, ref: null, tickAt: 0, dpr: 1, trail: null };

  const smooth = x => { x = Math.max(0, Math.min(1, x)); return x * x * (3 - 2 * x); };
  const hash = (a, b) => { const x = Math.sin(a * 127.1 + b * 311.7 + 17.3) * 43758.5453; return x - Math.floor(x); };

  function sample(f) {
    const S = R.scene.samples, n = S.length / 2;
    f = f - Math.floor(f);
    const p = f * n, i = Math.floor(p) % n, k = p - Math.floor(p), j = (i + 1) % n;
    return { x: S[2 * i] + (S[2 * j] - S[2 * i]) * k, y: S[2 * i + 1] + (S[2 * j + 1] - S[2 * i + 1]) * k };
  }
  function place(frac, lane) {
    const a = sample(frac), b = sample(frac + 0.0025);
    const dx = b.x - a.x, dy = b.y - a.y, n = Math.hypot(dx, dy) || 1;
    return { x: a.x - dy / n * lane, y: a.y + dx / n * lane, ang: Math.atan2(dy, dx) };
  }

  // ---------- décor (dessiné une seule fois) ----------
  function buildBackground() {
    const c = document.createElement('canvas');
    const s = R.dpr * Math.max(1, R.cv.clientWidth / W) * 2.4;      // décor net même zoomé
    c.width = Math.round(W * s); c.height = Math.round(H * s);
    const g = c.getContext('2d'); g.scale(s, s);
    // herbe
    g.fillStyle = '#B9DD9E'; g.fillRect(0, 0, W, H);
    for (let i = 0; i < 90; i++) {            // bandes de tonte façon affiche
      g.fillStyle = i % 2 ? 'rgba(255,255,255,.07)' : 'rgba(20,90,60,.05)';
      g.fillRect(0, i * 1.7, W, 0.9);
    }
    const S = R.scene.samples, n = S.length / 2;
    const dist = (x, y) => { let m = 1e9; for (let i = 0; i < n; i += 2) m = Math.min(m, Math.hypot(S[2 * i] - x, S[2 * i + 1] - y)); return m; };
    // étang dans l'intérieur de la boucle
    g.fillStyle = '#8FD0E8'; g.strokeStyle = '#141A33'; g.lineWidth = .6;
    g.beginPath(); g.ellipse(112, 62, 17, 8, -0.15, 0, TAU); g.fill(); g.stroke();
    // arbres et rochers à l'écart de la route
    let seed = 7, placed = 0;
    for (let k = 0; k < 400 && placed < 34; k++) {
      const x = hash(k, 1) * W, y = hash(k, 2) * H;
      if (dist(x, y) < 12 || Math.hypot(x - 112, (y - 62) * 2) < 22) continue;
      placed++;
      if (hash(k, 3) < 0.18) { // rocher
        g.fillStyle = '#A9AFC4'; g.strokeStyle = '#141A33'; g.lineWidth = .5;
        g.beginPath(); g.ellipse(x, y, 3.2, 2.3, 0.3, 0, TAU); g.fill(); g.stroke();
      } else {
        const r = 2.6 + hash(k, 4) * 2.2;
        g.fillStyle = 'rgba(20,26,51,.18)'; g.beginPath(); g.ellipse(x + 1, y + 1.4, r, r * .8, 0, 0, TAU); g.fill();
        g.fillStyle = hash(k, 5) < .5 ? '#3FA36B' : '#2F8A5B'; g.strokeStyle = '#141A33'; g.lineWidth = .5;
        g.beginPath(); g.arc(x, y, r, 0, TAU); g.fill(); g.stroke();
        g.fillStyle = 'rgba(255,255,255,.22)'; g.beginPath(); g.arc(x - r * .3, y - r * .3, r * .38, 0, TAU); g.fill();
      }
    }
    // route : contour encre, sable, ligne centrale
    const path = new Path2D(); path.moveTo(S[0], S[1]);
    for (let i = 1; i < n; i++) path.lineTo(S[2 * i], S[2 * i + 1]); path.closePath();
    g.lineJoin = 'round'; g.lineCap = 'round';
    g.strokeStyle = '#141A33'; g.lineWidth = 15; g.stroke(path);
    g.strokeStyle = '#F4E8CB'; g.lineWidth = 12; g.stroke(path);
    // tronçons teintés (côte, descente, technique, sprint)
    const tint = { Climb: 'rgba(255,79,139,.55)', Descent: 'rgba(31,203,142,.55)', Technical: 'rgba(255,201,51,.7)', Sprint: 'rgba(60,91,255,.5)' };
    for (const sg of R.scene.segments) {
      if (!tint[sg.type]) continue;
      g.strokeStyle = tint[sg.type]; g.lineWidth = 12; g.beginPath();
      const a = Math.floor(sg.from * n), b = Math.ceil(sg.to * n);
      for (let i = a; i <= b; i++) { const q = sample(i / n); i === a ? g.moveTo(q.x, q.y) : g.lineTo(q.x, q.y); }
      g.stroke();
    }
    g.strokeStyle = 'rgba(20,26,51,.55)'; g.lineWidth = .55; g.setLineDash([2.2, 3.2]); g.stroke(path); g.setLineDash([]);
    // ligne d'arrivée à damier
    const f0 = place(0, 0); g.save(); g.translate(f0.x, f0.y); g.rotate(f0.ang);
    for (let i = 0; i < 6; i++) for (let j = 0; j < 2; j++) { g.fillStyle = (i + j) % 2 ? '#141A33' : '#fff'; g.fillRect(-1.2 + j * 1.2, -6 + i * 2, 1.2, 2); }
    g.restore();
    R.bg = c;
  }

  // ---------- coureurs ----------
  function riderSpec(team, k, j) {
    return { slot: 2.2 + j * 2.9 + hash(k, j) * 1.2, lane: (j % 2 ? 1 : -1) * (0.4 + hash(j, k + 3) * 0.9) * (j ? 1 : 0.2),
             amp: 1.2 + hash(k + 5, j) * 2.2, w: 0.5 + hash(k, j + 9) * 0.8, ph: hash(j, k) * TAU };
  }
  function prepare(data) {
    data.teams.forEach((T, k) => {
      T.lane = ((k % 6) - 2.5) * 1.55;
      T.grid = 8 + Math.floor(k / 2) * 9 + (k % 2) * 3;
      T.spec = T.riders.map((_, j) => riderSpec(T, k, j));
      T.attacks = T.attacks || [];
    });
    return data;
  }
  function teamFrac(k, t) {
    const F = R.data.frames, snap = R.data.snap;
    const i = Math.max(0, Math.min(F.length - 2, Math.floor(t / snap)));
    const kk = Math.max(0, Math.min(1, (t - i * snap) / snap));
    let p = F[i][k] + (F[i + 1][k] - F[i][k]) * kk;
    const fin = R.data.finish[k];
    if (t > fin) p = 1 + 0.08 * (1 - Math.exp(-(t - fin) / 2.4));       // après l'arrivée, on roule au ralenti
    return p;
  }
  function riderPos(k, j, t, tf) {
    const T = R.data.teams[k], sp = T.spec[j], U = 1 / R.data.meters;
    let off = -sp.slot * U + sp.amp * Math.sin(sp.w * t + sp.ph) * U;
    for (const a of T.attacks) if (a.rider === j) {
      const u = t - a.t;
      if (u > 0 && u < R.data.attackDur + 2.5) off += 15 * U * smooth(u / 1.6) * (1 - smooth((u - R.data.attackDur) / 2.5));
    }
    off -= T.grid * U * Math.max(0, 1 - t / 4);                          // départ en grille, qui se défait pendant 4 s
    return tf + off;
  }

  function drawRider(x, y, ang, T, me, scale) {
    const g = R.ctx;
    g.save(); g.translate(x, y); g.rotate(ang); g.scale(scale, scale);
    g.fillStyle = 'rgba(20,26,51,.28)'; g.beginPath(); g.ellipse(.3, .9, 2.4, 1, 0, 0, TAU); g.fill();
    g.fillStyle = '#141A33';
    g.beginPath(); g.roundRect(-3.1, -.45, 1.6, .9, .4); g.fill();
    g.beginPath(); g.roundRect(1.5, -.45, 1.6, .9, .4); g.fill();
    g.fillStyle = T.base; g.strokeStyle = '#141A33'; g.lineWidth = .4;
    g.beginPath(); g.ellipse(-.2, 0, 1.8, 1.1, 0, 0, TAU); g.fill();
    g.save(); g.clip(); g.fillStyle = T.accent; g.fillRect(-.5, -1.2, .7, 2.4); g.restore();
    g.beginPath(); g.ellipse(-.2, 0, 1.8, 1.1, 0, 0, TAU); g.stroke();
    g.fillStyle = me ? '#fff' : '#F4E8CB'; g.beginPath(); g.arc(1.0, 0, .62, 0, TAU); g.fill(); g.stroke();
    g.restore();
  }

  function frame(now) {
    R.raf = requestAnimationFrame(frame);
    if (!R.cv || !R.cv.isConnected) return;
    const dt = Math.min(0.1, (now - (R.last || now)) / 1000); R.last = now;
    if (R.state === 'playing') {
      R.t += dt * R.speed;
      if (R.t >= R.data.end) { R.t = R.data.end; R.state = 'done'; tick(true); }
      else if (now - R.tickAt > 100) { R.tickAt = now; tick(false); }
    }
    draw(dt);
  }
  function tick(done) { if (R.ref) R.ref.invokeMethodAsync('OnRaceTick', R.t, !!done); }

  const CAM = { x: 100, y: 75, z: 1.6, ready: false };
  function draw(dt) {
    const g = R.ctx, cv = R.cv;
    const w = Math.round(cv.clientWidth * R.dpr), h = Math.round(cv.clientHeight * R.dpr);
    if (cv.width !== w || cv.height !== h) { cv.width = w; cv.height = h; buildBackground(); }
    const d = R.data, items = [], row = [];
    for (let k = 0; k < d.teams.length; k++) {
      const tf = teamFrac(k, R.t), T = d.teams[k];
      for (let j = 0; j < T.riders.length; j++) {
        const f = riderPos(k, j, R.t, tf);
        const q = place(f, T.lane + T.spec[j].lane);
        items.push({ q, k, j, f }); row.push(f);
      }
    }
    // caméra : cadre tous les coureurs, zoom limité, mouvement amorti (jamais de saut)
    let x0 = 1e9, x1 = -1e9, y0 = 1e9, y1 = -1e9;
    for (const it of items) { x0 = Math.min(x0, it.q.x); x1 = Math.max(x1, it.q.x); y0 = Math.min(y0, it.q.y); y1 = Math.max(y1, it.q.y); }
    const tz = Math.max(1.5, Math.min(3.0, Math.min(W / (x1 - x0 + 46), H / (y1 - y0 + 40))));
    const tx = (x0 + x1) / 2, ty = (y0 + y1) / 2;
    const a = CAM.ready ? 1 - Math.exp(-Math.min(dt, 0.1) * 3.2) : 1; CAM.ready = true;
    CAM.x += (tx - CAM.x) * a; CAM.y += (ty - CAM.y) * a; CAM.z += (tz - CAM.z) * a;
    const hw = W / 2 / CAM.z, hh = H / 2 / CAM.z;
    const cx = Math.max(hw, Math.min(W - hw, CAM.x)), cy = Math.max(hh, Math.min(H - hh, CAM.y));
    const sc = cv.width / W * CAM.z;
    g.setTransform(1, 0, 0, 1, 0, 0);
    g.setTransform(sc, 0, 0, sc, -(cx - hw) * sc, -(cy - hh) * sc);
    g.drawImage(R.bg, 0, 0, W, H);
    items.sort((p, q) => p.q.y - q.q.y);
    for (const it of items) drawRider(it.q.x, it.q.y, it.q.ang, d.teams[it.k], it.k === 0, it.k === 0 ? 1.15 : 1);
    let best = null; for (const it of items) if (it.k === 0 && (!best || it.f > best.f)) best = it;
    if (best) {
      const fs = 4.2; g.font = '800 ' + fs + 'px Figtree, system-ui, sans-serif'; const tw = g.measureText(d.meLabel).width + 3.4;
      const lx = best.q.x - tw / 2, ly = best.q.y - 8.5;
      g.fillStyle = '#FFC933'; g.strokeStyle = '#141A33'; g.lineWidth = .55;
      g.beginPath(); g.roundRect(lx, ly, tw, fs + 1.8, 1.6); g.fill(); g.stroke();
      g.fillStyle = '#141A33'; g.textBaseline = 'middle'; g.fillText(d.meLabel, lx + 1.7, ly + (fs + 1.8) / 2 + .2);
    }
    if (R.trail) { R.trail.push(row); if (R.trail.length > 4000) R.trail.shift(); }
  }

  function setData(json) {
    const d = prepare(typeof json === 'string' ? JSON.parse(json) : json);
    R.data = d; R.t = d.startAt || 0; CAM.ready = false; R.state = d.frames.length > 2 && !d.idle ? 'playing' : 'idle'; R.tickAt = 0;
  }

  window.ech = window.ech || {};
  window.ech.race = {
    attach: function (canvas, sceneJson, ref, idleJson) {
      R.cv = canvas; R.ctx = canvas.getContext('2d'); R.ref = ref; R.dpr = Math.min(2, window.devicePixelRatio || 1);
      R.scene = JSON.parse(sceneJson); R.bg = null; R.last = 0;
      if (!R.data || idleJson) { if (idleJson) setData(idleJson); }
      cancelAnimationFrame(R.raf); R.raf = requestAnimationFrame(frame);
    },
    detach: function () { R.cv = null; R.ref = null; cancelAnimationFrame(R.raf); },
    play: function (json) { setData(json); },
    idle: function (json) { setData(json); R.state = 'idle'; },
    seek: function (t) { if (R.data) R.t = Math.min(t, R.data.end || t); },
    setSpeed: function (s) { R.speed = s; },
    skip: function () { if (R.data) { R.t = R.data.end; R.state = 'done'; tick(true); } },
    // pour les tests : trace des positions (tours cumulés) de chaque coureur à chaque image
    startTrace: function () { R.trail = []; },
    trace: function () { return R.trail; }
  };
})();
