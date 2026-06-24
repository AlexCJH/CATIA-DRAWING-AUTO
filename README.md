# CATIA V5 R35 Auto Drawing Generator

CATIA V5 R35 자동도면 생성기는 설계자가 3D 모델에 지정한 도면 생성 기준 정보를 읽고, 회사 표준 CATDrawing 템플릿을 기반으로 CATDrawing 저장 흐름을 자동화하기 위한 Windows 프로그램이다.

현재 구현은 회사 표준 CATDrawing 템플릿을 열어 `SaveAs`하고, `GS_DRAWING_INFO` marker 기반으로 Front View 1개를 생성한다. Projection View, Detail View, Section View, Dimension 생성, PDF 출력, 표제란 자동 입력은 아직 구현하지 않는다.

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

활성 CATPart에는 `GS_DRAWING_INFO` 안에 아래 marker가 필요하다.

```text
MAIN_VIEW_PLANE
TOP_DIRECTION
```

`MAIN_VIEW_PLANE`은 정면으로 볼 기준면을 결정하고, `TOP_DIRECTION`은 도면의 기본 0도 위쪽 방향을 결정한다. UI에서는 `View Side`의 `Normal` / `Opposite` 선택으로 보이는 면을 바꾸고, `View Rotation`의 `0/90/180/270` 선택으로 같은 면을 유지한 채 도면상 회전을 보정한다.

Global X/Y/Z 수동 방향 선택 UI는 폐기되었으며, Front View 방향 제어는 marker 기반 방식만 사용한다.

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
- Marker 기반 Front View 1개 생성
- output 폴더로 활성문서명_도면사이즈.CATDrawing SaveAs
- CATIA COM 예외 상세 로그 출력

## 주의사항

- 빈 CATDrawing을 새로 생성하지 않는다.
- UI에서 CATIA API를 직접 호출하지 않는다.
- Front View 방향 제어는 `MAIN_VIEW_PLANE` + `TOP_DIRECTION` + `ViewSide` + `ViewRotation` 구조만 사용한다.

