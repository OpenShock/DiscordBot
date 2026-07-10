import { execSync } from 'node:child_process';
import fs from 'node:fs';

// Refreshes the committed OpenAPI snapshot from the running Activity API, then regenerates the client.
// Point at a different instance with API_OPENAPI_URL (defaults to the local dev API on port 5000, the
// same target Vite proxies "/api" to). The API must be running in Development for /openapi to be exposed.
const SPEC_URL =
  process.env.API_OPENAPI_URL ??
  `${process.env.VITE_API_PROXY_TARGET ?? 'http://localhost:5000'}/openapi/v1.json`;
const SPEC_FILE = 'openapi.json';
const OUTPUT_DIR = 'src/lib/api/generated';

console.log(`Fetching OpenAPI document from ${SPEC_URL} ...`);
try {
  const res = await fetch(SPEC_URL);
  if (!res.ok) throw new Error(`HTTP ${res.status}`);
  const spec = JSON.stringify(await res.json(), null, 2);
  fs.writeFileSync(SPEC_FILE, spec + '\n');
  console.log(`  Wrote ${SPEC_FILE}`);
} catch (err) {
  console.error(`Could not fetch the spec: ${err.message}`);
  console.error(
    'Is the Activity API running in Development? Set API_OPENAPI_URL to override the source.'
  );
  process.exit(1);
}

if (fs.existsSync(OUTPUT_DIR)) {
  fs.rmSync(OUTPUT_DIR, { recursive: true, force: true });
  console.log(`  Deleted ${OUTPUT_DIR}`);
}

console.log('\nRunning @hey-api/openapi-ts...');
try {
  execSync('pnpm exec openapi-ts', { stdio: 'inherit', shell: true });
} catch {
  console.error('Codegen failed.');
  process.exit(1);
}

console.log('\nAPI regeneration complete.');
