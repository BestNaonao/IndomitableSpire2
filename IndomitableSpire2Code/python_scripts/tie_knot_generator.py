import math
from time import sleep

from PIL import Image, ImageDraw, ImageFilter, ImageColor
import cairo
import numpy as np

# --- 全局参数设置 ---

IMAGE_SIZE = 1024
CENTER = (IMAGE_SIZE / 2, IMAGE_SIZE / 2)

# 颜色设置 (使用十六进制色号)
BLACK_DIAMOND_COLOR = "#181c28"  # 底部黑色菱形色号
CHAMPAGNE_COLOR = "#CBAD93"  # 香槟金色辉光与核心色号
WHITE_CROSS_STAR_COLOR = "#FFFFFF"  # 白色十字星色号

# 底部黑色菱形参数
# 中心到上下左右的半轴长 (a)，中心到左上...的半轴长 (b)
BLACK_DIAMOND_LENGTH_A = 480  # 主轴半长
BLACK_DIAMOND_LENGTH_B = 480  # 次轴半长

# 白色十字星参数
# 中心到上下左右的半轴长 (长轴) 和中心到左上...的半轴长 (短轴)
WHITE_CROSS_STAR_MAIN_ARM_LENGTH = 450  # 上下左右半轴长
WHITE_CROSS_STAR_CORNER_ARM_LENGTH = 120  # 对角线方向半轴长

# 香槟金花瓣参数
CHAMPAGNE_PETAL_LENGTH = 324  # 花瓣长度 (根据实际尺寸调整)
CHAMPAGNE_PETAL_WIDTH = 36  # 花瓣最宽处半宽
CHAMPAGNE_PETAL_CURVATURE = 0.4  # 弯曲程度 (0.3-0.6 效果较好)

# 全局过渡参数 (可以调整整体效果)
GLOBAL_FEATHERING_PIXELS = 1  # 全局羽化像素数


# --- 辅助函数 ---

def hex_to_rgba(hex_color, alpha=255):
    """将十六进制色号转换为 RGBA 元组."""
    color = ImageColor.getrgb(hex_color)
    return color + (alpha,)


def generate_arc_diamond_path(ctx, length_a, length_b, curvature):
    """使用贝塞尔曲线在 Pycairo 上绘制弧边菱形路径."""
    # 顶点坐标
    a = (0, -length_a)
    b = (length_b, 0)
    c = (0, length_a)
    d = (-length_b, 0)

    # 贝塞尔曲线控制点偏移
    # curvature 控制控制点相对于顶点的位置。
    # curvature=0 => 控制点在顶点 (尖锐); curvature=1 => 控制点在边中点 (更圆)。
    offset_a = length_a * curvature
    offset_b = length_b * curvature

    # 移动到 A 点，绘制 A-B-C-D-A 各边
    ctx.move_to(*a)
    ctx.curve_to(offset_b, -length_a, length_b, -offset_a, *b)
    ctx.curve_to(length_b, offset_a, offset_b, length_a, *c)
    ctx.curve_to(-offset_b, length_a, -length_b, offset_a, *d)
    ctx.curve_to(-length_b, -offset_a, -offset_b, -length_a, *a)

    ctx.close_path()


def create_mask_from_cairo_path(path_func, *args):
    """运行 Pycairo 路径函数，并将结果转换为 Pillow 蒙版."""
    surface = cairo.ImageSurface(cairo.FORMAT_ARGB32, IMAGE_SIZE, IMAGE_SIZE)
    ctx = cairo.Context(surface)
    ctx.translate(CENTER[0], CENTER[1])  # 将原点移动到中心
    path_func(ctx, *args)
    ctx.set_source_rgba(1, 1, 1, 1)  # 白色填充
    ctx.fill()
    surface.flush()
    mask_pil = Image.frombuffer("RGBA", (IMAGE_SIZE, IMAGE_SIZE), surface.get_data(), "raw", "RGBA", 0, 1)
    mask_pil = mask_pil.getchannel("A")  # 提取 A 通道作为蒙版
    return mask_pil

# ==================== 修改1: 新的花瓣路径函数 ====================
def generate_petal_path(ctx, length, width, curvature):
    """
    创建从中心点出发向外弯曲的花瓣形状
    length: 花瓣长度
    width: 花瓣最宽处的半宽
    curvature: 弯曲程度 (0-1, 越大越弯曲)
    """
    # 起点在中心 (0, 0)
    center = (0, 0)
    # 花瓣尖端
    tip = (0, -length)
    # 花瓣两侧最宽点
    side_left = (-width, -length * 0.6)
    side_right = (width, -length * 0.6)

    # 控制点偏移（决定弯曲程度）
    ctrl_offset_x = width * curvature
    ctrl_offset_y = length * curvature * 0.5

    ctx.move_to(*center)

    # 中心 -> 右侧 -> 尖端 (右半花瓣)
    ctx.curve_to(
        ctrl_offset_x, -length * 0.3,  # 控制点1
                       width + ctrl_offset_x, -length * 0.5,  # 控制点2
        *side_right
    )
    ctx.curve_to(
        width + ctrl_offset_x, -length * 0.7,
        ctrl_offset_x, -length * 0.9,
        *tip
    )

    # 尖端 -> 左侧 -> 中心 (左半花瓣)
    ctx.curve_to(
        -ctrl_offset_x, -length * 0.9,
                        -width - ctrl_offset_x, -length * 0.7,
        *side_left
    )
    ctx.curve_to(
        -width - ctrl_offset_x, -length * 0.5,
        -ctrl_offset_x, -length * 0.3,
        *center
    )

    ctx.close_path()

# --- 主图像生成 ---
# ==================== 修改2: 主生成函数中的调整 ====================
def generate_image(black_color, champagne_color, cross_star_color):
    final_img = Image.new("RGBA", (IMAGE_SIZE, IMAGE_SIZE), (0, 0, 0, 0))
    draw = ImageDraw.Draw(final_img)

    # --- 黑色菱形背景 (保持不变) ---
    diamond_base = Image.new("RGBA", (IMAGE_SIZE, IMAGE_SIZE), hex_to_rgba(black_color))
    diamond_mask = Image.new("L", (IMAGE_SIZE, IMAGE_SIZE), 0)
    diamond_draw = ImageDraw.Draw(diamond_mask)
    diamond_vertices = [
        (CENTER[0], CENTER[1] - BLACK_DIAMOND_LENGTH_A),
        (CENTER[0] + BLACK_DIAMOND_LENGTH_B, CENTER[1]),
        (CENTER[0], CENTER[1] + BLACK_DIAMOND_LENGTH_A),
        (CENTER[0] - BLACK_DIAMOND_LENGTH_B, CENTER[1])
    ]
    diamond_draw.polygon(diamond_vertices, fill=255)
    feathered_diamond_mask = diamond_mask.filter(ImageFilter.GaussianBlur(GLOBAL_FEATHERING_PIXELS))
    final_img.paste(diamond_base, (0, 0), feathered_diamond_mask)

    # --- 白色十字星 (保持不变) ---
    cross_star_mask = Image.new("L", (IMAGE_SIZE, IMAGE_SIZE), 0)
    cross_draw = ImageDraw.Draw(cross_star_mask)
    vertices_main = [
        (CENTER[0], CENTER[1] - WHITE_CROSS_STAR_MAIN_ARM_LENGTH),
        (CENTER[0] + WHITE_CROSS_STAR_MAIN_ARM_LENGTH, CENTER[1]),
        (CENTER[0], CENTER[1] + WHITE_CROSS_STAR_MAIN_ARM_LENGTH),
        (CENTER[0] - WHITE_CROSS_STAR_MAIN_ARM_LENGTH, CENTER[1])
    ]
    length_diagonal = WHITE_CROSS_STAR_CORNER_ARM_LENGTH
    vertices_diagonal = [
        (CENTER[0] + length_diagonal * math.cos(math.pi / 4), CENTER[1] - length_diagonal * math.sin(math.pi / 4)),
        (CENTER[0] + length_diagonal * math.cos(math.pi / 4), CENTER[1] + length_diagonal * math.sin(math.pi / 4)),
        (CENTER[0] - length_diagonal * math.cos(math.pi / 4), CENTER[1] + length_diagonal * math.sin(math.pi / 4)),
        (CENTER[0] - length_diagonal * math.cos(math.pi / 4), CENTER[1] - length_diagonal * math.sin(math.pi / 4))
    ]
    final_vertices = [
        vertices_main[0], vertices_diagonal[0],
        vertices_main[1], vertices_diagonal[1],
        vertices_main[2], vertices_diagonal[2],
        vertices_main[3], vertices_diagonal[3]
    ]
    cross_draw.polygon(final_vertices, fill=255)
    feathered_cross_mask = cross_star_mask.filter(ImageFilter.GaussianBlur(GLOBAL_FEATHERING_PIXELS))
    white_cross_base = Image.new("RGBA", (IMAGE_SIZE, IMAGE_SIZE), hex_to_rgba(cross_star_color))
    final_img.paste(white_cross_base, (0, 0), feathered_cross_mask)

    # --- 修改3: 移除中心弧边菱形，改用四片花瓣 ---
    # 原代码中的 arc_diamond 部分删除或注释掉

    # --- 修改4: 生成四片弧形花瓣 ---
    petal_base_mask = create_mask_from_cairo_path(
        generate_petal_path,
        CHAMPAGNE_PETAL_LENGTH,  # 新参数：花瓣长度
        CHAMPAGNE_PETAL_WIDTH,  # 新参数：花瓣宽度
        CHAMPAGNE_PETAL_CURVATURE  # 新参数：弯曲程度 (建议 0.3-0.6)
    )

    # 旋转花瓣到四个对角方向
    petal_mask = Image.new("L", (IMAGE_SIZE, IMAGE_SIZE), 0)
    for angle in [45, 135, 225, 315]:  # 四个对角方向
        rotated_petal = petal_base_mask.rotate(angle, expand=False, center=CENTER)
        # 使用最大值合并，避免重叠区域变暗
        petal_mask = Image.fromarray(
            np.maximum(np.array(petal_mask), np.array(rotated_petal))
        )

    # 羽化花瓣边缘
    feathered_petal_mask = petal_mask.filter(ImageFilter.GaussianBlur(GLOBAL_FEATHERING_PIXELS))
    champagne_petal_base = Image.new("RGBA", (IMAGE_SIZE, IMAGE_SIZE), hex_to_rgba(champagne_color))
    final_img.paste(champagne_petal_base, (0, 0), feathered_petal_mask)

    return final_img


sleep(0.5)
# 生成与保存图像
image_pro = generate_image(BLACK_DIAMOND_COLOR, CHAMPAGNE_COLOR, WHITE_CROSS_STAR_COLOR)

# 为了获得最佳质量和颜色，我们保存为具有嵌入色号信息的 PNG
filename = "indomitable_tie_knot.png"
image_pro.save(filename)
print(f"图像已保存为 {filename}")