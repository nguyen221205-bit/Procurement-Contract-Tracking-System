import { readFileSync, readdirSync, writeFileSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import path from 'node:path';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const directory = path.join(root, 'ProcurementSystem.Core/Enums');
const enums = {};
for (const filename of readdirSync(directory).filter((name) => name.endsWith('.cs')).sort()) {
  const source = readFileSync(path.join(directory, filename), 'utf8').replace(/\/\/[^\n]*/g, '');
  const match = source.match(/public enum (\w+)\s*\{([^}]+)\}/);
  if (!match) continue; // String constants are not numeric enums.
  const members = {};
  for (const item of match[2].split(',').map((value) => value.trim()).filter(Boolean)) {
    const member = item.match(/^(\w+)\s*=\s*(-?\d+)$/);
    if (!member) throw new Error(`Unsupported enum member: ${filename}: ${item}`);
    members[member[1]] = Number(member[2]);
  }
  enums[match[1]] = members;
}
const output = '// Generated from ProcurementSystem.Core/Enums. Run npm run generate:enums.\n'
  + `export const ENUMS = Object.freeze(${JSON.stringify(enums, null, 2)});\n`;
const target = path.join(root, 'frontend/src/api/enums.generated.js');
if (process.argv.includes('--check')) {
  if (readFileSync(target, 'utf8').replace(/\r\n/g, '\n') !== output) throw new Error('Enum definitions changed. Run npm run generate:enums.');
} else writeFileSync(target, output);
