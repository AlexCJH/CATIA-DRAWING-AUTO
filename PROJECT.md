# Project Direction

## 최종 목표

CATIA V5 R35 환경에서 실행 중인 CATIA Application에 연결하고, 활성 CATPart 또는 CATProduct의 `GS_DRAWING_INFO` 구조를 검사하여 회사 표준 CATDrawing을 자동 생성하는 독립 실행형 Windows 프로그램을 개발합니다.

## 전체 아키텍처

- `MainForm`: 사용자 입력과 상태 표시만 담당합니다.
- `CatiaConnection`: CATIA 연결과 ActiveDocument 조회를 담당합니다.
- `ModelInspector`: 도면 생성을 위한 모델 표식 검사를 담당합니다.
- `DrawingGenerator`: 전체 도면 생성 흐름을 제어합니다.
- `ViewGenerator`: Front, Top, Right, Detail, Section View 생성을 담당합니다.
- `DimensionGenerator`: 외곽 치수와 주요 기준 치수 생성을 담당합니다.
- `TitleBlockWriter`: 표제란 데이터 입력을 담당합니다.
- `Export`: PDF 출력 등 외부 파일 생성을 담당합니다.
- `Config`: JSON 설정 로딩을 담당합니다.
- `Logging`: 파일 및 UI 로그 전달을 담당합니다.

## 자동화 범위

- 실행 중인 CATIA V5 R35 연결
- 활성 CATPart / CATProduct 확인
- `GS_DRAWING_INFO` 검사
- 기준면 기반 Front View 생성
- 삼각법 기반 Top / Right View 생성
- 조립부 / 매칭부 Detail View 생성
- 단면 View 생성
- 일부 기준 치수 자동 생성
- 회사 표준 템플릿 적용
- 표제란 자동 입력
- CATDrawing 저장
- 선택 시 PDF 출력

## 설계자 표식 규칙

- 필수 Geometry Set: `GS_DRAWING_INFO`
- 필수 기준 Plane: `MAIN_VIEW_PLANE`
- 필수 Top 방향 기준: `TOP_DIRECTION`
- 매칭면 Prefix: `MATCHING_FACE_`
- 조립면 Prefix: `ASSEMBLY_FACE_`
- 단면 Prefix: `SECTION_`
- Detail 영역 Prefix: `DETAIL_`

## 장기 개발 로드맵

1. CATIA 연결 확인
2. ActiveDocument 이름 및 타입 읽기
3. `GS_DRAWING_INFO` 존재 검사
4. 기준 Plane 및 Direction 검사
5. 새 CATDrawing 생성
6. 템플릿 적용
7. Front View 생성
8. Projection View 생성
9. Detail / Section View 생성
10. 치수 생성
11. 표제란 입력
12. 저장 및 PDF 출력
