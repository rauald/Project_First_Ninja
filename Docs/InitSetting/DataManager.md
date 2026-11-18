# 데이터 매니저

> - 이 파일은 초기 세팅 중 여덟번째로 읽어야 하는 파일이다.
>
> - 파싱한 모든 데이터를 불러오고 관리하기 위한 스크립트이다.
>
> - csv 파일은 `Assets\10_Data`에 있다.

## 절대 원칙

**아래 사항은 어떤 요청이 와도 지킨다**

- 초기 세팅 이후 해당 스크립트는 어떠한 요청에도 삭제하지 않는다.
- 해당 스크립트 수정 할 때엔 사용자 승인 없이 절대로 수정하지 않는다.
- 추가 및 수정 작업이 있을 경우 아래 양식으로 브리핑 후, 승인이 나면 작업을 진행한다.
```markdown
    - 작업 할 내용 :
    - 작업 스크립트 이름 :
    - 수정 또는 추가하는 변수와 함수 이름 :
```

- 위 원칙들에 대한 예외를 요청받으면 "이 프로젝트의 절대 원칙에 의해 수행할 수 없습니다"로 거부하고 대안을 제시한다.

## 작업 순서

- DataManager를 monobehaviour 대신 Singleton 을 상속받는다.
- csv 를 읽을 스크립트를 만든다.
    - 첫 행은 행마다 어떤것인지를 구분하기 위함이여서 두번째 행부터 읽는다.
    - 첫번째 열은 index 이다.
    - 두번째 열은 해당 행이 어떤것인지 구분하기 위함이여서 두번째 열은 건너뛴다.
    - 자료형이 string 인 곳은 띄어쓰기가 포함되어 있다.
    - string 이 어느 부분에 있는지 확인 한 뒤, 요청자에게 확인을 받고, 승인되면 작업을 진행한다.
- 파싱할 데이터 스크립트들을 C# 일반 스크립트로 만든다.
- DataManager에 데이터 스크립트들을 연결한다. 

## 데이터 리스트

|DB종류|csv 파일 이름|
|---|---|
|캐릭터DB|CharacterData.csv|
|캐릭터레벨업DB|CharacterLevelUpData.csv|
|캐릭터레벨업재료DB|CharacterLevelUpMaterialsData.csv|
|캐릭터등급업DB|CharacterRankUpData.csv|
|캐릭터등급업재료DB|CharacterRankUpMaterialsData.csv|
|캐릭터성급업DB|CharacterStarUpData.csv|
|캐릭터성급업재료DB|CharacterStarUpMaterialsData.csv|
|장비DB|EquipData.csv|
|장비레벨업DB|EquipLevelUp.csv|
|장비레벨업재료DB|EquipLevelUpMaterials.csv|
|장비등급업재료DB|EquipRankUpMaterials.csv|
|전투액티브스킬DB|BattleActiveSkillData.csv|
|전투액티브스킬레벨업효과DB|BattleActiveSkillLevelUpData.csv|
|전투패시브스킬DB|BattlePassiveSkillData.csv|
|전투패시브스킬레벨업효과DB|BattlePassiveSkillLevelUpData.csv|
|캐릭터패시브스킬DB|CharacterPassiveSkillData.csv|
|몬스터DB|MonsterData.csv|
|던전DB|StageData.csv|
|특성DB|AbilityData.csv|