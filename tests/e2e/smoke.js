// Test de bout en bout dans un vrai navigateur (Chromium sans interface).
//   1) dotnet publish src/Echappee.Web -c Release -o /tmp/site
//   2) (cd /tmp/site/wwwroot && python3 -m http.server 8099 &)
//   3) npm i playwright && node tests/e2e/smoke.js [url]
// Échoue (code 1) à la moindre erreur JavaScript ou requête cassée.
const { chromium } = require('playwright');
const URL = process.argv[2] || 'http://localhost:8099/';
const exe = process.env.CHROMIUM_PATH || undefined;

(async () => {
  const browser = await chromium.launch(exe ? { executablePath: exe } : {});
  const page = await (await browser.newContext({ viewport: { width: 400, height: 800 }, locale: 'fr-FR' })).newPage();
  const errs = [];
  page.on('pageerror', e => errs.push('PAGEERR ' + e.message));
  page.on('requestfailed', r => { if (!/fonts\.g/.test(r.url())) errs.push('REQFAIL ' + r.url()); });
  const must = async (cond, msg) => { if (!cond) { errs.push('ASSERT ' + msg); } };

  await page.goto(URL);
  await page.waitForSelector('.app', { timeout: 90000 });
  await page.click('text=Réclamer');                                    // bonus quotidien
  await page.click('.infra .card >> nth=0 >> button');                  // première amélioration (capital de départ)
  await must(await page.locator('.infra .card >> nth=0 >> .lv').innerText().then(t => /1/.test(t)), 'niveau 1 après achat');

  await page.click('text=Lancer la course');
  await page.click('text=Passer');
  await page.waitForSelector('.result', { timeout: 30000 });
  await must((await page.locator('.rank').innerText()).includes('/ 12'), 'classement affiché');

  for (const label of ['Équipe', 'Plan de course', 'Ligues', 'Boutique']) {
    await page.click(`.nav >> text=${label}`); await page.waitForTimeout(200);
  }
  await page.click('.nav >> text=Équipe');
  await page.click('text=Pack gratuit'); await page.waitForSelector('.pulls');
  await must(await page.locator('.pull').count() === 1, 'un tirage gratuit');
  await page.click('.sheet >> text=OK');

  // sauvegarde : recharger conserve la progression
  await page.waitForTimeout(500);
  await page.reload(); await page.waitForSelector('.app', { timeout: 90000 });
  const prog = await page.evaluate(() => JSON.parse(localStorage.getItem('echappee.save.v1')).State.RacesPlayed);
  await must(prog === 1, 'sauvegarde : 1 course jouée');

  await browser.close();
  if (errs.length) { console.error(errs.join('\n')); process.exit(1); }
  console.log('OK : parcours complet sans erreur');
})();
