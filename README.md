# CATIA V5 R35 Auto Drawing Generator

CATIA V5 R35 자동도면 생성기는 설계자가 3D 모델에 지정한 Marker 기반 도면 생성 기준 정보를 읽고, 회사 표준 CATDrawing 템플릿 위에 기본 3면도와 후속 주요 치수 생성을 보조 자동화하기 위한 Windows 프로그램이다.

현재 구현은 회사 표준 CATDrawing 템플릿을 열어 `SaveAs`하고, `GS_DRAWING_INFO` Marker 기반으로 `FRONT_VIEW`, `TOP_VIEW`, `RIGHT_VIEW`를 생성한다. 현재 `TOP_VIEW` / `RIGHT_VIEW`는 독립 Generative View 방식으로 안정 생성하며, CATIA API 기반 Projection View 방식은 향후 STEP 5A에서 다시 실험한다.

Detail View, Section View, PDF 출력, 표제란 자동 입력은 현재 보류 상태다.

## 실행 환경

- Windows
- CATIA V5 R35
- .NET 8 SDK 이상
- UI: WinForms
- CATIA 제어 방식: COM Automation API

## 템플릿 준비

사용자는 `templates` 폴더에 실제 회사 표준 CATDrawing 템플릿 파일을 직접 넣어야 한다.

필요한 파일명 규칙:

```text
templates/STD_A4_TEMPLATE.CATDrawing
templates/STD_A3_TEMPLATE.CATDrawing
templates/STD_A2_TEMPLATE.CATDrawing
templates/STD_A1_TEMPLATE.CATDrawing
```

템플릿에는 회사 표제란, 로고, 투상법 표시, 개정란, 기본 주석이 포함되어야 한다.

## 모델 Marker 준비

활성 CATPart에는 `GS_DRAWING_INFO` 안에 아래 Marker가 필요하다.

```text
MAIN_VIEW_PLANE
TOP_DIRECTION
```

`MAIN_VIEW_PLANE`은 정면으로 볼 기준면을 결정하고, `TOP_DIRECTION`은 도면의 기본 0도 위쪽 방향을 결정한다. UI에서는 `View Side`의 `Normal` / `Opposite` 선택으로 보이는 면을 바꾸고, `View Rotation`의 `0/90/180/270` 선택으로 같은 면을 유지한 채 도면상 회전을 보정한다.

향후 치수 생성을 위해 추가 Marker가 더 필요해질 수 있다. 현재는 다음 계열을 계획 중이며, 구체 명명 규칙은 다음 단계에서 확정한다.

```text
KEY_DIMENSION_POINTS
DIMENSION_POINT_*
DIMENSION_LINE_*
DIMENSION_PLANE_*
OUTER_DIMENSION_BOX
```

Global X/Y/Z 수동 방향 선택 UI는 폐기되었으며, Front View 방향 제어는 Marker 기반 방식만 사용한다.

## 설치 방법

1. .NET 8 SDK 이상을 설치한다.
2. `CatiaAutoDrawing/CatiaAutoDrawing.sln`을 Visual Studio에서 연다.
3. `src/CatiaAutoDrawing` 프로젝트를 빌드한다.

## 실행 방법

Visual Studio에서 `CatiaAutoDrawing` 프로젝트를 시작 프로젝트로 설정한 뒤 실행한다.

## 현재 구현 상태

- WinForms 기본 UI
- CATIA 연결 확인
- ActiveDocument 이름 및 타입 읽기
- 모델 표식 검사
- 도면 사이즈 A4/A3/A2/A1 선택
- View Side Normal/Opposite 선택
- View Rotation 0/90/180/270 선택
- 사이즈별 회사 표준 CATDrawing 템플릿 열기
- Marker 기반 `FRONT_VIEW` 생성
- 독립 Generative View 방식 `TOP_VIEW` / `RIGHT_VIEW` 생성
- output 폴더로 `활성문서명_도면사이즈.CATDrawing` SaveAs
- CATIA COM 예외 상세 로그 출력

## 다음 방향

- STEP 5A: CATIA API 기반 Projection View 생성 재실험
- 다음 주요 기능 목표: Marker 기반 부분 치수 생성
- Detail View / Section View / PDF / 표제란 자동 입력은 현재 보류

## 주의사항

- 빈 CATDrawing을 새로 생성하지 않는다.
- UI에서 CATIA API를 직접 호출하지 않는다.
- Front View 방향 제어는 `MAIN_VIEW_PLANE` + `TOP_DIRECTION` + `ViewSide` + `ViewRotation` 구조만 사용한다.
- 현재 `TOP_VIEW` / `RIGHT_VIEW` 안정 방식은 독립 Generative View 방식이며, Projection API 방식은 실험 완료 전까지 fallback을 유지한다.