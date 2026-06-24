# MVP Scope

초기 MVP는 아래 범위로 고정한다.

```text
STEP 0: CATIA 실행 여부 확인
STEP 1: 활성 문서 이름 읽기
STEP 2: 활성 문서 표식 확인
STEP 3: CATDrawing 템플릿 열기 및 SaveAs 검증
STEP 4: 템플릿 CATDrawing에 Front View 1개 생성 후 SaveAs
STEP 4-1A: Front View Global Axis 수동 방향 선택 검증 완료 및 폐기
STEP 4-1B: MAIN_VIEW_PLANE + TOP_DIRECTION + ViewSide + ViewRotation 기반 Front View 방향 적용
STEP 5: Projection View 생성
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

## STEP 4-1A: Front View Global Axis 수동 방향 선택 검증 완료 및 폐기

결과:

1. `+X`, `-X`, `+Y`, `-Y`, `+Z`, `-Z` Global Axis 수동 선택 방식은 CATIA `DefineFrontView` API 검증용 실험으로 완료했다.
2. 실제 부품의 도면 기준면이 CATIA Global XYZ축과 평행하지 않은 경우 원하는 정면도를 안정적으로 만들기 어렵다는 한계를 확인했다.
3. 현재 UI, Config, Context, 생성 흐름에서는 Global Axis 수동 선택 방식을 폐기한다.
4. Front View 방향 제어는 STEP 4-1B Marker 기반 방식만 사용한다.

## STEP 4-1A 제외 항목

```text
- Global Axis 수동 선택 방식 재도입
- Projection View 생성
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
```

## STEP 4-1B: MAIN_VIEW_PLANE + TOP_DIRECTION + ViewSide + ViewRotation 기반 Front View 방향 적용

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

## STEP 4-1B 제외 항목
```text
- Projection View 생성
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
- 표제란 자동 입력
```

## STEP 5: Projection View 생성

목표:

1. 기존 `FRONT_VIEW` 생성 성공 후 Projection View 생성을 시작한다.
2. `FRONT_VIEW`를 기준으로 `TOP_VIEW`를 생성한다.
3. `FRONT_VIEW`를 기준으로 `RIGHT_VIEW`를 생성한다.
4. `TOP_VIEW`는 `FRONT_VIEW` 위쪽에 임시 고정 좌표로 배치한다.
5. `RIGHT_VIEW`는 `FRONT_VIEW` 오른쪽에 임시 고정 좌표로 배치한다.
6. Scale은 `FRONT_VIEW`와 동일하게 유지한다.
7. Projection View 생성 성공/실패 로그를 Front View 생성 로그와 구분한다.
8. Projection View 생성 실패 시에도 CATDrawing SaveAs 흐름은 유지한다.

## STEP 5 제외 항목

```text
- Global Axis 수동 방향 선택 기능 재도입
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
- 표제란 자동 입력
- 자동 View 크기 계산
- 자동 배율 계산
```

MVP 밖의 기능은 구현하지 않고 TODO 또는 별도 모듈의 향후 작업으로 남긴다.

