# Project Direction

## 최종 목표

CATIA V5 R35 환경에서 실행 중인 CATIA Application에 연결하고, 활성 CATPart 또는 CATProduct의 도면 생성 기준 정보를 검증한 뒤 회사 표준 CATDrawing 템플릿을 열어 자동 도면 생성 흐름을 구성하는 Windows 프로그램을 개발한다.

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
- `DrawingGenerator`: CATDrawing 템플릿 열기, output 폴더 SaveAs 등 도면 생성 흐름만 담당한다.
- `ViewGenerator`: 향후 새 도면을 생성하지 않고, 열린 템플릿 도면에 View를 추가하는 역할을 담당한다.
- `DimensionGenerator`: 향후 검증된 View에 치수를 생성하는 역할을 담당한다.
- `TitleBlockWriter`: 향후 표제란 데이터 입력을 담당한다.
- `Export`: 향후 PDF 출력 또는 기타 파일 생성을 담당한다.
- `Config`: JSON 설정 로딩을 담당한다.
- `Logging`: 파일 및 UI 로그 전달을 담당한다.

## 자동화 범위

- 실행 중인 CATIA V5 R35 연결
- 활성 CATPart / CATProduct 확인
- `GS_DRAWING_INFO` 검사
- 회사 표준 CATDrawing 템플릿 열기
- 선택한 A4/A3/A2/A1 사이즈별 템플릿 선택
- output 폴더로 CATDrawing SaveAs
- 향후 열린 템플릿 도면에 View 추가
- 향후 Detail / Section View 생성
- 향후 치수 생성
- 향후 표제란 자동 입력
- 향후 선택적 PDF 출력

## 설계 표식 규칙

- 필수 Geometry Set: `GS_DRAWING_INFO`
- 필수 기준 Plane: `MAIN_VIEW_PLANE`
- 필수 Top 방향 기준: `TOP_DIRECTION`
- 매칭면 Prefix: `MATCHING_FACE_`
- 조립면 Prefix: `ASSEMBLY_FACE_`
- 단면 Prefix: `SECTION_`
- Detail 영역 Prefix: `DETAIL_`

## 초기 개발 로드맵

1. CATIA 연결 확인
2. ActiveDocument 이름 및 타입 읽기
3. `GS_DRAWING_INFO` 존재 검사
4. 기준 Plane 및 Direction 검사
5. 회사 표준 CATDrawing 템플릿 열기 및 SaveAs
6. 열린 템플릿 도면에 Front View 추가
7. STEP 4-1A: Front View 방향을 수동 선택값으로 지정하는 방식 검증
8. Projection View 추가
9. Detail / Section View 추가
10. 치수 생성
11. 표제란 입력
12. 저장 및 PDF 출력

## STEP 4-1A 방향

STEP 4-1A는 `MAIN_VIEW_PLANE` / `TOP_DIRECTION` 자동 해석을 구현하지 않는다. 사용자가 UI에서 선택한 Front View Direction과 Top Direction을 벡터로 변환해 CATIA Front View 방향 지정 API 동작을 검증한다.
