// Builds the form builder and page builder component bundles consumed by the
// Xperience framework from the conventional wwwroot/Content/Bundles/... paths
// (see PageBuilderBundlesOptions and FormBuilderBundlesOptions defaults).
//
// For each bundle the matching source files are concatenated in alphabetical
// order and minified with esbuild. Page builder bundles are always written
// (even when a source folder has no files) so the checked-in outputs stay
// stable; form builder bundles are only written when the wwwroot/FormBuilder
// source folder exists.
import { readdirSync, readFileSync, writeFileSync, mkdirSync, existsSync } from 'node:fs';
import { dirname, join, extname } from 'node:path';
import { fileURLToPath } from 'node:url';
import { transformSync } from 'esbuild';

const projectRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

const bundles = [
    { source: 'wwwroot/PageBuilder/Public', output: 'wwwroot/Content/Bundles/Public/pageComponents', types: ['css', 'js'], required: true },
    { source: 'wwwroot/PageBuilder/Admin', output: 'wwwroot/Content/Bundles/Admin/pageComponents', types: ['css', 'js'], required: true },
    { source: 'wwwroot/FormBuilder/Public', output: 'wwwroot/Content/Bundles/Public/formComponents', types: ['css', 'js'], required: false },
    { source: 'wwwroot/FormBuilder/Admin', output: 'wwwroot/Content/Bundles/Admin/formComponents', types: ['css'], required: false }
];

function collectSources(sourceDir, extension) {
    if (!existsSync(sourceDir)) {
        return [];
    }
    return readdirSync(sourceDir, { recursive: true })
        .map(String)
        .filter((file) => extname(file).toLowerCase() === `.${extension}`)
        .sort()
        .map((file) => join(sourceDir, file));
}

for (const bundle of bundles) {
    const sourceDir = join(projectRoot, bundle.source);
    if (!bundle.required && !existsSync(join(projectRoot, bundle.source.split('/').slice(0, -1).join('/')))) {
        continue;
    }

    for (const type of bundle.types) {
        const content = collectSources(sourceDir, type)
            .map((file) => readFileSync(file, 'utf8').replace(/^﻿/, ''))
            .join('\n');
        const outputPath = join(projectRoot, `${bundle.output}.${type}`);
        mkdirSync(dirname(outputPath), { recursive: true });
        writeFileSync(outputPath, content);

        const { code } = transformSync(content, { loader: type, minify: true });
        writeFileSync(join(projectRoot, `${bundle.output}.min.${type}`), code);
        console.log(`${bundle.output}.${type} (${content.length} B) + .min.${type} (${code.length} B)`);
    }
}
