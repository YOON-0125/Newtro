# Wave Pattern System - 완성 가이드

## 📊 시스템 개요

**WavePatternManager**는 기존 **EnemyManager** 위에 덧씌우는 형태로 구현된 고급 웨이브 시스템으로, 4가지 전략적 공격 패턴을 제공합니다.

## 🏗️ 아키텍처

```
WavePatternManager (메인 컨트롤러)
├── PatternData.cs (데이터 구조)
├── WavePatternCountdownUI.cs (UI 시스템)
└── EnemyManager (기존 시스템과 연동)
```

## ⚔️ 구현된 패턴들

### 1. 🔵 Circle Siege (원형 포위)
- **적 타입**: BasicSkeleton
- **형태**: 플레이어 중심 360도 원형 배치
- **특징**: 12기 기본, 10m 반지름, 1.3x 경험치
- **전략**: 전방향 압박을 통한 포위 섬멸

### 2. 🛡️ Shield Wall (방패 결계)  
- **적 타입**: ShieldSkeleton
- **형태**: 180도 반원형 방어벽
- **특징**: 8기 기본, 12m 반지름, 1.5x 경험치
- **전략**: 방어 중심의 압박 및 반격

### 3. 🏰 Mixed Barrier (혼합 결계)
- **적 타입**: 내부 ShieldSkeleton + 외부 BasicSkeleton  
- **형태**: 이중 원형 구조 (6기 + 12기)
- **특징**: 6m/10m 이중 반지름, 1.8x 경험치
- **전략**: 방어선과 공격선의 계층적 방어

### 4. ⚔️ Line Charge (직선 돌격)
- **적 타입**: DualBladeSkeleton
- **형태**: 8방향 랜덤 돌격 웨이브
- **특징**: 8기 기본, 18m 거리, 자동 소멸, 2.2x 경험치  
- **전략**: 고속 돌격을 통한 기습 공격

## 🎮 확률 시스템

```csharp
기본 확률: 15% (baseChance)
웨이브 보너스: 웨이브 5 이후 +5%/웨이브 (waveBonus)
최대 확률: 웨이브 20에서 90%

패턴별 최소 웨이브:
- Circle Siege: 웨이브 1
- Shield Wall: 웨이브 3  
- Mixed Barrier: 웨이브 5
- Line Charge: 웨이브 7
```

## 🖥️ UI 시스템

**WavePatternCountdownUI**는 패턴 시작 시 **3→2→1** 시각적 카운트다운을 제공:
- **색상 변화**: 빨강→노랑→초록
- **스케일 애니메이션**: 1.2x → 1.0x
- **알파 애니메이션**: 페이드 인/아웃
- **배경 효과**: 반투명 색상 강조

## 🔧 테스트 및 디버깅

### Context Menu 테스트 도구
```csharp
// WavePatternManager Component에서 우클릭
"Force Pattern Next Wave" - 다음 웨이브에 강제 패턴 실행
"Test Circle Siege Pattern Now" - 원형 포위 즉시 테스트  
"Test Shield Wall Pattern Now" - 방패 결계 즉시 테스트
"Test Mixed Barrier Pattern Now" - 혼합 결계 즉시 테스트
"Test Line Charge Pattern Now" - 직선 돌격 즉시 테스트
```

### Visual Debugging (Gizmos)
- **Circle Siege**: 청록 원 + 빨간 스폰 점
- **Shield Wall**: 초록 반원 + 파란 방패 큐브
- **Mixed Barrier**: 자홍/청록 이중 원 + 연결선
- **Line Charge**: 빨간/회색 원 + 노란 돌격 화살표

## 📈 성능 최적화

### 메모리 관리
```csharp
- 적 사망 시 자동 패턴 리스트 정리
- 코루틴 기반 비동기 처리
- 이벤트 기반 느슨한 결합
- 거리 기반 자동 소멸 (Line Charge)
```

### 로드 분산
```csharp
- 스폰 간격을 통한 프레임 분산
- 배치별 대기 시간 (0.08-0.15초)
- UI 애니메이션과 게임 로직 분리
```

## 🎯 설정 가능한 매개변수

### Inspector에서 조정 가능한 값들:
```csharp
// 확률 설정
baseChance: 0.15f (15% 기본 확률)
waveBonus: 0.05f (웨이브당 5% 증가)  
bonusStartWave: 5 (보너스 시작 웨이브)

// 패턴별 설정
enemyCount: 적 수량
spawnRadius: 스폰 반지름
expMultiplier: 경험치 배율
weight: 랜덤 선택 가중치

// Line Charge 전용
chargeInterval: 돌격 간격
despawnDistance: 자동 소멸 거리
```

## 🔌 기존 시스템과의 통합

### EnemyManager 연동
- **이벤트 기반**: `OnWaveStart` 이벤트 구독
- **비파괴적**: 기존 스폰 시스템 유지
- **공존**: 일반 적과 패턴 적 동시 존재
- **난이도 연동**: 기존 난이도 스케일링 활용

### 데미지/경험치 시스템
- **기존 시스템 활용**: `EnemyBase` 상속 구조 사용
- **패턴 보너스**: 경험치 배율을 통한 추가 보상
- **시각적 구분**: "[패턴]" 이름 태그 및 스케일 변경

## 🚀 확장 방법

### 새로운 패턴 추가
1. `PatternType` enum에 새 타입 추가
2. `WavePatternData`에 전용 설정 추가
3. `ExecutePattern`에 새 케이스 추가
4. 해당 패턴 메소드 구현
5. `SetupDefaultPatterns`에 등록

### UI 확장  
- 패턴별 특수 효과 추가
- 사운드 시스템 연동
- 패턴 완료 알림 시스템
- 패턴 통계 UI

## ⚠️ 주의사항

### 성능 고려사항
- **최대 적 수 제한**: EnemyManager의 `maxEnemies` 설정 확인
- **메모리 누수 방지**: 적 사망 시 이벤트 리스너 정리
- **코루틴 관리**: 패턴 중단 시 모든 코루틴 정리

### 디버그 모드
```csharp
enableDebugLogs = true  // 상세 로그 출력
forcePatternNextWave = true  // 강제 패턴 실행
```

## 🎉 완성된 기능들

✅ **4가지 전략적 패턴** 완전 구현  
✅ **확률 기반 발동** 시스템  
✅ **UI 카운트다운** 3→2→1  
✅ **시각적 디버깅** Gizmos 지원  
✅ **테스트 도구** Context Menu  
✅ **성능 최적화** 메모리 관리  
✅ **기존 시스템 연동** 비파괴적 통합  
✅ **Inspector 설정** 완전 커스터마이징

---

**Wave Pattern System v1.0 - 구현 완료** 🎊
*기존 EnemyManager와 완벽 통합된 고급 전략 패턴 시스템*