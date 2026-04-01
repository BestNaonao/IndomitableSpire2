"""
Godot场景文件图层提取工具
自动查找项目根目录，解析.tscn文件，提取图层并合成图片
"""

import re
from pathlib import Path

from PIL import Image, ImageChops


def parse_tscn_file(tscn_path):
    """
    解析Godot .tscn场景文件，提取图层信息
    返回图层列表，包含路径、modulate颜色、顺序等信息
    """
    layers = []
    ext_resources = {}
    
    with open(tscn_path, 'r', encoding='utf-8') as f:
        content = f.read()
    
    # 解析外部资源引用
    ext_pattern = r'\[ext_resource.*?path="(.*?)"\s+id="(.*?)"\]'
    for match in re.finditer(ext_pattern, content, re.DOTALL):
        path = match.group(1).split("res://")[1]
        res_id = match.group(2)
        ext_resources[res_id] = path
    
    # 解析TextureRect节点（图层）
    # 查找所有TextureRect节点及其属性
    node_pattern = r'\[node name="(.*?)" type="TextureRect".*?\](.*?)(?=\[node|\[ext_resource|\[sub_resource|$)'
    
    for match in re.finditer(node_pattern, content, re.DOTALL | re.IGNORECASE):
        node_name = match.group(1)
        node_content = match.group(2)
        
        # 只处理Layer相关的节点
        if 'Layer' not in node_name:
            continue
        
        # 提取纹理引用
        texture_match = re.search(r'texture\s*=\s*ExtResource\("(.*?)"\)', node_content)
        if not texture_match:
            continue
        
        texture_id = texture_match.group(1)
        texture_path = ext_resources.get(texture_id)
        
        if not texture_path:
            continue
        
        # 提取modulate颜色
        modulate_match = re.search(r'modulate\s*=\s*Color\(([\d.,\s]+)\)', node_content)
        modulate = None
        if modulate_match:
            color_values = [float(v.strip()) for v in modulate_match.group(1).split(',')]
            if len(color_values) >= 3:
                modulate = (color_values[0], color_values[1], color_values[2])
            elif len(color_values) >= 4:
                modulate = (color_values[0], color_values[1], color_values[2])
        
        # 提取节点在场景树中的路径（用于确定顺序）
        parent_match = re.search(r'parent="(.+?)"', node_content)
        parent_path = parent_match.group(1) if parent_match else "."
        
        # 提取位置信息用于排序
        order_match = re.search(r'Layer(\d+)', node_name)
        order = int(order_match.group(1)) if order_match else 0
        
        layers.append({
            'name': node_name,
            'path': texture_path,
            'modulate': modulate,
            'parent': parent_path,
            'order': order
        })
    
    # 按order排序
    layers.sort(key=lambda x: x['order'])
    
    return layers


def apply_modulate(image, modulate_color):
    """
    对图片应用modulate颜色（乘法混合）
    Godot的modulate是逐通道乘法
    """
    if modulate_color is None:
        return image
    
    # 确保图片是RGBA模式
    if image.mode != 'RGBA':
        image = image.convert('RGBA')
    
    # 创建modulate颜色层
    r, g, b = modulate_color
    width, height = image.size
    
    # 创建纯色层
    modulate_layer = Image.new('RGBA', (width, height), 
                                (int(r * 255), int(g * 255), int(b * 255), 255))
    
    # 使用multiply混合模式
    result = ImageChops.multiply(image, modulate_layer)
    
    return result


def composite_layers(layers, project_root):
    """
    合成所有图层
    """
    if not layers:
        print("错误: 没有找到任何图层")
        return None
    
    # 加载第一个图层作为基础
    base_path = project_root / layers[0]['path']
    if not base_path.exists():
        print(project_root)
        print(layers[0]['path'])
        print(f"错误: 找不到图层文件: {base_path}")
        return None
    
    base_image = Image.open(base_path).convert('RGBA')
    result = base_image
    
    print(f"加载基础图层: {layers[0]['path']}")
    
    # 依次叠加其他图层
    for i, layer in enumerate(layers[1:], 1):
        layer_path = project_root / layer['path']
        if not layer_path.exists():
            print(f"警告: 找不到图层文件: {layer_path}，跳过")
            continue
        
        layer_image = Image.open(layer_path).convert('RGBA')
        
        # 确保尺寸一致
        if layer_image.size != result.size:
            layer_image = layer_image.resize(result.size, Image.Resampling.LANCZOS)
        
        # 应用modulate颜色
        if layer['modulate']:
            layer_image = apply_modulate(layer_image, layer['modulate'])
            print(f"应用modulate到 {layer['name']}: {layer['modulate']}")
        
        # 使用alpha通道合成
        result = Image.alpha_composite(result, layer_image)
        print(f"叠加图层 {i}: {layer['path']}")
    
    return result


def resize_and_save(image, output_path, size=(24, 24)):
    """
    缩放图片并保存
    """
    # 使用LANCZOS重采样获得更好的缩小质量
    resized = image.resize(size, Image.Resampling.LANCZOS)
    
    # 确保输出目录存在
    output_path.parent.mkdir(parents=True, exist_ok=True)
    
    # 保存为PNG（保持透明度）
    resized.save(output_path, 'PNG')
    print(f"已保存: {output_path} ({size[0]}x{size[1]})")
    
    return resized


def find_tscn_files(project_root, search_dirs=None):
    """
    在项目根目录下查找所有.tscn文件
    """
    tscn_files = []
    
    if search_dirs is None:
        search_dirs = ['IndomitableSpire2/scenes', 'IndomitableSpire2', '.']
    
    for search_dir in search_dirs:
        dir_path = project_root / search_dir
        if dir_path.exists():
            for tscn in dir_path.rglob('*.tscn'):
                tscn_files.append(tscn)
    
    return tscn_files


def main():
    """
    主函数
    """
    print("=" * 60)
    print("Godot场景图层提取工具")
    print("=" * 60)
    
    # 1. 查找项目根目录
    project_root = Path("E:\\IndomitableSpire2_Workspace\\IndomitableSpire2")
    print(f"项目根目录: {project_root}")
    
    # 2. 指定要处理的场景文件（根据用户提供的格式）
    # 可以修改为自动查找或命令行参数
    tscn_file = project_root / "IndomitableSpire2/scenes/vfx/energy/indomitable_energy_counter.tscn"
    
    # 如果指定的文件不存在，尝试查找
    if not tscn_file.exists():
        print(f"指定的场景文件不存在: {tscn_file}")
        print("正在搜索包含'energy_counter'的场景文件...")
        
        tscn_files = find_tscn_files(project_root)
        for f in tscn_files:
            if 'energy_counter' in str(f).lower() or 'energycounter' in str(f).lower():
                tscn_file = f
                print(f"找到: {tscn_file}")
                break
        
        if not tscn_file.exists():
            print("错误: 未找到合适的场景文件")
            print("请手动指定场景文件路径")
            return
    
    # 3. 解析场景文件
    print(f"\n解析场景文件: {tscn_file}")
    layers = parse_tscn_file(tscn_file)
    
    if not layers:
        print("错误: 未能从场景文件中解析出图层")
        return
    
    print(f"找到 {len(layers)} 个图层:")
    for layer in layers:
        mod_str = f" modulate={layer['modulate']}" if layer['modulate'] else ""
        print(f"  - {layer['name']}: {layer['path']}{mod_str}")
    
    # 4. 合成图层
    print("\n合成图层...")
    composite_image = composite_layers(layers, project_root)
    
    if composite_image is None:
        print("错误: 图层合成失败")
        return
    
    # 5. 缩放并保存
    output_dir = project_root / "IndomitableSpire2/images/packed/sprite_fonts"
    output_file = output_dir / "indomitable_energy_icon.png"
    
    print(f"\n缩放并保存到: {output_file}")
    resize_and_save(composite_image, output_file, size=(24, 24))
    
    # 6. 可选：也保存原始尺寸版本
    original_output = output_dir / "indomitable_energy_icon_original.png"
    composite_image.save(original_output, 'PNG')
    print(f"原始尺寸版本: {original_output}")
    
    print("\n" + "=" * 60)
    print("处理完成!")
    print("=" * 60)


if __name__ == "__main__":
    main()