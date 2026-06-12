import re
from pathlib import Path
from fractions import Fraction


def check_and_fix_power_tres(folder_path):
    folder = Path(folder_path)

    # 1. 检查文件夹是否存在
    if not folder.is_dir():
        print(f"❌ 错误: 文件夹 '{folder_path}' 不存在或不是一个有效的目录。")
        return

    # 2. 查找所有匹配 *_power_*.tres 的文件 (包含子文件夹)
    # 如果只需要当前目录，可以将 rglob 改为 glob
    files = list(folder.rglob('*_power_*.tres'))

    if not files:
        print(f"⚠️ 提示: 在 '{folder_path}' 中未找到匹配 '*_power_*.tres' 的文件。")
        return

    total_files = len(files)
    has_region_count = 0
    default_region_count = 0

    # 3. 正则表达式：匹配 region = Rect2(x, y, w, h)
    # 支持整数、浮点数、负数以及参数间的空格
    pattern = re.compile(r'region\s*=\s*Rect2\(\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*,\s*([-\d\.]+)\s*\)')

    print(f"🔍 开始检查 {total_files} 个文件...\n")

    # 4. 遍历处理每个文件
    for file_path in files:
        try:
            # 读取文件所有行
            with open(file_path, 'r', encoding='utf-8') as f:
                lines = f.readlines()

            # 获取末尾 5 行 (如果文件不足 5 行，则取全部)
            last_5_lines = lines[-5:] if len(lines) >= 5 else lines
            last_5_text = "".join(last_5_lines)

            # 在末尾 5 行中搜索匹配项
            match = pattern.search(last_5_text)

            if match:
                has_region_count += 1
                # 检查四个参数是否分别为 0, 0, 64, 64
                try:
                    x, y, w, h = map(float, match.groups())
                    if x == 0.0 and y == 0.0 and w == 64.0 and h == 64.0:
                        default_region_count += 1
                except ValueError:
                    pass  # 理论上正则已保证是数字，此处为防御性编程
            else:
                # 如果没有找到，则在文件末尾添加
                with open(file_path, 'a', encoding='utf-8') as f:
                    # 确保追加前有换行符，避免与上一行粘连
                    if lines and not lines[-1].endswith('\n'):
                        f.write('\n')
                    f.write('region = Rect2(0, 0, 64, 64)\n')
                print(f"✅ 已添加 region: {file_path}")

        except Exception as e:
            print(f"❌ 处理文件 {file_path} 时出错: {e}")

    # 5. 统计并打印最终结果
    print("\n" + "=" * 50)
    print("📊 最终统计结果")
    print("=" * 50)
    print(f"符合文件名格式 (*_power_*.tres) 的文件总数: {total_files}")

    # 使用 fractions.Fraction 自动计算并化简分数
    frac_has = Fraction(has_region_count, total_files)
    frac_default = Fraction(default_region_count, total_files)

    print(f"\n1. 末尾5行包含 'region = Rect2(...)' 的文件数: {has_region_count}")
    print(f"   占总文件数的比例 (分数): {frac_has}  (约 {has_region_count / total_files * 100:.2f}%)")
    print(f"\n2. 参数与默认值 (0, 0, 64, 64) 完全相同的文件数: {default_region_count}")
    print(f"   占总文件数的比例 (分数): {frac_default}  (约 {default_region_count / total_files * 100:.2f}%)")
    print("=" * 50)


if __name__ == "__main__":
    # 交互式获取文件夹路径，直接回车则默认使用当前目录
    target_folder = input("请输入要检查的文件夹路径 (直接回车使用当前目录 './'): ").strip()
    if not target_folder:
        target_folder = "."

    check_and_fix_power_tres(target_folder)