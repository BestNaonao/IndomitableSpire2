"""按遗物池、原版稀有度和原有行号整理模组遗物本地化。"""

import json
import os
import re
import sys
from pathlib import Path


RELICS_DIR = Path(__file__).resolve().parents[1] / "Relics"
# 与原版 Core/Entities/Relics/RelicRarity.cs 的声明顺序一致。
RARITIES = ("None", "Starter", "Common", "Uncommon", "Rare", "Shop", "Event", "Ancient")
GROUPS = ("indomitable", "implacable", "shared", "sea_glass")
KEY_PATTERN = re.compile(r'^\s*"([^"]+)"\s*:')
CLASS_PATTERN = re.compile(r"\bclass\s+(\w+)\s*:\s*(\w+)")
RARITY_PATTERN = re.compile(r"\bRarity\s*=>\s*RelicRarity\.(\w+)")


def to_key(name):
    return re.sub(r"(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", "_", name).upper()


def relic_metadata():
    metadata = {}
    for source in RELICS_DIR.rglob("*.cs"):
        code = source.read_text(encoding="utf-8-sig")
        declaration = CLASS_PATTERN.search(code)
        rarity = RARITY_PATTERN.search(code)
        if not declaration or not rarity:
            continue
        name, base = declaration.groups()
        if base == "IndomitableRelic":
            group = "indomitable"
        elif base == "ImplacableRelic":
            group = "implacable"
        elif base == "IndomitableSpire2Relic" and "[Pool(typeof(SharedRelicPool))]" in code:
            group = "shared"
        else:
            raise ValueError(f"无法确定遗物 {name} 的卡池：{source}")
        if rarity.group(1) not in RARITIES:
            raise ValueError(f"未知遗物稀有度：{source}")
        metadata[f"INDOMITABLESPIRE2-{to_key(name)}"] = (group, RARITIES.index(rarity.group(1)))
    return metadata


def process_file(file_path, metadata=None):
    file_path = Path(file_path)
    metadata = metadata if metadata is not None else relic_metadata()
    original = file_path.read_text(encoding="utf-8-sig")
    parsed = json.loads(original)
    lines = original.splitlines()
    entries = {}
    for line_number, line in enumerate(lines):
        match = KEY_PATTERN.match(line)
        if not match:
            continue
        key = match.group(1)
        if "." not in key:
            raise ValueError(f"无后缀的本地化键：{key}")
        relic, suffix = key.rsplit(".", 1)
        entries.setdefault(relic, []).append((suffix, line_number, line.rstrip().removesuffix(",")))

    if {f"{relic}.{suffix}" for relic, items in entries.items() for suffix, _, _ in items} != set(parsed):
        raise ValueError(f"请确认 {file_path} 的每个本地化条目都独占一行")

    def order(relic):
        group, rarity = ("sea_glass", 0) if relic.startswith("SEA_GLASS.") else metadata[relic]
        return GROUPS.index(group), rarity, entries[relic][0][1]

    ordered_relics = sorted(entries, key=order)
    ordered_lines = []
    previous_group = None
    for relic in ordered_relics:
        group = "sea_glass" if relic.startswith("SEA_GLASS.") else metadata[relic][0]
        if previous_group is not None and group != previous_group:
            ordered_lines.append("")
        previous_group = group
        ordered_lines.extend(line for _, _, line in sorted(entries[relic], key=lambda item: item[0], reverse=True))

    for index, line in enumerate(ordered_lines):
        if line and any(next_line for next_line in ordered_lines[index + 1:]):
            ordered_lines[index] += ","
    result = "{\n" + "\n".join(ordered_lines) + "\n}\n"
    if json.loads(result) != parsed:
        raise ValueError(f"排序后本地化内容发生变化：{file_path}")
    file_path.write_text(result, encoding="utf-8", newline="")


def main():
    if len(sys.argv) != 2 or not os.path.isdir(sys.argv[1]):
        print("用法: python relics_sort.py <本地化目录>")
        sys.exit(1)
    metadata = relic_metadata()
    for root, _, files in os.walk(sys.argv[1]):
        if "relics.json" in files:
            path = Path(root) / "relics.json"
            print(f"正在处理 {path}...")
            process_file(path, metadata)
    print("处理完成。")


if __name__ == "__main__":
    main()
