# Project Direction

## 최종 목표

CATIA V5 R35 환경에서 실행 중인 CATIA Application에 연결하고, 활성 CATPart 또는 CATProduct의 Marker 기반 도면 생성 기준 정보를 해석해 회사 표준 CATDrawing 템플릿 위에 기본 3면도와 설계자가 지정한 주요 치수를 보조 자동 생성하는 Windows 프로그램을 개발한다.

이 프로젝트는 완전 자동 도면 생성기가 아니다. 설계자가 `GS_DRAWING_INFO`와 후속 치수 대상 정보를 명시적으로 준비한 모델을 대상으로, 실무 친화적인 보조 자동화를 제공하는 것이 현재의 최종 방향이다.

도면 문서는 빈 CATDrawing을 새로 생성하지 않는다. 사용자가 선택한 도면 사이즈에 맞는 회사 표준 CATDrawing 템플릿을 `templates` 폴더에서 열고, `output` 폴더에 `SaveAs` 하는 방식을 기본 생성 흐름으로 사용한다.

## 전체 아키텍처

- `MainForm`: 사용자 입력과 상태 표시만 담당한다.
- `CatiaConnection`: CATIA 연결과 ActiveDocument 조회를 담당한다.
- `ModelInspector`: 도면 생성을 위한 모델 표식 검사를 담당한다.
- `DrawingGenerator`: CATDrawing 템플릿 열기, SaveAs, 전체 흐름 제어만 담당한다.
- `ViewGenerator`: Front View, Projection View, fallback View 생성을 담당한다.
- `DimensionGenerator`: 향후 치수 대상 탐색과 치수 생성 실험을 담당한다.
- `TitleBlockWriter`: 현재 로드맵에서는 보류한다.
- `Export`: 현재 로드맵에서는 PDF 출력을 보류한다.
- `Config`: JSON 설정 로딩을 담당한다.
- `Logging`: 파일 및 UI 로그 전달을 담당한다.

## 자동화 범위

현재 목표 범위:

- 실행 중인 CATIA V5 R35 연결
- 활성 CATPart / CATProduct 확인
- `GS_DRAWING_INFO` 검사
- 회사 표준 CATDrawing 템플릿 열기
- 선택한 A4/A3/A2/A1 사이즈별 템플릿 선택
- output 폴더로 CATDrawing SaveAs
- 열린 템플릿 도면에 Marker 기반 `FRONT_VIEW` 추가
- `FRONT_VIEW` 기준 `TOP_VIEW` / `RIGHT_VIEW` 추가
- 치수 대상 지정 방식 탐색

보류 또는 연기 범위:

- 실제 치수 생성 대량 자동화
- Detail View 생성
- Section View 생성
- PDF 출력
- 표제란 자동 입력
- 자동 View 크기 계산
- 자동 View 배치 계산

## STEP 5A 상태

- CATIA API 기반 `TOP_VIEW` / `RIGHT_VIEW` Projection View 생성 수동 검증 완료
- 독립 Generative View 방식은 fallback으로 유지
- CATIA 트리 Projection View 아이콘 확인 및 원본 3D 수정 후 Drawing Update 연동 확인

## STEP 6 방향 변경

기존 STEP 6의 목표였던 “Marker 기반 일부 치수 생성”은 바로 구현하지 않는다. 설계자 UX를 고려해, 먼저 치수 대상 지정 방식을 단순화할 수 있는지 검토한다.

새 구조:

1. STEP 6A: Color-based Dimension Target Detection
2. STEP 6B: 색상 또는 폴더 기반 대상에서 실제 치수 생성 API 실험
3. STEP 6C: Marker / Color / Folder 혼합 방식의 실무 규칙 확정

## 치수 대상 지정 후보

- 색상 기반 방식
- `GS_DIMENSION_TARGET` 폴더 기반 방식
- 이름 Prefix 방식
- Publication 방식은 장기 검토
- Parameter 문자열 방식은 후순위

색상 기반 후보 예시:

- 빨간색 평면 2개: 두 평면 사이 거리 치수 후보
- 파란색 원통면 1개: 지름 치수 후보
- 노란색 원통면 2개: 중심 간 거리 치수 후보
- 초록색 면: 기준면 치수 후보

이번 STEP 6A에서는 실제 치수를 생성하지 않는다. 우선 다음만 검토한다.

- 특정 색상이 부여된 Face / Edge / Surface / HybridShape 탐색 가능 여부
- 색상 정보 읽기 가능 여부
- 형상 타입 구분 가능 여부
- 색상별 치수 대상 그룹화 가능 여부

## 초기 개발 로드맵

1. STEP 0: CATIA 연결
2. STEP 1: ActiveDocument 읽기
3. STEP 2: Marker 검사
4. STEP 3: CATDrawing 템플릿 Open + SaveAs
5. STEP 4: Marker 기반 `FRONT_VIEW` 생성
6. STEP 4-1: `ViewSide` / `ViewRotation`
7. STEP 5: `TOP_VIEW` / `RIGHT_VIEW` 생성
8. STEP 5A: CATIA API 기반 Projection View 생성 및 fallback 유지
9. STEP 6A: Color-based Dimension Target Detection
10. STEP 6B: 색상 또는 폴더 기반 대상에서 치수 생성 API 실험
11. STEP 6C: Marker / Color / Folder 혼합 규칙 확정
12. 후순위 보류: Detail View, Section View, PDF, 표제란 자동 입력

## 방향 제어 원칙

STEP 4-1의 최종 Front View 방향 제어 방식은 `GS_DRAWING_INFO` 안의 `MAIN_VIEW_PLANE` + `TOP_DIRECTION` Marker 기반이다. `MAIN_VIEW_PLANE` normal vector는 정면으로 볼 면을 결정하고, `TOP_DIRECTION` direction vector는 기본 0도 위쪽 방향을 결정한다. 사용자는 UI의 `View Side`로 normal/opposite 면을 선택하고 `View Rotation`의 `0/90/180/270` 값으로 같은 면 안에서 도면상 회전만 보정한다.

Global XYZ 수동 방향 선택 방식은 실험 완료 후 폐기되었으며 다시 도입하지 않는다.