# ID 변경 백업 파일
## 생성일: 2025-08-17

### 변경 목적
모든 업그레이드 ID를 언더바 형식(`chain_lightning_level_up`)에서 파스칼케이스 형식(`ChainLightningLevelUp`)으로 통일

---

## 📋 ID 변경 매핑 테이블

### **New Weapon IDs**
| 수정 전 | 수정 후 |
|---------|---------|
| `"new_fireball"` | `"NewFireball"` |
| `"new_chain_lightning"` | `"NewChainLightning"` |
| `"new_electric_sphere"` | `"NewElectricSphere"` |
| `"new_frost_nova"` | `"NewFrostNova"` |

### **Level Up IDs**
| 수정 전 | 수정 후 |
|---------|---------|
| `"fireball_level_up"` | `"FireballLevelUp"` |
| `"chain_lightning_level_up"` | `"ChainLightningLevelUp"` |
| `"electric_sphere_level_up"` | `"ElectricSphereLevelUp"` |
| `"frost_nova_level_up"` | `"FrostNovaLevelUp"` |

### **General Upgrade IDs**
| 수정 전 | 수정 후 |
|---------|---------|
| `"weapon_damage_boost"` | `"WeaponDamageBoost"` |
| `"weapon_speed_boost"` | `"WeaponSpeedBoost"` |
| `"health_boost"` | `"HealthBoost"` |
| `"movement_speed_boost"` | `"MovementSpeedBoost"` |

### **Target IDs**
| 수정 전 | 수정 후 |
|---------|---------|
| `"movement_speed"` | `"MovementSpeed"` |

---

## 🔧 수정 파일 목록

### **1. UpgradeSystem.cs**
- 하드코딩된 모든 `id =` 값들 (총 9개)
- `case` 문의 모든 문자열들 (총 10개)
- `prerequisites` 배열의 모든 값들 (총 6개)
- `GetWeaponNameFromPrerequisite()` 메서드의 case 문들 (총 6개)

### **2. UpgradeOptionUI.cs**
- `GetEnglishDescription()` 메서드의 모든 case 문들 (총 10개)
- `GetWeaponUpgradeValueText()` 메서드의 case 문들 (총 2개)
- `GetPlayerUpgradeValueText()` 메서드의 case 문들 (총 5개)

---

## ⚠️ 롤백 시 주의사항

1. **Inspector 설정도 함께 변경**: Unity Inspector의 All Upgrades 리스트도 이전 ID로 되돌려야 함
2. **prerequisites 배열**: 선행조건 참조가 모두 일치해야 함
3. **대소문자 구분**: ID는 대소문자를 정확히 구분함

---

## 📝 변경 로그
- **2025-08-17**: 초기 ID 변경 작업 시작
- **2025-08-17**: 코드 수정 완료 ✅
  - UpgradeSystem.cs: 27개 위치 수정 완료
  - UpgradeOptionUI.cs: 17개 위치 수정 완료
  - 총 44개 ID 변경 완료

## ✅ 수정 완료 상태
- [x] 모든 하드코딩된 ID 변경
- [x] 모든 case 문 변경  
- [x] 모든 prerequisites 배열 변경
- [x] 대소문자 구분 로직 제거 (.ToLower() 제거)
- [ ] Unity Inspector에서 All Upgrades 리스트 수정 필요