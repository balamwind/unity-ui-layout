# 화면 스펙 JSON 형식

화면 하나당 `Assets/UI_Generated/Specs/<screen>.json` 파일 하나. 배치도(render_layout.py)와 프리팹(UISpecBuilder.cs)이 같은 파일을 읽으므로, 배치를 고칠 때는 항상 이 JSON만 고친다. C# 코드를 화면별로 따로 쓰지 않는다.

전체 예시: `references/spec-example.json`(가로 화면 HUD, 레이아웃 그룹·앵커 사용), `references/spec-example-popup.json`(Dim + 중앙 창 + 세로 버튼 목록).

## 화면 필드

| 필드 | 설명 |
|---|---|
| screen | 화면 이름. 프리팹 파일명이 된다 (UI_HUD) |
| refWidth, refHeight | Canvas Scaler 기준 해상도 |
| match | Match Width Or Height 값 (0~1) |
| sortingOrder | Canvas 정렬 순서 |
| previewSafeArea | 배치도·캡처에서 흉내 낼 노치 영역 {left, right, top, bottom}, 기준 해상도 단위. 모바일 세로 예시: top 132, bottom 102. PC는 전부 0 |
| tmpFont | TMP 모드에서 쓸 TMP_FontAsset 경로 (한글 텍스트가 있으면 필수). 레거시 모드면 무시 |
| elements | 요소 배열. **부모가 자식보다 먼저 나와야 한다** |

## 요소 필드

| 필드 | 기본값 | 설명 |
|---|---|---|
| name | (필수) | 오브젝트 이름. 화면 안에서 유일해야 함. 이름 규칙은 unity-ui-rules.md |
| parent | SafeArea | `Root`(노치 영역까지 덮음), `SafeArea`, 또는 앞에 나온 요소 이름 |
| type | image | group, image, text, button, dim |
| anchor | Center | TopLeft, TopCenter, TopRight, MiddleLeft, Center, MiddleRight, BottomLeft, BottomCenter, BottomRight, StretchAll, StretchTop, StretchBottom, StretchLeft, StretchRight, StretchHorizontal, StretchVertical |
| x, y | 0 | anchoredPosition. 오른쪽·위쪽이 +. TopRight 앵커면 보통 x, y가 음수 |
| w, h | 0 | 크기. Stretch 축은 여백 차이(0이면 부모에 꽉 참), 고정 축은 실제 크기 |
| role | Panel | 임시 색 구분: Background, Panel, Button, Icon, Bar |
| sprite | "" | 스프라이트 경로 (Assets/...). 비우면 TEMP_ 임시 이미지 |
| spriteName | "" | 스프라이트 시트일 때 서브 스프라이트 이름 |
| text | "" | text의 내용, button의 라벨 (비우면 라벨 없는 아이콘 버튼) |
| fontSize | 40 | 글자 크기 |
| align | Center | TopLeft, Top, TopRight, Left, Center, Right, BottomLeft, Bottom, BottomRight |
| layout | "" | vertical, horizontal, grid. 지정하면 이 요소가 레이아웃 부모가 된다 |
| childAlign | 기본값 | UpperLeft … LowerRight. 기본: vertical=UpperCenter, horizontal=MiddleLeft, grid=UpperCenter |
| spacing, padding | 0 | 레이아웃 간격, 안쪽 여백 |
| cellW, cellH | 100 | grid 셀 크기 |

## 타입별 규칙

- **dim**: 부모 전체를 덮는 반투명 검정, 클릭 차단. anchor·x·y·w·h 무시. 팝업 화면에서 `parent: "Root"`로 가장 먼저 둔다.
- **레이아웃 부모의 자식**: anchor·x·y는 무시되고 w·h가 LayoutElement preferred 크기로 들어간다.
- **button**: 이미지 + Button + (text가 있으면) 자식 Txt_Label.
- **group**: 빈 RectTransform. 영역 묶음이나 레이아웃 부모로 쓴다.

## 흔한 실수

- 자식을 부모보다 먼저 적음 → 부모를 못 찾아 생성에서 빠진다
- StretchTop에 w를 1080으로 적음 → 폭이 두 배가 된다 (Stretch 축은 여백 차이라 0이어야 함)
- 전체 배경을 SafeArea 안에 둠 → 노치 부분이 비어 보인다. 배경은 `parent: "Root"`
