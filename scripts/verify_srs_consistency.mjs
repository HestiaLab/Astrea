#!/usr/bin/env node
/**
 * Astrea SRS mechanical consistency checks.
 * Usage: node scripts/verify_srs_consistency.mjs
 */
import { readFileSync } from "node:fs";

const SRS_PATH = "docs/SRS.md";
const errors = [];
const err = (message) => errors.push(message);

let text;
try {
  // Git may check the Markdown file out with CRLF on Windows. Normalize it so
  // the structural regular expressions have identical behavior on every OS.
  text = readFileSync(SRS_PATH, "utf8").replace(/\r\n/g, "\n");
} catch {
  console.log(`FAILED (1):\n  - 找不到 ${SRS_PATH}`);
  process.exit(1);
}
const lines = text.split(/\r?\n/);
const fenceCount = lines.filter((line) => line.trim() === "```").length;
if (fenceCount % 2 !== 0) err(`代码围栏数量 ${fenceCount} 不是偶数`);

const requirementPattern = /^```\n(REQ-(?:F|NF)-\d{3})\s+([🔴🟡🟢])\s+\[Actor:\s*([^\]]+)\]\n\nTitle\n([^\n]+)\n\nDescription\n([\s\S]*?)\n\nSource\n([\s\S]*?)\n\nAcceptance Criteria\n([\s\S]*?)^```/gmu;
const requirements = [...text.matchAll(requirementPattern)].map((match) => ({
  id: match[1],
  priority: match[2],
  actor: match[3].trim(),
  title: match[4].trim(),
  description: match[5],
  source: match[6],
  criteria: match[7],
}));
const ids = requirements.map((requirement) => requirement.id);
const bodyIds = new Set(ids);
if (ids.length !== bodyIds.size) {
  const duplicates = [...new Set(ids.filter((id, index) => ids.indexOf(id) !== index))];
  err(`重复需求编号: ${duplicates.join(", ")}`);
}
const headerCount = [...text.matchAll(/^REQ-(?:F|NF)-\d{3}\s+[🔴🟡🟢]/gmu)].length;
if (requirements.length !== headerCount) {
  err(`完整需求块 ${requirements.length} != 需求头 ${headerCount}`);
}

const roleStart = text.indexOf("### 1.3.2 角色定义");
const roleEnd = text.indexOf("## 1.4 文档约定");
if (roleStart === -1 || roleEnd === -1) {
  err("§1.3.2 角色定义缺失");
} else {
  const definedRoles = new Set(
    [...text.slice(roleStart, roleEnd).matchAll(/^\|\s*([^|]+?)\s*\|/gmu)]
      .map((match) => match[1].trim())
      .filter((role) => !["English", "---------"].includes(role)),
  );
  const undefinedRoles = [...new Set(requirements.map((requirement) => requirement.actor))]
    .filter((role) => !definedRoles.has(role));
  if (undefinedRoles.length) err(`需求使用了未定义角色: ${undefinedRoles.join(", ")}`);
}

const rangeMatch = text.match(/F:\s*(\d{3})~(\d{3})；NF:\s*(\d{3})~(\d{3})/u);
if (!rangeMatch) {
  err("§1.4.1 缺少可解析的 F/NF 编号范围");
} else {
  const ranges = [
    ["REQ-F", Number(rangeMatch[1]), Number(rangeMatch[2])],
    ["REQ-NF", Number(rangeMatch[3]), Number(rangeMatch[4])],
  ];
  for (const [prefix, low, high] of ranges) {
    const actual = new Set(
      ids.filter((id) => id.startsWith(prefix)).map((id) => Number(id.split("-").at(-1))),
    );
    const missing = [];
    for (let number = low; number <= high; number += 1) {
      if (!actual.has(number)) missing.push(number);
    }
    const extra = [...actual].filter((number) => number < low || number > high);
    if (missing.length) err(`${prefix} 缺失编号: ${missing.join(", ")}`);
    if (extra.length) err(`${prefix} 超出声明范围: ${extra.join(", ")}`);
  }
}

for (const requirement of requirements) {
  const acceptanceCriteria = requirement.criteria.split(/\nAC\d+[^\n]*\n/u).slice(1);
  if (!acceptanceCriteria.length) err(`${requirement.id} 没有可解析的 AC`);
  acceptanceCriteria.forEach((criterion, index) => {
    for (const keyword of ["Given ", "When ", "Then "]) {
      if (!criterion.includes(keyword)) {
        err(`${requirement.id} AC${index + 1} 缺少 ${keyword.trim()}`);
      }
    }
  });
}

const priorityStart = text.indexOf("## 5.1 按优先级");
const sectionStart = text.indexOf("## 5.2 按章节");
const appendixAStart = text.indexOf("## 附录A");
const appendixBStart = text.indexOf("## 附录B");
const appendixCStart = text.indexOf("## 附录C");
if ([priorityStart, sectionStart, appendixAStart, appendixBStart, appendixCStart].includes(-1)) {
  err("§5 或附录 A/B/C 标题缺失");
} else {
  const priorityText = text.slice(priorityStart, sectionStart);
  const sectionText = text.slice(sectionStart, appendixAStart);
  const claimedPriorities = Object.fromEntries(
    [...priorityText.matchAll(/\|\s*([🔴🟡🟢])\s+P[012]\s*\|\s*(\d+)\s*\|/gu)]
      .map((match) => [match[1], Number(match[2])]),
  );
  const totalMatch = priorityText.match(/\|\s*合计\s*\|\s*(\d+)\s*\|/u);
  if (!totalMatch) err("§5.1 缺少合计行");
  else if (Number(totalMatch[1]) !== requirements.length) {
    err(`需求总数 ${requirements.length} != §5 声称 ${totalMatch[1]}`);
  }
  for (const priority of ["🔴", "🟡", "🟢"]) {
    const actual = requirements.filter((requirement) => requirement.priority === priority).length;
    if (actual !== claimedPriorities[priority]) {
      err(`优先级 ${priority} 统计 ${actual} != §5 声称 ${claimedPriorities[priority]}`);
    }
  }

  const claimedSections = Object.fromEntries(
    [...sectionText.matchAll(/\|\s*§(3\.\d+|4)(?:\s+[^|]*)?\|[^|]*\|\s*(\d+)\s*\|/gu)]
      .map((match) => [match[1], Number(match[2])]),
  );
  const lineById = new Map();
  lines.forEach((line, index) => {
    const match = line.match(/^(REQ-(?:F|NF)-\d{3})\s+[🔴🟡🟢]/u);
    if (match) lineById.set(match[1], index);
  });
  const actualSections = {};
  for (const [id, lineIndex] of lineById.entries()) {
    let section = null;
    for (let index = lineIndex - 1; index >= 0; index -= 1) {
      const match = lines[index].match(/^#+\s+(3\.\d+|4)\s/u);
      if (match) { section = match[1]; break; }
    }
    actualSections[section] = (actualSections[section] || 0) + 1;
  }
  for (const [section, claimed] of Object.entries(claimedSections)) {
    if ((actualSections[section] || 0) !== claimed) {
      err(`§${section} 数量 ${actualSections[section] || 0} != §5 声称 ${claimed}`);
    }
  }
  for (const section of Object.keys(actualSections)) {
    if (!(section in claimedSections)) err(`§5 未声明的需求章节: ${section}`);
  }

  const appendixARows = [...text.slice(appendixAStart, appendixBStart).matchAll(
    /\|\s*(REQ-(?:F|NF)-\d{3})\s*\|\s*([🔴🟡🟢])\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|/gu,
  )].map((match) => ({ id: match[1], priority: match[2], actor: match[3].trim(), title: match[4].trim() }));
  if (appendixARows.length !== new Set(appendixARows.map((row) => row.id)).size) {
    err("附录 A 存在重复需求行");
  }
  const appendixA = new Map(appendixARows.map((row) => [row.id, row]));
  for (const requirement of requirements) {
    const row = appendixA.get(requirement.id);
    if (!row) err(`附录 A 缺少 ${requirement.id}`);
    else if (row.priority !== requirement.priority || row.actor !== requirement.actor || row.title !== requirement.title) {
      err(`附录 A ${requirement.id} 与正文不一致`);
    }
  }
  for (const id of appendixA.keys()) {
    if (!bodyIds.has(id)) err(`附录 A 多出 ${id}`);
  }

  const appendixBRows = [...text.slice(appendixBStart, appendixCStart).matchAll(
    /\|\s*(REQ-F-\d{3})\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|/gu,
  )].map((match) => ({ id: match[1], title: match[2].trim(), explanation: match[3].trim() }));
  if (appendixBRows.length !== new Set(appendixBRows.map((row) => row.id)).size) {
    err("附录 B 存在重复需求行");
  }
  const deferredIds = new Set(requirements.filter((requirement) => requirement.source.includes("Deferred")).map((requirement) => requirement.id));
  const appendixBIds = new Set(appendixBRows.map((row) => row.id));
  if ([...deferredIds].some((id) => !appendixBIds.has(id)) || [...appendixBIds].some((id) => !deferredIds.has(id))) {
    err(`附录 B 与 Deferred 需求不一致`);
  }
  const requirementById = new Map(requirements.map((requirement) => [requirement.id, requirement]));
  for (const row of appendixBRows) {
    const requirement = requirementById.get(row.id);
    if (!requirement || row.title !== requirement.title) err(`附录 B ${row.id} 标题与正文不一致`);
    if (requirement?.priority !== "🟢") err(`附录 B ${row.id} 不是 P2`);
    if (!row.explanation.includes("Deferred")) err(`附录 B ${row.id} 未明确标记 Deferred`);
  }
}

lines.forEach((line, index) => {
  if (/^REQ-(?:F|NF)-\d{3}\s/u.test(line)) return;
  for (const match of line.matchAll(/REQ-(?:F|NF)-\d{3}/gu)) {
    if (!bodyIds.has(match[0])) err(`行${index + 1} 引用不存在的 ${match[0]}`);
  }
});

if (errors.length) {
  console.log(`FAILED (${errors.length}):`);
  errors.forEach((message) => console.log("  -", message));
  process.exit(1);
}
console.log(`ALL CHECKS PASSED（需求 ${requirements.length} 条）`);
process.exit(0);
