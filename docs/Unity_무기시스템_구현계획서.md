# Unity 무기 시스템 구현 계획서

## 1) 목표
- 현재 단일 무기(`DefaultWeaponPolicy`) 구조에서 **4개 장착 가능한 다중 무기 시스템**으로 확장한다.
- 기존 게임 루프를 크게 흔들지 않되, 새 무기 타입(회전/투사체/영역/지속딜)과 증강 영구 적용, 시너지 규칙을 구현한다.
- 코드 레벨에서 OOP + SOLID + 확장성 우선으로 구성한다.

## 2) 현재 구조와 병합 포인트
- 현재는 `IWeaponPolicy`가 플레이어 공격 수치와 스킬/궁극기 파라미터를 제공.
- `PlayerView`가 매 공격 쿼터마다 `OverlapCircleAll`로 단일 판정을 수행.
- `GameBootstrap`에서 `new DefaultWeaponPolicy()`를 단일 주입.
- `GameHudPresenter`에서 무기명/스탯을 화면에 표시.

### 전환 전략
1. `IWeaponPolicy`는 **호환 유지**를 위해 어댑터로 축소 사용.
2. 실제 전투는 새 무기 런타임 객체(`WeaponRuntime`)가 담당.
3. 기존 HUD/부트스트랩은 새 로더 인터페이스에서 값을 조회하도록 교체.

## 3) 추가할 핵심 타입

### Domain
- `WeaponId` (`enum`)
  - `Melee`, `Arrow`, `Boomerang`, `Turret`, `GroundZone`, `BlackHole`, `PoisonCloud`, `LaserPet`, `RagePet`, `StunPet`
- `WeaponType`
  - `Rotation`, `Projectile`, `Area`, `Persistent`
- `WeaponTargetingMode`
  - `FixedDirection`, `AutoNearest`
- `WeaponTemplateSO : ScriptableObject`
  - `id`, `name`, `description`
  - `attackType`, `targetingMode`
  - `icon`, `baseDamage`, `range`, `cooldown`, `projectileCount`
  - `dotPerSecond`, `stunTime`, `slowPercent`, `poisonNonLethal`, `isDamageOverTime`
  - `maxLevel`, `levelCurves`
- `WeaponLevelData`
  - `level`, `damageMultiplier`, `speedMultiplier`, `durationMultiplier`, `specialMultiplier`
- `WeaponUpgradeRuleSO`
  - 증강 카테고리, 적용 우선순위, 중첩 규칙
- `WeaponSynergyRule`
  - `sourceWeaponId`, `targetWeaponId`, `requiredLevel`, `bonusType`, `bonusValue`, `maxStack`
- `WeaponRuntime`
  - `template`, `level`, `upgradeState`
  - `ApplyUpgrade(...)`, `TryGetStatsAt(int stage)`
- `WeaponSlot`
  - `index`, `weaponRuntime`, `isLocked`
- `WeaponLoadout`
  - 슬롯 4개 고정 배열
  - `TryAddWeapon`, `TryApplyUpgrade`, `GetActiveWeapons`, `CanAddMore`

### Domain/Service 인터페이스
- `IWeaponProvider`
  - `IReadOnlyList<WeaponTemplateSO> GetAllTemplates()`
- `IUpgradeProvider`
  - `WeaponUpgradeRuleSO GetUpgradeRule(UpgradeKind kind)`
- `IWeaponSyncService`
  - 런/세션 직렬화/역직렬화

### Application
- `WeaponCatalogService`
  - 무기 템플릿 캐싱 및 조회
- `WeaponLoadoutService`
  - `InitializeFromRun(RunSessionService)`/`ApplyUpgradeChoice(...)`
- `UpgradeService`
  - 증강 선택(영구 적용), 제거 불가 제약 검증
  - 동일 증강 중복 규칙 적용
- `WeaponResolveService`
  - 공격 타이밍 계산: 모든 장착 무기 중 즉시 발동/쿨타임/타깃 계산

### Presentation
- `WeaponOrchestrator : MonoBehaviour`
  - 플레이어 위치 기준으로 장착 무기별 Update/Attack 실행
- `WeaponBehavior` (abstract)
  - `ExecuteAttack(AttackContext)`/`ApplyUpgrade(UpgradeContext)`
- 하위 동작 클래스
  - `RotationWeaponBehavior`
  - `ProjectileWeaponBehavior`
  - `AreaZoneWeaponBehavior`
  - `PersistentWeaponBehavior`
- `ITargetingStrategy`
  - `GetDirection(...)`, `GetTarget(...)`
  - 구현: `DirectionTargeting`, `AutoNearestTargeting`
- `WeaponSlotView`, `WeaponDetailView`
  - 슬롯별 무기 아이콘/레벨/업그레이드 히스토리 표시

## 4) 데이터 파이프라인
1. 런 시작 시 `WeaponCatalogService`가 `Resources/Weapons`의 템플릿 로드
2. `GameBootstrap`가 `WeaponLoadoutService` 초기화 및 `WeaponOrchestrator.Bind(...)`
3. 매 프레임:
   - 입력/이동 처리
   - 무기 오케스트레이터 `Tick(deltaTime, runState, runContext)`
   - 각 무기는 쿨다운/타깃/스킬 적용 후 공격 실행
4. `GameHudPresenter`는 슬롯 정보 + 현재 공격/보조 스탯 표기
5. 증강 선택 시 `UpgradeService`가 무기에 즉시 적용, HUD 갱신

## 5) 핵심 변경 코드 지점
- `GameBootstrap`
  - 무기 주입: `DefaultWeaponPolicy` -> `WeaponLoadoutService`/`WeaponCatalogService`로 교체
  - 궁극기/무기 UI 바인딩 구조 분리
- `PlayerView`
  - 기존 `ExecuteAttack()` 직접 판정 제거
  - `WeaponOrchestrator`에 위임
- `GameHudPresenter`
  - 기존 단일 무기 텍스트 → 슬롯 반복 렌더링으로 변경
- 신규 ScriptableObject/서비스 파일 추가

## 6) SOLID 검증 포인트
- SRP: 각 무기 스크립트는 자기 타입 판정/판정결과만 처리
- OCP: 신규 무기 추가는 `WeaponTemplateSO` + `WeaponBehavior` 추가만으로 반영
- LSP: 모든 무기 동작이 공통 인터페이스(`ExecuteAttack`)를 따름
- ISP: 타깃팅, 이펙트 처리, 업그레이드 적용을 분리
- DIP: 난수/시간/풀링/랜덤성을 모두 인터페이스로 주입

## 7) 병렬 작업 분리
- 무기 모델링/자원 담당: `WeaponTemplateSO`, 밸런스 데이터
- 전투 시스템 담당: `WeaponOrchestrator`, 동작 클래스
- UI 담당: 슬롯 뷰, 증강 UI, 시너지 뷰
- 운영/밸런스 담당: 스테이지별 규칙, 증강 확률, 제한치

## 8) 완료 기준 (MVP)
- 최대 4개 무기 장착, 슬롯 UI 표시
- 증강 1회 선택은 즉시 반영 + 롤백 불가
- 방향형/추적형 발사 타입 구분 적용
- 장판/블랙홀/독구름이 각각 별도 동작으로 분리 출력
- 기존 단일 무기에서 발생하던 기본 공격 동작 회귀 없음

## 9) 클릭 아키텍처(적용)
- HUD의 무기 슬롯은 `GameHudPresenter`가 `IWeaponLoadoutReadModel`을 바인딩해 생성/렌더링한다.
- 슬롯 클릭 이벤트는 `TrySelectWeapon(weaponId)` 호출만 수행하고, 상세 패널 오픈은 Presenter가 담당한다.
- 상세 데이터는 `WeaponDefinition + WeaponStats`로 읽기 전용 조회한다.
- 핵심 원칙:
  - 입력 처리(클릭)와 상태 변경(선택)은 `IWeaponLoadoutReadModel` 경계에서 분리
  - 렌더링(HUD)과 전투 처리(Player/Orchestrator)는 분리
  - 신규 무기 추가는 `WeaponDefinition` 카탈로그 확장으로 흡수
