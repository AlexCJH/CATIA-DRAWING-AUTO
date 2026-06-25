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

## STEP 3: CATDrawing 템플릿 열기 및 SaveAs 검증

목표:

1. 활성 CATPart 또는 CATProduct 확인
2. 사용자가 도면 사이즈 A4/A3/A2/A1 중 하나 선택
3. 선택한 사이즈에 해당하는 CATDrawing 템플릿 경로 확인
4. `CATIA Documents.Open(templatePath)`로 템플릿 열기
5. `output` 폴더에 `활성문서명_도면사이즈.CATDrawing`으로 `SaveAs`
6. 로그 출력

## STEP 4: 템플릿 CATDrawing에 Front View 1개 생성 후 SaveAs

목표:

1. CATIA 연결
2. 활성 CATPart 또는 CATProduct 확인
3. 템플릿 CATDrawing 열기
4. 첫 번째 Sheet 획득
5. 활성 CATPart 또는 CATProduct 기준 Front View 1개 생성 시도
6. View 이름을 `FRONT_VIEW`로 설정
7. View 위치를 Sheet 중앙 근처 임시 좌표로 배치
8. Scale을 `1.0`으로 설정
9. `output` 폴더에 CATDrawing `SaveAs`
10. 로그 출력

## STEP 4-1: MAIN_VIEW_PLANE + TOP_DIRECTION + ViewSide + ViewRotation 기반 Front View 방향 적용

목표:

1. CATPart의 `GS_DRAWING_INFO` 안에서 `MAIN_VIEW_PLANE`과 `TOP_DIRECTION` 검색
2. `MAIN_VIEW_PLANE` Reference 획득
3. `TOP_DIRECTION` Reference 획득
4. SPAWorkbench Measurable API로 Plane normal vector 추출
5. SPAWorkbench Measurable API로 Line direction vector 추출
6. `MAIN_VIEW_PLANE` normal vector로 정면으로 볼 면 결정
7. `TOP_DIRECTION` direction vector로 기본 0도 위쪽 방향 결정
8. `ViewSide`의 `Normal` / `Opposite` 값으로 Plane normal 방향 또는 반대 방향 선택
9. `ViewRotation`의 `0/90/180/270` 값으로 같은 면을 유지한 채 도면상 회전 보정
10. `viewRight`와 `viewUp` vector를 `DefineFrontView`에 적용

## STEP 5: TOP_VIEW / RIGHT_VIEW 생성

현재 상태:

1. `TOP_VIEW` / `RIGHT_VIEW`는 현재 독립 Generative View 방식으로 생성 성공 상태다.
2. CATIA Projection API 방식은 아직 안정 검증이 끝나지 않았고 STEP 5A에서 다시 실험한다.
3. 현재 독립 Generative View 방식은 안정 fallback으로 유지한다.

현재 목표:

1. 기존 `FRONT_VIEW` 생성 성공 후 `TOP_VIEW` 생성
2. 기존 `FRONT_VIEW` 생성 성공 후 `RIGHT_VIEW` 생성
3. 각 View를 동일 Sheet에 고정 위치로 배치
4. SaveAs 흐름 유지
5. 생성 성공/실패 로그를 Front View와 구분

## STEP 5A: CATIA Projection View API 재시도

목표:

1. `FRONT_VIEW` 기준 실제 CATIA Projection View API를 다시 실험한다.
2. `DefineProjectionView` 또는 다른 Projection API 호출 방식을 검증한다.
3. 성공 시 API 방식으로 전환 가능성을 검토한다.
4. 실패 시 현재 독립 Generative View 방식을 fallback으로 유지한다.

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