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
STEP 5A: CATIA Projection View API 우선 경로 + fallback 유지
다음 기능 목표: Marker 기반 부분 치수 생성
```

## STEP 5 결과

1. `TOP_VIEW` / `RIGHT_VIEW`는 CATIA API Projection View 방식이 우선 경로다.
2. 독립 Generative View 방식은 fallback으로 유지한다.
3. CATIA 트리 Projection View 아이콘과 원본 3D 수정 후 Drawing Update 연동은 수동 검증 완료 상태다.

## STEP 5A 결과 정리

현재 상태 구분은 다음처럼 해석한다.

- `ApiSuccessConfirmed`: API 경로가 자동 판정 가능한 범위에서 성공
- `ApiCandidateNeedsManualVerification`: 코드 내부 상태명은 유지 가능하나, 현재 프로젝트 기준 수동 검증은 이미 완료되었다.
- `ApiFailedFallbackSucceeded`: API 실패 후 fallback 성공
- `AllFailed`: API와 fallback 모두 실패

현재 운영 원칙:

1. CATIA API Projection View 경로를 기본 시도 경로로 유지한다.
2. 자동으로 Projection View 아이콘을 확정 판정하지 못하더라도, 수동 검증 완료 사실은 문서와 로그에 반영한다.
3. API 실패 시 독립 Generative View fallback을 사용한다.
4. fallback은 제거하지 않는다.

## 다음 기능 목표: Marker 기반 부분 치수 생성

목표:

1. 완전 자동 치수 생성은 구현하지 않는다.
2. 설계자가 지정한 Marker 기반 부분 치수만 생성 대상으로 삼는다.
3. `KEY_DIMENSION_POINTS`, `DIMENSION_POINT_*`, `DIMENSION_LINE_*`, `DIMENSION_PLANE_*`, `OUTER_DIMENSION_BOX` 계열 Marker 구조를 다음 단계에서 구체화한다.

## 보류 또는 취소된 로드맵 항목

```text
- Detail View 생성
- Section View 생성
- PDF 출력
- 표제란 자동 입력
- 자동 View 크기 계산
- 자동 배율 계산
- 완전 자동 치수 생성
```