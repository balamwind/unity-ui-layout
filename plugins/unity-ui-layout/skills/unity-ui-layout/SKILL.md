---
name: unity-ui-layout
description: 게임 기획서를 읽고 컨셉을 파악한 뒤, 레퍼런스 게임(Game UI Database 링크)을 유저와 확인하고, 화면별 스펙 JSON → 배치도 이미지로 배치를 확인받은 다음, Unity UGUI(TextMeshPro 또는 레거시 Text)로 타이틀·메뉴·HUD·팝업 등 전체 UI 프리팹을 생성하고 스크린샷으로 검수한 뒤, 유저가 지정한 씬에 배치까지 하는 스킬. 유저가 "UI 배치해줘", "UI 잡아줘", "HUD 만들어줘", "기획서 보고 UI 만들어줘", "타이틀/메인메뉴/설정/결과창 화면 만들어줘", "UI 레이아웃", "UI 배치도"처럼 Unity 프로젝트의 UI 화면 구성·배치를 요청하면, "스킬"이라는 말이 없어도 반드시 이 스킬을 사용할 것. UI 기능 로직(버튼 이벤트 연결, 데이터 바인딩)만 요청하는 경우에는 사용하지 않는다.
license: MIT
---

# Unity UI Layout

기획서 → 컨셉 확인 → 화면 목록 → 레퍼런스 확인 → 배치도 확인 → 에셋 매칭 → 프리팹 생성 → 스크린샷 검수 → 씬 배치 순서로 진행한다.
확인 단계에서 유저 답을 받기 전에는 다음 단계로 넘어가지 않는다. UI는 취향이 강하게 갈리는 작업이라, 중간 확인 없이 끝까지 만들면 대부분 다시 만들게 된다.

유저에게 말할 때 UI 구성을 그린 그림은 **"배치도"**라고 부른다. "목업", "와이어프레임" 같은 용어는 쓰지 않는다.

## 범위

- 대상: UGUI, 타이틀·메뉴·설정·HUD·일시정지·결과창·팝업 등 전체 UI 화면
- 산출물: 화면별 스펙 JSON, 배치도 SVG, UI 프리팹, 검수 스크린샷, 지정한 씬에 배치된 프리팹 인스턴스
- 범위 밖: 버튼 이벤트 연결, 데이터 바인딩, 애니메이션, 화면 전환 로직. 오브젝트 이름만 명확하게 지어서 나중에 연결하기 쉽게 한다.

## 폴더 구성

모든 생성물은 `Assets/UI_Generated/` 아래에만 둔다. 기존 프리팹은 수정하지 않는다. 기존 씬은 8단계에서 유저가 승인한 씬에만, 백업을 남긴 뒤 에디터 코드로 프리팹 인스턴스를 추가한다. `.unity`·`.prefab` YAML은 읽기만 하고 직접 편집하지 않는다 (GUID·fileID 참조가 쉽게 깨진다).

```
Assets/UI_Generated/
├── Editor/        이 스킬의 assets/Editor/*.cs 복사본
├── Scripts/       SafeArea.cs
├── Specs/         화면별 스펙 JSON (배치의 유일한 원본)
├── Prefabs/       생성된 프리팹 (재생성 시 덮어씀)
├── placement.json 씬 배치 계획 (8단계)
└── Output~/       Unity가 임포트하지 않는 폴더
    ├── Layouts/       배치도 SVG
    ├── Screenshots/   검수 스크린샷
    ├── SceneBackups/  씬 배치 전 원본 씬 백업
    └── report.txt     생성·캡처·배치 리포트
```

## 0단계: 프로젝트 확인

1. `git status`로 커밋 안 된 변경이 많으면 먼저 커밋할지 묻는다.
2. **TextMeshPro 확인**: 버전은 따지지 않고 "쓸 수 있는 상태인지"만 본다.
   - `Assets/` 아래에 `TMP Settings.asset`이 있으면 (`find Assets -name "TMP Settings.asset"`) 사용 가능 → TMP 모드로 진행.
   - 없으면 유저에게 묻는다: "TextMeshPro가 활성화되어 있지 않아요. 활성화해서 쓸까요, 레거시 Text로 만들까요?"
     - 활성화 → `Window > TextMeshPro > Import TMP Essential Resources` 실행을 요청한다. 메뉴가 없으면 Package Manager에서 TextMeshPro 패키지 설치가 먼저 필요하다고 안내한다. 완료 후 다시 확인한다.
     - 레거시 → 레거시 모드로 진행.
   - 모드는 파일로 결정된다: TMP 모드면 `UIBuilderTMP.cs`를 복사하고, 레거시 모드면 복사하지 않는다 (이미 있으면 유저 확인 후 삭제). 나머지 코드는 TMP에 의존하지 않아서 TMP가 없는 프로젝트에서도 컴파일된다.
3. **한글 폰트 (TMP 모드만)**: TMP 기본 폰트에는 한글이 없어 네모로 나온다. 화면에 한글 텍스트가 들어가면 프로젝트의 TMP 폰트 에셋을 찾아 (`grep -rl "m_FaceInfo" Assets --include="*.asset"`) 어느 것을 쓸지 묻고, 스펙의 `tmpFont`에 경로를 넣는다. 없으면 유저에게 Font Asset Creator로 만들지, 영어 텍스트로 둘지 묻는다. 레거시 Text는 OS 폰트로 대체되어 한글이 나온다.
4. 스킬의 `assets/Editor/*.cs`(TMP 모드가 아니면 UIBuilderTMP.cs 제외)를 `Assets/UI_Generated/Editor/`로, `assets/Scripts/SafeArea.cs`를 `Assets/UI_Generated/Scripts/`로 복사한다. 이미 있으면 덮어쓰기 전에 차이를 확인한다.

## 1단계: 기획서 읽고 컨셉 확인

기획서 경로를 모르면 묻는다. 기획서를 읽고 아래 항목을 채운 컨셉 요약을 만든다. 기획서에 없는 항목은 "(추정)"을 붙인다.

| 항목 | 예시 |
|---|---|
| 장르 / 한 줄 설명 | 탑다운 서바이벌 로그라이크 |
| 플랫폼 · 화면 방향 | 모바일 세로 |
| 입력 방식 | 터치 (가상 조이스틱) |
| 분위기 · 아트 톤 | 다크 판타지, 픽셀아트 |
| HUD 밀도 | 최소형 / 보통 / 정보 많음 |
| 기준 해상도 · Match | 1080x1920, Match 0 (references/unity-ui-rules.md) |

요약을 보여주고 맞는지, 고칠 부분이 있는지 묻는다. 정정하면 반영해서 다시 보여준다.

## 2단계: 화면 목록 확정

기획서에서 필요한 화면을 뽑는다. 화면 종류별 일반 구성은 `references/screen-catalog.md`를 참고한다. 기획서에 없지만 대부분 게임에 필요한 화면(설정, 일시정지, 확인 팝업 등)은 "(추가 제안)"으로 표시하고 확인받는다.

화면이 많으면 그룹으로 나눈다: 아웃게임(타이틀·메인메뉴·설정 등) / 인게임(HUD·일시정지) / 결과·팝업. 이후 단계는 그룹 단위로 진행한다.

## 3단계: 레퍼런스 게임 확인

그룹마다 레퍼런스 후보 2~3개를 골라, 유저가 링크를 직접 열어보고 고르게 한다. 장르, 화면 방향, HUD 밀도가 컨셉과 맞는 게임으로 고른다.

**링크 찾기**
- WebSearch로 `gameuidatabase <게임 이름>`을 검색해 Game UI Database의 게임 페이지(`gameData.php?id=...`)를 찾는다. 안 나오면 영문 정식 명칭, 부제 포함 이름 등으로 바꿔 다시 검색한다.
- 검색 결과에 실제로 나온 URL만 쓴다. URL을 추측해서 만들지 않는다.
- 링크를 끝내 못 찾은 게임은 후보에서 빼고, 링크가 확인되는 다른 게임으로 바꾼다. 유저가 직접 보고 고르는 단계라 링크 없는 후보는 판단 재료가 되지 못한다.
- Game UI Database의 스크린샷은 다운로드하거나 분석하지 않는다. 사이트 약관이 콘텐츠의 AI·머신러닝 용도 사용을 금지하기 때문이다. 링크만 전달해 유저가 직접 보게 한다.

**제시 방법**
- 후보와 링크는 반드시 **일반 채팅 메시지**로 보여준다. 선택지 버튼이 뜨는 질문 도구(AskUserQuestion 등) 안에서는 링크를 클릭할 수 없으므로, 링크를 질문 도구에 넣지 않는다.
- 링크는 클릭할 수 있게 마크다운 링크 `[게임 이름](URL)` 형식으로 쓴다.
- 후보마다: 링크, 고른 이유 한 줄, 이 게임 UI의 대략적 구조 (일반 지식 기반이므로 "대략"이라고 밝히고 링크로 확인해달라고 한다).
- 메시지 끝에 "링크를 열어보고 마음에 드는 게임을 알려주세요. 화면별로 섞어서 골라도 됩니다."처럼 채팅으로 답해달라고 요청하고, 유저 답이 올 때까지 기다린다. 질문 도구로 선택을 받고 싶다면 링크가 담긴 채팅 메시지를 먼저 보낸 뒤, 선택지에는 게임 이름만 넣는다.

예시:
```
인게임(HUD) 레퍼런스 후보예요. 링크를 열어 UI를 직접 확인해 보세요.

1. [Vampire Survivors](https://www.gameuidatabase.com/gameData.php?id=...)
   이유: 세로 화면 로그라이크, HUD가 상단에만 있어 전투 화면이 넓음
   대략적 구조: 상단 경험치바 전체 폭, 좌상단 획득 무기 아이콘 줄, 상단 중앙 타이머

2. [...](...)

마음에 드는 게임을 알려주세요. 화면별로 섞어서 골라도 됩니다.
```
(예시의 URL은 형식 설명용이다. 실제로는 검색으로 확인한 URL만 쓴다.)

**유저 응답 처리**
- 하나 선택 → 4단계
- 조합 요청 (예: "HUD는 A, 인벤토리는 B") → 화면별로 레퍼런스를 나눠 기록
- 전부 거절 → 무엇이 안 맞았는지 묻고 새 후보를 같은 방식으로 제시. 3번 거절되면 원하는 게임 이름이나 직접 찍은 스크린샷을 요청한다.
- 유저가 직접 찍어 올린 스크린샷은 분석해도 된다.

## 4단계: 스펙 작성과 배치도 확인

1. 화면마다 `Assets/UI_Generated/Specs/<screen>.json`을 작성한다. 형식은 `references/spec-format.md`, 예시는 `references/spec-example.json`(가로 액션 게임 HUD)과 `references/spec-example-popup.json`(Dim + 팝업 창), 크기·앵커 기준은 `references/unity-ui-rules.md`를 따른다.
2. 배치도를 그린다:
   ```
   python <스킬경로>/scripts/render_layout.py Assets/UI_Generated/Specs/UI_HUD.json Assets/UI_Generated/Output~/Layouts/UI_HUD.svg --print-rects
   ```
   배치도는 요소를 실제 예상 크기의 색 박스로 그리고, 그룹은 점선, 노치 영역은 붉은 띠로 구역을 나눠 보여준다. 스크립트는 파이썬 표준 라이브러리만 쓴다.
3. `ERROR`가 나오면 스펙을 고쳐 다시 그린다. `WARN`(화면 밖, 버튼 겹침)은 의도된 것인지 판단해서 고치거나 유저에게 알린다.
4. 비율이 다른 기기에서 깨지는지도 그려본다: 세로면 `--size 1080x2400`(길쭉한 폰)과 `--size 1440x1920`(태블릿), 가로면 `--size 2400x1080`과 `--size 1440x1080`.
5. 유저에게 SVG 경로를 알려주고 브라우저로 열어 확인해달라고 한다 (Windows는 `start <경로>`, macOS는 `open <경로>`로 직접 열어줘도 된다). 요소 목록을 짧게 함께 적어준다.
6. 수정 요청이 오면 JSON만 고치고 배치도를 다시 그린다. 승인될 때까지 반복한다.

**파이썬이 없을 때**: 스펙을 쓴 뒤 Unity 메뉴 `Tools > UI Layout Gen > Capture Drafts (Specs Only)`를 실행해달라고 한다. 스프라이트 없이 임시 색으로만 렌더링한 PNG가 `Output~/Screenshots/*_draft_*.png`로 나오므로 그걸로 확인받는다.

## 5단계: 에셋 매칭

1. 스프라이트 목록 수집: `grep -rl "textureType: 8" Assets --include="*.meta"` (8 = Sprite). `Assets/UI_Generated/`는 제외.
2. 파일명·폴더명 키워드로 매칭 (btn/button, icon, hp/health, bar/gauge, frame/panel/window, bg/background, joystick 등).
3. 후보가 여러 개거나 애매하면 이미지 파일을 직접 열어 확인하고, 그래도 애매하면 후보를 모아 한 번에 묻는다. 확실한 매칭은 바로 적용한다.
4. 매칭된 경로를 스펙의 `sprite`(시트면 `spriteName`도)에 넣는다. 매칭이 없으면 비워둔다 → 생성 시 `TEMP_` 접두사가 붙은 역할별 단색 이미지가 된다.

매칭 결과표(요소 → 에셋 경로 또는 TEMP)를 보여준 뒤 생성으로 넘어간다.

## 6단계: 생성과 캡처

Claude Code는 에디터를 직접 조작할 수 없으므로 실행 방법을 묻는다.

- **에디터가 열려 있으면**: 컴파일이 끝난 뒤 `Tools > UI Layout Gen > Generate And Capture` 실행을 요청한다. 컴파일 에러가 나면 Console 내용을 붙여달라고 한다.
- **에디터가 닫혀 있고 Unity 실행 파일 경로를 알려주면**: 직접 실행한다. `-nographics`는 붙이지 않는다 (캡처에 GPU가 필요).
  ```
  "<Unity 경로>" -batchmode -quit -projectPath "<프로젝트>" -executeMethod UILayoutGen.UIGenMenu.GenerateAndCapture -logFile "<프로젝트>/Assets/UI_Generated/Output~/unity.log"
  ```

실행이 끝나면 `Assets/UI_Generated/Output~/report.txt`를 읽는다. 텍스트 모드, 저장된 프리팹, TEMP 목록, 경고·에러, 캡처 파일 경로가 들어 있다.

## 7단계: 스크린샷 검수

`Output~/Screenshots/`의 PNG(화면마다 기준 해상도 + 비율 다른 2종)를 직접 열어 보고, 배치도와 비교해 아래를 점검한다.

- 요소가 배치도 위치·크기와 다른 곳에 있는지 (앵커·피벗 실수)
- 화면 밖으로 나가거나 노치 영역을 침범한 요소
- 겹치는 버튼, 가려진 텍스트
- 텍스트가 박스를 넘치거나 잘리는지, 한글이 네모로 나오는지 (TMP 폰트 문제)
- 적용한 스프라이트가 늘어나 깨져 보이는지 (9-slice border 필요 여부)
- 길쭉한 폰·태블릿 비율에서 가운데가 비거나 요소끼리 붙는지

문제가 있으면 스펙 JSON을 고치고 배치도 확인 → 재생성 → 재캡처를 반복한다. 스크린샷이 비어 있거나 검게만 나오면 캡처 환경 문제일 수 있으니, 유저에게 프리팹을 씬에 올려 Game 뷰 스크린샷을 보내달라고 요청해 같은 방식으로 점검한다.

검수가 끝나면 8단계로 넘어간다.

## 8단계: 씬 배치

1. 씬 목록을 모은다: `find Assets -name "*.unity" -not -path "Assets/UI_Generated/*"`.
2. 화면별로 어느 씬에 넣을지 배치안을 만든다. 기본 활성 여부도 정한다: 타이틀·메뉴·HUD는 활성, 팝업·일시정지·레벨업·결과창은 비활성 (게임 로직에서 켜야 하므로).
3. 씬마다 현재 상태를 읽기 전용으로 확인한다 (씬 파일을 수정하지 않고 grep만 한다).
   - 이미 배치된 UI: 프리팹의 `.meta`에서 `guid:`를 읽어 씬 파일에 그 guid가 있는지 확인
   - EventSystem 유무: `m_FirstSelected:` 존재 여부
   - 기존 캔버스 수: `m_PixelPerfect:` 개수 (대략값)
4. **씬마다** 아래를 보여주고 확인받는다. 유저가 빼라고 한 씬·화면은 계획에서 뺀다.
   ```
   [Assets/Scenes/Game.unity]
   추가: UI_HUD (활성), UI_Pause (비활성), UI_Result (비활성)
   이미 있음: 없음
   EventSystem: 없음 → 추가 예정
   기존 캔버스: 2개 → 새 UI와 겹칠 수 있음, 기존 UI를 교체하는 건지 확인 필요
   ```
   기존 캔버스가 있으면 그게 옛 UI인지 물어본다. 기존 오브젝트를 지우거나 끄는 건 이 스킬이 하지 않는다. 유저에게 직접 정리해달라고 안내한다.
5. 승인된 내용만 `Assets/UI_Generated/placement.json`에 쓴다.
   ```json
   {
     "scenes": [
       { "scene": "Assets/Scenes/Game.unity",
         "screens": [ { "screen": "UI_HUD", "active": true }, { "screen": "UI_Pause", "active": false } ] }
     ]
   }
   ```
6. 실행: 에디터에서는 `Tools > UI Layout Gen > Apply To Scenes`, batchmode면 `-executeMethod UILayoutGen.UIApply.ApplyToScenes`. 에디터에서 실행하면 작업 중인 씬에 저장 안 된 변경이 있을 때 저장 여부를 먼저 묻고, 끝나면 원래 열려 있던 씬으로 돌아간다.
7. 동작 방식: 씬마다 원본을 `Output~/SceneBackups/`에 백업한 뒤 프리팹 인스턴스로 추가하고 저장한다. 같은 프리팹이 이미 있으면 중복으로 넣지 않고 건너뛴다. EventSystem이 없으면 추가한다 (새 Input System이 켜져 있으면 InputSystemUIInputModule 사용).
8. `report.txt`를 읽어 씬별 결과(추가, 건너뜀, EventSystem, 기존 캔버스, 백업 경로, 에러)를 확인한다.
9. 유저에게 해당 씬을 열어 Play로 확인해달라고 요청한다. 되돌리고 싶으면 백업 파일로 교체하거나 git으로 되돌리면 된다.

프리팹 인스턴스로 들어가기 때문에, 나중에 스펙을 고쳐 재생성하면 씬에 배치된 UI도 자동으로 갱신된다. 씬에서 인스턴스를 직접 수정한 부분(오버라이드)은 유지되지만, 재생성으로 사라진 오브젝트에 대한 수정은 없어진다.

## 최종 보고

```
## 생성 결과
- 텍스트 모드: TextMeshPro / Legacy Text
- 프리팹: Assets/UI_Generated/Prefabs/UI_Title.prefab 외 N개
- 적용된 에셋: N개
- 임시 이미지(TEMP_): 요소 이름 → 필요한 에셋 설명 (크기 포함)
- 검수: 확인한 해상도, 고친 문제, 남은 문제
- 씬 배치: 씬별 추가된 화면, 건너뛴 화면, 백업 경로
- 확인 필요: 기존 캔버스와 겹칠 수 있는 씬, 비활성으로 둔 화면 (게임 로직에서 켜야 함)
- 주의: Prefabs 폴더는 재생성 시 덮어써짐. 직접 수정할 프리팹은 다른 폴더로 옮긴 뒤 수정
```
