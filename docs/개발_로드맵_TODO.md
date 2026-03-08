# 개발 로드맵 (TODO)

## 기준
- 기간: 3주 스프린트 기준(1주 1단위)
- 목표: 구현 가능한 순차적 확장
- 우선순위: P0(필수), P1(핵심), P2(보강)

## Sprint 1 (기본 인프라 + 핵심 구조)

### P0
1. [x] 무기 도메인 모델 추가
   - `WeaponTemplateSO`, `WeaponRuntime`, `WeaponSlot`, `WeaponLoadout` 생성
2. [x] `WeaponOrchestrator` 기본 프레임 구현
3. [x] `PlayerView` 공격 로직을 오케스트레이터로 분기
4. [x] `GameBootstrap`에서 무기 시스템 초기화 경로 추가

### P1
1. [x] 4슬롯 UI 프로토타입 (아이콘/레벨/잠금상태)
2. [x] 증강 데이터(`WeaponUpgradeRuleSO`) 및 즉시 적용 적용기 생성
3. [x] 증강 영구 적용/제거 불가 규칙 강제

### P1 보너스
1. [x] 기존 HUD에 슬롯별 DPS/쿨다운/공격타입 표시
2. [x] 기본 공격 데미지/쿨다운/범위가 무기별로 달라지도록 분기

## 현재 구현 상태 (업데이트)
- `WeaponLoadoutService` 기반 4슬롯 기본 로드아웃 초기화가 `GameBootstrap`에 연결됨.
- HUD는 좌상단 세로 슬롯 리스트 + 슬롯 클릭 상세 패널(확인 버튼 닫기) 구조로 동작.
- 레벨업 선택 시 기본 업그레이드(공격력 선택)에서 전체 슬롯 레벨이 함께 상승하도록 연결됨.
- 플레이어 이동 입력은 `FrameTick` 우선 + `MoveAxis` fallback 구조로 보강됨(입력 포트 일시 불안정 시 이동 끊김 완화).
- 플레이어 공격 넉백은 옵션화(`PlayerView`): 일반 몬스터만 넉백 적용, 보스는 넉백 면역.
- 초기 화력 밸런스는 `WeaponCatalog` 및 `StageProfile` 기본값 기준으로 하향 조정됨.

### 완료 기준
- 게임 실행 시 4개 슬롯이 비어있는 상태로 시작
- 무기 장착/공격이 단일 무기와 동일하게 동작(회귀 없음)
- 증강 선택 즉시 효과 반영

## Sprint 2 (무기 타입 구현)

### P0
1. [x] 회전 무기 동작 구현
2. [x] 투사체 무기 공통 기반 구현(생성/이동/사망/쿨다운)
   - 화살, 부메랑, 포탑(체력+어그로) 구현
3. [x] 영역 무기 기반 구현
   - 장판, 블랙홀, 독구름

### P1
1. [x] 지속딜 펫형 구현
   - 레이저/광란/기절
2. [x] 목표 추적 전략 분리
   - 고정 방향형
   - 자동 가까운 적 추적형

### P2
1. [x] 속성 효과(슬로우/기절/중독) 정합성 테스트
2. [x] 오브젝트 풀 연동

### 완료 기준
- 10개 무기 타입이 모두 생성/공격 가능
- 방향형/추적형 발사 분기 동작
- 장판/독/기절/둔화가 적중 시 확인됨

## Sprint 3 (시너지 + 보스 + 점수)

### P0
1. [x] 시너지 규칙(2중 조합) 엔진 추가
2. [x] 10단계 보스 스폰 파이프라인
3. [x] 스테이지 기반 맵 교체 호출 연동

### P1
1. [x] 종료 화면 점수/통계 UI 확장
2. [x] 게임오버/재시작 흐름 정리
3. [x] 밸런스 튜닝 지표 수집 로그(라운드 종료)

### P2
1. [x] 적 타입 추가(자폭, 증식, 군집)
2. [ ] 보스 상성(무기 타입/속성) 분기

### 완료 기준
- 스테이지 100에서 종료/재시작 동작
- 시너지 1개 이상 발동
- 보스 간 구간 전환과 점수표시 안정 동작

## 리스크/점검 항목
- 증강 제거 불가로 인한 초반 과투입 방지(최대치 제한 필요)
- 다중 무기 동시 판정 성능(적 수치 급증 구간)
- DOT/중독 비치명 규칙 예외 처리 충돌
- 시너지 계산 중복 적용/중복 스택 경합

## Boss 리팩토링 실행 체크리스트 (현재 스프린트 반영)

### P0 (이번 스프린트 필수)
1. [x] 보스 스킬 도메인 계약 추가
   - `Assets/Scripts/Domain/Boss/BossRuntimeContracts.cs`
   - `ITargetingStrategy`, `ISkillEffect`, `IDamageCalculator`, `IProjectileSpawner`, `IAreaResolver`, `ICooldownScheduler`, `ISkillSelector`, `IBossSkillExecutor`
2. [x] ScriptableObject 기반 보스 설정 스키마 1차 추가
   - `Assets/Scripts/Domain/Boss/BossConfigSo.cs`
   - `BossConfigSO`, `BossPhaseSO`, `BossSkillSO`, `TargetingStrategySO`, `SkillEffectSO`(+ 기본 구현)
3. [x] 보스 스폰 책임 분리 1차
   - `SpawnService.TrySpawnBoss` 내부 계산 로직을 `BossSpawnPolicy`로 이관
   - 파일: `Assets/Scripts/Application/Boss/BossSpawnPolicy.cs`

### P1 (다음 구현 단위)
1. [x] 보스 스킬 런타임 서비스 기본 구현
   - `Assets/Scripts/Application/Boss/BossSkillRuntimeServices.cs`
   - `BossSkillCatalog`, `BossCooldownScheduler`, `WeightedSkillSelector`, `BossSkillExecutor`, `BossPhaseResolver`
2. [ ] 보스 1종 PoC에 `BossSkillExecutor` 실제 연결
   - `BossController` 또는 보스 전용 런타임 어댑터 신규 도입
   - 기존 단일 공격 루틴과 동등 동작 검증
3. [ ] 스킬 파이프라인 이펙트를 실제 전투 판정과 연결
   - `DamageCalculator`/`ProjectileSpawner`/`AreaResolver` 인프라 어댑터 구현

### P1 (진행 업데이트)
1. [x] 보스 1종 런타임 어댑터 연결
   - `Assets/Scripts/Presentation/Boss/BossSkillBrain.cs`
   - `GameBootstrap` 보스 스폰 경로에서 `BossSkillBrain` 초기화 연결
2. [x] 전투 어댑터 1차 연결
   - `BossSkillBrain` 내부에 `BossDamageCalculator`, `BossProjectileSpawner`, `BossAreaResolver` 연결
   - `DamageEffect`/`AreaDamageEffect`가 실제 타깃 컨텍스트를 통해 적용

### P2 (안정화/운영)
1. [ ] Boss SO 유효성 검사기(에디터) 추가
   - 필수 필드/중복 ID/위상(phase) 범위/Null 참조 검증
2. [ ] DI 조립 경계 명확화
   - `GameBootstrap` 직접 `new` 축소, 보스 조립용 installer/composition 단계 분리
3. [ ] KPI 기반 디버그 로그 추가
   - `SkillAttempt`, `CastFail`, `TargetMissing`, `HitSuccess`
