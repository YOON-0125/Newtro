# 🎮 보물상자 & 메인메뉴 상점 시스템 Unity 설정 가이드

## 📋 목차
1. [시스템 매니저 설정](#1-시스템-매니저-설정)
2. [보상 애니메이션 UI 설정](#2-보상-애니메이션-ui-설정)
3. [메인메뉴 UI 설정](#3-메인메뉴-ui-설정)
4. [업그레이드 상점 UI 설정](#4-업그레이드-상점-ui-설정)
5. [유물 가챠 UI 설정](#5-유물-가챠-ui-설정)
6. [보관함 UI 설정](#6-보관함-ui-설정)
7. [프리팹 설정](#7-프리팹-설정)
8. [게임 씬 설정](#8-게임-씬-설정)
9. [테스트 및 디버깅](#9-테스트-및-디버깅)

---

## 1. 시스템 매니저 설정

### 1.1 시스템 매니저 GameObject 생성

1. **SystemManagers GameObject 생성**:
   - Hierarchy에서 우클릭 → Create Empty
   - 이름을 `SystemManagers`로 변경

2. **각 시스템 스크립트 추가**:
   - `SystemManagers` GameObject에 다음 스크립트들을 Add Component:
     - `GoldSystem`
     - `PermanentUpgradeSystem`
     - `ArtifactGachaSystem`
     - `UpgradeEffectApplier`

### 1.2 GoldSystem 설정
```
Inspector 설정:
- Enable Debug Logs: ✓
- Initial Gold: 0 (또는 원하는 초기값)
```

### 1.3 PermanentUpgradeSystem 설정
```
Inspector 설정:
- Available Upgrades:
  
  [0] 튼튼한 체력:
    - Type: MaxHealth
    - Display Name: "튼튼한 체력"
    - Description: "최대 체력을 1하트 증가시킵니다"
    - Cost: 2500
    - Effect Value: 4
    - Is Percentage: false
    - Max Level: 7
    - Cost Multiplier: 1.3

  [1] 강력한 타격:
    - Type: Damage
    - Display Name: "강력한 타격"
    - Description: "모든 무기 데미지를 10% 증가시킵니다"
    - Cost: 2500
    - Effect Value: 0.1
    - Is Percentage: true
    - Max Level: 10
    - Cost Multiplier: 1.25

  [2] 신속한 발걸음:
    - Type: MoveSpeed
    - Display Name: "신속한 발걸음"
    - Description: "이동속도를 10% 증가시킵니다"
    - Cost: 2500
    - Effect Value: 0.1
    - Is Percentage: true
    - Max Level: 8
    - Cost Multiplier: 1.2

  [3] 풍부한 지식:
    - Type: ExpMultiplier
    - Display Name: "풍부한 지식"
    - Description: "경험치 획득량을 15% 증가시킵니다"
    - Cost: 2500
    - Effect Value: 0.15
    - Is Percentage: true
    - Max Level: 10
    - Cost Multiplier: 1.4

- Enable Debug Logs: ✓
```

### 1.4 ArtifactGachaSystem 설정
```
Inspector 설정:

가챠 설정:
- Gacha Cost: 5000
- Common Chance: 60
- Rare Chance: 25
- Epic Chance: 12
- Legendary Chance: 3

유물 데이터베이스 (자동으로 채워짐, 필요시 수정):
- Common Artifacts: [기본 3개]
- Rare Artifacts: [기본 3개]
- Epic Artifacts: [기본 2개] 
- Legendary Artifacts: [기본 2개]

디버그:
- Enable Debug Logs: ✓
- Show Probability Check: ✓ (필요시)
```

### 1.5 UpgradeEffectApplier 설정
```
Inspector 설정:
- Enable Debug Logs: ✓
```

---

## 2. 보상 애니메이션 UI 설정

### 2.1 RewardAnimationUI 생성

1. **Canvas 생성**:
   - Hierarchy → 우클릭 → UI → Canvas
   - 이름: `RewardAnimationUI`
   - Canvas Component:
     - Render Mode: `Screen Space - Overlay`
     - Sort Order: `10`
   - Canvas Scaler:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Match: `0.5`

2. **CanvasGroup 추가**:
   - RewardAnimationUI에 Add Component → Canvas Group

3. **BackgroundPanel 생성**:
   - RewardAnimationUI → 우클릭 → UI → Image
   - 이름: `BackgroundPanel`
   - Image 설정:
     - Source Image: None
     - Color: `Black (0, 0, 0, 200)`
   - RectTransform:
     - Anchor: `Top Center`
     - Pos Y: `-100`
     - Width: `600`, Height: `80`

4. **RewardText 생성**:
   - BackgroundPanel → 우클릭 → UI → Text - TextMeshPro
   - 이름: `RewardText`
   - TextMeshProUGUI 설정:
     - Text: (비워두기)
     - Font Size: `24`
     - Color: `White`
     - Alignment: `Center Middle`
     - Auto Size: `Best Fit` (Min: 18, Max: 30)
   - RectTransform:
     - Anchor: `Stretch Stretch`
     - Margins: Left 20, Right 20, Top 10, Bottom 10

### 2.2 RewardAnimationUI 스크립트 연결

1. **스크립트 첨부**:
   - RewardAnimationUI GameObject에 `RewardAnimationUI` 스크립트 Add Component

2. **Inspector 설정**:
```
UI 참조:
- Canvas Group: (자동 연결)
- Background Panel: BackgroundPanel 드래그
- Reward Text: RewardText 드래그

애니메이션 설정:
- Typing Speed: 0.05
- Display Duration: 1.0
- Fade In Duration: 0.3
- Fade Out Duration: 0.5

타이핑 메시지:
- Determining Message: "보상 결정 중"
- Dot Animation: "..."

보상별 색상:
- Gold Color: Yellow (255, 235, 4)
- Health Color: Red (255, 50, 50)
- Clear Map Color: Cyan (0, 255, 255)
- Awakening Color: Magenta (255, 0, 255)
- Default Color: White

배경 설정:
- Background Color: Black (0, 0, 0, 204)

디버그:
- Enable Debug Logs: ✓
```

---

## 3. 메인메뉴 UI 설정

### 3.1 메인메뉴 씬 생성

1. **새 씬 생성**:
   - File → New Scene
   - 2D Template 선택
   - 저장: `MainMenuScene`

2. **MainMenuCanvas 생성**:
   - Hierarchy → 우클릭 → UI → Canvas
   - 이름: `MainMenuCanvas`
   - Canvas Scaler 설정:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`

### 3.2 메인메뉴 패널 구조 생성

```
MainMenuCanvas
├── MainMenuPanel
│   ├── Title (TextMeshPro)
│   ├── GoldDisplay (TextMeshPro)
│   ├── ButtonGroup (Vertical Layout Group)
│   │   ├── StartGameButton
│   │   ├── UpgradeShopButton  
│   │   ├── ArtifactGachaButton
│   │   ├── InventoryButton
│   │   ├── SettingsButton
│   │   └── ExitButton
├── UpgradeShopPanel
├── ArtifactGachaPanel
├── InventoryPanel
└── SettingsPanel
```

### 3.3 MainMenuManager GameObject 생성

1. **빈 GameObject 생성**:
   - 이름: `MainMenuManager`
   - `MainMenuManager` 스크립트 Add Component

2. **Inspector 설정**:
```
메뉴 패널들:
- Main Menu Panel: MainMenuPanel 드래그
- Upgrade Shop Panel: UpgradeShopPanel 드래그
- Artifact Gacha Panel: ArtifactGachaPanel 드래그
- Inventory Panel: InventoryPanel 드래그
- Settings Panel: SettingsPanel 드래그

메인 메뉴 버튼들:
- Start Game Button: StartGameButton 드래그
- Upgrade Shop Button: UpgradeShopButton 드래그
- Artifact Gacha Button: ArtifactGachaButton 드래그
- Inventory Button: InventoryButton 드래그
- Settings Button: SettingsButton 드래그
- Exit Button: ExitButton 드래그

골드 표시:
- Gold Text: GoldDisplay 드래그
- Gold Format: "골드: {0}"

게임 시작 설정:
- Game Scene Name: "GameScene" (또는 실제 게임 씬 이름)

디버그:
- Enable Debug Logs: ✓
```

---

## 4. 업그레이드 상점 UI 설정

### 4.1 UpgradeShopPanel 구조

```
UpgradeShopPanel
├── Header
│   ├── BackButton
│   ├── Title ("업그레이드 상점")
│   └── GoldText
├── ScrollView
│   └── Content (Viewport → Content)
│       └── UpgradeList (Vertical Layout Group)
└── Footer (선택사항)
```

### 4.2 MainMenuUpgradeShop 스크립트 설정

1. **UpgradeShopPanel에 스크립트 추가**:
   - `MainMenuUpgradeShop` 스크립트 Add Component

2. **Inspector 설정**:
```
UI 참조:
- Back Button: BackButton 드래그
- Upgrade Scroll View: ScrollView 드래그
- Upgrade List Parent: Content 드래그
- Upgrade Item Prefab: null (코드로 생성)

골드 표시:
- Gold Text: GoldText 드래그
- Gold Format: "보유 골드: {0}"

상점 설정:
- Purchasable Color: Green (51, 153, 51, 230)
- Unpurchasable Color: Red (153, 51, 51, 230)
- Max Level Color: Gray (102, 102, 102, 230)

레이아웃 설정:
- Item Spacing: 10
- Item Size: (400, 120)

디버그:
- Enable Debug Logs: ✓
```

### 4.3 ScrollView 설정

1. **ScrollView 설정**:
   - Movement Type: `Clamped`
   - Horizontal: `false`
   - Vertical: `true`
   - Scroll Sensitivity: `30`

2. **Content 설정**:
   - Content Size Fitter: `Vertical Fit = Preferred Size`
   - Vertical Layout Group:
     - Spacing: `10`
     - Child Alignment: `Upper Center`
     - Child Force Expand Width: `true`

---

## 5. 유물 가챠 UI 설정

### 5.1 ArtifactGachaPanel 구조

```
ArtifactGachaPanel
├── Header
│   ├── BackButton
│   ├── Title ("유물 가챠")
│   └── GoldText
├── MainArea
│   ├── GachaButton (큰 버튼)
│   ├── CostText
│   ├── ProbabilityInfo
│   │   ├── CommonProbText
│   │   ├── RareProbText
│   │   ├── EpicProbText
│   │   └── LegendaryProbText
│   └── StatisticsText
├── ResultPanel (처음에 비활성화)
│   ├── ArtifactIcon
│   ├── ArtifactName
│   ├── ArtifactDesc
│   ├── ArtifactRarity
│   └── CloseButton
```

### 5.2 ArtifactGachaUI 스크립트 설정

1. **ArtifactGachaPanel에 스크립트 추가**:
   - `ArtifactGachaUI` 스크립트 Add Component

2. **Inspector 설정**:
```
UI 참조:
- Back Button: BackButton 드래그
- Gacha Button: GachaButton 드래그
- Gacha Cost Text: CostText 드래그
- Gold Text: GoldText 드래그

가챠 결과 UI:
- Result Panel: ResultPanel 드래그
- Artifact Icon: ArtifactIcon 드래그
- Artifact Name Text: ArtifactName 드래그
- Artifact Desc Text: ArtifactDesc 드래그
- Artifact Rarity Text: ArtifactRarity 드래그
- Result Close Button: CloseButton 드래그

확률 표시 UI:
- Common Prob Text: CommonProbText 드래그
- Rare Prob Text: RareProbText 드래그
- Epic Prob Text: EpicProbText 드래그
- Legendary Prob Text: LegendaryProbText 드래그

통계 UI:
- Total Artifact Count Text: StatisticsText 드래그

애니메이션 설정:
- Gacha Animation Duration: 2
- Gacha Animation Text: "가챠 중..."
- Gacha Animation Curve: (기본값)

등급별 색상:
- Common Color: Gray (128, 128, 128)
- Rare Color: Blue (0, 100, 255)
- Epic Color: Magenta (255, 0, 255)  
- Legendary Color: Yellow (255, 235, 4)

UI 텍스트 포맷:
- Gold Format: "보유 골드: {0}"
- Cost Format: "{0}골드"
- Count Format: "보유 유물: {0}개"
- Prob Format: "{0}: {1}%"

디버그:
- Enable Debug Logs: ✓
```

---

## 6. 보관함 UI 설정

### 6.1 InventoryPanel 구조

```
InventoryPanel
├── Header
│   ├── BackButton
│   ├── Title ("유물 보관함")
│   └── Statistics
│       ├── TotalCount
│       ├── CommonCount
│       ├── RareCount
│       ├── EpicCount
│       └── LegendaryCount
├── FilterAndSort
│   ├── Filters (Horizontal Layout Group)
│   │   ├── ShowAllToggle
│   │   ├── ShowCommonToggle
│   │   ├── ShowRareToggle
│   │   ├── ShowEpicToggle
│   │   └── ShowLegendaryToggle
│   └── Sort
│       ├── SortDropdown
│       └── SortAscendingToggle
├── ScrollView
│   └── Content (Grid Layout Group)
└── DetailPanel (처음에 비활성화)
    ├── DetailIcon
    ├── DetailName
    ├── DetailDesc
    ├── DetailRarity
    ├── DetailEffect
    └── CloseButton
```

### 6.2 ArtifactInventoryUI 스크립트 설정

1. **InventoryPanel에 스크립트 추가**:
   - `ArtifactInventoryUI` 스크립트 Add Component

2. **Inspector 설정**:
```
UI 참조:
- Back Button: BackButton 드래그
- Inventory Scroll View: ScrollView 드래그
- Inventory List Parent: Content 드래그

필터 UI:
- Show All Toggle: ShowAllToggle 드래그
- Show Common Toggle: ShowCommonToggle 드래그
- Show Rare Toggle: ShowRareToggle 드래그
- Show Epic Toggle: ShowEpicToggle 드래그
- Show Legendary Toggle: ShowLegendaryToggle 드래그

정렬 UI:
- Sort Dropdown: SortDropdown 드래그
- Sort Ascending Toggle: SortAscendingToggle 드래그

통계 UI:
- Total Count Text: TotalCount 드래그
- Common Count Text: CommonCount 드래그
- Rare Count Text: RareCount 드래그
- Epic Count Text: EpicCount 드래그
- Legendary Count Text: LegendaryCount 드래그

상세 정보 패널:
- Detail Panel: DetailPanel 드래그
- Detail Icon: DetailIcon 드래그
- Detail Name Text: DetailName 드래그
- Detail Desc Text: DetailDesc 드래그
- Detail Rarity Text: DetailRarity 드래그
- Detail Effect Text: DetailEffect 드래그
- Detail Close Button: CloseButton 드래그

레이아웃 설정:
- Item Spacing: 10
- Item Size: (150, 180)
- Items Per Row: 4

등급별 색상:
- Common Color: Gray (128, 128, 128)
- Rare Color: Blue (0, 100, 255)
- Epic Color: Magenta (255, 0, 255)
- Legendary Color: Yellow (255, 235, 4)

UI 텍스트 포맷:
- Count Format: "{0}개"
- Total Format: "총 {0}개"

디버그:
- Enable Debug Logs: ✓
```

### 6.3 Grid Layout Group 설정

```
Content GameObject:
- Grid Layout Group:
  - Cell Size: (150, 180)
  - Spacing: (10, 10)
  - Constraint: Fixed Column Count
  - Constraint Count: 4
  - Child Alignment: Upper Left

- Content Size Fitter:
  - Horizontal Fit: Unconstrained
  - Vertical Fit: Preferred Size
```

---

## 7. 프리팹 설정

### 7.1 보물상자 프리팹 확인

1. **기존 프리팹 확인**:
   - `PF Props - Chest 01` (닫힌 상자)
   - `PF Props - Chest 01 Open` (열린 상자)

2. **TreasureChest 프리팹 생성**:
   - 빈 GameObject 생성
   - `TreasureChest` 스크립트 추가
   - BoxCollider2D 추가 (Is Trigger: ✓)
   - AudioSource 추가 (Play On Awake: ✗)

3. **Inspector 설정**:
```
상태:
- Is Opened: false

프리팹 참조:
- Closed Chest Prefab: PF Props - Chest 01 드래그
- Opened Chest Prefab: PF Props - Chest 01 Open 드래그

사운드:
- Open Sound: (원하는 사운드 클립)
- Reward Sound: (원하는 사운드 클립)

이펙트:
- Open Effect: (선택사항)
- Effect Duration: 1

자동 제거:
- Auto Destroy Delay: 3
- Enable Auto Destroy: ✓

디버그:
- Enable Debug Logs: ✓
```

### 7.2 SystemManagers 프리팹 생성

1. **SystemManagers를 프리팹으로 만들기**:
   - SystemManagers GameObject를 Project 창으로 드래그
   - `Prefabs/Systems/` 폴더에 저장

---

## 8. 게임 씬 설정

### 8.1 게임 씬에 시스템 배치

1. **SystemManagers 프리팹 배치**:
   - SystemManagers 프리팹을 게임 씬에 드래그

2. **RewardAnimationUI 배치**:
   - RewardAnimationUI 프리팹을 게임 씬에 드래그

3. **TreasureSpawner 설정**:
   - 기존 TreasureSpawner GameObject 선택
   - Inspector에서 설정:
```
스폰 설정:
- Treasure Chest Prefab: TreasureChest 프리팹 드래그
- Player Target: Player GameObject 드래그

스폰 거리 설정:
- Min Spawn Distance: 30
- Max Spawn Distance: 50
- Despawn Distance: 50

스폰 개수 설정 (시간대별):
- Count 0 To 1 Min: (0, 1)
- Count 1 To 3 Min: (1, 2)
- Count 3 To 5 Min: (2, 3)
- Count 5 To 10 Min: (3, 5)
- Count 10 To 15 Min: (4, 10)

리스폰 설정:
- Respawn Delay: 5

맵 경계 설정:
- Map Radius: 100

디버그:
- Enable Debug Logs: ✓
- Show Gizmos: ✓
```

### 8.2 기존 GoldUI 연결 확인

1. **GoldUI 확인**:
   - HUD Canvas의 GoldUI가 있는지 확인
   - GoldSystem과 자동 연결되는지 테스트

---

## 9. 테스트 및 디버깅

### 9.1 기본 기능 테스트

1. **골드 시스템 테스트**:
   - 게임 실행 → 보물상자와 충돌 → 골드 획득 확인
   - 게임 종료 후 재실행 → 골드 유지 확인

2. **업그레이드 시스템 테스트**:
   - 메인메뉴 → 업그레이드 상점 → 구매 테스트
   - 게임 실행 → 효과 적용 확인 (체력, 이동속도 등)

3. **가챠 시스템 테스트**:
   - 메인메뉴 → 유물 가챠 → 가챠 실행
   - 획득 애니메이션 확인
   - 보관함에서 유물 확인

4. **보관함 테스트**:
   - 유물 표시 확인
   - 필터 및 정렬 기능 확인
   - 상세 정보 확인

### 9.2 디버그 명령어 활용

1. **Inspector Context Menu 사용**:
   - `MainMenuManager`: "테스트 골드 추가", "모든 데이터 리셋"
   - `PermanentUpgradeSystem`: "업그레이드 데이터 리셋"
   - `ArtifactGachaSystem`: "테스트 가챠", "보유 유물 리셋"
   - `RewardAnimationUI`: "테스트 애니메이션 - 골드"

2. **디버그 로그 활용**:
   - Console 창에서 각 시스템의 동작 확인
   - 에러 발생 시 로그 메시지 확인

### 9.3 밸런싱 조정

모든 수치는 Inspector에서 실시간 조정 가능:

1. **업그레이드 비용 및 효과**:
   - `PermanentUpgradeSystem`의 Available Upgrades 배열

2. **가챠 확률**:
   - `ArtifactGachaSystem`의 Gacha 설정

3. **보물상자 스폰**:
   - `TreasureSpawner`의 Count 설정

4. **애니메이션 타이밍**:
   - `RewardAnimationUI`의 Animation 설정

---

## 🎉 완성!

이제 완전한 보물상자 시스템과 메인메뉴 상점이 구축되었습니다!

### ✅ 구현된 기능들:
- 💰 **영구 골드 시스템** (PlayerPrefs 저장)
- 📦 **보물상자 상호작용** (4가지 보상 타입)
- 🎬 **보상 애니메이션 UI** (타이핑 효과)
- 🔧 **영구 업그레이드 상점** (4가지 업그레이드)
- 🎰 **유물 가챠 시스템** (4등급 확률)
- 📚 **유물 보관함** (필터/정렬 기능)
- 🏠 **메인메뉴 통합 UI**

### 🎮 사용법:
1. 게임에서 보물상자 획득으로 골드 수집
2. 메인메뉴에서 골드로 업그레이드/가챠 구매
3. 획득한 유물들을 보관함에서 관리

모든 설정값들이 Inspector에서 조정 가능하여 쉽게 밸런싱할 수 있습니다! 🚀