# MVP Scope

초기 MVP는 아래 범위로 고정합니다.

```text
STEP 0: CATIA 실행 여부 확인
STEP 1: 활성 문서 이름 읽기
STEP 2: 활성 문서 타입 확인
STEP 3: 로그 출력
```

## MVP에서 제외할 항목

```text
- View 생성
- Drawing 생성
- Template 적용
- Detail View
- Section View
- Dimension
- PDF Export
```

MVP 밖의 기능은 구현하지 않고 TODO 또는 `NotImplementedException`으로 남깁니다.
