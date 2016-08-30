# TmxImporter

tiled map editor:
[http://www.mapeditor.org](http://www.mapeditor.org)

tmx map format:
[http://doc.mapeditor.org/reference/tmx-map-format/](http://doc.mapeditor.org/reference/tmx-map-format/)

### 설명
Tiled Map Editor 로 만든 결과물을 Unity3D로 불러오는 기능.

작업시 Unity3D 버전: 5.3

### 사용법
TmxImporterUnityProject 의 Assets/Scenes/Main.unity 참고

### 새로운 씬에서 사용법
빈 씬을 만들었을시, 빈 게임오브젝트는 만들고 Assets/Modules/TmxImporter/TmxImporter.cs 를 컴포넌트로 추가한다.

인스펙터의 추가한 컴포넌트의 tmx file에 tmx 확장자 파일을 드래그드랍후 인스펙터 상의 import 버튼 누른다. 

tmx 파일을 파싱해서 데이터를 만들어 준다.

만들어주는 데이터는 다음과 같다.

* 이미지 파일 중복시 복제
* 이미지 파일을 sprite 형태로 바꾸고, sprite 들로 잘라준다.
* AnimationClip 생성
* AnimatorController 생성

문제 없이 작업이 끝나면 인스펙터 창의 construct 버튼을 누른다. 

map 이름의 게임오브젝트가 생성된다.

이때 하는 작업들은 다음과 같다.

* 레이어 별로 게임오브젝트를 만들어 주고, order 값을 조정함으로 해서 보여주는 순서를 조정한다.
* 충돌 정보를 만들어준다. 현재 다음 2가지 타입 지원
    * Rect
    * Polygon
* 에니메이션 생성. 에니메이션은 플레이 후에 확인 가능하다.

### 구현 안된 기능

* 
