# Unity UI Layout

[English](README.md) | **한국어**

게임 기획서를 읽고 Unity UGUI 화면(타이틀, 메뉴, HUD, 팝업)을 만들어 주는 [Claude](https://claude.ai) 스킬입니다. 모든 단계에서 확인을 받은 뒤에 다음으로 넘어가는 방식이라, 마음에 안 드는 UI가 통째로 만들어지는 일이 없습니다.

## 진행 순서

1. **컨셉 확인** – 기획서를 읽고 장르, 플랫폼, 화면 방향, 입력 방식, 아트 톤, HUD 밀도를 정리해서 맞는지 묻습니다.
2. **화면 목록** – 필요한 화면을 뽑아 확인받습니다.
3. **레퍼런스 게임** – [Game UI Database](https://www.gameuidatabase.com/) 링크를 채팅으로 보여줍니다. 직접 열어보고 고르면 됩니다.
4. **배치도** – 화면마다 JSON 스펙을 쓰고 실제 비율로 그립니다 (색 박스, 구역, 노치 영역, 다른 화면비). 승인되거나 수정 요청이 없을 때까지 반복합니다.
5. **에셋 매칭** – 프로젝트에서 맞는 스프라이트를 찾아 연결합니다. 없으면 역할별 색의 `TEMP_` 임시 이미지를 넣습니다.
6. **프리팹 생성** – 에디터 스크립트가 `Assets/UI_Generated/` 아래에 프리팹을 만듭니다.
7. **스크린샷 검수** – 프리팹을 해상도 3종으로 렌더링해 승인된 배치도와 비교하고, 문제가 있으면 고칩니다.
8. **씬 배치** – 승인한 씬에만 프리팹을 배치합니다. 저장 전에 백업을 남깁니다.

## 예시

스펙: [`references/spec-example.json`](plugins/unity-ui-layout/skills/unity-ui-layout/references/spec-example.json) (횡스크롤 액션 게임 HUD, 모바일 가로). 붉은 띠는 노치 영역입니다.

![HUD 배치도](docs/example-hud.png)

Dim 배경과 세로 버튼 목록이 있는 팝업: [`spec-example-popup.json`](plugins/unity-ui-layout/skills/unity-ui-layout/references/spec-example-popup.json)

![일시정지 배치도](docs/example-pause.png)

## 설치

**Claude Code** (권장) – 터미널에서 명령어 두 줄:

```bash
claude plugin marketplace add balamwind/unity-ui-layout
claude plugin install unity-ui-layout@unity-ui-layout
```

Claude Code 세션 안에서는 앞에 `/`를 붙여 같은 명령어를 쓸 수 있습니다 (`/plugin marketplace add ...`). 나중에 업데이트할 때는 `/plugin marketplace update unity-ui-layout`.

**Claude.ai / Claude Desktop** – [최신 릴리스](https://github.com/balamwind/unity-ui-layout/releases/latest)에서 `unity-ui-layout.skill`을 받아 Claude의 스킬 설정에서 업로드하세요.

**수동 설치 (Claude Code)** – 최신 릴리스에서 `unity-ui-layout.skill`을 받은 뒤:

```bash
unzip unity-ui-layout.skill -d ~/.claude/skills/
```

## 사용법

평소 말투로 요청하면 됩니다.

- `기획서 보고 UI 배치해줘`
- `횡스크롤 액션 게임 HUD 만들어줘`
- `Lay out the UI from docs/GDD.md`

## 요구 사항

- UGUI를 쓰는 Unity 프로젝트. TextMeshPro는 선택이며, 없으면 활성화할지 레거시 `Text`로 만들지 물어봅니다.
- 배치도를 그리기 위한 Python 3 (표준 라이브러리만 사용). 없으면 Unity에서 임시 색 초안 이미지를 대신 렌더링합니다.
- 프로젝트 폴더에 접근할 수 있는 Claude Code, 그리고 Unity 에디터가 열려 있거나 batch 모드용 Unity 실행 파일 경로.

## 생성되는 폴더

```
Assets/UI_Generated/
├── Editor/         생성·스크린샷·씬 배치 스크립트
├── Scripts/        SafeArea.cs
├── Specs/          화면별 JSON (배치의 유일한 원본)
├── Prefabs/        생성된 프리팹 (재생성 시 덮어씀)
├── placement.json  승인된 씬 배치 계획
└── Output~/        배치도, 스크린샷, 씬 백업, report.txt
```

## 설계 방침

- **스펙 하나, 결과물 둘.** 배치도와 프리팹을 같은 JSON, 같은 앵커·레이아웃 규칙으로 만들어서, 승인한 그림 그대로 만들어집니다.
- **레퍼런스는 링크만.** Game UI Database 약관이 콘텐츠의 AI·머신러닝 용도 사용을 금지하므로, 스크린샷은 다운로드하거나 분석하지 않고 링크만 전달합니다.
- **승인 전에는 기존 씬을 건드리지 않습니다.** 저장 전에 씬을 백업하고, YAML을 직접 편집하지 않으며, 기존 캔버스는 알려주기만 하고 지우지 않습니다.
- **범위 밖:** 버튼 이벤트 연결, 데이터 바인딩, 애니메이션, 화면 전환.

## 현재 상태

배치도 단계까지는 실제 Claude 대화에서 동작을 확인했습니다. Unity 에디터 스크립트(프리팹 생성, 스크린샷 캡처, 씬 배치)는 여러 Unity 버전에서 검증되지 않았습니다. 문제가 생기면 Unity 버전과 Console 로그 또는 `Output~/report.txt`를 첨부해서 이슈로 남겨 주세요.

## 라이선스

[MIT](LICENSE) (라이선스 본문은 영어 원문만 효력이 있습니다.)

Anthropic, Unity Technologies, Game UI Database와 관련이 없으며 이들의 보증을 받은 것이 아닙니다. Unity와 TextMeshPro는 Unity Technologies의 상표입니다.
