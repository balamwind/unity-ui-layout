# Unity UI 배치 규칙

4단계 명세 작성과 6단계 코드 작성 시 이 규칙을 따른다. 유저가 다르게 요청하면 유저 요청이 우선이다.

## Canvas Scaler 기본값

모두 Scale With Screen Size, Match Width Or Height.

| 플랫폼·방향 | Reference Resolution | Match | 이유 |
|---|---|---|---|
| 모바일 세로 | 1080 x 1920 | 0 (Width) | 폰마다 세로 길이 차이가 커서 가로 기준이 안정적 |
| 모바일 가로 | 1920 x 1080 | 1 (Height) | 울트라와이드 폰 대응, 좌우 여백은 앵커로 흡수 |
| PC / 콘솔 | 1920 x 1080 | 0.5 | 16:9 중심, 16:10·21:9를 절충 |

1단계 컨셉 요약에 이 값을 넣고 유저 확인을 받는다.

## 계층 구조

```
UI_<화면> (Canvas, CanvasScaler, GraphicRaycaster)
├── Img_Bg            전체 배경 (StretchAll, SafeArea 밖 — 노치 영역까지 채움)
└── SafeArea          (StretchAll + SafeArea 컴포넌트)
    ├── Grp_Top
    ├── Grp_Middle
    └── Grp_Bottom
```

팝업은 이렇게 만든다.

```
UI_Popup_<이름> (Canvas, sortingOrder 100)
├── Img_Dim           StretchAll, 검정 alpha 0.6, raycastTarget true (뒤 클릭 차단)
└── SafeArea
    └── Panel_Window  Center
        ├── Txt_Title
        ├── Txt_Body
        └── Grp_Buttons (HorizontalLayout)
```

## Sorting Order

| 레이어 | sortingOrder |
|---|---|
| HUD | 0 |
| 메뉴·전체화면 UI | 10 |
| 팝업 | 100 |
| 시스템 (로딩, 토스트) | 200 |

## 앵커 규칙

- 화면 가장자리에 붙는 요소는 가장 가까운 모서리·변에 앵커를 건다. 가장자리 요소에 Center 앵커를 쓰면 화면비가 바뀔 때 떠다닌다.
- 가로로 꽉 차는 바(상단바, 하단 탭)는 StretchTop / StretchBottom.
- 목록·메뉴 버튼 묶음은 부모에 Vertical/HorizontalLayoutGroup을 쓰고 자식 위치는 레이아웃에 맡긴다.
- UIBuilder의 Stretch 앵커에서 size는 늘어나는 축은 여백 차이(0 = 꽉 참), 고정 축은 실제 크기다.

## 크기 기준 (1080 폭 기준)

- 터치 버튼 최소 120 x 120 (손가락 크기 약 9mm)
- 본문 텍스트 36~44, 제목 60~80, 작은 라벨 28 이상
- 화면 가장자리 여백 40
- PC는 마우스 기준이라 버튼 최소 64 x 64까지 허용

## 이름 규칙

| 접두사 | 용도 |
|---|---|
| UI_ | 화면 루트 (프리팹 이름과 동일) |
| Grp_ | 빈 그룹 |
| Panel_ | 배경 이미지가 있는 영역 |
| Img_ | 이미지 |
| Txt_ | TextMeshPro 텍스트 |
| Btn_ | 버튼 |
| TEMP_ | 임시 이미지 (다른 접두사 앞에 붙음: TEMP_Img_HpBar) |

## 기타

- 클릭이 필요 없는 Image와 Text는 raycastTarget을 끈다 (UIBuilder가 기본 처리).
- 테두리가 있는 스프라이트(border 설정됨)는 Image.Type을 Sliced로 쓴다 (UIBuilder가 자동 처리).

## 임시 색상표

UIBuilder.TempColor에 정의되어 있다. 역할별로 색을 통일해야 스크린샷만 봐도 무엇이 임시인지 구분된다.

| 이름 | 용도 | 색 |
|---|---|---|
| Background | 전체 배경 | 어두운 회색 |
| Panel | 창·패널 | 회청색 |
| Button | 버튼 | 파랑 |
| Icon | 아이콘 | 주황 |
| Bar | 게이지 | 초록 |
| Dim | 팝업 뒤 어둡게 | 검정 반투명 |
