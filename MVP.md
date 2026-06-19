# MVP Scope

초기 MVP는 아래 범위로 고정한다.

```text
STEP 0: CATIA 실행 여부 확인
STEP 1: 활성 문서 이름 읽기
STEP 2: 활성 문서 표식 확인
STEP 3: CATDrawing 템플릿 열기 및 SaveAs 검증
```

## STEP 3: CATDrawing 템플릿 열기 및 SaveAs 검증

목표:

1. 활성 CATPart 또는 CATProduct 확인
2. 사용자가 도면 사이즈 A4/A3/A2/A1 중 하나 선택
3. 선택한 사이즈에 해당하는 CATDrawing 템플릿 경로 확인
4. `CATIA Documents.Open(templatePath)`로 템플릿 열기
5. `output` 폴더에 `활성문서명_도면사이즈.CATDrawing`으로 `SaveAs`
6. 로그 출력

## STEP 3 제외 항목

```text
- 새 빈 CATDrawing 생성
- View 생성
- Projection View 생성
- Detail View 생성
- Section View 생성
- Dimension 생성
- PDF 출력
- 표제란 자동 입력
```

MVP 밖의 기능은 구현하지 않고 TODO 또는 별도 모듈의 향후 작업으로 남긴다.
