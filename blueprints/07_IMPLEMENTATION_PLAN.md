# 実装企画書

## 0. 目的

本書は、既存の設計入力（要件定義／アーキテクチャ／ドメインモデル／状態遷移／UIフロー／データ設計）をもとに、**Unity/C#で実装するための作業計画（タスク化・順序・運用）**を確定する。

ゴールは次の状態：

* Vertical Slice（縦切り）を最短で成立させる
* 以後の拡張（全ステージ／演出／調整）に耐える最低限の規律（層分割・依存ルール）を守る
* 少人数のチームで「今日やること」が迷わない Backlog と運用ルールを持つ

---

## 1. スコープ（実装対象の整理）

### 1.1 コア（Must：ゲームとして成立）

* Title（はじめる／ステージ選択／オプション／やめる）
* StageSelect / Option（Title上のフローティングUI、戻るESC/B）
* GameScene：IntroStory（任意）→ Battle → OutroStory（任意）→ Titleへ戻りStageSelect自動表示
* BattlePhase（開始→会話→ボス起動→戦闘→撃破→終了／ゲームオーバー）
* Pause（GameOver中は不可／BGM継続、SE停止、ゲーム進行停止）
* GameOver（Retry＝BossBootから／StageSelectへ戻る）
* プレイヤー（移動、被弾、無敵、ダッシュ吸収、ボス接触ダメージ）
* ロボット攻撃アイコン（スイッチ踏み→攻撃シーケンス→弾生成）
* 敵（雑魚）／ボス（複数HPゲージ、フェーズ、ドロップ間隔制限）
* 弾（RobotBullet/EnemyBullet）、アイテム（エネルギー合成、上限管理、回復・無敵等）
* HUD（自機HP、ボスHP複数ゲージ、エネルギー、特殊エネルギー）
* Save/Load（JSON）：ステージ解放、クリアランク、設定（Option閉じ・ステージクリアで保存）

### 1.2 早期に入れたい（Should：初期版で欲しい）

* クリアランク算出（最低限：仮の条件でOK → 後で差替）
* 画質設定（exe版のみ：解像度／ウィンドウ・フルスクリーン）
* 自機HP 3割未満の点滅＋SE
* デバッグ導線（スポーン・当たり判定可視化・無敵切替）

### 1.3 余裕があれば（Could）

* 演出強化（パーティクル、フェード、カメラ）
* チュートリアル
* 難易度
* ローカライズ（textKey化）

---

## 2. 実装方針（アーキテクチャ／データ／フローの要点）

### 2.1 レイヤ構成（依存の規律）

* Domain（純C#）：ルール・状態（GameSession/BattleContext/Player等）
* Application（純C#）：ユースケース（GameFlow/Option/StageClear）
* Contracts（純C#）：境界I/F（ISaveRepository, IMasterDataRepository, ISettingsApplier等）＋最小DTO
* Presentation（Unity）：MonoBehaviour/UI/入力/表示/Prefab/Pool
* Infrastructure（Unity/純C#）：JSON保存、設定反映、SOリポジトリ

禁止：

* Domain/Application から UnityEngine を参照しない
* UI OnClick から SceneLoad 直叩きしない（UseCase経由）
* 個別オブジェクトが GameMode/BattlePhase を直接書き換えない（UseCase→GameSession更新）

### 2.2 状態の真実（Single Source of Truth）

* **Domain：GameSession** が GameMode/InGameMode/PauseState/currentStage 等を保持
* Battleの状態は BattleContext（BattlePhase、Player/Robot/Boss/Enemy/Bullet/Item）

### 2.3 データの3分類

* MasterData：ScriptableObject（StageDefinition/Params/Definitions/Stories等）
* RuntimeState：純C#（GameSession/BattleContext/各Entity）
* SaveData：純C# Serializable → JSON

---

## 3. Vertical Slice（縦切り）定義

### 3.1 最初の縦切りゴール（M1）

「見た目は仮」で良いので、以下が一連で動くこと：

1. Titleで「はじめる」を押す
2. Stage1開始（GameScene）
3. IntroStory（任意：1ページでも可）をSpace/Aで進められる
4. BossBoot（タイトル表示）→ Combatに入る
5. 自機が移動でき、ダッシュができ、敵弾を吸収してエネルギーが増える
6. ロボットの1つの攻撃アイコンを踏むと弾が出てボスにダメージが入る
7. ボスHPゲージを0にすると撃破 → BattleEnd → Titleへ戻りStageSelectが自動表示
8. Optionで音量を触ると即時反映し、閉じると保存される

### 3.2 縦切りの最小コンテンツ

* ステージ：1つ（stage_01）
* 敵：雑魚1種（召喚は後回しでも可）
* ボス：ゲージ1〜2本（まずは2本推奨、UI検証）
* 攻撃：通常攻撃シーケンス1つ（特殊攻撃は後）
* アイテム：エネルギーのみ（回復/無敵は後）

---

## 4. フェーズ（マイルストーン）

### Phase 0：基盤構築（最初の数日〜1週）

* リポジトリ／Unityプロジェクト初期化
* asmdef分割（Domain/Application/Contracts/Infrastructure/Presentation）
* Boot/Title/Game のScene雛形
* Composition Root（BootEntryPoint）実装：Saveロード→設定反映→Titleへ

成果：プロジェクトが「規律を守った形」で起動し、画面遷移の土台がある

### Phase 1：Vertical Slice（M1）

* Title→Game→Titleの一周
* Story（最小）／Battle（最小）／HUD（最小）／Option保存

成果：最低限遊べる（試遊デモ可能）

### Phase 2：要件充足の完成形（M2）

* 5ステージ前提のStageSelect、解放／ランク保存
* ボス複数ゲージ・フェーズ／雑魚召喚
* アイテム（回復・無敵・特殊エネルギー）
* GameOver/Pauseの仕様完全対応

成果：仕様どおりの機能が一通り揃う

### Phase 3：ブラッシュアップ（M3）

* 演出、調整、デバッグツール充実
* 最適化（Profiler → ボトルネックタスク化）

成果：完成度向上、提出/公開品質へ

---

## 5. タスク分割ルール（ID・粒度・完了条件）

### 5.1 粒度

* 1タスク＝数時間〜1日
* 大きい場合は「検証タスク」と「実装タスク」に割る

### 5.2 タスクID体系（例）

* SET：基盤（Project/asmdef/CI等）
* FLOW：ゲームフロー（UseCase/状態遷移/Scene）
* UI：画面/UI（Title/StageSelect/Option/HUD/Pause/GameOver）
* STORY：紙芝居/会話
* BTL：バトル進行
* PL：プレイヤー
* ROB：ロボット/攻撃
* EN：敵/ボス
* IT：アイテム
* DATA：SO/Save/Repository
* DBG：デバッグ/テスト

### 5.3 DoD（Definition of Done）最低要件

* 実機（PlayMode）で再現手順があり、例外ログが出ない
* 仕様にあるガード条件（例：GameOver中ポーズ不可）が満たされる
* Domain/Application に UnityEngine参照が混入していない
* 影響範囲が分かる（依存タスク・変更箇所メモ）

---

## 6. 初期バックログ（Phase 0〜Phase 1：縦切り中心）

### 6.1 Phase 0：基盤

* SET-01 asmdef分割（Domain/Application/Contracts/Infrastructure/Presentation）
* SET-02 フォルダ構成作成（Scripts/Game/...、MasterData、Prefabs、Scenes）
* SET-03 BootScene/TitleScene/GameScene雛形作成
* SET-04 BootEntryPoint（手動DI）：Repository生成→SaveLoad→Settings適用→Titleへ
* DATA-01 Contracts定義（ISaveRepository/IMasterDataRepository/ISettingsApplier）
* DATA-02 JsonSaveRepository（保存先・破損時バックアップ）
* DATA-03 UnitySettingsApplier（Audio/Graphicsの最小反映）
* DATA-04 ScriptableObjectMasterDataRepository（Stage/Paramsの最小取得）

依存：SET-01 → DATA系（参照関係固定）

### 6.2 Title/Option/StageSelect（縦切り必須）

* FLOW-01 GameFlowUseCase雛形（StartFromTitle/Open/Close/StartGame/ReturnToTitleWithStageSelect）
* UI-01 TitleRoot（S10）実装：フォーカス移動、決定でUseCase呼び出し
* UI-02 StageSelectWindow（S11）実装：解放状況表示、選択StartGame、ESCでClose
* UI-03 OptionWindow（S12）実装：BGM/SEスライダー+ON/OFF、即時反映、閉じで保存
* UI-04 ScreenStack/最前面判定（戻る優先順位の統一窓口）

依存：FLOW-01 → UI-01/02/03

### 6.3 GameSceneの縦切り（Story→Battle→Exit）

* FLOW-02 GameSceneEntryPoint：StageId受領→IntroStory有無判定→進行開始
* STORY-01 StoryPlayer（純C#）最小実装（Start/Next/IsFinished）
* UI-05 StoryScreen（S31）：画像＋文章、Space/Aで進行

依存：DATA-04（StorySequence取得）→ STORY-01 → UI-05 → FLOW-02

### 6.4 Battle（最小：BossBoot→Combat→BossDefeated→End）

* BTL-01 BattleContext生成（Player/Robot/Boss最低限）
* BTL-02 BattleFlowService最小（BossBoot→Combat→BossDefeated→BattleEnd）
* UI-06 BossTitleOverlay（S33：一定時間で終了）
* UI-07 GameHUD（S30：HP/エネルギー/ボスHPゲージ）

依存：BTL-01 → BTL-02 → UI-06/UI-07

### 6.5 Player（移動・被弾・無敵・ダッシュ吸収）

* PL-01 入力（移動/ダッシュ）取得（Presentation）→Domainへ渡す経路
* PL-02 Player.Move（ロボット内移動・壁衝突は暫定）
* PL-03 ダッシュ（開始/持続/減速/CT）＋ダッシュ中無敵
* PL-04 EnemyBullet吸収（衝突通知→エネルギー加算＋弾消失）
* PL-05 被弾→無敵3秒（通常時のみ）

依存：BTL-01（Player存在）→ PL-01/02 → PL-03 → PL-04/05

### 6.6 Robot攻撃（アイコン1つで弾発射→ボスダメージ）

* ROB-01 AttackIcon/AttackSequenceService最小（開始/攻撃/終了のタイマ）
* ROB-02 スイッチ踏み判定（Presentation）→ TryTriggerAttack
* ROB-03 RobotBullet生成要求（SpawnRequest）→ Pool/Prefab生成
* EN-01 Boss被弾処理（TakeDamage、ゲージ減少、0判定）

依存：ROB-01 → ROB-02 → ROB-03 → EN-01

### 6.7 Pause/GameOver（縦切りで最低限）

* UI-08 PauseMenu（S40）：再開／ステージ選択へ
* BTL-03 PauseService（GameOver中不可、isPaused切替）
* UI-09 GameOver（S41）：Retry（BossBootへリセット）／ステージ選択へ
* BTL-04 Retryリセット（BattleContext.ResetForRetry）

依存：BTL-02 → BTL-03/UI-08、EN/PLの死亡条件 → UI-09/BTL-04

### 6.8 Save/Loadトリガ

* DATA-05 OptionUseCase：Apply→Close時Save
* DATA-06 StageClearUseCase：BattleEnd時にランク算出（暫定）→Save

依存：FLOW-01/BTL-02 → DATA-05/06

---

## 7. 依存関係（詰まりやすいポイント）

* Input方式（旧/新InputSystem）を早期に確定（UIフォーカスと共存）
* MasterData ID設計（StageId/ItemId等）を先に固定（Saveとの結合点）
* Pauseの時間（scaled/unscaled）を最初から意識：UIはunscaled
* 画面スタック（戻る優先順位）を共通化しないと、UIが増えるほど破綻

---

## 8. 開発運用

### 8.1 週次（30〜60分）

* 次のマイルストーンに対して「今週のMust」を確定
* 依存で詰まっているタスクを優先的に潰す（仕様穴/unknown数値など）

### 8.2 日次（5〜10分）

* 今日やるタスクを最大2〜3個に絞る
* 終了時にステータス更新（Done / Doing / Blocked）

### 8.3 ブランチ/レビュー（最小ルール）

* feature/* で作業 → dev → main
* Domain/Application を触る変更は、相互にレビュー（責務侵食チェック）

---

## 9. リスクと対策（先回り）

* **unknownパラメータ多数**：すべてSO（PlayerStaticParams等）へ寄せて差替可能にする
* **UIフォーカス移動が複雑化**：ScreenStack＋FocusNavigationを最小実装で早期に通す
* **オブジェクト増加で破綻**：SpawnRequest＋Pool方針を縦切りから採用
* **レイヤ侵食**：asmdefで物理的に防ぐ（UnityEngine参照禁止）

---

## 付録A：タスクテンプレ

* ID：
* タイトル：
* 目的：
* 依存：
* 作業：
* DoD（完了条件）：
* 確認手順：
* メモ（将来の拡張／リスク）：
