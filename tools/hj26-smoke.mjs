import { chromium, webkit } from 'playwright';
import fs from 'fs';

const url = process.argv[2] || 'https://daltonvstout.com/games/paisho/?v=hj26-0111';
const outDir =
  process.argv[3] ||
  '/Users/dalton/Library/Application Support/Cursor/AgentStores/cursor_agent_stores/bc-076ca061-9bd3-41cd-819e-6f7004622d46/files/media/hj26';
fs.mkdirSync(outDir, { recursive: true });

async function smoke(browserType, name) {
  const browser = await browserType.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 800 } });
  const errors = [];
  const range = [];
  page.on('pageerror', (e) => {
    const msg = String(e && e.message ? e.message : e);
    errors.push(msg);
    if (/RangeError|invoke_iii|Maximum call stack/i.test(msg)) range.push(msg);
  });
  page.on('console', (msg) => {
    if (msg.type() !== 'error') return;
    const t = msg.text();
    if (/RangeError|invoke_iii|Maximum call stack/i.test(t)) range.push(t);
  });

  await page.goto(url, { waitUntil: 'domcontentloaded', timeout: 120000 });
  await page.waitForSelector('#unity-canvas', { timeout: 120000 });
  await page.waitForTimeout(50000);
  const titleShot = `${outDir}/${name}-title.png`;
  await page.screenshot({ path: titleShot, fullPage: true });

  const box = await page.locator('#unity-canvas').boundingBox();
  if (box) {
    // TitleMenu: "New Game vs Computer"
    await page.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.48);
  }
  await page.waitForTimeout(55000);
  const playShot = `${outDir}/${name}-play.png`;
  await page.screenshot({ path: playShot, fullPage: true });

  if (box) {
    await page.mouse.click(box.x + box.width * 0.5, box.y + box.height * 0.44);
    await page.waitForTimeout(20000);
    await page.screenshot({ path: `${outDir}/${name}-play2.png`, fullPage: true });
  }

  const productVersion = await page.evaluate(() => {
    const scripts = Array.from(document.scripts).map((s) => s.textContent || '').join('\n');
    const m = scripts.match(/productVersion:\s*"([^"]+)"/);
    return m ? m[1] : null;
  });

  const result = {
    name,
    productVersion,
    errorCount: errors.length,
    rangeErrorCount: range.length,
    range,
    errors: errors.slice(0, 12),
    titleShot,
    playShot,
  };
  fs.writeFileSync(`${outDir}/${name}-smoke.json`, JSON.stringify(result, null, 2));
  await browser.close();
  return result;
}

const results = [await smoke(webkit, 'webkit'), await smoke(chromium, 'chromium')];
console.log(JSON.stringify(results, null, 2));
process.exit(results.some((r) => r.rangeErrorCount > 0) ? 2 : 0);
