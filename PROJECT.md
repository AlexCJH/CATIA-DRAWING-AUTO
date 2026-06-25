# Project Direction

## 최종 목표

CATIA V5 R35 환경에서 실행 중인 CATIA Application에 연결하고, 활성 CATPart 또는 CATProduct의 Marker 기반 도면 생성 기준 정보를 해석해 회사 표준 CATDrawing 템플릿 위에 기본 3면도와 설계자가 지정한 주요 치수를 보조 자동 생성하는 Windows 프로그램을 개발한다.

이 프로젝트는 완전 자동 도면 생성기가 아니다. 설계자가 `GS_DRAWING_INFO`와 후속 치수 Marker를 명시적으로 준비한 모델을 대상으로, Marker 기반 보조 자동화를 제공하는 것이 현재의 최종 방향이다.

도면 문서는 빈 CATDrawing을 새로 생성하지 않는다. 사용자가 선택한 도면 사이즈에 맞는 회사 표준 CATDrawing 템플릿을 `templates` 폴더에서 열고, `output` 폴더에 `SaveAs` 하는 방식을 기본 생성 흐름으로 사용한다.

## 템플릿 구조

`templates` 폴더에는 실제 회사 표준 CATDrawing 파일을 사이즈별로 배치한다.

```text
templates/STD_A4_TEMPLATE.CATDrawing
templates/STD_A3_TEMPLATE.CATDrawing
templates/STD_A2_TEMPLATE.CATDrawing
templates/STD_A1_TEMPLATE.CATDrawing
```

각 템플릿에는 회사 표제란, 로고, 투상법 표시, 개정란, 기본 주석이 포함되어야 한다.

## 전체 아키텍처

- `MainForm`: 사용자 입력과 상태 표시만 담당한다.
- `CatiaConnection`: CATIA 연결과 ActiveDocument 조회를 담당한다.
- `ModelInspector`: 도면 생성을 위한 모델 표식 검사를 담당한다.
- `DrawingGenerator`: CATDrawing 템플릿 열기, SaveAs, 전체 흐름 제어만 담당한다.
- `ViewGenerator`: Front View, Projection View, fallback View 생성을 담당한다.
- `DimensionGenerator`: 향후 Marker 기반 부분 치수 생성만 담당한다.
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
- Marker 기반 부분 치수 생성

보류 또는 연기 범위:

- Detail View 생성
- Section View 생성
- PDF 출력
- 표제란 자동 입력
- 자동 View 크기 계산
- 자동 View 배치 계산

## 설계 표식 규칙

기본 View용 Marker:

- 필수 Geometry Set: `GS_DRAWING_INFO`
- 필수 기준 Plane: `MAIN_VIEW_PLANE`
- 필수 Top 방향 기준: `TOP_DIRECTION`

향후 치수용 Marker 방향:

- `KEY_DIMENSION_POINTS`
- `DIMENSION_POINT_*`
- `DIMENSION_LINE_*`
- `DIMENSION_PLANE_*`
- `OUTER_DIMENSION_BOX`

구체적인 치수 Marker 명명 규칙은 다음 단계에서 확정한다.

## 현재까지 완료된 단계

- STEP 0: CATIA 연결 확인
- STEP 1: ActiveDocument 이름 및 타입 읽기
- STEP 2: `GS_DRAWING_INFO` 및 Marker 검사
- STEP 3: 회사 표준 CATDrawing 템플릿 Open + SaveAs
- STEP 4: Marker 기반 `FRONT_VIEW` 생성
- STEP 4-1: `ViewSide` / `ViewRotation` 기반 방향 제어
- STEP 5: `TOP_VIEW` / `RIGHT_VIEW` 생성
  - 현재 안정 방식: 독립 Generative View 방식
  - 유지 원칙: fallback으로 계속 보존

## STEP 5A CATIA API Projection View 실험

STEP 5A는 `FRONT_VIEW`를 기준으로 실제 CATIA Projection View API를 다시 시도하는 실험 단계다.

현재 원칙은 다음과 같다.

- CATIA API 방식은 먼저 시도한다.
- API 방식이 명확히 성공하면 해당 결과를 로그로 구분한다.
- API 방식이 예외 없이 candidate view만 만들었고 자동 검증이 불완전하면 manual verification required 상태로 기록한다.
- API 방식이 실패하거나 빈 View로 판단되면 기존 독립 Generative View fallback으로 `TOP_VIEW` / `RIGHT_VIEW`를 생성한다.
- fallback은 API 검증이 끝날 때까지 제거하지 않는다.

## 초기 개발 로드맵

1. STEP 0: CATIA 연결
2. STEP 1: ActiveDocument 읽기
3. STEP 2: Marker 검사
4. STEP 3: CATDrawing 템플릿 Open + SaveAs
5. STEP 4: Marker 기반 `FRONT_VIEW` 생성
6. STEP 4-1: `ViewSide` / `ViewRotation`
7. STEP 5: `TOP_VIEW` / `RIGHT_VIEW` 생성
8. STEP 5A: CATIA API 기반 Projection View 생성 재시도
9. STEP 6: Marker 기반 부분 치수 생성
10. 후순위 보류: View 자동 배치, PDF, Detail View, Section View, 표제란 자동 입력

## 방향 제어 원칙

STEP 4-1의 최종 Front View 방향 제어 방식은 `GS_DRAWING_INFO` 안의 `MAIN_VIEW_PLANE` + `TOP_DIRECTION` Marker 기반이다. `MAIN_VIEW_PLANE` normal vector는 정면으로 볼 면을 결정하고, `TOP_DIRECTION` direction vector는 기본 0도 위쪽 방향을 결정한다. 사용자는 UI의 `View Side`로 normal/opposite 면을 선택하고 `View Rotation`의 `0/90/180/270` 값으로 같은 면 안에서 도면상 회전만 보정한다.

Global XYZ 수동 방향 선택 방식은 실험 완료 후 폐기되었으며 다시 도입하지 않는다.