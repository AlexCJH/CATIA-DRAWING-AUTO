# MVP Scope

초기 MVP는 아래 범위로 고정한다.

```text
STEP 0: CATIA 실행 여부 확인
STEP 1: 활성 문서 이름 읽기
STEP 2: 활성 문서 표식 확인
STEP 3: CATDrawing 템플릿 열기 및 SaveAs 검증
STEP 4: 템플릿 CATDrawing에 Front View 1개 생성 후 SaveAs
STEP 4-1A: Front View 수동 방향 선택 검증
STEP 4-1B: MAIN_VIEW_PLANE + TOP_DIRECTION Marker 기반 Front View 방향 적용
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

## STEP 4 제외 항목

```text
- MAIN_VIEW_PLANE 기준 방향 적용
- TOP_DIRECTION 적용
- Projection View 생성
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
- 표제란 자동 입력
```

## STEP 4-1A: Front View 수동 방향 선택 검증

목표:

1. 사용자가 Front View Direction을 `+X`, `-X`, `+Y`, `-Y`, `+Z`, `-Z` 중 하나로 선택
2. 사용자가 Top Direction을 `+X`, `-X`, `+Y`, `-Y`, `+Z`, `-Z` 중 하나로 선택
3. 선택값을 방향 벡터로 변환
4. Front View Direction과 Top Direction이 서로 평행하면 오류 처리
5. CATIA `DefineFrontView`에 수동 방향 벡터 적용
6. Front View 방향 변화 확인

## STEP 4-1A 제외 항목

```text
- MAIN_VIEW_PLANE 자동 해석
- TOP_DIRECTION 자동 해석
- Projection View 생성
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
```

## STEP 4-1B: MAIN_VIEW_PLANE + TOP_DIRECTION Marker 기반 Front View 방향 적용

목표:

1. CATPart의 `GS_DRAWING_INFO` 안에서 `MAIN_VIEW_PLANE`과 `TOP_DIRECTION` 검색
2. `MAIN_VIEW_PLANE` Reference 획득
3. `TOP_DIRECTION` Reference 획득
4. SPAWorkbench Measurable API로 Plane normal vector 추출
5. SPAWorkbench Measurable API로 Line direction vector 추출
6. 두 벡터를 Normalize
7. `TOP_DIRECTION`이 `MAIN_VIEW_PLANE` normal과 평행하면 오류 처리
8. `TOP_DIRECTION`이 `MAIN_VIEW_PLANE`과 평행하지 않으면 오류 처리
9. Front View 방향에 Marker 기반 벡터 적용

## STEP 4-1B 제외 항목

```text
- Projection View 생성
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
- 표제란 자동 입력
```

MVP 밖의 기능은 구현하지 않고 TODO 또는 별도 모듈의 향후 작업으로 남긴다.
