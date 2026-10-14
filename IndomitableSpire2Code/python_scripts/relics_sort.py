import os
import re
import sys

def process_file(file_path):
    # 读取文件内容
    with open(file_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    # 正则表达式匹配 JSON 的键值对行，提取键名
    kv_pattern = re.compile(r'^\s*"([^"]+)"\s*:')

    blocks = []
    current_prefix = None
    current_items = []
    current_empty_lines = []

    for line in lines:
        stripped = line.strip()
        if not stripped:
            # 处理空行
            if current_prefix is not None and current_items:
                # 如果当前块已有内容，遇到空行则结束当前块，空行作为下一个块的前导空行
                blocks.append({
                    'type': 'prefix',
                    'prefix': current_prefix,
                    'leading_empty_lines': current_empty_lines,
                    'items': current_items
                })
                current_prefix = None
                current_items = []
                current_empty_lines = [line]
            else:
                current_empty_lines.append(line)
        else:
            match = kv_pattern.match(line)
            if match:
                key = match.group(1)
                # 分离前缀和后缀 (以最后一个 '.' 为界)
                if '.' in key:
                    prefix, suffix = key.rsplit('.', 1)
                else:
                    prefix, suffix = key, ""

                if current_prefix is None:
                    current_prefix = prefix
                    current_items = []
                elif current_prefix != prefix:
                    # 前缀改变，结束当前块
                    blocks.append({
                        'type': 'prefix',
                        'prefix': current_prefix,
                        'leading_empty_lines': current_empty_lines,
                        'items': current_items
                    })
                    current_prefix = prefix
                    current_items = []
                    current_empty_lines = []

                current_items.append({'suffix': suffix, 'line': line})
            else:
                # 非键值对的非空行（如 {, }, 注释等），作为独立块处理以保持结构
                if current_prefix is not None:
                    blocks.append({
                        'type': 'prefix',
                        'prefix': current_prefix,
                        'leading_empty_lines': current_empty_lines,
                        'items': current_items
                    })
                    current_prefix = None
                    current_items = []
                    current_empty_lines = []

                blocks.append({
                    'type': 'other',
                    'leading_empty_lines': current_empty_lines,
                    'line': line
                })
                current_empty_lines = []

    # 处理文件末尾剩余的内容
    if current_prefix is not None:
        blocks.append({
            'type': 'prefix',
            'prefix': current_prefix,
            'leading_empty_lines': current_empty_lines,
            'items': current_items
        })
    elif current_empty_lines:
        blocks.append({
            'type': 'other',
            'leading_empty_lines': current_empty_lines,
            'line': None
        })

    # 重建文件内容
    output_lines = []
    for block in blocks:
        output_lines.extend(block['leading_empty_lines'])
        if block['type'] == 'prefix':
            # 按照后缀的逆字典序（降序：title > flavor > description > additional...）排列
            sorted_items = sorted(block['items'], key=lambda x: x['suffix'], reverse=True)
            for item in sorted_items:
                output_lines.append(item['line'])
        else:
            if block['line'] is not None:
                output_lines.append(block['line'])

    # 写回文件
    with open(file_path, 'w', encoding='utf-8') as f:
        f.writelines(output_lines)

def main():
    if len(sys.argv) < 2:
        print("用法: python sort_relics.py <目录路径>")
        sys.exit(1)

    directory = sys.argv[1]
    if not os.path.isdir(directory):
        print(f"错误: {directory} 不是一个有效的目录。")
        sys.exit(1)

    # 遍历目录及子目录，寻找 relics.json
    for root, _, files in os.walk(directory):
        for file in files:
            if file == 'relics.json':
                file_path = os.path.join(root, file)
                print(f"正在处理 {file_path}...")
                process_file(file_path)
    print("处理完成。")

if __name__ == '__main__':
    main()