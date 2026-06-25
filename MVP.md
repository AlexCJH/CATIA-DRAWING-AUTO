# MVP Scope

초기 MVP와 다음 단계 목표는 아래 범위로 정리한다.

```text
STEP 0: CATIA 실행 여부 확인
STEP 1: 활성 문서 이름 읽기
STEP 2: 활성 문서 표식 확인
STEP 3: CATDrawing 템플릿 열기 및 SaveAs 검증
STEP 4: 템플릿 CATDrawing에 Front View 1개 생성 후 SaveAs
STEP 4-1: MAIN_VIEW_PLANE + TOP_DIRECTION + ViewSide + ViewRotation 기반 Front View 방향 적용
STEP 5: TOP_VIEW / RIGHT_VIEW 생성
STEP 5A: CATIA Projection View API 재시도
다음 기능 목표: Marker 기반 부분 치수 생성
```

## STEP 5 현재 상태

1. `TOP_VIEW` / `RIGHT_VIEW`는 현재 독립 Generative View 방식으로 안정 생성된다.
2. 이 방식은 현재 stable fallback으로 유지한다.
3. CATIA Projection API 방식은 STEP 5A에서 별도로 검증한다.

## STEP 5A: CATIA Projection View API 재시도

목표:

1. `FRONT_VIEW` 기준 실제 CATIA Projection View API를 다시 실험한다.
2. `DefineProjectionView` 또는 다른 Projection API 호출 방식을 검증한다.
3. API 방식이 명확히 성공하면 해당 결과를 별도 로그 상태로 남긴다.
4. API 방식이 candidate view만 만들고 자동 검증이 불완전하면 manual verification required 상태로 남긴다.
5. API 방식이 실패하거나 빈 View로 판단되면 stable fallback인 독립 Generative View 방식으로 전환한다.

상태 구분:

- `ApiSuccessConfirmed`
- `ApiCandidateNeedsManualVerification`
- `ApiFailedFallbackSucceeded`
- `AllFailed`

제외 항목:

```text
- Detail View
- Section View
- PDF
- 표제란 자동 입력
- 치수 생성
```

## 다음 기능 목표: Marker 기반 부분 치수 생성

목표:

1. 완전 자동 치수 생성은 구현하지 않는다.
2. 설계자가 지정한 Marker 기반 부분 치수만 생성 대상으로 삼는다.
3. `KEY_DIMENSION_POINTS`, `DIMENSION_POINT_*`, `DIMENSION_LINE_*`, `DIMENSION_PLANE_*`, `OUTER_DIMENSION_BOX` 계열 Marker 구조를 다음 단계에서 구체화한다.

## 보류 또는 취소된 로드맵 항목

아래 항목은 현재 MVP/근접 로드맵에서 연기 또는 보류한다.

```text
- STEP 6~10 기존 자동 View 배치/정렬/검증 중심 계획
- STEP 12 표제란 자동 입력
- Detail View 생성
- Section View 생성
- PDF 출력
- 자동 View 크기 계산
- 자동 배율 계산
- 완전 자동 치수 생성
```

MVP 밖의 기능은 구현하지 않고 TODO 또는 별도 모듈의 향후 작업으로 남긴다.