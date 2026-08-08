#!/usr/bin/env node
/**
 * Finds localization keys in ja-JP.json that are not referenced anywhere in the codebase.
 *
 * Detection rules:
 *   - AXAML:  {Binding [Key], Source={x:Static loc:Localizer.Instance}}  -> literal [Key]
 *   - C#:     Loc.Key.Path  (e.g. Localizer.Instance[Loc.Main.AddItem])
 *
 * Usage: node Tools/FindUnusedLocalizationKeys.mjs
 */

import fs from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const jsonPath = path.join(repoRoot, 'AvatarExplorer.Core', 'Data', 'Localization', 'ja-JP.json');

const json = JSON.parse(fs.readFileSync(jsonPath, 'utf-8'));
const allKeys = Object.keys(json).sort();

const watchExtensions = new Set(['.axaml', '.cs']);
const ignoreDirs = new Set(['bin', 'obj', 'node_modules', '.git', '.github', '.opencode']);
const ignoreFiles = new Set(['LocalizationKeys.g.cs']);

function collectFiles(dir, result = []) {
    for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
        if (entry.isDirectory()) {
            if (!ignoreDirs.has(entry.name)) collectFiles(path.join(dir, entry.name), result);
        } else if (
            watchExtensions.has(path.extname(entry.name)) &&
            !ignoreFiles.has(entry.name)
        ) {
            result.push(path.join(dir, entry.name));
        }
    }
    return result;
}

const files = collectFiles(repoRoot);
const axamlTexts = [];
const csTexts = [];
for (const file of files) {
    const text = fs.readFileSync(file, 'utf-8');
    (file.endsWith('.axaml') ? axamlTexts : csTexts).push(text);
}

const unusedKeys = allKeys.filter((key) => {
    const usedInAxaml = axamlTexts.some((t) => t.includes(`[${key}]`));
    const usedInCs = csTexts.some((t) => t.includes(`Loc.${key}`));
    return !usedInAxaml && !usedInCs;
});

console.log(`Keys in ja-JP.json: ${allKeys.length}`);
console.log(`Used keys:          ${allKeys.length - unusedKeys.length}`);
console.log(`Unused keys:        ${unusedKeys.length}`);
console.log('---');
for (const key of unusedKeys) {
    console.log(key);
}
