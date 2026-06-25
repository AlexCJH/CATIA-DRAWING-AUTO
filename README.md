# CATIA V5 R35 Auto Drawing Generator

CATIA V5 R35 자동도면 생성기는 설계자가 3D 모델에 지정한 Marker 기반 도면 생성 기준 정보를 읽고, 회사 표준 CATDrawing 템플릿 위에 기본 3면도와 후속 주요 치수 생성을 보조 자동화하기 위한 Windows 프로그램이다.

현재 구현은 회사 표준 CATDrawing 템플릿을 열어 `SaveAs`하고, `GS_DRAWING_INFO` Marker 기반으로 `FRONT_VIEW`, `TOP_VIEW`, `RIGHT_VIEW`를 생성한다.

현재 구현 상태:

- CATIA API 기반 `TOP_VIEW` / `RIGHT_VIEW` Projection View 생성
- 독립 Generative View fallback 유지
- 원본 3D 수정 후 Drawing Update 연동 수동 확인 완료

## 실행 환경

- Windows
- CATIA V5 R35
- .NET 8 SDK 이상
- UI: WinForms
- CATIA 제어 방식: COM Automation API

## 모델 Marker 준비

활성 CATPart에는 `GS_DRAWING_INFO` 안에 아래 Marker가 필요하다.

```text
MAIN_VIEW_PLANE
TOP_DIRECTION
```

`MAIN_VIEW_PLANE`은 정면으로 볼 기준면을 결정하고, `TOP_DIRECTION`은 도면의 기본 0도 위쪽 방향을 결정한다. UI에서는 `View Side`의 `Normal` / `Opposite` 선택으로 보이는 면을 바꾸고, `View Rotation`의 `0/90/180/270` 선택으로 같은 면을 유지한 채 도면상 회전을 보정한다.

## 현재 구현 상태

- CATIA 연결 확인
- ActiveDocument 이름 및 타입 읽기
- 모델 표식 검사
- 도면 사이즈 A4/A3/A2/A1 선택
- View Side Normal/Opposite 선택
- View Rotation 0/90/180/270 선택
- 사이즈별 회사 표준 CATDrawing 템플릿 열기
- Marker 기반 `FRONT_VIEW` 생성
- CATIA API 기반 `TOP_VIEW` / `RIGHT_VIEW` Projection View 생성
- 독립 Generative View fallback 유지
- 원본 3D 수정 후 Drawing Update 연동 확인
- output 폴더로 `활성문서명_도면사이즈.CATDrawing` SaveAs
- CATIA COM 예외 상세 로그 출력

## 다음 방향

기존 다음 방향이었던 Marker 기반 부분 치수 생성은 바로 구현하지 않는다.

STEP 6A에서는 다음을 먼저 검토한다.

- 색상 지정 형상 탐색
- 형상 타입 확인
- 색상 기반 또는 `GS_DIMENSION_TARGET` 기반 치수 대상 지정 가능성 확인
- 실제 치수 생성은 다음 단계로 이월

장기적으로는 색상 기반 또는 `GS_DIMENSION_TARGET` 기반 치수 대상 탐색 후, Marker 기반 일부 치수 생성으로 확장한다.

## 주의사항

- 빈 CATDrawing을 새로 생성하지 않는다.
- UI에서 CATIA API를 직접 호출하지 않는다.
- Front View 방향 제어는 `MAIN_VIEW_PLANE` + `TOP_DIRECTION` + `ViewSide` + `ViewRotation` 구조만 사용한다.
- CATIA API Projection View가 실패하면 독립 Generative View fallback을 사용한다.