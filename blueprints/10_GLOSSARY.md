# 用語集（Glossary）

> 目的：用語の揺れ・誤解を減らし、仕様/設計/実装の会話コストを下げる。

---

## 1. シーン/画面

### Boot
起動直後の初期化用シーン（任意）。セーブロードやマスタ初期化を担当。

### Title（TitleRoot）
タイトル画面本体。StageSelect/Optionを“タイトルの上にモーダル表示”する。

### Game
戦闘・ストーリー進行を行うシーン。

### Modal / Overlay
- Modal：入力を奪う（PauseMenu, GameOverなど）
- Overlay：常駐表示（HUD, 会話ログなど）

### ScreenStack
「TitleRootの上にStageSelect/Optionを重ねる」「InGameでPauseを重ねる」など、画面の重なりを管理する考え方。

---

## 2. 状態（Domain）

### GameMode
Title / StageSelect / Option / InGame など、ゲーム全体の大枠モード。

### InGameMode
StoryBeforeBattle / Battle / StoryAfterBattle など、InGame内のサブモード。

### BattlePhase
BattleStart → ConversationIntro → BossBoot → Combat → BossDefeated → ConversationOutro → BattleEnd / GameOver

### PauseState
isPaused のこと。Pause中でもUI入力は動く（unscaled time）。

---

## 3. データ

### MasterData（マスターデータ）
ScriptableObjectなどで持つ“静的パラメータ”。例：EnemyStaticParams, StageDefinitionなど。

### SaveData（永続データ）
JSONで保存するデータ。例：ステージ解放、クリアランク、設定。

### StageProgress
各ステージのアンロック状況とクリアランクを持つ。

### GameSettings
BGM/SE音量やON/OFFなどの設定。Optionで即時反映し、閉じると保存。

---

## 4. バトル要素

### Energy（エネルギー）
攻撃アイコン起動に使用される資源。フィールド上で合成される。

### SpecialEnergy（特殊エネルギー）
特殊攻撃アイコン起動に使用される資源。

### AttackSequence
攻撃/特殊攻撃の進行定義。energyCost / dropMultiplier などを持つ。

### dropMultiplier
攻撃によるドロップ倍率。ボスのエネルギードロップ量計算に影響。

---

## 5. アーキテクチャ用語

### Layered / Clean風
Domain（純C#）を中心に、Application（手順）、Contracts（境界IF）、Presentation（Unity/UI）、Infrastructure（I/O実装）に分ける。

### MVP（UI）
Viewは見た目とイベント発火だけ。PresenterがUseCaseを呼び、Viewを更新する。

### Port / Adapter
Port = Contractsのインタフェース。Adapter = Infrastructureの実装。

### 手動DI（Composition Root）
EntryPointでGameServicesを作ってPresenterへ渡す方式（DIコンテナは使わない）。

---

## 9. 表記ルール
- “Phase” は BattlePhase を指す
- “Screen” は UI画面（S10など）を指す
- “Master” は ScriptableObject の静的定義
- “Save” は JSON永続化
