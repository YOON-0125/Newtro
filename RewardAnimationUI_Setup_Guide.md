# RewardAnimationUI 프리팹 생성 가이드

## 🎯 목표
보물상자 보상 애니메이션을 위한 UI 프리팹을 생성하고 설정합니다.

## 📋 프리팹 생성 단계

### 1. 기본 UI 구조 생성

1. **Hierarchy에서 UI 생성**:
   - 우클릭 → UI → Canvas → 새 Canvas 생성
   - Canvas 이름을 `RewardAnimationUI`로 변경

2. **Canvas 설정**:
   - Canvas Component:
     - Render Mode: `Screen Space - Overlay`
     - Sort Order: `10` (다른 UI 위에 표시되도록)
   - Canvas Scaler Component:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1920 x 1080`
     - Match: `0.5` (Width/Height 균형)

### 2. UI 컴포넌트 추가

1. **CanvasGroup 컴포넌트 추가**:
   - RewardAnimationUI GameObject 선택
   - Add Component → Canvas Group

2. **배경 패널 생성**:
   - RewardAnimationUI 우클릭 → UI → Image
   - 이름: `BackgroundPanel`
   - Image Component 설정:
     - Source Image: `None` (단색 배경)
     - Color: `Black (0, 0, 0, 200)` - 반투명 검정
   - RectTransform 설정:
     - Anchor: `Top Center`
     - Pos X: `0`, Pos Y: `-100` (화면 상단에서 100px 아래)
     - Width: `600`, Height: `80`

3. **텍스트 생성**:
   - BackgroundPanel 우클릭 → UI → Text - TextMeshPro
   - 이름: `RewardText`
   - TextMeshProUGUI Component 설정:
     - Text: `""` (빈 문자열)
     - Font Size: `24`
     - Color: `White`
     - Alignment: `Center Middle`
     - Auto Size: `Best Fit`
     - Min: `18`, Max: `30`
   - RectTransform 설정:
     - Anchor: `Stretch Stretch`
     - Left: `20`, Right: `20`, Top: `10`, Bottom: `10`

### 3. RewardAnimationUI 스크립트 연결

1. **스크립트 첨부**:
   - RewardAnimationUI GameObject 선택
   - Add Component → `RewardAnimationUI` 스크립트

2. **Inspector 설정**:
   ```
   UI 참조:
   - Canvas Group: (자동 연결됨)
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
   - Gold Color: Yellow (255, 235, 4, 255)
   - Health Color: Red (255, 50, 50, 255)
   - Clear Map Color: Cyan (0, 255, 255, 255)
   - Awakening Color: Magenta (255, 0, 255, 255)
   - Default Color: White (255, 255, 255, 255)
   
   배경 설정:
   - Background Color: Black (0, 0, 0, 204)
   
   디버그:
   - Enable Debug Logs: ✓
   ```

### 4. 프리팹 저장

1. **프리팹 생성**:
   - RewardAnimationUI GameObject를 Project 창의 `Assets/Prefabs/UI/` 폴더로 드래그
   - 프리팹 이름: `RewardAnimationUI.prefab`

2. **씬에 배치**:
   - 현재 게임 씬에 프리팹을 드래그하여 배치
   - DontDestroyOnLoad로 설정되어 있어 씬 전환 시에도 유지됨

## ✅ 테스트 방법

### 1. Inspector 테스트
- RewardAnimationUI 스크립트의 Inspector에서
- 우클릭 → `테스트 애니메이션 - 골드` 선택
- 애니메이션이 정상적으로 재생되는지 확인

### 2. 게임 내 테스트
- 게임 실행 후 보물상자와 충돌
- 화면 상단에 보상 애니메이션이 표시되는지 확인
- 타이핑 효과 → 보상 결과 → 페이드아웃 순서 확인

## 🔧 문제 해결

### 텍스트가 표시되지 않는 경우
- TextMeshPro Font Asset이 올바른지 확인
- Canvas의 Sort Order가 충분히 높은지 확인

### 애니메이션이 작동하지 않는 경우
- 보물상자와 충돌 시 Console 로그 확인
- RewardAnimationUI Instance가 null인지 확인

### UI 위치가 잘못된 경우
- Canvas Scaler 설정 확인
- RectTransform Anchor 설정 다시 확인

## 📱 모바일 최적화 (옵션)

모바일에서 더 나은 표시를 원한다면:

1. **Safe Area 고려**:
   - BackgroundPanel의 Pos Y를 `-150`으로 조정

2. **폰트 크기 조정**:
   - Font Size: `20`
   - Min: `16`, Max: `24`

3. **패널 크기 조정**:
   - Width: `500`, Height: `70`

이제 Unity 에디터에서 이 가이드를 따라 프리팹을 생성하면 보물상자 보상 애니메이션 시스템이 완성됩니다!