// Drives the layout editor in a real browser and reports canvas render timings.
//
//   node tools/canvas-bench.mjs [--url http://localhost:5110] [--world <dir>] [--frames 120]
//
// Loads the page, optionally imports a world through the file input, then pans the
// canvas for the requested number of frames and prints the profiler's report.
// Playwright is expected from a global install.

import { createRequire } from 'node:module';
import { readdirSync } from 'node:fs';
import { join } from 'node:path';
import { execSync } from 'node:child_process';

function resolvePlaywright() {
  const require = createRequire(import.meta.url);
  const roots = [];
  try {
    roots.push(execSync('npm root -g', { encoding: 'utf8' }).trim());
  } catch {
    // fall through to the local resolution attempt
  }
  for (const root of roots) {
    try {
      return require(join(root, 'playwright'));
    } catch {
      // try the next root
    }
  }
  try {
    return require('playwright');
  } catch {
    throw new Error('playwright not found - install it globally: npm i -g playwright');
  }
}

function parseArgs(argv) {
  const options = {
    url: 'http://localhost:5110', world: null, frames: 120, zoom: 0, headed: false,
  };
  for (let index = 0; index < argv.length; index += 1) {
    const flag = argv[index];
    if (flag === '--url') options.url = argv[++index];
    else if (flag === '--world') options.world = argv[++index];
    else if (flag === '--frames') options.frames = Number(argv[++index]);
    else if (flag === '--zoom') options.zoom = Number(argv[++index]);
    else if (flag === '--headed') options.headed = true;
  }
  return options;
}

const options = parseArgs(process.argv.slice(2));
const { chromium } = resolvePlaywright();

const browser = await chromium.launch({
  headless: !options.headed,
  args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'],
});
const page = await browser.newPage({ viewport: { width: 1600, height: 900 } });

const consoleErrors = [];
page.on('console', message => {
  if (message.type() === 'error') consoleErrors.push(message.text());
});
page.on('pageerror', error => consoleErrors.push(`pageerror: ${error.message}`));

await page.goto(options.url, { waitUntil: 'domcontentloaded' });

// Blazor replaces the loading markup with the app once the runtime is up.
await page.waitForSelector('#canvasContainer canvas', { timeout: 120000 });
await page.waitForFunction(() => {
  const canvas = document.querySelector('#canvasContainer canvas');
  return canvas && canvas.width > 0 && canvas.height > 0;
}, { timeout: 60000 });

console.log('app loaded, canvas present');

if (options.world) {
  const files = readdirSync(options.world)
    .filter(name => name.endsWith('.mca') || name === 'level.dat' || name === 'map.xml')
    .map(name => join(options.world, name));

  console.log(`importing ${files.length} file(s) from ${options.world}`);
  const importStart = Date.now();
  await page.setInputFiles('input[type=file]', files);
  // The import runs on the UI thread; poll until blocks have landed in the map.
  // waitForFunction cannot be used here: an async predicate returns a Promise,
  // which is always truthy, so it would resolve on the first poll.
  let blockCount = 0;
  for (let attempt = 0; attempt < 600 && blockCount === 0; attempt += 1) {
    blockCount = await page.evaluate(async () => await window.readBlockCount());
    if (blockCount === 0) await page.waitForTimeout(500);
  }
  const importMs = Date.now() - importStart;
  console.log(`imported ${blockCount} blocks in ${importMs} ms`);
}

async function resetProfile() {
  await page.evaluate(() => window.resetRenderProfile && window.resetRenderProfile());
}

async function readProfile() {
  return page.evaluate(() => (window.readRenderProfile ? window.readRenderProfile() : null));
}

async function panFrames(frameCount) {
  const box = await page.locator('#canvasContainer').boundingBox();
  const centerX = box.x + box.width / 2;
  const centerY = box.y + box.height / 2;

  await page.mouse.move(centerX, centerY);
  await page.mouse.down({ button: 'middle' });

  for (let frame = 0; frame < frameCount; frame += 1) {
    const offset = (frame % 20) - 10;
    await page.mouse.move(centerX + offset, centerY + offset);
  }

  await page.mouse.up({ button: 'middle' });
}

if (options.zoom > 0) {
  const box = await page.locator('#canvasContainer').boundingBox();
  await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
  for (let step = 0; step < options.zoom; step += 1) {
    await page.mouse.wheel(0, -120);
    await page.waitForTimeout(120);
  }
  console.log(`zoomed in ${options.zoom} step(s)`);
}

console.log(`panning for ${options.frames} frames...`);
await resetProfile();
const wallStart = Date.now();
await panFrames(options.frames);
await page.waitForTimeout(500);
const wallMs = Date.now() - wallStart;

const profile = await readProfile();
console.log('\n=== render profile ===');
console.log(JSON.stringify(profile, null, 2));
console.log(`wall clock for the pan: ${wallMs} ms`);

if (consoleErrors.length) {
  console.log('\n=== console errors ===');
  for (const error of consoleErrors.slice(0, 20)) console.log(error);
}

await page.screenshot({ path: options.screenshot ?? '/tmp/canvas-bench.png' });
await browser.close();
