/** Render the editable SVG master. Requires Node.js and sharp (npm install sharp).
 * NODE_PATH can point at an existing sharp installation. No embedded bitmap,
 * glow or platform-specific renderer is needed; generated PNG keeps alpha.
 */
import { createRequire } from 'node:module';
import { readFile, writeFile } from 'node:fs/promises';
import { fileURLToPath } from 'node:url';
import path from 'node:path';
const require = createRequire(import.meta.url);
const sharp = require('sharp');
const brand = fileURLToPath(new URL('../assets/brand/', import.meta.url));
const svg = await readFile(path.join(brand, 'icon.svg'), 'utf8');
if (!process.argv.includes('--board-only')) await sharp(Buffer.from(svg), { density: 144 }).resize(2048, 2048).png().toFile(path.join(brand, 'icon-master.png'));
// Recolor every white element, including the interior pointer and its stroke.
const symbol = svg.replace(/<rect[^>]+\/>/, '').replaceAll('#FFFFFF', '#4B50CE');
if (!process.argv.includes('--board-only')) await writeFile(path.join(brand, 'symbol.svg'), symbol);
const board = `<svg xmlns="http://www.w3.org/2000/svg" width="1536" height="1024" viewBox="0 0 1536 1024">
<rect width="1536" height="1024" fill="#FFFFFF"/>
<svg x="76" y="70" width="420" height="420" viewBox="160 240 704 544">${symbol.replace(/^[\s\S]*?<svg[^>]*>/,'').replace(/<\/svg>\s*$/,'')}</svg>
<text x="520" y="290" font-family="Segoe UI, Arial, sans-serif" font-size="88" font-weight="600" fill="#282C67">Local Remote</text>
<text x="520" y="390" font-family="Segoe UI, Arial, sans-serif" font-size="88" font-weight="600" fill="#282C67">Desktop</text>
<svg x="70" y="570" width="320" height="300" viewBox="160 240 704 544">${symbol.replace(/^[\s\S]*?<svg[^>]*>/,'').replace(/<\/svg>\s*$/,'').replaceAll('screen-half','small-half')}</svg>
<svg x="1000" y="490" width="440" height="440" viewBox="0 0 1024 1024">${svg.replace(/^[\s\S]*?<svg[^>]*>/,'').replace(/<\/svg>\s*$/,'').replaceAll('screen-half','tile-half').replaceAll('blueberry','tile-blueberry')}</svg>
</svg>`;
await sharp(Buffer.from(board)).png().toFile(path.join(brand, 'identity-board.png'));
console.log('Rendered editable SVG master, solid symbol and identity presentation');
