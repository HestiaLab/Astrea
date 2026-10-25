#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""Astrea SRS mechanical consistency checks.

Usage: python scripts/verify_srs_consistency.py
Exit code: 0 when all checks pass, otherwise 1.
"""

import re
import sys
from collections import Counter, defaultdict
from pathlib import Path

SRS_PATH = Path("docs/SRS.md")
MATRIX_PATH = Path("docs/requirements-test-matrix.md")
ERRORS: list[str] = []
MATRIX_GAPS = 0


def err(message: str) -> None:
    ERRORS.append(message)


def parse_status_statistics(text: str) -> dict[str, tuple[int, int, int]]:
    start = text.find("## 5.3 按状态")
    end = text.find("## 附录A")
    if start == -1 or end == -1:
        err("§5.3 按状态 或附录 A 标题缺失")
        return {}

    block = text[start:end]
    rows = {
        status: (int(functional), int(non_functional), int(total))
        for status, functional, non_functional, total in re.findall(
            r"\|\s*(Active|Deferred|合计)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|\s*(\d+)\s*\|",
            block,
        )
    }
    missing = {"Active", "Deferred", "合计"} - set(rows)
    if missing:
        err(f"§5.3 缺少行: {sorted(missing)}")
    return rows


def validate_matrix(requirements: list[dict[str, str]], appendix_id_set: set[str]) -> None:
    global MATRIX_GAPS
    try:
        matrix_text = MATRIX_PATH.read_text(encoding="utf-8")
    except FileNotFoundError:
        err(f"找不到 {MATRIX_PATH}")
        return

    rows = re.findall(
        r"^\|\s*(REQ-(?:F|NF)-\d{3})\s*\|\s*([🔴🟡🟢])\s*\|\s*(Active|Deferred)\s*\|\s*(.*?)\s*\|$",
        matrix_text,
        re.MULTILINE,
    )
    matrix_ids = [row[0] for row in rows]
    if len(matrix_ids) != len(set(matrix_ids)):
        err("需求-测试矩阵存在重复需求行")

    body = {
        req["id"]: (req["priority"], req["title"].strip())
        for req in requirements
    }
    missing = sorted(set(body) - set(matrix_ids))
    extra = sorted(set(matrix_ids) - set(body))
    if missing:
        err(f"需求-测试矩阵缺少需求: {missing}")
    if extra:
        err(f"需求-测试矩阵多出需求: {extra}")

    for rid, priority, status, evidence in rows:
        if rid not in body:
            continue
        expected_priority, _ = body[rid]
        if priority != expected_priority:
            err(f"需求-测试矩阵 {rid} 优先级 {priority} != 正文 {expected_priority}")
        expected_status = "Deferred" if rid in appendix_id_set else "Active"
        if status != expected_status:
            err(f"需求-测试矩阵 {rid} 状态 {status} != 附录 B {expected_status}")
        if not evidence.strip():
            err(f"需求-测试矩阵 {rid} 缺少证据或 Gap 说明")
            continue
        if status == "Deferred" and "Deferred" not in evidence:
            err(f"需求-测试矩阵 {rid} 未标记 Deferred")
        if status == "Active" and evidence.startswith("Gap:"):
            MATRIX_GAPS += 1
            if priority == "🟢":
                err(f"需求-测试矩阵 {rid} 为 Active P2，不应使用发布阻断 Gap；请提供证据或调整状态")


def parse_claimed_statistics(text: str) -> tuple[dict[str, int], dict[str, int], int]:
    priority_start = text.find("## 5.1 按优先级")
    section_start = text.find("## 5.2 按章节")
    appendix_start = text.find("## 附录A")
    if min(priority_start, section_start, appendix_start) == -1:
        err("§5 或附录 A 标题缺失")
        return {}, {}, 0

    priority_text = text[priority_start:section_start]
    section_text = text[section_start:appendix_start]

    priorities = {
        icon: int(count)
        for icon, count in re.findall(r"\|\s*([🔴🟡🟢])\s+P[012]\s*\|\s*(\d+)\s*\|", priority_text)
    }
    total_match = re.search(r"\|\s*合计\s*\|\s*(\d+)\s*\|", priority_text)
    total = int(total_match.group(1)) if total_match else 0
    if not total_match:
        err("§5.1 缺少合计行")

    sections: dict[str, int] = {}
    for section, count in re.findall(
        r"\|\s*§(3\.\d+|4)(?:\s+[^|]*)?\|[^|]*\|\s*(\d+)\s*\|",
        section_text,
    ):
        sections[section] = int(count)
    return priorities, sections, total


def main() -> None:
    try:
        text = SRS_PATH.read_text(encoding="utf-8")
    except FileNotFoundError:
        err(f"找不到 {SRS_PATH}")
        finish(0)
        return

    lines = text.splitlines()
    fence_count = sum(line.strip() == "```" for line in lines)
    if fence_count % 2:
        err(f"代码围栏数量 {fence_count} 不是偶数")

    requirement_pattern = re.compile(
        r"(?ms)^```\n"
        r"(?P<id>REQ-(?:F|NF)-\d{3})\s+"
        r"(?P<priority>[🔴🟡🟢])\s+"
        r"\[Actor:\s*(?P<actor>[^\]]+)\]\n\n"
        r"Title\n(?P<title>[^\n]+)\n\n"
        r"Description\n(?P<description>.*?)\n\n"
        r"Source\n(?P<source>.*?)\n\n"
        r"Acceptance Criteria\n(?P<criteria>.*?)^```"
    )
    requirements = [match.groupdict() for match in requirement_pattern.finditer(text)]
    ids = [req["id"] for req in requirements]
    if len(ids) != len(set(ids)):
        duplicates = sorted(rid for rid, count in Counter(ids).items() if count > 1)
        err(f"重复需求编号: {duplicates}")

    header_ids = re.findall(r"^REQ-(?:F|NF)-\d{3}\s+[🔴🟡🟢]", text, re.MULTILINE)
    if len(requirements) != len(header_ids):
        err(f"完整需求块 {len(requirements)} != 需求头 {len(header_ids)}")

    role_start, role_end = text.find("### 1.3.2 角色定义"), text.find("## 1.4 文档约定")
    if role_start == -1 or role_end == -1:
        err("§1.3.2 角色定义缺失")
    else:
        defined_roles = {
            role.strip()
            for role in re.findall(r"^\|\s*([^|]+?)\s*\|", text[role_start:role_end], re.MULTILINE)
        }
        defined_roles -= {"English", "---------"}
        used_roles = {req["actor"].strip() for req in requirements}
        undefined_roles = sorted(used_roles - defined_roles)
        if undefined_roles:
            err(f"需求使用了未定义角色: {undefined_roles}")

    # Numbering declaration and continuity.
    range_match = re.search(r"F:\s*(\d{3})~(\d{3})；NF:\s*(\d{3})~(\d{3})", text)
    if not range_match:
        err("§1.4.1 缺少可解析的 F/NF 编号范围")
    else:
        f_lo, f_hi, nf_lo, nf_hi = map(int, range_match.groups())
        for prefix, lo, hi in (("REQ-F", f_lo, f_hi), ("REQ-NF", nf_lo, nf_hi)):
            actual = [int(rid.rsplit("-", 1)[1]) for rid in ids if rid.startswith(prefix)]
            expected = set(range(lo, hi + 1))
            missing = sorted(expected - set(actual))
            extra = sorted(set(actual) - expected)
            if missing:
                err(f"{prefix} 缺失编号: {missing}")
            if extra:
                err(f"{prefix} 超出声明范围: {extra}")

    # Given/When/Then completeness.
    for req in requirements:
        ac_parts = re.split(r"\nAC\d+[^\n]*\n", req["criteria"])[1:]
        if not ac_parts:
            err(f"{req['id']} 没有可解析的 AC")
            continue
        for index, ac in enumerate(ac_parts, 1):
            for keyword in ("Given ", "When ", "Then "):
                if keyword not in ac:
                    err(f"{req['id']} AC{index} 缺少 {keyword.strip()}")

    claimed_priorities, claimed_sections, claimed_total = parse_claimed_statistics(text)
    actual_priorities = Counter(req["priority"] for req in requirements)
    if claimed_total != len(requirements):
        err(f"需求总数 {len(requirements)} != §5 声称 {claimed_total}")
    for priority in ("🔴", "🟡", "🟢"):
        if claimed_priorities.get(priority) != actual_priorities.get(priority, 0):
            err(
                f"优先级 {priority} 统计 {actual_priorities.get(priority, 0)} "
                f"!= §5 声称 {claimed_priorities.get(priority)}"
            )

    # Requirements per chapter.
    line_by_id: dict[str, int] = {}
    for number, line in enumerate(lines, 1):
        match = re.match(r"^(REQ-(?:F|NF)-\d{3})\s+[🔴🟡🟢]", line)
        if match:
            line_by_id[match.group(1)] = number
    actual_sections: defaultdict[str | None, list[str]] = defaultdict(list)
    for rid, line_number in line_by_id.items():
        section = None
        for index in range(line_number - 2, -1, -1):
            match = re.match(r"^#+\s+(3\.\d+|4)\s", lines[index])
            if match:
                section = match.group(1)
                break
        actual_sections[section].append(rid)
    for section, claimed_count in claimed_sections.items():
        actual_count = len(actual_sections.get(section, []))
        if actual_count != claimed_count:
            err(f"§{section} 数量 {actual_count} != §5 声称 {claimed_count}")
    unexpected_sections = set(actual_sections) - set(claimed_sections)
    if unexpected_sections:
        err(f"§5 未声明的需求章节: {sorted(str(x) for x in unexpected_sections)}")

    # Appendix A: one exact row per requirement, including actor and title.
    a_start, a_end = text.find("## 附录A"), text.find("## 附录B")
    if a_start == -1 or a_end == -1:
        err("附录 A/B 标题缺失")
    else:
        rows = re.findall(
            r"\|\s*(REQ-(?:F|NF)-\d{3})\s*\|\s*([🔴🟡🟢])\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|",
            text[a_start:a_end],
        )
        if len(rows) != len({row[0] for row in rows}):
            err("附录 A 存在重复需求行")
        appendix = {rid: (priority, actor.strip(), title.strip()) for rid, priority, actor, title in rows}
        body = {
            req["id"]: (req["priority"], req["actor"].strip(), req["title"].strip())
            for req in requirements
        }
        for rid, expected in body.items():
            if rid not in appendix:
                err(f"附录 A 缺少 {rid}")
            elif appendix[rid] != expected:
                err(f"附录 A {rid} {appendix[rid]} != 正文 {expected}")
        for rid in appendix.keys() - body.keys():
            err(f"附录 A 多出 {rid}")

    # Appendix B is the authoritative Deferred list; it may contain any priority.
    appendix_id_set: set[str] = set()
    b_start, b_end = text.find("## 附录B"), text.find("## 附录C")
    if b_start == -1 or b_end == -1:
        err("附录 B/C 标题缺失")
    else:
        rows = re.findall(
            r"\|\s*(REQ-(?:F|NF)-\d{3})\s*\|\s*([^|]+?)\s*\|\s*([^|]+?)\s*\|",
            text[b_start:b_end],
        )
        appendix_ids = [row[0] for row in rows]
        appendix_id_set = set(appendix_ids)
        if len(appendix_ids) != len(appendix_id_set):
            err("附录 B 存在重复需求行")
        unknown = sorted(appendix_id_set - set(ids))
        if unknown:
            err(f"附录 B 引用不存在需求: {unknown}")
        title_by_id = {req["id"]: req["title"] for req in requirements}
        for rid, title, explanation in rows:
            if title.strip() != title_by_id.get(rid):
                err(f"附录 B {rid} 标题[{title.strip()}] != 正文[{title_by_id.get(rid)}]")
            if "Deferred" not in explanation:
                err(f"附录 B {rid} 未明确标记 Deferred")
        # Source markers are optional, but if present they must also be listed.
        deferred_source_ids = {req["id"] for req in requirements if "Deferred" in req["source"]}
        missing_source_rows = sorted(deferred_source_ids - appendix_id_set)
        if missing_source_rows:
            err(f"Source 标记 Deferred 但附录 B 未列出: {missing_source_rows}")

    # Active/Deferred statistics must agree with Appendix B.
    status_claims = parse_status_statistics(text)
    if status_claims:
        total_f = sum(1 for rid in ids if rid.startswith("REQ-F-"))
        total_nf = len(ids) - total_f
        deferred_f = sum(1 for rid in appendix_id_set if rid.startswith("REQ-F-"))
        deferred_nf = len(appendix_id_set) - deferred_f
        expected_status = {
            "Active": (total_f - deferred_f, total_nf - deferred_nf, len(ids) - len(appendix_id_set)),
            "Deferred": (deferred_f, deferred_nf, len(appendix_id_set)),
            "合计": (total_f, total_nf, len(ids)),
        }
        for status, expected in expected_status.items():
            actual = status_claims.get(status)
            if actual != expected:
                err(f"§5.3 {status} 统计 {actual} != 实际 {expected}")

    validate_matrix(requirements, appendix_id_set)
    if "--release" in sys.argv[1:] and MATRIX_GAPS:
        err(f"发布门禁失败：仍有 {MATRIX_GAPS} 条 Active 需求证据 Gap 未关闭")

    # All requirement references must exist.
    body_ids = set(ids)
    for line_number, line in enumerate(lines, 1):
        if re.match(r"^REQ-(?:F|NF)-\d{3}\s", line):
            continue
        for reference in re.findall(r"REQ-(?:F|NF)-\d{3}", line):
            if reference not in body_ids:
                err(f"行{line_number} 引用不存在的 {reference}")

    finish(len(requirements))


def finish(requirement_count: int) -> None:
    if ERRORS:
        print(f"FAILED ({len(ERRORS)}):")
        for message in ERRORS:
            print("  -", message)
        raise SystemExit(1)
    if MATRIX_GAPS:
        print(f"WARNING: {MATRIX_GAPS} tracked release-blocking gaps in {MATRIX_PATH}")
    print(f"ALL CHECKS PASSED（需求 {requirement_count} 条；已跟踪 Gap {MATRIX_GAPS} 条）")
    raise SystemExit(0)


if __name__ == "__main__":
    main()
