#!/usr/bin/env python3
"""
UI 스펙 JSON을 SVG 배치도로 그린다. 표준 라이브러리만 사용한다.
RectTransform 계산(앵커·피벗·sizeDelta)과 Vertical/Horizontal/Grid 레이아웃을
UIBuilder.cs와 같은 규칙으로 계산하므로, 배치도와 실제 프리팹의 배치가 일치한다.

사용법:
    python render_layout.py <spec.json> [출력.svg] [--size WxH] [--scale 0.5] [--print-rects]

--size   다른 화면비로 미리보기 (예: 1080x2400). 생략하면 기준 해상도.
--scale  SVG 표시 배율 (기본 0.4). 좌표 계산에는 영향 없음.
--print-rects  요소별 최종 사각형(기준 해상도 단위)을 출력. 화면 밖·겹침 점검용.
"""
import json
import math
import sys
from html import escape
from pathlib import Path

ANCHORS = {
    # name: (anchorMin, anchorMax, pivot)
    "TopLeft": ((0, 1), (0, 1), (0, 1)),
    "TopCenter": ((0.5, 1), (0.5, 1), (0.5, 1)),
    "TopRight": ((1, 1), (1, 1), (1, 1)),
    "MiddleLeft": ((0, 0.5), (0, 0.5), (0, 0.5)),
    "Center": ((0.5, 0.5), (0.5, 0.5), (0.5, 0.5)),
    "MiddleRight": ((1, 0.5), (1, 0.5), (1, 0.5)),
    "BottomLeft": ((0, 0), (0, 0), (0, 0)),
    "BottomCenter": ((0.5, 0), (0.5, 0), (0.5, 0)),
    "BottomRight": ((1, 0), (1, 0), (1, 0)),
    "StretchAll": ((0, 0), (1, 1), (0.5, 0.5)),
    "StretchTop": ((0, 1), (1, 1), (0.5, 1)),
    "StretchBottom": ((0, 0), (1, 0), (0.5, 0)),
    "StretchLeft": ((0, 0), (0, 1), (0, 0.5)),
    "StretchRight": ((1, 0), (1, 1), (1, 0.5)),
    "StretchHorizontal": ((0, 0.5), (1, 0.5), (0.5, 0.5)),
    "StretchVertical": ((0.5, 0), (0.5, 1), (0.5, 0.5)),
}

ROLE_COLORS = {
    "Background": "#2e2e33",
    "Panel": "#4d5c73",
    "Button": "#4080e6",
    "Icon": "#f29933",
    "Bar": "#4dcc66",
}
GROUP_STROKE = "#b0b0b0"
TEXT_FILL = "#ffffff"


class Rect:
    """Unity 좌표계 (원점 좌하단, y 위쪽)."""

    def __init__(self, x, y, w, h):
        self.x, self.y, self.w, self.h = x, y, w, h

    def __repr__(self):
        return f"({self.x:.0f}, {self.y:.0f}, {self.w:.0f}x{self.h:.0f})"


def rect_transform(parent: Rect, anchor: str, px, py, w, h):
    amin, amax, pivot = ANCHORS.get(anchor, ANCHORS["Center"])
    ax0 = parent.x + amin[0] * parent.w
    ay0 = parent.y + amin[1] * parent.h
    ax1 = parent.x + amax[0] * parent.w
    ay1 = parent.y + amax[1] * parent.h
    rw = (ax1 - ax0) + w
    rh = (ay1 - ay0) + h
    ref_x = ax0 + (ax1 - ax0) * pivot[0]
    ref_y = ay0 + (ay1 - ay0) * pivot[1]
    pos_x = ref_x + px
    pos_y = ref_y + py
    return Rect(pos_x - pivot[0] * rw, pos_y - pivot[1] * rh, rw, rh)


ALIGN = {
    # childAlign: (가로 비율 hx, 세로 비율 vy)  vy는 위쪽에서부터 0
    "UpperLeft": (0, 0), "UpperCenter": (0.5, 0), "UpperRight": (1, 0),
    "MiddleLeft": (0, 0.5), "MiddleCenter": (0.5, 0.5), "MiddleRight": (1, 0.5),
    "LowerLeft": (0, 1), "LowerCenter": (0.5, 1), "LowerRight": (1, 1),
}
DEFAULT_ALIGN = {"vertical": "UpperCenter", "horizontal": "MiddleLeft", "grid": "UpperCenter"}


def layout_children(parent: Rect, parent_spec, children):
    """UIBuilder와 동일: 자식 크기 = (w, h), 정렬은 childAlign (기본: vertical=UpperCenter, horizontal=MiddleLeft, grid=UpperCenter)."""
    kind = parent_spec.get("layout", "")
    pad = parent_spec.get("padding", 0)
    sp = parent_spec.get("spacing", 0)
    hx, vy = ALIGN.get(parent_spec.get("childAlign") or DEFAULT_ALIGN.get(kind, "UpperCenter"), (0.5, 0))
    ix, iy = parent.x + pad, parent.y + pad
    iw, ih = parent.w - 2 * pad, parent.h - 2 * pad
    out = []
    if kind == "vertical":
        total = sum(c.get("h", 0) for c in children) + sp * max(0, len(children) - 1)
        top = iy + ih - (ih - total) * vy
        for c in children:
            cw, ch = c.get("w", 0), c.get("h", 0)
            out.append(Rect(ix + (iw - cw) * hx, top - ch, cw, ch))
            top -= ch + sp
    elif kind == "horizontal":
        total = sum(c.get("w", 0) for c in children) + sp * max(0, len(children) - 1)
        left = ix + (iw - total) * hx
        for c in children:
            cw, ch = c.get("w", 0), c.get("h", 0)
            out.append(Rect(left, iy + ih - ch - (ih - ch) * vy, cw, ch))
            left += cw + sp
    elif kind == "grid":
        cw, ch = parent_spec.get("cellW", 100), parent_spec.get("cellH", 100)
        cols = max(1, int(math.floor((iw + sp) / (cw + sp))))
        n = len(children)
        used = min(cols, n) if n else 1
        rows = max(1, math.ceil(n / cols))
        block_w = used * cw + (used - 1) * sp
        block_h = rows * ch + (rows - 1) * sp
        left = ix + (iw - block_w) * hx
        top = iy + ih - (ih - block_h) * vy
        for i in range(n):
            r, col = divmod(i, cols)
            out.append(Rect(left + col * (cw + sp), top - (r + 1) * ch - r * sp, cw, ch))
    return out


def solve(spec, screen_w, screen_h):
    ref_w, ref_h = spec.get("refWidth", 1080), spec.get("refHeight", 1920)
    match = spec.get("match", 0)
    # CanvasScaler(MatchWidthOrHeight)와 같은 스케일 → 캔버스 논리 크기
    log_w = math.log2(screen_w / ref_w)
    log_h = math.log2(screen_h / ref_h)
    scale = 2 ** (log_w + (log_h - log_w) * match)
    cw, ch = screen_w / scale, screen_h / scale

    root = Rect(0, 0, cw, ch)
    ins = spec.get("previewSafeArea", {}) or {}
    safe = Rect(
        ins.get("left", 0),
        ins.get("bottom", 0),
        cw - ins.get("left", 0) - ins.get("right", 0),
        ch - ins.get("top", 0) - ins.get("bottom", 0),
    )
    rects = {"Root": root, "SafeArea": safe}
    specs = {"Root": {}, "SafeArea": {}}
    errors = []
    elements = spec.get("elements", [])

    # 레이아웃 부모의 자식은 한꺼번에 계산
    by_parent = {}
    for e in elements:
        by_parent.setdefault(e.get("parent", "SafeArea"), []).append(e)

    solved = []
    for e in elements:
        name = e.get("name")
        parent_name = e.get("parent", "SafeArea")
        if name in rects:
            errors.append(f"이름 중복: {name}")
            continue
        if parent_name not in rects:
            errors.append(f"{name}: 부모 '{parent_name}'가 없거나 자식보다 뒤에 있음")
            continue
        if e.get("anchor", "Center") not in ANCHORS and e.get("type") != "dim":
            errors.append(f"{name}: 알 수 없는 앵커 '{e.get('anchor')}'")
        parent_rect, parent_spec = rects[parent_name], specs[parent_name]
        if parent_spec.get("layout"):
            siblings = by_parent[parent_name]
            idx = siblings.index(e)
            r = layout_children(parent_rect, parent_spec, siblings)[idx]
        elif e.get("type") == "dim":
            r = Rect(parent_rect.x, parent_rect.y, parent_rect.w, parent_rect.h)
        else:
            r = rect_transform(parent_rect, e.get("anchor", "Center"), e.get("x", 0), e.get("y", 0),
                               e.get("w", 0), e.get("h", 0))
        rects[name] = r
        specs[name] = e
        solved.append((e, r))
    return (cw, ch), root, safe, solved, errors


def check(canvas_size, solved):
    """화면 밖으로 나간 요소, 형제끼리 겹치는 인터랙션 요소를 경고."""
    cw, ch = canvas_size
    warns = []
    for e, r in solved:
        if e.get("type") == "dim":
            continue
        if r.x < -0.5 or r.y < -0.5 or r.x + r.w > cw + 0.5 or r.y + r.h > ch + 0.5:
            warns.append(f"화면 밖: {e['name']} {r}")
        if r.w <= 0 or r.h <= 0:
            warns.append(f"크기 0 이하: {e['name']} {r}")
    buttons = [(e, r) for e, r in solved if e.get("type") == "button"]
    for i in range(len(buttons)):
        for j in range(i + 1, len(buttons)):
            (e1, a), (e2, b) = buttons[i], buttons[j]
            if a.x < b.x + b.w and b.x < a.x + a.w and a.y < b.y + b.h and b.y < a.y + a.h:
                warns.append(f"버튼 겹침: {e1['name']} / {e2['name']}")
    return warns


def to_svg(spec, canvas_size, safe, solved, disp_scale, title):
    cw, ch = canvas_size
    W, H = cw * disp_scale, ch * disp_scale

    def box(r):
        return r.x * disp_scale, (ch - r.y - r.h) * disp_scale, r.w * disp_scale, r.h * disp_scale

    fs = max(10, 22 * disp_scale * 2)
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{W:.0f}" height="{H + 40:.0f}" '
        f'viewBox="0 0 {W:.1f} {H + 40:.1f}" font-family="sans-serif">',
        f'<rect x="0" y="0" width="{W:.1f}" height="{H + 40:.1f}" fill="#0a0a0c"/>',
        f'<rect x="0" y="0" width="{W:.1f}" height="{H:.1f}" fill="#141417"/>',
    ]

    def fit(text, w, base):
        # 좁은 박스에서는 글자 크기를 줄여 박스 밖으로 넘치지 않게
        return max(6.0, min(base, w * 1.7 / max(1, len(text))))
    # 노치·홈바 영역 표시
    sx, sy, sw, sh = box(safe)
    if (sx, sy, sw, sh) != (0, 0, W, H):
        parts.append(f'<rect x="0" y="0" width="{W:.1f}" height="{H:.1f}" fill="#552222" opacity="0.5"/>')
        parts.append(f'<rect x="{sx:.1f}" y="{sy:.1f}" width="{sw:.1f}" height="{sh:.1f}" fill="#141417"/>')
    for e, r in solved:
        x, y, w, h = box(r)
        t = e.get("type", "image")
        label = escape(e.get("name", ""))
        if t == "group":
            parts.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="none" '
                         f'stroke="{GROUP_STROKE}" stroke-dasharray="6 4" stroke-width="1"/>')
            parts.append(f'<text x="{x + 4:.1f}" y="{y - 3:.1f}" font-size="{fit(label, w, fs * 0.7):.1f}" '
                         f'fill="{GROUP_STROKE}">{label}</text>')
        elif t == "dim":
            parts.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="#000" opacity="0.6"/>')
        elif t == "text":
            parts.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="none" '
                         f'stroke="#ffffff" stroke-opacity="0.35" stroke-width="1"/>')
            txt = escape(e.get("text", "") or label)
            tfs = max(8, e.get("fontSize", 40) * disp_scale)
            parts.append(f'<text x="{x + w / 2:.1f}" y="{y + h / 2:.1f}" font-size="{tfs:.1f}" fill="{TEXT_FILL}" '
                         f'text-anchor="middle" dominant-baseline="middle">{txt}</text>')
        else:
            color = ROLE_COLORS.get(e.get("role", "Panel"), ROLE_COLORS["Panel"])
            parts.append(f'<rect x="{x:.1f}" y="{y:.1f}" width="{w:.1f}" height="{h:.1f}" fill="{color}" '
                         f'stroke="#000" stroke-opacity="0.4" stroke-width="1" rx="3"/>')
            size_label = f'{e.get("w", 0):.0f}x{e.get("h", 0):.0f}'
            if t == "button":
                # 버튼: 가운데에 버튼 글자(없으면 이름), 여유가 있으면 크기
                inner = escape(e.get("text", "")) if e.get("text") else label
                ifs = fit(inner, w, fs * 0.8)
                parts.append(f'<text x="{x + w / 2:.1f}" y="{y + h / 2:.1f}" font-size="{ifs:.1f}" fill="#fff" '
                             f'text-anchor="middle" dominant-baseline="middle">{inner}</text>')
                if h > ifs * 2.6:
                    parts.append(
                        f'<text x="{x + w / 2:.1f}" y="{y + h / 2 + ifs * 1.2:.1f}" font-size="{ifs * 0.75:.1f}" '
                        f'fill="#fff" fill-opacity="0.7" text-anchor="middle" dominant-baseline="middle">{size_label}</text>')
            else:
                # 이미지·패널: 이름은 왼쪽 위 구석에 작게 (안에 겹쳐 놓인 글자·노브를 가리지 않도록)
                nfs = fit(label, w - 8, fs * 0.55)
                parts.append(f'<text x="{x + 4:.1f}" y="{y + nfs + 2:.1f}" font-size="{nfs:.1f}" fill="#fff" '
                             f'fill-opacity="0.85">{label}</text>')
                if h > nfs * 4:
                    parts.append(f'<text x="{x + 4:.1f}" y="{y + h - 4:.1f}" font-size="{nfs * 0.85:.1f}" '
                                 f'fill="#fff" fill-opacity="0.6">{size_label}</text>')
    parts.append(f'<text x="6" y="{H + 26:.1f}" font-size="16" fill="#888">{escape(title)}</text>')
    parts.append("</svg>")
    return "\n".join(parts)


def main(argv):
    if len(argv) < 2:
        print(__doc__)
        return 1
    spec_path = Path(argv[1])
    out_path = None
    size = None
    disp_scale = 0.4
    print_rects = False
    i = 2
    while i < len(argv):
        a = argv[i]
        if a == "--size":
            w, h = argv[i + 1].lower().split("x")
            size = (float(w), float(h))
            i += 2
        elif a == "--scale":
            disp_scale = float(argv[i + 1])
            i += 2
        elif a == "--print-rects":
            print_rects = True
            i += 1
        else:
            out_path = Path(a)
            i += 1

    spec = json.loads(spec_path.read_text(encoding="utf-8-sig"))
    screen = size or (spec.get("refWidth", 1080), spec.get("refHeight", 1920))
    canvas_size, root, safe, solved, errors = solve(spec, *screen)
    warns = check(canvas_size, solved)

    if out_path is None:
        out_path = spec_path.with_name(f'{spec.get("screen", spec_path.stem)}_{int(screen[0])}x{int(screen[1])}.svg')
    title = f'{spec.get("screen", "")}  {int(screen[0])}x{int(screen[1])}  (canvas {canvas_size[0]:.0f}x{canvas_size[1]:.0f})'
    out_path.parent.mkdir(parents=True, exist_ok=True)
    out_path.write_text(to_svg(spec, canvas_size, safe, solved, disp_scale, title), encoding="utf-8")

    print(f"SVG: {out_path}")
    if print_rects:
        for e, r in solved:
            print(f"  {e['name']:<24} {r}")
    for m in errors:
        print(f"ERROR: {m}")
    for m in warns:
        print(f"WARN: {m}")
    return 1 if errors else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
