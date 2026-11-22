#!/usr/bin/env node
/**
 * Portfolio WebGL asset budget for Resources/*.glb
 *
 * glTFast embeds textures as uncompressed Texture2D sub-assets (no TextureImporter
 * max-size). This script keeps Resource paths + .meta GUIDs.
 *
 * Board.glb: texture resize only by default (mesh simplify historically broke
 * GateHome/GateForeign pitch → CellSpacing drift). Optional --strip-gardens
 * removes Garden* nodes (decor only; gates untouched).
 *
 * PlayerStand.glb / tiles: index unindexed TRIANGLES, weld, simplify.
 *
 * Requires Node 18+. From repo root:
 *   node tools/shrink-webgl-glbs.mjs
 *   node tools/shrink-webgl-glbs.mjs --board-stand-only --strip-gardens
 */
import { execSync } from 'child_process';
import { readdirSync, statSync, unlinkSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');
const boardStandOnly = process.argv.includes('--board-stand-only');
const stripGardens = process.argv.includes('--strip-gardens');

function ensureDeps() {
  const dir = join(root, 'tools', 'glbopt-deps');
  execSync(
    `mkdir -p "${dir}" && cd "${dir}" && npm init -y >/dev/null 2>&1 && npm install --silent @gltf-transform/core@4 @gltf-transform/functions@4 @gltf-transform/extensions@4 meshoptimizer`,
    { stdio: 'inherit', shell: true },
  );
  return dir;
}

function positionWeld(prim, decimals = 5) {
  const pos = prim.getAttribute('POSITION');
  const uv = prim.getAttribute('TEXCOORD_0');
  const idx = prim.getIndices();
  if (!pos || !idx) return;
  const posA = pos.getArray();
  const uvA = uv ? uv.getArray() : null;
  const idxA = idx.getArray();
  const map = new Map();
  const newPos = [];
  const newUv = uvA ? [] : null;
  const remap = new Int32Array(pos.getCount());
  const fmt = (n) => n.toFixed(decimals);
  for (let i = 0; i < pos.getCount(); i++) {
    const k = `${fmt(posA[i * 3])}|${fmt(posA[i * 3 + 1])}|${fmt(posA[i * 3 + 2])}`;
    if (!map.has(k)) {
      map.set(k, newPos.length / 3);
      newPos.push(posA[i * 3], posA[i * 3 + 1], posA[i * 3 + 2]);
      if (newUv) newUv.push(uvA[i * 2], uvA[i * 2 + 1]);
    }
    remap[i] = map.get(k);
  }
  const vcount = newPos.length / 3;
  const newIdx =
    vcount < 65536 ? new Uint16Array(idx.getCount()) : new Uint32Array(idx.getCount());
  for (let i = 0; i < idx.getCount(); i++) newIdx[i] = remap[idxA[i]];
  pos.setArray(new Float32Array(newPos));
  if (uv && newUv) uv.setArray(new Float32Array(newUv));
  prim.setAttribute('NORMAL', null);
  prim.setAttribute('TANGENT', null);
  idx.setArray(newIdx);
}

function ensureIndexedTriangles(doc, prim) {
  if (prim.getIndices()) return false;
  const pos = prim.getAttribute('POSITION');
  if (!pos || prim.getMode() !== 4) return false;
  const n = pos.getCount();
  if (n % 3 !== 0) {
    console.warn(`skip ensureIndexed: vert count ${n} not divisible by 3`);
    return false;
  }
  const arr = n < 65536 ? new Uint16Array(n) : new Uint32Array(n);
  for (let i = 0; i < n; i++) arr[i] = i;
  prim.setIndices(doc.createAccessor().setType('SCALAR').setArray(arr));
  return true;
}

async function loadLibs(depDir) {
  const { NodeIO } = await import(join(depDir, 'node_modules/@gltf-transform/core/dist/index.js'));
  const { ALL_EXTENSIONS } = await import(
    join(depDir, 'node_modules/@gltf-transform/extensions/dist/index.js')
  );
  const fns = await import(join(depDir, 'node_modules/@gltf-transform/functions/dist/index.js'));
  const { MeshoptSimplifier } = await import(
    join(depDir, 'node_modules/meshoptimizer/meshopt_simplifier.js')
  );
  await MeshoptSimplifier.ready;
  return { NodeIO, ALL_EXTENSIONS, fns, MeshoptSimplifier };
}

function stripGardenNodes(doc) {
  const remove = [];
  for (const node of doc.getRoot().listNodes()) {
    const name = node.getName() || '';
    if (/^Garden/i.test(name)) remove.push(node);
  }
  for (const node of remove) node.dispose();
  console.log(`stripped ${remove.length} Garden* nodes`);
}

async function meshSimplify(doc, { fns, MeshoptSimplifier }, ratio, error) {
  for (const mesh of doc.getRoot().listMeshes()) {
    for (const prim of mesh.listPrimitives()) {
      if (ensureIndexedTriangles(doc, prim)) {
        console.log('indexed previously-unindexed TRIANGLES primitive');
      }
      // Coarser weld (3 dp) merges split verts on unindexed exports; 5 dp barely helps.
      positionWeld(prim, 3);
    }
  }

  await doc.transform(
    fns.dedup(),
    fns.weld({ overwrite: true }),
    fns.simplify({ simplifier: MeshoptSimplifier, ratio, error, lockBorder: false }),
    // Do NOT run normals({ overwrite: true }) after simplify — on gltf-transform 4.5
    // it de-indexes TRIANGLES meshes (observed on PlayerStand).
    fns.prune(),
  );
}

async function resizeGlb(path, texMax) {
  const tmp = path + '.tmp.glb';
  execSync(
    `npx --yes @gltf-transform/cli@4 resize "${path}" "${tmp}" --width ${texMax} --height ${texMax}`,
    { stdio: 'pipe' },
  );
  return tmp;
}

async function processFile(rel, texMax, kind) {
  const path = join(root, rel);
  const tmp = await resizeGlb(path, texMax);
  const depDir = ensureDeps();
  const libs = await loadLibs(depDir);
  const io = new libs.NodeIO().registerExtensions(libs.ALL_EXTENSIONS);
  const doc = await io.read(tmp);

  if (kind === 'board') {
    if (stripGardens) stripGardenNodes(doc);
    await doc.transform(libs.fns.dedup(), libs.fns.prune());
  } else if (kind === 'stand') {
    await meshSimplify(doc, libs, 0.08, 0.04);
  } else {
    await meshSimplify(doc, libs, 0.1, 0.025);
  }

  await io.write(path, doc);
  try {
    unlinkSync(tmp);
  } catch {}
  console.log(rel, `${(statSync(path).size / 1e6).toFixed(2)} MB`);
}

async function main() {
  ensureDeps();

  // Board: texture-only (+ optional Garden* strip). Never mesh-simplify gates.
  await processFile('Assets/Resources/Board/Board.glb', 1024, 'board');

  // PlayerStand: index → weld → simplify (was 57k unindexed TRIANGLES).
  await processFile('Assets/Resources/Board/PlayerStand.glb', 512, 'stand');

  if (boardStandOnly) {
    console.log('--board-stand-only: skipping PieceVisuals');
    return;
  }

  for (const f of readdirSync(join(root, 'Assets/Resources/PieceVisuals')).filter((x) =>
    x.endsWith('.glb'),
  )) {
    await processFile(`Assets/Resources/PieceVisuals/${f}`, 512, 'tile');
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
