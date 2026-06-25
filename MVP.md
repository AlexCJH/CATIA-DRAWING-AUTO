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
다음 기능 목표: 치수 대상 지정 방식 탐색
```

## STEP 5 결과

1. `TOP_VIEW` / `RIGHT_VIEW`는 CATIA API Projection View 방식이 우선 경로다.
2. 독립 Generative View 방식은 fallback으로 유지한다.
3. CATIA 트리 Projection View 아이콘과 원본 3D 수정 후 Drawing Update 연동은 수동 검증 완료 상태다.

## STEP 6A: Color-based Dimension Target Detection

목표:

1. 색상 지정 형상 탐색
2. 색상 정보 읽기
3. 형상 타입 로그 출력
4. 치수 대상 그룹화 가능성 검토

제외:

```text
- 실제 치수 생성
- Detail View
- Section View
- PDF
- 표제란
- 자동 전체 치수
```

## 다음 단계 방향

- STEP 6B: 색상 또는 `GS_DIMENSION_TARGET` 기반 대상에서 실제 치수 생성 API 실험
- STEP 6C: Marker / Color / Folder 혼합 방식의 실무 규칙 확정

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