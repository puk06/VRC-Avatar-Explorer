// @ts-nocheck

/**
 * check-json.js
 * ベースとなるJSONと比較対象のJSONを比較して、不足・余剰・型違いのキーを一覧表示します。
 *
 * 使い方:
 *   node check-json.js <base.json> <target.json>
 *
 * 例:
 *   node check-json.js base.json target.json
 */
 
const fs = require("node:fs");
const path = require("node:path");
 
// ── ANSI カラーコード ──────────────────────────────────────────────────────────
const C = {
  reset: "\x1b[0m",
  bold: "\x1b[1m",
  red: "\x1b[31m",
  green: "\x1b[32m",
  yellow: "\x1b[33m",
  cyan: "\x1b[36m",
  gray: "\x1b[90m",
};
 
// ── ユーティリティ ─────────────────────────────────────────────────────────────
function colorize(color, text) {
  return `${C[color]}${text}${C.reset}`;
}
 
function loadJson(filePath) {
  const abs = path.resolve(filePath);
  if (!fs.existsSync(abs)) {
    console.error(colorize("red", `ファイルが見つかりません: ${abs}`));
    process.exit(1);
  }
  try {
    return JSON.parse(fs.readFileSync(abs, "utf-8"));
  } catch (e) {
    console.error(colorize("red", `JSON パースエラー (${filePath}): ${e.message}`));
    process.exit(1);
  }
}
 
function getType(value) {
  if (value === null) return "null";
  if (Array.isArray(value)) return "array";
  return typeof value;
}
 
// ── 比較ロジック ───────────────────────────────────────────────────────────────
const issues = {
  missing: [],   // ベースにあるが target にないキー
  extra: [],     // target にあるがベースにないキー
  typeMismatch: [], // 両方にあるが型が違うキー
};
 
/**
 * 再帰的に比較する。
 * @param {object} base    ベースオブジェクト
 * @param {object} target  比較対象オブジェクト
 * @param {string} parentPath  現在のキーパス（例: "user.address.city"）
 */
function compare(base, target, parentPath = "") {
  const baseKeys = Object.keys(base);
  const targetKeys = new Set(Object.keys(target));
 
  for (const key of baseKeys) {
    const fullPath = parentPath ? `${parentPath}.${key}` : key;
    const baseVal = base[key];
    const baseType = getType(baseVal);
 
    // ── 不足キー
    if (!targetKeys.has(key)) {
      issues.missing.push({ path: fullPath, type: baseType });
      continue;
    }
 
    const targetVal = target[key];
    const targetType = getType(targetVal);
 
    // ── 型の不一致
    if (baseType !== targetType) {
      issues.typeMismatch.push({
        path: fullPath,
        expected: baseType,
        actual: targetType,
      });
      continue;
    }
 
    // ── 両方がオブジェクトなら再帰
    if (baseType === "object") {
      compare(baseVal, targetVal, fullPath);
    }
 
    // ── 両方が配列の場合、要素がオブジェクトなら先頭要素で再帰チェック
    if (baseType === "array" && baseVal.length > 0 && targetVal.length > 0) {
      const baseElem = baseVal[0];
      const targetElem = targetVal[0];
      if (getType(baseElem) === "object" && getType(targetElem) === "object") {
        compare(baseElem, targetElem, `${fullPath}[0]`);
      }
    }
 
    targetKeys.delete(key);
  }
 
  // ── 余剰キー（target にあってベースにない）
  for (const key of targetKeys) {
    const fullPath = parentPath ? `${parentPath}.${key}` : key;
    issues.extra.push({ path: fullPath, type: getType(target[key]) });
  }
}
 
// ── 結果表示 ───────────────────────────────────────────────────────────────────
function printResults(baseFile, targetFile) {
  const total = issues.missing.length + issues.extra.length + issues.typeMismatch.length;
 
  console.log("");
  console.log(colorize("bold", "═══════════════════════════════════════════════════"));
  console.log(colorize("bold", "  JSON 比較レポート"));
  console.log(colorize("bold", "═══════════════════════════════════════════════════"));
  console.log(`  ${colorize("cyan", "ベース   :")} ${baseFile}`);
  console.log(`  ${colorize("cyan", "対象     :")} ${targetFile}`);
  console.log(colorize("bold", "───────────────────────────────────────────────────"));
 
  // 不足キー
  console.log(`\n${colorize("red", "【不足キー】")} (ベースにあるが対象にない) — ${issues.missing.length} 件`);
  if (issues.missing.length === 0) {
    console.log(colorize("gray", "  なし"));
  } else {
    for (const item of issues.missing) {
      console.log(`  ${colorize("red", "✗")} ${colorize("bold", item.path)} ${colorize("gray", `(${item.type})`)}`);
    }
  }
 
  // 余剰キー
  console.log(`\n${colorize("yellow", "【余剰キー】")} (対象にあるがベースにない) — ${issues.extra.length} 件`);
  if (issues.extra.length === 0) {
    console.log(colorize("gray", "  なし"));
  } else {
    for (const item of issues.extra) {
      console.log(`  ${colorize("yellow", "+")} ${colorize("bold", item.path)} ${colorize("gray", `(${item.type})`)}`);
    }
  }
 
  // 型不一致
  console.log(`\n${colorize("cyan", "【型の不一致】")} — ${issues.typeMismatch.length} 件`);
  if (issues.typeMismatch.length === 0) {
    console.log(colorize("gray", "  なし"));
  } else {
    for (const item of issues.typeMismatch) {
      console.log(
        `  ${colorize("cyan", "~")} ${colorize("bold", item.path)} ` +
        `${colorize("gray", `期待: ${item.expected} / 実際: ${item.actual}`)}`
      );
    }
  }
 
  console.log(`\n${colorize("bold", "───────────────────────────────────────────────────")}`);
  if (total === 0) {
    console.log(colorize("green", "  ✓ 差異なし。すべてのキーが一致しています！"));
  } else {
    console.log(`  合計 ${colorize("bold", String(total))} 件の差異が見つかりました。`);
  }
  console.log(colorize("bold", "═══════════════════════════════════════════════════\n"));
}
 
// ── メイン ─────────────────────────────────────────────────────────────────────
function main() {
  const args = process.argv.slice(2);
  if (args.length < 2) {
    console.log(`\n使い方: node check-json.js <base.json> <target.json>\n`);
    process.exit(1);
  }
 
  const [baseFile, targetFile] = args;
  const base = loadJson(baseFile);
  const target = loadJson(targetFile);
 
  if (getType(base) !== "object" || getType(target) !== "object") {
    console.error(colorize("red", "エラー: 両方のファイルのトップレベルはオブジェクトである必要があります。"));
    process.exit(1);
  }
 
  compare(base, target);
  printResults(baseFile, targetFile);
 
  // 差異があれば終了コード 1
  const total = issues.missing.length + issues.extra.length + issues.typeMismatch.length;
  process.exit(total > 0 ? 1 : 0);
}
 
main();
