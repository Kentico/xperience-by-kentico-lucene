// Builds every top-level file in Styles/ as a Tailwind entry point, so adding a new
// bundle is just adding a file there — no script or package.json changes needed.
// Shared building blocks live in Styles/partials/ and are compiled only through the
// entries that @import them. Pass --watch to rebuild all bundles on change; Tailwind
// watches the whole @import graph of each entry, partials included.
// Kept dependency-free on purpose - it spawns the local tailwindcss CLI directly.
import { spawn } from 'node:child_process';
import { readdirSync } from 'node:fs';
import { createRequire } from 'node:module';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const require = createRequire(import.meta.url);
const cliPackage = require.resolve('@tailwindcss/cli/package.json');
const tailwindCli = join(dirname(cliPackage), 'dist', 'index.mjs');

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');
const stylesDir = join(projectRoot, 'Styles');

const watch = process.argv.includes('--watch');

// Output names keep the historical PascalCase convention (site.css -> Site.css).
const entries = readdirSync(stylesDir, { withFileTypes: true })
    .filter((file) => file.isFile() && file.name.endsWith('.css'))
    .map(({ name }) => ({
        input: join(stylesDir, name),
        output: join(projectRoot, 'wwwroot', 'Content', 'Styles', name[0].toUpperCase() + name.slice(1))
    }));

if (entries.length === 0) {
    console.error('No CSS entry points found in Styles/.');
    process.exit(1);
}

function run(entry) {
    // --optimize runs Lightning CSS on the output, flattening native nesting and
    // downleveling selectors so the compiled CSS keeps the browser support of the
    // fully flattened stylesheets the old Less pipeline produced.
    const args = [tailwindCli, '-i', entry.input, '-o', entry.output, '--optimize'];
    if (watch) {
        args.push('--watch');
    }
    return spawn(process.execPath, args, { stdio: 'inherit' });
}

const children = entries.map(run);

let shuttingDown = false;

function shutdown() {
    shuttingDown = true;
    for (const child of children) {
        child.kill();
    }
}

process.on('SIGINT', shutdown);
process.on('SIGTERM', shutdown);

for (const [index, child] of children.entries()) {
    child.on('exit', (code) => {
        if (code !== 0) {
            process.exitCode = code ?? 1;
        }
        // In watch mode a dead watcher means its bundle silently stops rebuilding,
        // so fail fast instead of keeping the remaining watchers running.
        if (watch && !shuttingDown) {
            console.error(`Tailwind watcher for ${entries[index].input} exited unexpectedly, shutting down.`);
            process.exitCode ??= 1;
            shutdown();
        }
    });
}
