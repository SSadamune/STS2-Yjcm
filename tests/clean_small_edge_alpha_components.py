from __future__ import annotations

import argparse
from collections import deque
from pathlib import Path

from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser(
        description="Remove tiny disconnected alpha components that touch an image edge."
    )
    parser.add_argument("image", type=Path)
    parser.add_argument("--max-pixels", type=int, default=64)
    args = parser.parse_args()

    image = Image.open(args.image).convert("RGBA")
    width, height = image.size
    pixels = image.load()
    occupied = {(x, y) for y in range(height) for x in range(width) if pixels[x, y][3] > 0}
    visited: set[tuple[int, int]] = set()
    removed: list[tuple[int, tuple[int, int, int, int]]] = []

    for seed in occupied:
        if seed in visited:
            continue
        queue = deque([seed])
        visited.add(seed)
        component: list[tuple[int, int]] = []
        touches_edge = False

        while queue:
            x, y = queue.popleft()
            component.append((x, y))
            touches_edge |= x == 0 or y == 0 or x == width - 1 or y == height - 1
            for nx in range(max(0, x - 1), min(width, x + 2)):
                for ny in range(max(0, y - 1), min(height, y + 2)):
                    point = (nx, ny)
                    if point in occupied and point not in visited:
                        visited.add(point)
                        queue.append(point)

        if touches_edge and len(component) <= args.max_pixels:
            xs = [point[0] for point in component]
            ys = [point[1] for point in component]
            bbox = (min(xs), min(ys), max(xs) + 1, max(ys) + 1)
            for x, y in component:
                pixels[x, y] = (0, 0, 0, 0)
            removed.append((len(component), bbox))

    image.save(args.image)
    print(f"removed={removed}")
    print(f"alpha_bbox={image.getchannel('A').getbbox()}")


if __name__ == "__main__":
    main()
