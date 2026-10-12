// Render approved SVG illustrations for native apps. NODE_PATH may supply sharp.
import {createRequire} from 'node:module';
import {fileURLToPath} from 'node:url';
const sharp=createRequire(import.meta.url)('sharp');
for(const name of ['initiate-connection','accept-connection']) {
 const source=fileURLToPath(new URL(`../design/entry-review/${name}.svg`,import.meta.url));
 const output=fileURLToPath(new URL(`../assets/interface/${name}.png`,import.meta.url));
 await sharp(source,{density:288}).resize(960,744).png().toFile(output);
}
