#!/usr/bin/env node
/**
 * Portfolio WebGL asset budget for Resources/*.glb
 *
 * glTFast embeds textures as uncompressed Texture2D sub-assets (no TextureImporter
 * max-size). This script keeps Resource paths + .meta GUIDs, and:
 *   1) resizes embedded images (Board 1024, tiles/stand 512)
 *   2) position-welds split verts, simplifies, recomputes normals
 *
 * Requires Node 18+. From repo root:
 *   node tools/shrink-webgl-glbs.mjs
 */
import { execSync } from 'child_process';
import { readdirSync, statSync, unlinkSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const root = join(__dirname, '..');

function ensureDeps() {
  const dir = join(root, 'tools', '.glbopt-deps');
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

async function main() {
  const depDir = ensureDeps();
  const { NodeIO } = await import(join(depDir, 'node_modules/@gltf-transform/core/dist/index.modern.js'));
  const { ALL_EXTENSIONS } = await import(
    join(depDir, 'node_modules/@gltf-transform/extensions/dist/index.modern.js')
  );
  const { weld, simplify, dedup, prune, normals } = await import(
    join(depDir, 'node_modules/@gltf-transform/functions/dist/index.modern.js')
  );
  const { MeshoptSimplifier } = await import(
    join(depDir, 'node_modules/meshoptimizer/meshopt_simplifier.module.js')
  );
  await MeshoptSimplifier.ready;

  // Board.glb: texture resize only. Mesh simplify/weld broke gate-pitch measurement
  // so CellSpacing no longer matched the board (tiles/points drifted).
  const textureOnly = [['Assets/Resources/Board/Board.glb', 2048]];
  const meshJobs = [
    ['Assets/Resources/Board/PlayerStand.glb', 512, 0.08, 0.04],
    ...readdirSync(join(root, 'Assets/Resources/PieceVisuals'))
      .filter((f) => f.endsWith('.glb'))
      .map((f) => [`Assets/Resources/PieceVisuals/${f}`, 512, 0.1, 0.025]),
  ];

  for (const [rel, texMax] of textureOnly) {
    const path = join(root, rel);
    const tmp = path + '.tmp.glb';
    execSync(
      `npx --yes @gltf-transform/cli@4 resize "${path}" "${tmp}" --width ${texMax} --height ${texMax}`,
      { stdio: 'pipe' },
    );
    const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
    const doc = await io.read(tmp);
    await doc.transform(dedup(), prune());
    await io.write(path, doc);
    try {
      unlinkSync(tmp);
    } catch {}
    console.log(rel, `(tex-only) ${(statSync(path).size / 1e6).toFixed(2)} MB`);
  }

  for (const [rel, texMax, ratio, error] of meshJobs) {
    const path = join(root, rel);
    const tmp = path + '.tmp.glb';
    execSync(
      `npx --yes @gltf-transform/cli@4 resize "${path}" "${tmp}" --width ${texMax} --height ${texMax}`,
      { stdio: 'pipe' },
    );
    const io = new NodeIO().registerExtensions(ALL_EXTENSIONS);
    const doc = await io.read(tmp);
    for (const mesh of doc.getRoot().listMeshes()) {
      for (const prim of mesh.listPrimitives()) positionWeld(prim, 5);
    }
    await doc.transform(
      dedup(),
      weld({ overwrite: true }),
      simplify({ simplifier: MeshoptSimplifier, ratio, error, lockBorder: false }),
      normals({ overwrite: true }),
      prune(),
    );
    await io.write(path, doc);
    try {
      unlinkSync(tmp);
    } catch {}
    console.log(rel, `${(statSync(path).size / 1e6).toFixed(2)} MB`);
  }
}

main().catch((err) => {
  console.error(err);
  process.exit(1);
});
