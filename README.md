# CATIA V5 R35 Auto Drawing Generator

CATIA V5 R35 자동도면 생성기는 설계자가 3D 모델에 지정한 도면 생성 기준 정보를 읽어 CATDrawing 생성을 자동화하기 위한 Windows 프로그램입니다.

현재 단계는 기능 구현이 아니라 초기 아키텍처 구축 단계입니다. CATIA View 생성, 치수 생성, PDF 출력 등은 아직 구현하지 않습니다.

## 실행 환경

- Windows
- CATIA V5 R35
- .NET 8 SDK 이상
- UI: WinForms
- CATIA 제어 방식: COM Automation API

## 설치 방법

1. .NET 8 SDK 이상을 설치합니다.
2. 이 저장소의 `CatiaAutoDrawing/CatiaAutoDrawing.sln`을 Visual Studio에서 엽니다.
3. `src/CatiaAutoDrawing` 프로젝트를 빌드합니다.

## 실행 방법

Visual Studio에서 `CatiaAutoDrawing` 프로젝트를 시작 프로젝트로 설정한 뒤 실행합니다.

## 현재 구현 상태

- WinForms 기본 UI 생성
- 설정 모델 정의
- 로그 클래스 기본 구조 생성
- CATIA 연결 서비스 인터페이스 정의
- ActiveDocument 정보 DTO 정의
- 주요 기능 모듈의 역할과 TODO 정의

## 주의사항

- 아직 CATIA COM API 호출은 구현하지 않았습니다.
- CATIA 자동도면 생성 기능은 MVP 단계 이후 작은 단위로 구현합니다.
- UI에서 CATIA API를 직접 호출하지 않습니다.
