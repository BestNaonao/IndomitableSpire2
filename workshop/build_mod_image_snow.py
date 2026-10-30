"""Rebuild the seamless 420px liquid-glitter snow WebP (Pillow + NumPy).

The illustrated base is an imagegen edit; this script only animates particles
and encodes/validates the requested delivery format. The original PNG is not
modified. Every trajectory is periodic over the chosen 5–8-second loop.
"""
from __future__ import annotations

import argparse
import hashlib
import io
import json
import math
import random
import struct
import time
from dataclasses import dataclass
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFilter, _webp

ROOT = Path(__file__).resolve().parents[1]
BASE_PATH = ROOT / "workshop/animation_assets/tashkent_snow_base.png"
DEFAULT_OUTPUT = ROOT / "TashkentSpire2/mod_image_snow.png"  # Existing project filename; payload is animated WebP.
BUILD = ROOT / "workshop/.build/mod_image_snow"
SIZE = 420
DURATION_MS = 5000
TAU = 2 * math.pi


@dataclass(frozen=True)
class Flake:
    x: float
    y: float
    radius: float
    alpha: float
    phase: float
    drift: float
    turns: int
    style: int


def flakes(count: int = 26) -> list[Flake]:
    rng = random.Random(19400103)
    result = []
    far_count = count // 2
    near_count = round(count / 6)
    near_start = count - near_count
    for i in range(count):
        near = i >= near_start
        middle = far_count <= i < near_start
        radius = rng.uniform(9.0, 12.0) if near else rng.uniform(3.0, 5.0) if middle else rng.uniform(0.9, 1.5)
        x = rng.random() * SIZE
        if near:
            # Keep the large flakes distributed across the whole illustration;
            # the previous random seed put two at x≈10, mostly clipped away.
            x = (i - near_start + 0.45 + 0.1 * x / SIZE) * SIZE / near_count
        result.append(Flake(
            x, rng.random() * SIZE, radius,
            rng.uniform(225, 255) if near else rng.uniform(180, 230) if middle else rng.uniform(140, 195),
            rng.random() * TAU, rng.uniform(13, 31) if near else rng.uniform(8, 20),
            2 if i in (near_start + 1, count - 2) else 1, 2 if near else 1 if middle else 0,
        ))
    return result


class SnowLoop:
    def __init__(self, stable_lossy: bool = True, quality: int = 90, count: int = 26) -> None:
        self.base = Image.open(BASE_PATH).convert("RGBA").resize((SIZE, SIZE), Image.Resampling.LANCZOS)
        self.original_base = self.base.copy()
        self.stable_lossy = stable_lossy
        self.base_bitstream = None
        if stable_lossy:
            compressed = io.BytesIO()
            self.base.convert("RGB").save(compressed, format="WEBP", quality=quality, method=6)
            self.base_bitstream = compressed.getvalue()
            self.base = Image.open(io.BytesIO(self.base_bitstream)).convert("RGBA")
        self.particles = flakes(count)
        self.scale = 2 if stable_lossy else 3

    def frame(self, seconds: float) -> Image.Image:
        # Evaluating at exactly T produces precisely frame(0); the exported
        # sequence excludes that duplicate endpoint, preserving constant motion.
        u = (seconds % (DURATION_MS / 1000)) / (DURATION_MS / 1000)
        layer = Image.new("RGBA", (SIZE * self.scale, SIZE * self.scale))
        draw = ImageDraw.Draw(layer)
        opacity = 1.0
        if self.stable_lossy:
            # Liquid-glitter particles softly settle/fade around the seam.
            # The first frame is the exact decoded lossy base, allowing it to
            # be stored once as VP8 while later patches preserve its pixels.
            edge_seconds = min(u, 1-u) * (DURATION_MS / 1000)
            fade = min(1.0, edge_seconds / 0.35)
            opacity = fade * fade * (3 - 2 * fade)
        for flake in self.particles:
            phase = TAU * u
            x = (flake.x + flake.drift * math.sin(phase + flake.phase)
                 + 4 * math.sin(2 * phase + flake.phase * 0.71)) % SIZE
            y = (flake.y + SIZE * flake.turns * u
                 + 5 * math.sin(phase + flake.phase)) % SIZE
            # A gentle reduction over the face keeps the expression readable.
            distance = ((x - 215) / (53 + flake.radius)) ** 2 + ((y - 103) / (45 + flake.radius)) ** 2
            face_fade = 0.18 + 0.82 * min(1.0, max(0.0, distance - 0.1))
            twinkle = 0.91 + 0.09 * math.cos(2 * phase + flake.phase)
            alpha = round(flake.alpha * face_fade * twinkle * opacity)
            # Gentle in-plane sway plus a full side-on/back/front tumble.
            # Signed projection visibly flips the irregular snow fragments.
            angle = flake.phase + 0.42 * math.sin(phase + flake.phase) + 0.1 * math.sin(2 * phase)
            squash = math.cos(phase + flake.phase)
            for dx in (-SIZE, 0, SIZE):
                for dy in (-SIZE, 0, SIZE):
                    cx, cy = x + dx, y + dy
                    margin = flake.radius + 2
                    if -margin <= cx <= SIZE + margin and -margin <= cy <= SIZE + margin:
                        self.draw_flake(draw, flake, cx, cy, angle, squash, alpha)
        # Soften the white interior/blue rim without adding a wide glow halo
        # or blurring the illustration. Clipping to the original particle
        # footprint also keeps the high-FPS increment frames compact.
        soft = layer.convert("RGBa").filter(ImageFilter.GaussianBlur(self.scale * 0.32)).convert("RGBA")
        soft.putalpha(ImageChops.darker(layer.getchannel("A"), soft.getchannel("A")))
        layer = soft.resize((SIZE, SIZE), Image.Resampling.BOX if self.stable_lossy else Image.Resampling.LANCZOS)
        return Image.alpha_composite(self.base, layer).convert("RGB")

    def draw_flake(self, draw: ImageDraw.ImageDraw, flake: Flake,
                   x: float, y: float, angle: float, squash: float, alpha: int) -> None:
        s = self.scale
        x, y, radius = x * s, y * s, flake.radius * s
        if flake.style == 0:
            draw.ellipse((x-radius, y-radius, x+radius, y+radius), fill=(224, 239, 255, alpha))
            return
        # Irregular six-armed flakes, rather than rigid geometric icons.
        # A cool, soft rim separates the bright interior from the white coat.
        shade = (75, 116, 184, int(alpha * 0.52))
        snow = (246, 251, 255, alpha)
        rays = 6
        segments = []
        for spoke in range(rays):
            a = angle + spoke * TAU / rays + 0.04 * math.sin(flake.phase + spoke * 2.3)
            arm_radius = radius * (0.91 + 0.09 * math.sin(spoke * 2.13 + flake.phase))
            vx, vy = math.cos(a) * arm_radius * squash, math.sin(a) * arm_radius
            segments.append((x, y, x+vx, y+vy))
            # A single loose branch tier keeps the large silhouettes readable
            # while avoiding the previous precise, lace-like crystal interior.
            tiers = (0.66,) if flake.style == 2 else ()
            for along in tiers:
                for side in (-1, 1):
                    branch = a + side * math.pi / 3
                    px, py = x + vx * along, y + vy * along
                    length = arm_radius * 0.28
                    segments.append((px, py, px - math.cos(branch)*length*squash,
                                     py - math.sin(branch)*length))
        for segment in segments:
            draw.line(segment, fill=shade, width=max(2, round(s * (1.6 if flake.style == 2 else 1.3))))
        for segment in segments:
            draw.line(segment, fill=snow, width=max(1, round(s * (0.9 if flake.style == 2 else 0.7))))
        core = radius * (0.21 if flake.style == 2 else 0.16)
        core_x = max(s * 0.25, core * abs(squash))
        draw.ellipse((x-core_x, y-core, x+core_x, y+core), fill=snow)


def riff_info(path: Path) -> dict:
    data = path.read_bytes()
    assert data[:4] == b"RIFF" and data[8:12] == b"WEBP"
    durations, lossy, lossless = [], 0, 0
    offset = 12
    while offset + 8 <= len(data):
        tag = data[offset:offset+4]
        length = struct.unpack_from("<I", data, offset+4)[0]
        if tag == b"ANMF":
            payload = data[offset+8:offset+8+length]
            durations.append(int.from_bytes(payload[12:15], "little"))
            inner = 16
            while inner + 8 <= len(payload):
                code = payload[inner:inner+4]
                count = struct.unpack_from("<I", payload, inner+4)[0]
                lossy += code == b"VP8 "
                lossless += code == b"VP8L"
                inner += 8 + count + (count & 1)
        offset += 8 + length + (length & 1)
    return {"frames": len(durations), "duration_ms": sum(durations),
            "frame_durations_ms": sorted(set(durations)),
            "lossy_frames": lossy, "lossless_frames": lossless}


def reuse_lossy_base(animation: bytes, static_webp: bytes) -> bytes:
    """Replace frame zero with VP8 yielding identical decoded canonical pixels.

    Animation was encoded losslessly from that exact decoded VP8 base. Reusing
    the original lossy bitstream preserves animation pixels and avoids encoding
    a large lossless copy of the detailed illustration as the first frame.
    """
    source = static_webp[12:]
    assert source[:4] == b"VP8 ", "Expected an opaque lossy static WebP"
    chunks, replaced = [], False
    offset = 12
    while offset + 8 <= len(animation):
        tag = animation[offset:offset+4]
        length = struct.unpack_from("<I", animation, offset+4)[0]
        end = offset + 8 + length + (length & 1)
        if tag == b"ANMF" and not replaced:
            header = animation[offset+8:offset+24]
            assert int.from_bytes(header[0:3], "little") == 0
            assert int.from_bytes(header[3:6], "little") == 0
            assert int.from_bytes(header[6:9], "little") + 1 == SIZE
            assert int.from_bytes(header[9:12], "little") + 1 == SIZE
            payload = header + source
            chunks.append(tag + struct.pack("<I", len(payload)) + payload + (b"\0" if len(payload) & 1 else b""))
            replaced = True
        else:
            chunks.append(animation[offset:end])
        offset = end
    assert replaced
    payload = b"WEBP" + b"".join(chunks)
    return b"RIFF" + struct.pack("<I", len(payload)) + payload


def encode(loop: SnowLoop, output: Path, fps: int, quality: int, mixed: bool, method: int) -> dict:
    count = round(fps * DURATION_MS / 1000)
    timestamps = [round(i * DURATION_MS / count) for i in range(count + 1)]
    durations = [b-a for a, b in zip(timestamps, timestamps[1:])]
    assert min(durations) > 10, "Keep frame durations out of browser-defined <=10ms behaviour"
    assert loop.frame(0).tobytes() == loop.frame(DURATION_MS / 1000).tobytes()
    print(f"Rendering {count} frames at {fps} FPS...", flush=True)
    frames = []
    for i in range(count):
        frames.append(loop.frame(i * DURATION_MS / 1000 / count))
        if i % 120 == 0:
            print(f"  frame {i}/{count}", flush=True)
    output.parent.mkdir(parents=True, exist_ok=True)
    print(f"Encoding quality={quality}, mixed={mixed}...", flush=True)
    start = time.monotonic()
    # Same libwebp encoder as Pillow.save_all, with progress reports for long
    # high-FPS trials. Minimize-size mode suppresses unnecessary keyframes.
    encoder = _webp.WebPAnimEncoder((SIZE, SIZE), 0, 0, True, 9, 17,
                                   mixed and not loop.stable_lossy, False)
    for i, frame in enumerate(frames):
        encoder.add(frame.convert("RGBA").getim(), timestamps[i], loop.stable_lossy, quality, 100, method)
        if i % 30 == 0:
            print(f"  encoded {i}/{count}, {time.monotonic()-start:.1f}s", flush=True)
    encoder.add(None, DURATION_MS, loop.stable_lossy, quality, 100, 0)
    encoded = encoder.assemble("", "", "")
    if loop.stable_lossy:
        encoded = reuse_lossy_base(encoded, loop.base_bitstream)
    if output.resolve() == DEFAULT_OUTPUT.resolve() and len(encoded) >= 1_000_000:
        raise ValueError("The final delivery exceeds the strict 1,000,000-byte limit; existing output was not replaced")
    output.write_bytes(encoded)
    metrics = {"width": SIZE, "height": SIZE, "requested_fps": fps,
               "quality": quality, "mixed": mixed, "method": method, "bytes": output.stat().st_size,
               "particles": len(loop.particles), "stable_lossy_base": loop.stable_lossy,
               "encoding_seconds": round(time.monotonic()-start, 2), **riff_info(output)}
    decoded = Image.open(output)
    assert decoded.size == (SIZE, SIZE) and decoded.info.get("loop") == 0
    assert metrics["duration_ms"] == DURATION_MS
    errors, face_errors, steps, unique = [], [], [], set()
    first_decoded = previous_decoded = None
    for i in range(decoded.n_frames):
        decoded.seek(i)
        rgb = np.asarray(decoded.convert("RGB"), dtype=np.float32)
        source = np.asarray(frames[i], dtype=np.float32)
        unique.add(hashlib.sha256(rgb.astype(np.uint8).tobytes()).digest())
        if i % max(1, count // 24) == 0:
            errors.append(float(np.mean((rgb-source)**2)))
            face_errors.append(float(np.mean((rgb[58:147, 169:262]-source[58:147, 169:262])**2)))
        if first_decoded is None:
            first_decoded = rgb
        if previous_decoded is not None:
            steps.append(float(np.mean(np.abs(rgb-previous_decoded))))
        previous_decoded = rgb
    seam = float(np.mean(np.abs(first_decoded - previous_decoded)))
    mean_error = max(1e-8, float(np.mean(errors)))
    mean_face_error = max(1e-8, float(np.mean(face_errors)))
    metrics.update({"decoded_unique_frames": len(unique),
                    "effective_fps": decoded.n_frames / (DURATION_MS/1000),
                    "psnr_db": round(10 * math.log10(255**2/mean_error), 2),
                    "face_psnr_db": round(10 * math.log10(255**2/mean_face_error), 2),
                    "loop_source_endpoint_identical": True,
                    "mean_adjacent_frame_difference": round(float(np.mean(steps)), 5),
                    "loop_seam_difference": round(seam, 5),
                    "seam_to_median_step_ratio": round(seam/max(1e-8, float(np.median(steps))), 3)})
    if loop.stable_lossy:
        base = np.asarray(loop.base.convert("RGB"), dtype=np.float32)
        original = np.asarray(loop.original_base.convert("RGB"), dtype=np.float32)
        base_error = float(np.mean((base-original)**2))
        face_error = float(np.mean((base[58:147, 169:262]-original[58:147, 169:262])**2))
        metrics["base_psnr_db"] = round(10 * math.log10(255**2/base_error), 2)
        metrics["base_face_psnr_db"] = round(10 * math.log10(255**2/face_error), 2)
    report = output.with_suffix(".metrics.json")
    report.write_text(json.dumps(metrics, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(json.dumps(metrics, ensure_ascii=False), flush=True)
    return metrics


def contact_sheet(loop: SnowLoop, path: Path) -> None:
    sheet = Image.new("RGB", (SIZE*3, SIZE*2))
    for i in range(6):
        sheet.paste(loop.frame(float(i)), ((i % 3)*SIZE, (i // 3)*SIZE))
    path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(path)


def main() -> None:
    global DURATION_MS
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--fps", type=int, default=55)
    parser.add_argument("--quality", type=int, default=90)
    parser.add_argument("--mixed", action="store_true")
    parser.add_argument("--method", type=int, choices=range(7), default=4)
    parser.add_argument("--stable-lossy", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--duration", type=float, default=5.0)
    parser.add_argument("--particles", type=int, default=26)
    parser.add_argument("--output", type=Path, default=DEFAULT_OUTPUT)
    parser.add_argument("--preview-only", action="store_true")
    parser.add_argument("--validate-existing", type=Path)
    options = parser.parse_args()
    assert 5 <= options.duration <= 8
    assert 20 <= options.particles <= 64
    DURATION_MS = round(options.duration * 1000)
    loop = SnowLoop(options.stable_lossy, options.quality, options.particles)
    if options.validate_existing is not None:
        validate_existing(loop, options.validate_existing, options.fps)
        return
    BUILD.mkdir(parents=True, exist_ok=True)
    contact_sheet(loop, BUILD / "contact_sheet.png")
    loop.frame(0).save(BUILD / "preview.png")
    if not options.preview_only:
        encode(loop, options.output, options.fps, options.quality, options.mixed, options.method)


def validate_existing(loop: SnowLoop, path: Path, fps: int) -> None:
    info = riff_info(path)
    assert info["frames"] == round(fps * DURATION_MS / 1000)
    assert info["duration_ms"] == DURATION_MS
    assert info["lossy_frames"] == 1 and info["lossless_frames"] == info["frames"]-1
    assert path.stat().st_size < 1_000_000
    assert loop.frame(0).tobytes() == loop.frame(DURATION_MS/1000).tobytes()
    image = Image.open(path)
    assert image.size == (SIZE, SIZE) and image.info.get("loop") == 0
    unique, timestamp = set(), 0
    for i in range(image.n_frames):
        image.seek(i)
        actual = image.convert("RGB")
        source_index = round(timestamp * fps / 1000)
        expected = loop.frame(source_index / fps)
        assert actual.tobytes() == expected.tobytes(), f"Decoded pixel mismatch at frame {i}"
        unique.add(hashlib.sha256(actual.tobytes()).digest())
        timestamp += image.info["duration"]
    assert len(unique) == image.n_frames, "Do not inflate FPS using duplicate frames"
    assert timestamp == DURATION_MS
    print(json.dumps({"validation": "PASS", "file": str(path), "bytes": path.stat().st_size,
                      "pixel_exact_to_canonical_lossy_base_and_particles": True,
                      "source_endpoints_identical": True, "unique_frames": len(unique), **info}), flush=True)


if __name__ == "__main__":
    main()
