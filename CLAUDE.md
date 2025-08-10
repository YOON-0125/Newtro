# 뱀파이어 서바이벌류 게임 설계 문서

## 📖 프로젝트 개요
뱀파이어 서바이벌 스타일의 2D 액션 게임으로, 플레이어는 다양한 무기를 사용해 몰려오는 적들과 싸우며 생존하는 게임입니다.

---

## ❤️ 체력 시스템 (Health System)

### 기본 구조
- **하트 기반 체력**: 1하트 = 4등분 (1/4, 2/4, 3/4, 4/4)
- **초기 체력**: 하트 3개 (총 12 체력)
- **최대 체력**: 하트 10개 (총 40 체력)
- **하트 확장**: 게임 진행 중 최대 체력 증가 가능

### 데미지 체계
**일반 적**:
- 충돌/기본공격: 1/4칸 ~ 1/2칸 (1~2 데미지)

**보스급**:
- 충돌/공격: 2칸 이상 (8+ 데미지)

### 구현 파일
- `HealthSystem.cs` - 체력 로직 관리
- `HealthBarUI.cs` - UI 하트 표시 관리

---

## ⚔️ 무기 시스템 (Weapon System)

### 시스템 아키텍처
```
WeaponManager (무기 관리자)
├── WeaponBase (추상 무기 클래스)
│   ├── ProjectileWeapon (투사체 무기) → Fireball
│   ├── ChainWeapon (연쇄 무기) → Lightning Chain
│   └── FieldWeapon (지역 무기) → Ice Field
├── ElementalSystem (속성 시스템)
├── WeaponUpgradeSystem (레벨링)
└── DamageCalculator (데미지 계산)
```

### 무기 타입

#### 1. 파이어볼 (Fireball) - 화염 속성
- **타입**: ProjectileWeapon
- **동작**: 화염구체 발사 → 적 적중 → 작은 반경 폭발
- **레벨업**: 폭발 반경 증가, 데미지 증가
- **특성**: 지속 데미지(DoT), 범위 공격

#### 2. 라이트닝 체인 (Lightning Chain) - 번개 속성
- **타입**: ChainWeapon  
- **동작**: 가장 가까운 적 타겟 → 번개 타격 → 연쇄 효과
- **레벨업**: 연쇄 개수 증가, 연쇄 범위 증가
- **특성**: 즉시 타격, 다중 타겟

#### 3. 아이스 필드 (Ice Field) - 얼음 속성
- **타입**: FieldWeapon
- **동작**: 플레이어 발밑에 주기적으로 얼음 필드 생성
- **레벨업**: 쿨다운 감소, 필드 크기 증가
- **특성**: 지속 지역 데미지, 슬로우 효과

### 원소 속성 시스템

#### 화염 (Fire)
- **고유 효과**: 화상 상태이상, 범위 확장
- **강화 방향**: 폭발 반경 ↑, DoT 지속시간 ↑

#### 번개 (Lightning)  
- **고유 효과**: 스턴, 다중 타겟
- **강화 방향**: 연쇄 개수 ↑, 연쇄 범위 ↑

#### 얼음 (Ice)
- **고유 효과**: 이동속도 감소, 지역 제어
- **강화 방향**: 쿨다운 감소, 필드 크기 ↑, 슬로우 강도 ↑

**※ 속성 상성 시스템은 추후 구현 예정**

---

## 👹 적 시스템 (Enemy System)

### 적 클래스 구조
```
EnemyBase (추상 적 클래스)
├── MeleeEnemy (근접 충돌형)
│   ├── BasicSkeleton (기본 스켈레톤)
│   └── DualBladeSkeleton (쌍칼 스켈레톤)
├── RangedEnemy (원거리 공격형)
│   └── SkeletonMage (스켈레톤 마법사)
└── DefensiveEnemy (방어형)
    └── ShieldSkeleton (방패 스켈레톤)
```

### 적 상세 스펙

#### 1. 기본 스켈레톤 (BasicSkeleton)
- **타입**: 근접 충돌형
- **행동**: 플레이어 추적 → 충돌 데미지
- **이동속도**: 보통
- **데미지**: 1/4칸 (1 데미지)
- **특수능력**: 없음

#### 2. 스켈레톤 마법사 (SkeletonMage)
- **타입**: 원거리 공격형
- **행동**: 천천히 이동 → 투사체 발사
- **이동속도**: 느림
- **공격 데미지**: 1/2칸 (2 데미지)
- **특수능력**: 원거리 마법 투사체

#### 3. 쌍칼 스켈레톤 (DualBladeSkeleton)
- **타입**: 근접 충돌형 (고속)
- **행동**: 빠른 추적 → 충돌 데미지
- **이동속도**: 빠름
- **데미지**: 1/4~1/2칸 (1~2 데미지)
- **특수능력**: 고속 이동

#### 4. 방패 스켈레톤 (ShieldSkeleton)
- **타입**: 방어형 근접
- **행동**: 천천히 추적 + 랜덤 무적 패턴
- **이동속도**: 느림
- **데미지**: 1/2칸 (2 데미지)
- **특수능력**: 2초마다 랜덤하게 1초간 정지 + 무적

---

## 🎮 게임플레이 시스템

### 코어 루프
1. **적 스폰**: 다양한 타입의 적들이 지속적으로 스폰
2. **전투**: 자동/수동 무기 시스템으로 적 처치
3. **레벨업**: 경험치 획득 → 무기 강화 선택
4. **생존**: 체력 관리하며 최대한 오래 생존

### 진행 시스템
- **무기 레벨업**: 각 무기별 고유 강화 요소
- **체력 증가**: 게임 진행 중 최대 하트 개수 증가
- **난이도 증가**: 시간이 지날수록 더 강한 적들 등장

---

## 🛠️ 기술적 구현 사항

### 주요 스크립트 구조
```
Scripts/
├── Health/
│   ├── HealthSystem.cs
│   └── HealthBarUI.cs
├── Weapons/
│   ├── WeaponManager.cs
│   ├── WeaponBase.cs
│   ├── ProjectileWeapon.cs
│   ├── ChainWeapon.cs
│   └── FieldWeapon.cs
├── Enemies/
│   ├── EnemyBase.cs
│   ├── MeleeEnemy.cs
│   ├── RangedEnemy.cs
│   └── DefensiveEnemy.cs
├── Systems/
│   ├── ElementalSystem.cs
│   ├── DamageCalculator.cs
│   └── GameManager.cs
└── UI/
    └── GameUI.cs
```

### UI 구조
```
Canvas (LeftTopUI)
├── HealthBarUI (Panel)
│   └── Horizontal Layout Group
│       ├── Heart (Image) × 5~10개
└── WeaponUI (Panel) - 추후 구현
```

---

## 📝 개발 우선순위

### Phase 1: 핵심 시스템
1. ✅ 체력 시스템 완료
2. 🔄 무기 시스템 구현 중
3. ⏳ 기본 적 AI 구현 예정

### Phase 2: 게임플레이
1. 무기-적 상호작용
2. 레벨업 시스템
3. UI 개선

### Phase 3: 확장
1. 추가 무기/적 타입
2. 속성 상성 시스템
3. 보스 시스템

---

## 🎯 설계 철학
- **간단한 조작**: 플레이어는 이동만, 무기는 자동 발동
- **명확한 피드백**: 데미지, 체력 변화가 직관적으로 보임
- **점진적 복잡성**: 기본 시스템부터 시작해 점차 확장
- **시각적 명확성**: 하트 기반 체력, 속성별 색상 구분

---

## 📚 이전 개발 참고사항 (Legacy Notes)
- **게임오버 시**: 유물 없음, 재시작만
- **난이도 증가**: 클리어할 때마다 적 스탯 +10%
- **유물 효과**: 영구적 능력 강화 (체력, 데미지, 속도 등), 영구적 스킬 추가 (스킬은 추후 구현)

## 📱 UI/UX 설계

### 모바일 설계

- **화면 구성**: 상단 상태바 + 중앙 게임 영역 + 하단 버튼, 세이프존 고려
- **기준 해상도**: 375px 너비 (모바일 표준)

### 화면별 구성

0. 화면 구성은 사용자가 직접 Unity에서 진행. 아래는 현재 화면
1. **HUD**:

2. **레벨업**: 전체 화면 오버레이로 업그레이드 선택
3. **게임 클리어**: 결과 표시 + 유물 3개 중 선택
4. **게임오버**: 결과 표시 + 재시작/홈 버튼

#### 이전 개발 시 문제점 참고사항(lesson learned)

- **문제점**: 초기 방어막 구현 시 데미지 미적용 및 통계 누락 문제 발생. 회전 구체 형태로 변경 시도했으나, 기능 미작동 및 복잡도 증가로 롤백 결정.
- **해결**:
  - `GameStateContext.tsx`: `shield` 무기 타입을 `{ level, damage, radius, cooldown }`으로 재정의.
  - `WeaponManager.ts`:
    - 방어막을 단일 반투명 원(`PIXI.Graphics`)으로 구현.
    - `updateShield` 메서드는 시각적 업데이트 및 쿨다운 관리만 담당.
    - `getShieldDamageTargets()` 메서드를 추가하여, 방어막 범위 내의 적 목록을 반환하도록 변경.
  - `GameEngine.ts`:
    - `gameLoop`에서 `weaponManager.getShieldDamageTargets()`를 호출하여 데미지 대상 적 목록을 가져옴.
    - 가져온 적들에게 `handleDamageEvent()`를 통해 데미지를 적용하고 통계를 기록하도록 중앙화.
  - `LevelUp.tsx`: 방어막 업그레이드 시 `damage`, `radius`, `cooldown`이 증가하도록 로직 수정.
- **결과**: 방어막이 정상적으로 데미지를 입히고 통계에 반영됨.

#### 📊 **전투 통계 시스템 구축**

- **목표**: 게임 승리 시 "적 처치 수, 경험치, 무기별 처치 수, 최대 데미지를 낸 무기와 최대 데미지"를 표시.
- **구현**:
  - `GameStateContext.tsx`:
    - `GameStats` 인터페이스 정의 (`enemiesKilled`, `experienceGained`, `damageDealt`, `weaponStats`, `highestDamage`).
    - `GameState`에 `stats: GameStats` 속성 추가 및 초기화 로직 구현.
    - `UPDATE_STATS` 액션 타입 추가 및 리듀서 로직 구현.
  - `GameEngine.ts`:
    - `handleDamageEvent()` 메서드를 중앙 데미지 처리 및 통계 기록 함수로 리팩토링.
    - 적 처치 및 데미지 발생 시 `UPDATE_STATS` 액션을 `dispatch`하여 통계 업데이트.
  - `Victory.tsx`: `GameState`의 `stats` 데이터를 활용하여 승리 화면에 상세 전투 통계 표시.
- **결과**: 게임 승리 시 상세한 전투 통계가 정상적으로 표시됨.

#### 🔄 **플레이어 체력/경험치 업데이트 문제 해결**

- **문제점**: 통계 시스템 도입 후 플레이어 체력 및 경험치 업데이트가 고정되거나 누락되는 현상 발생.
- **원인**: 여러 `dispatch` 호출이 한 프레임 내에서 충돌하여 상태 업데이트가 덮어쓰여짐.
- **해결**:
  - `GameEngine.ts`:
    - 플레이어 체력 업데이트를 `gameLoop`의 `UPDATE_PLAYER` 액션에 통합.
    - 적 처치 시 경험치/점수 `dispatch`를 제거하고, 모든 통계 관련 업데이트를 `UPDATE_STATS` 액션으로 통일.
  - `GameStateContext.tsx`: `UPDATE_STATS` 리듀서에서 통계뿐만 아니라 경험치 및 점수 증가, 레벨업 처리까지 모두 담당하도록 로직 통합.
- **결과**: 플레이어 체력 및 경험치가 실시간으로 정확하게 업데이트됨.

#### 👾 **적 겹침 및 충돌 문제 해결**

- **문제점**: 적들이 한 점에 겹쳐서 자동 공격이 인식하지 못하고, 플레이어 피격이 제대로 되지 않으며, 새로운 적 스폰이 멈추는 문제 발생.
- **원인**: 적들이 서로의 위치를 고려하지 않고 플레이어만 추적하여 겹침 현상 발생. 플레이어 피격 로직이 무적 시간 동안 모든 충돌을 무시하여 데미지 누락.
- **해결**:
  - `Enemy.ts`:
    - `update` 메서드에 `otherEnemies` 인자를 추가하여 다른 적들과의 **분리(Separation) 로직** 구현. (서로 겹치지 않도록 밀어내는 힘 적용)
    - `isInvincible()` 메서드를 추가하여 플레이어의 무적 상태를 외부에 노출.
  - `EnemyManager.ts`:
    - `update` 메서드에서 각 적의 `update` 호출 시, 해당 적을 제외한 `allEnemies` 배열을 전달.
  - `GameEngine.ts`:
    - `handleCollisions`에서 플레이어 피격 로직을 수정. `collisions.playerEnemyCollisions`가 존재하고 플레이어가 무적 상태가 아닐 때만 **단 한 번** 데미지를 적용하도록 변경.
- **결과**: 적들이 더 이상 겹치지 않고 자연스럽게 퍼지며, 플레이어 피격이 무적 시간과 연동되어 정확하게 작동함.

#### ♻️ **적 클래스 리팩토링 및 '추적자' 구현 (1단계)**

- **목표**: 다양한 적 타입을 유연하게 추가할 수 있는 구조 마련 및 '추적자' 적 구현.
- **구현**:
  - `src/game/entities/behaviors` 폴더 신설.
  - `IEnemyBehavior.ts` 인터페이스 정의 (적 행동 패턴의 계약).
  - `Enemy.ts` 리팩토링:
    - `EnemyType` enum 추가 (`Basic`, `Chaser`, `Giant`, `Sniper`).
    - 생성자에서 `EnemyType`과 `IEnemyBehavior`를 주입받도록 변경.
    - `createSprite` 메서드를 적 타입에 따라 다른 모양과 색상을 그리도록 수정.
    - `update` 메서드가 주입된 `behavior.update()`를 호출하도록 위임.
    - `setPosition`, `getSpeed` 등 필요한 getter/setter 추가.
  - `BasicChaserBehavior.ts` 구현: 기존의 플레이어 추적 및 적 분리 로직을 이 클래스로 이동.
  - `EnemyManager.ts`:
    - `update` 메서드에서 `gameTime`을 받아 `getEnemyTypeForCurrentTime()`을 통해 현재 시간에 맞는 적 타입을 결정.
    - `getBehaviorForType()`을 통해 해당 타입에 맞는 `IEnemyBehavior` 인스턴스를 반환.
    - `Enemy` 생성 시 새로운 생성자 시그니처에 맞춰 `EnemyType`과 `behavior`를 전달.
- **결과**: 새로운 적 타입을 쉽게 추가할 수 있는 확장성 있는 구조가 마련되었으며, 게임 시간에 따라 '추적자' 적이 정상적으로 스폰됨.

#### 👾 **새로운 적 타입 구현 (2단계: 거인, 3단계: 저격수)**

- **목표**: `enemy_basic.md`에 명시된 '거인'과 '저격수' 적을 구현하고 게임에 통합.
- **구현**:
  - **거인 (Giant)**:
    - `GiantBehavior.ts` 구현: `BasicChaserBehavior`를 상속받아 느린 추적 행동 구현.
    - `EnemyManager.ts`:
      - `getEnemyTypeForCurrentTime()`에 `EnemyType.Giant` 스폰 확률 추가 (게임 시간 2분부터).
      - `getBehaviorForType()`에서 `EnemyType.Giant`에 `GiantBehavior` 반환.
      - `removeEnemy()` 메서드에 거인(`EnemyType.Giant`) 사망 시 `spawnSplitEnemies()` 호출 로직 추가.
      - `spawnSplitEnemies()` 구현: 거인 사망 위치에 `EnemyType.Basic` 적들을 여러 마리(게임 시간에 따라 증가) 생성하여 퍼뜨림.
  - **저격수 (Sniper)**:
    - `SniperBehavior.ts` 구현: `IEnemyBehavior`를 구현하여 플레이어와 거리 유지 및 원거리 공격 로직 포함.
    - `WeaponManager.ts`:
      - `Projectile` 인터페이스에 `isEnemyProjectile` 속성 추가.
      - `fireEnemyProjectile()` 메서드 추가: 적이 투사체를 발사할 수 있도록 구현 (색상, 크기 다르게).
      - `createProjectile()` 수정: `isEnemyProjectile` 인자를 받아 적 투사체 생성 로직 포함.
    - `CollisionManager.ts`:
      - `CollisionResult`에 `enemyProjectilePlayerCollisions` 추가.
      - `checkCollisions()` 수정: 적 투사체와 플레이어 간의 충돌을 감지하여 `enemyProjectilePlayerCollisions`에 추가.
    - `GameEngine.ts`:
      - `gameLoop`에서 `weaponManager.fireProjectiles()` 호출 시 `enemies` 인자 전달.
      - `handleCollisions()` 수정: `enemyProjectilePlayerCollisions`를 순회하며 플레이어에게 데미지 적용 및 투사체 제거.
    - `EnemyManager.ts`:
      - 생성자에서 `WeaponManager` 인스턴스를 주입받도록 변경.
      - `EnemyUpdateContext`에 `weaponManager`와 `stage`를 추가하여 `SniperBehavior`에서 접근 가능하도록 함.
      - `getEnemyTypeForCurrentTime()`에 `EnemyType.Sniper` 스폰 확률 추가 (게임 시간 5분부터).
      - `getBehaviorForType()`에서 `EnemyType.Sniper`에 `SniperBehavior` 반환.
- **결과**: '거인'과 '저격수' 적이 게임에 성공적으로 통합되었으며, 각자의 행동 패턴(분열, 원거리 공격)을 정상적으로 수행함.

#### 🎁 유물 시스템 구현 및 관련 버그 수정

- **목표**: 게임의 핵심 재플레이 요소인 '유물' 시스템을 구현하고, 이 과정에서 발생한 다양한 버그를 해결하여 유물 효과가 정상적으로 누적 적용되도록 함.
- **구현**:
  - **데이터 정의 및 저장**:
    - `src/data/artifacts.ts`: 모든 유물의 ID, 이름, 설명, 효과(합연산/곱연산, 적용 능력치 경로)를 정의하는 중앙 데이터베이스 구축.
    - `src/utils/storage.ts`: 플레이어가 획득한 유물 ID 목록을 `LocalStorage`에 저장하고 불러오는 유틸리티 함수 구현.
  - **상태 관리 통합**:
    - `src/contexts/GameStateContext.tsx`:
      - `GameState` 인터페이스에 `ownedArtifacts: ArtifactID[]` 속성 추가 및 `player.speed` 속성 추가.
      - `initialState`에서 `loadOwnedArtifacts()`를 통해 보유 유물 목록을 초기화.
      - **`START_NEW_ROUND` 액션 도입**: 유물 획득(`newArtifactId`)과 다음 라운드 시작을 하나의 원자적 액션으로 통합. 이 리듀서는 `initialState`를 기반으로 라운드별 초기화가 필요한 속성(`score`, `time`, `stats` 등)만 리셋하고, `ownedArtifacts`, `difficulty`, `player`, `weapons` 등 **유물 효과로 강화된 능력치들은 이전 라운드의 상태를 유지**하도록 로직을 개선.
      - `applyArtifacts` 함수를 리듀서 내부에서 호출하여, 유물 효과가 적용된 최종 `GameState`를 직접 반환하도록 하여 `GameState` 자체가 항상 "진실의 원천"이 되도록 함.
  - **유물 효과 적용 로직**:
    - `src/game/systems/ArtifactSystem.ts`: `applyArtifacts` 함수 구현. `GameState`와 `ownedArtifacts`를 받아, 유물 효과(합연산/곱연산)를 순서대로 적용하여 강화된 `GameState`를 반환. 특히 `stat` 경로의 깊이(`player.speed` vs `weapons.projectile.damage`)를 정확히 파악하여 해당 능력치에 값을 적용하도록 개선.
  - **게임 엔진 연동**:
    - `src/components/GameCanvas.tsx`: `state.isPlaying` 상태 변화 시 `applyArtifacts`를 호출하여 유물 효과가 적용된 `finalState`를 계산하고, `gameEngine.restart(finalState)`를 호출하여 엔진을 초기화하고 새로운 상태를 반영.
    - `src/game/GameEngine.ts`:
      - `start(initialState)` 및 `restart(initialState)` 메서드가 유물 효과가 적용된 `GameState`를 받아 엔진 내부의 플레이어(`this.player.speed`, `this.player.maxHealth`) 및 무기 관련 속성들을 업데이트하도록 수정.
      - `updateFromGameState` 메서드에서 `GameState`의 플레이어 및 무기 속성을 `GameEngine` 내부 속성에 정확히 반영하도록 로직 개선.
  - **UI 및 기타 수정**:
    - `src/components/Victory.tsx`: 승리 화면에서 유물 선택 UI를 `ui_layout.md`에 명시된 카드 형태로 표시되도록 스타일 수정. 유물 선택 시 `START_NEW_ROUND` 액션을 디스패치하도록 변경.
    - `src/components/GameOver.tsx`: 게임 오버 후 재시작 시 `START_NEW_ROUND` 액션을 사용하도록 변경.
    - `src/index.css`: Tailwind CSS 클래스들이 정상적으로 적용되도록 `@tailwind` 지시문 추가.
    - `src/game/entities/Player.ts`: `Player` 클래스에 `public speed` 및 `public maxHealth` 속성 추가.
    - `src/game/managers/WeaponManager.ts`: `OrbitalWeaponInstance` 인터페이스 도입 및 `orbitalWeapons` 배열 타입 변경, `addOrbitalWeapon` 메서드에 `damage` 인자 추가, `updateWeaponStats`에서 모든 무기 속성을 `GameState`에 따라 업데이트하도록 로직 확장.
- **결과**:
  - 유물 효과가 매 라운드마다 정상적으로 누적 적용됨 (예: 직선탄 데미지 `15 -> 18.75 -> 20.625`).
  - 난이도 증가 메커니즘이 정상적으로 작동하여 다음 라운드에 반영됨.
  - 승리 화면의 유물 선택 UI가 의도한 대로 카드 형태로 표시됨.
  - 게임 재시작 시 무한 루프 및 상태 초기화 문제가 해결됨.
  - 이 과정에서 발생했던 수많은 타입스크립트 및 문법 오류들이 해결되어 코드의 안정성이 향상됨.

#### 이전 개발 시 문제점 참고사항(lesson learned) 끝.
