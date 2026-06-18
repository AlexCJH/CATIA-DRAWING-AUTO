# Codex Development Rules

- 한 번에 하나의 기능만 구현한다.
- 기존 폴더 구조를 임의로 변경하지 않는다.
- 기능 추가 전 반드시 `MVP.md`를 확인한다.
- CATIA API를 추측해서 구현하지 않는다.
- 검증되지 않은 CATIA API 코드는 TODO 주석으로 유지한다.
- `MainForm`에 로직을 집중시키지 않는다.
- UI에서 CATIA COM API를 직접 호출하지 않는다.
- 로그 없는 기능을 구현하지 않는다.
- 항상 빌드 가능한 상태를 유지한다.
- 변경 후 `CHANGELOG.md`를 업데이트한다.
- 한 파일에 여러 모듈의 책임을 몰아넣지 않는다.
- PDF, View, Dimension 기능을 동시에 구현하지 않는다.
