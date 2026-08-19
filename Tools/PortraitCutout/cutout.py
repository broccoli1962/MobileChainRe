"""Cel-shaded portrait cutout.

Do not use rembg. Soft mattes leave smear/halos in hair.

Pipeline:
1. Generate the portrait on a flat magenta chroma backdrop (#FF00FF).
2. Run this script to flood-fill the backdrop from the image border.
3. Write a hard 0/255 alpha PNG.

Also rejects (or warns on) silhouettes that touch the top edge, which
means hair/head was cropped.
"""

from __future__ import annotations

import argparse
import sys
from pathlib import Path

import cv2
import numpy as np
from PIL import Image
from scipy import ndimage

CHROMA_HEX = "FF00FF"
CHROMA_RGB = np.array([255, 0, 255], dtype=np.float32)

# Generation contract for portraits. Keep in sync with this cutout.
PORTRAIT_PROMPT_PREFIX = f"""
Japanese manga color illustration portrait in the art style of 葉佐乃 (Kazutake Hazano):
soft even lineart, rounded gentle facial contours, small mouth, large kind eyes with
simple highlights, subtle blush, delicate warm coloring, hard-edged two-tone cel shading.
Sharp clean edges. No blur, no bloom, no glow, no particles, no airbrush smear,
no chromatic aberration, no 3D, no photorealism.

COMPOSITION: close-up bust, head and shoulders only. The entire head and ALL hair
must sit fully inside the frame. Leave at least 12% empty margin on top, left, and right.
Hair must not touch or exit any edge. Do not crop the crown, bangs, or side hair.

BACKGROUND: perfectly flat solid magenta chroma-key #{CHROMA_HEX}, uniform, no gradient,
no shadows on the backdrop, no scenery.

Original character. No text, no UI, no watermark, no halo.
""".strip()

NEAR_CHROMA = 52.0
NEAR_BG_EXPAND = 78.0
NEAR_CHROMA_PUNCH = 90.0
MIN_TOP_MARGIN = 16
MIN_SIDE_MARGIN = 4


def _sample_bg(rgb: np.ndarray) -> np.ndarray:
    h, w = rgb.shape[:2]
    n = max(8, min(h, w) // 32)
    patches = [
        rgb[:n, :n],
        rgb[:n, w - n :],
        rgb[h - n :, :n],
        rgb[h - n :, w - n :],
    ]
    samples = np.concatenate([p.reshape(-1, 3) for p in patches], axis=0).astype(np.float32)
    sampled = np.median(samples, axis=0)
    # Prefer true chroma if corners drifted only slightly.
    if np.linalg.norm(sampled - CHROMA_RGB) < 80:
        return CHROMA_RGB.copy()
    return sampled


def _color_dist(rgb: np.ndarray, color: np.ndarray) -> np.ndarray:
    return np.linalg.norm(rgb.astype(np.float32) - color.reshape(1, 1, 3), axis=2)


def cutout_rgba(src: Image.Image) -> tuple[Image.Image, dict]:
    rgb = np.array(src.convert("RGB"), dtype=np.uint8)
    h, w = rgb.shape[:2]
    bg = _sample_bg(rgb)
    dist = _color_dist(rgb, bg)

    chroma_dist = _color_dist(rgb, CHROMA_RGB)
    is_chroma = chroma_dist < NEAR_CHROMA_PUNCH
    similar = (dist < NEAR_CHROMA) | is_chroma
    # Seed from top/left/right and bottom corners only.
    # Bust shoulders often touch the bottom edge; flooding the whole
    # bottom row would eat white/cream clothing.
    border = np.zeros((h, w), dtype=bool)
    border[0, :] = True
    border[:, 0] = True
    border[:, -1] = True
    corner_n = max(8, min(h, w) // 32)
    border[-1, :corner_n] = True
    border[-1, w - corner_n :] = True

    structure = np.ones((3, 3), dtype=bool)
    bg_mask = ndimage.binary_propagation(border & similar, mask=similar, structure=structure)
    expand = (dist < NEAR_BG_EXPAND) | is_chroma
    bg_mask = ndimage.binary_propagation(bg_mask, mask=expand, structure=structure)
    # Enclosed hair gaps painted with chroma/backdrop must stay transparent.
    bg_mask = bg_mask | is_chroma | (dist < NEAR_CHROMA)

    char = ~bg_mask
    labeled, count = ndimage.label(char)
    if count > 0:
        sizes = ndimage.sum(char, labeled, index=range(1, count + 1))
        keep = int(np.argmax(sizes)) + 1
        char = labeled == keep

    alpha = np.where(char, 255, 0).astype(np.uint8)

    # Magenta despill on the silhouette edge only. Do not soften alpha.
    edge = cv2.dilate((~char).astype(np.uint8), np.ones((3, 3), np.uint8), iterations=2).astype(bool) & char
    out_rgb = rgb.astype(np.float32)
    r, g, b = out_rgb[..., 0], out_rgb[..., 1], out_rgb[..., 2]
    magenta = ((r + b) * 0.5) - g
    spill = edge & (magenta > 16)
    pull = np.clip((magenta - 16) * 0.7, 0, 120)
    out_rgb[..., 0] = np.where(spill, np.clip(r - pull, 0, 255), r)
    out_rgb[..., 2] = np.where(spill, np.clip(b - pull, 0, 255), b)

    rgba = np.dstack([out_rgb.astype(np.uint8), alpha])
    ys, xs = np.where(alpha > 0)
    report = {
        "ok": True,
        "warnings": [],
        "bg": [int(v) for v in bg],
        "opaque": int(alpha.size and ys.size),
    }
    if ys.size == 0:
        report["ok"] = False
        report["warnings"].append("empty_cutout")
        return Image.fromarray(rgba, "RGBA"), report

    top, bottom, left, right = int(ys.min()), int(ys.max()), int(xs.min()), int(xs.max())
    report["bbox"] = [left, top, right, bottom]
    if top < MIN_TOP_MARGIN:
        report["ok"] = False
        report["warnings"].append(f"head_cropped_top margin={top}")
    if left < MIN_SIDE_MARGIN or right > w - 1 - MIN_SIDE_MARGIN:
        report["warnings"].append(f"side_touch left={left} right={w - 1 - right}")
    return Image.fromarray(rgba, "RGBA"), report


def process_file(src: Path, dst: Path) -> dict:
    img = Image.open(src)
    out, report = cutout_rgba(img)
    dst.parent.mkdir(parents=True, exist_ok=True)
    out.save(dst, "PNG")
    report["src"] = str(src)
    report["dst"] = str(dst)
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description="Hard-alpha chroma cutout for unit portraits.")
    parser.add_argument("--input", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--glob", default="Unit_*.png")
    args = parser.parse_args()

    files = sorted(args.input.glob(args.glob))
    if not files:
        print(f"no files matching {args.glob} in {args.input}", file=sys.stderr)
        return 1

    failed = 0
    for src in files:
        report = process_file(src, args.output / src.name)
        status = "OK" if report["ok"] else "FAIL"
        warn = ",".join(report["warnings"]) if report["warnings"] else "-"
        print(f"{status} {src.name} warnings={warn}")
        if not report["ok"]:
            failed += 1
    return 1 if failed else 0


if __name__ == "__main__":
    raise SystemExit(main())
