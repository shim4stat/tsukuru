# データ設計書

## 0. 参照ドキュメント

* アーキテクチャ設計書
* ドメインモデル設計書3（提出ファイル名：ドメインモデル設計書4）
* 要件定義書

本書は上記と矛盾しない形で、Unity＋C#における **マスターデータ（ScriptableObject）／ランタイム状態（純C#）／永続データ（SaveData/JSON）** の設計を確定する。

---

## 1. 目的

* 調整対象（パラメータ）をコードから分離し、少人数でも変更に強い運用を可能にする。
* **Unity依存（Prefab/Sprite/AudioMixer等）** と **ゲームルール（純C#）** の境界を保ち、テスト容易性と保守性を高める。
* セーブデータの破壊的変更を最小化し、バージョン管理・移行ポリシーを定める。

---

## 2. データの3分類と配置（最重要）

### 2.1 分類

1. **マスターデータ（静的）**

* 例：StageDefinition / PlayerStaticParams / EnemyStaticParams / BossStaticParams / AttackSequenceDefinition / BulletDefinition / ItemDefinition / StorySequenceDefinition
* 変更：主に開発中の調整で変更（ビルド後は基本固定）
* 表現：ScriptableObject（Unityアセット）

2. **ランタイム状態（可変）**

* 例：GameSession / BattleContext / Player（現在HP/エネルギー等）/ Boss（現在ゲージ等）/ Bullet / ItemInstance
* 変更：毎フレーム変化し得る
* 表現：純C#（Domain）＋必要に応じてPresentationでView

3. **永続データ（セーブ/設定）**

* 例：SaveData（ステージ解放、クリアランク、設定）
* 変更：プレイを跨いで保持
* 表現：`[Serializable]` な純C#クラス → JSON保存（Infrastructure）

### 2.2 レイヤと責務（アーキテクチャ準拠）

* **Domain（純C#）**：

  * SaveData / GameSession / BattleContext など「状態とルール」
  * UnityEngine型（GameObject/ScriptableObject/Sprite等）は持たない
* **Contracts（純C#）**：

  * `IMasterDataRepository` / `ISaveRepository` / `ISettingsApplier` 等のポート
  * 層を跨ぐDTO（必要最小限）
* **Infrastructure（Unity/純C#）**：

  * ScriptableObjectを読み取り `IMasterDataRepository` を実装
  * JSONで `ISaveRepository` を実装
  * 設定反映を `ISettingsApplier` として実装
* **Presentation（Unity）**：

  * Prefab/Sprite/Audio等のUnityアセット参照
  * UI表示・入力をUseCase/Domainへ橋渡し

---

## 3. ID設計（参照の基盤）

### 3.1 目的

* セーブデータとマスターデータを安全に結び付ける（UnityEngine.Object参照をセーブに入れない）。
* 将来の追加・削除・並び替えに耐える。

### 3.2 IDの形式

* 原則：**不変・一意・自然言語を避ける**
* 型：基本は `string`（人間が読める）
* 例（推奨接頭辞）：

  * `stage_01` …（StageId）
  * `enemy_slime_01` …（EnemyId）
  * `boss_01` …（BossId）
  * `item_energy_small` …（ItemId）
  * `bullet_robot_rapid` …（RobotBulletId）
  * `bullet_enemy_basic` …（EnemyBulletId）
  * `atk_seq_a` …（AttackSequenceId）
  * `story_stage01_intro` …（StoryId）
  * `speaker_player` …（SpeakerId）

### 3.3 参照ルール

* **マスター同士**：SO参照で良い（編集性優先）
* **セーブ**：IDのみ（文字列・数値のみ）
* **ロード時**：ID → Repositoryで解決（辞書引き）

### 3.4 ID検証

* Editor/PlayModeで「重複ID検知」を実行できること（OnValidateや起動時チェック）。

---

## 4. マスターデータ設計（ScriptableObject）

> 方針：
>
> * “ゲームルールに必要な数値” と “Unity表示に必要な参照（Prefab/Sprite等）” は同一SOに入れても良いが、**DomainはUnity型を直接触らない**。
> * Domainが必要とする値は `IMasterDataRepository` が **純C#の読み取りモデル（DTO/record/struct）** として返す。

### 4.1 StageDefinition（ステージ定義）

* 目的：タイトルのステージ選択、開始ステージ決定、紙芝居の有無判定に使う。
* 主フィールド例：

  * `string id`（StageId）
  * `string displayName`
  * `int orderIndex`（並び順）
  * `bool hasIntroStory` / `string introStoryId`
  * `bool hasOutroStory` / `string outroStoryId`
  * （任意）`bool hasIntroConversation` / `bool hasOutroConversation`（バトル内会話の有無）

### 4.2 PlayerStaticParams（自機：静的パラメータ）

要件の静的パラメータをすべてデータ化する。

* `int maxHp`
* `int maxEnergy`
* `int maxSpecialEnergy`（要件：最大3）
* `float walkSpeed`
* `float invincibleAfterHitSeconds`（要件：3秒）
* `float dashSpeed`
* `float dashDeceleration`
* `float dashCooldownSeconds`
* `float dashDurationSeconds`
* `int dashDamageToBoss`
* `float hitRadiusNormal`
* `float hitRadiusDash`

### 4.3 EnemyStaticParams（雑魚敵：静的パラメータ）

* `string id`（EnemyId）
* `int maxHp`
* `int attackPower`
* `int absorbableEnergyAmount`（ダッシュ吸収/撃破時に得られる量）
* `List<DropRateEntry> dropTable`（下記）

`DropRateEntry`

* `string itemId`
* `int rate`（要件：100超えも可）

（表示側のみ）

* `GameObject prefab`（Presentationが使用）

### 4.4 BossStaticParams（ボス：静的パラメータ）

* `string id`（BossId）
* `List<int> gaugeMaxHps`（複数ゲージ最大HP）
* `int baseDropEnergyAmount`
* `float minDropIntervalSeconds`（要件：0.1秒に1回まで）
* （任意）`List<BossPhaseDefinition> phases`（攻撃パターン参照。詳細未確定のため拡張枠として確保）

`BossPhaseDefinition`（拡張枠）

* `string phaseId`
* `string enemySpawnPatternId`（雑魚召喚パターン）
* `string bulletPatternId`（弾幕パターン）

### 4.5 AttackSequenceDefinition（攻撃/特殊攻撃シーケンス）

ロボットの攻撃アイコンに紐付く定義。

* `string id`（AttackSequenceId）
* `bool isSpecial`
* `int energyCost`（通常）
* `int specialEnergyCost`（要件：特殊は1）
* `float phaseStartSeconds`
* `float phaseAttackSeconds`
* `float phaseEndSeconds`
* `string robotBulletId`（生成するBullet種別）
* `float dropMultiplier`（要件：攻撃によってドロップ倍率変動）

### 4.6 RobotBulletDefinition / EnemyBulletDefinition（弾定義）

`RobotBulletDefinition`

* `string id`
* `int damage`
* `float lifetimeSeconds`
* `bool destroyOnHit`
* `float dropMultiplier`
* （表示側のみ）`GameObject prefab`

`EnemyBulletDefinition`

* `string id`
* `int damage`
* `float lifetimeSeconds`
* `int absorbableEnergyAmount`（ダッシュ吸収で得られる）
* （表示側のみ）`GameObject prefab`

### 4.7 ItemDefinition（アイテム定義）

要件：回復薬/無敵/エネルギー/特殊エネルギー。

* `string id`（ItemId）
* `ItemType type`（Energy/Heal/Invincible/SpecialEnergy）
* `int maxCountOnField`（要件：回復3 / 無敵2 / エネルギー200）
* `bool canMerge`（要件：エネルギーは合成）

タイプ別パラメータ例：

* Heal：`int healHpAmount`
* Invincible：`float invincibleSeconds`
* Energy：`int baseEnergyAmount`

（表示側のみ）

* `GameObject prefab`
* `Sprite icon`

### 4.8 ItemSpawnParams（スポーン/ドロップの調整用パラメータ）

「ルール」はDomainに置くが、閾値やクールタイムは調整対象としてデータ化する。

* `float lowHpHealThresholdRatio`（要件：0.2）
* `float lowHpHealCooldownSeconds`（要件：30）
* `float naturalEnergySpawnRate`（未確定：自然発生の強さ）
* `Vector2 naturalSpawnArea`（ロボット内の発生範囲定義：実装都合で必要なら）

### 4.9 StorySequenceDefinition（紙芝居/会話）

要件：一枚絵＋文章、Space/Aで進行。

* `string id`（StoryId）
* `List<StoryPageDefinition> pages`

`StoryPageDefinition`

* `Sprite background`
* `string text`（将来ローカライズなら `textKey` 推奨）
* `string speakerId`（任意）

### 4.10 FocusGraphDefinition（UIフォーカス隣接）※仕様未確定のため拡張枠

* 目的：フォーカス移動（隣接選択肢）をデータで定義しやすくする。
* 例：画面ごとにグラフを持つ

  * `string screenId`（title_menu / pause_menu / gameover_menu / stage_select / option など）
  * `List<FocusEdge>`

`FocusEdge`

* `string fromElementId`
* `FocusDirection dir`
* `string toElementId`

---

## 5. ランタイム状態設計（Domain：純C#）

### 5.1 主要ルート

* `GameSession`

  * GameMode（Title/StageSelect/Option/InGame）
  * InGameMode（StoryBeforeBattle/Battle/StoryAfterBattle）
  * PauseState（isPaused）
  * currentStageId
  * `BattleContext?` / `StoryPlayer?`

* `BattleContext`

  * `BattlePhase`（BattleStart/ConversationIntro/BossBoot/Combat/BossDefeated/ConversationOutro/BattleEnd/GameOver）
  * `Player` / `Robot` / `Boss`
  * `List<Enemy>` / `List<RobotBullet>` / `List<EnemyBullet>` / `List<ItemInstance>`

### 5.2 マスターとの結びつけ

* ランタイムエンティティは、

  * 初期化時に `IMasterDataRepository` から静的値を受け取り
  * プレイ中は **動的値のみ** を更新する。

例：

* `Player` は `PlayerStaticParams` 由来の上限・速度などを保持（純C#の読み取りモデルとして保持）
* `Enemy` は `EnemyStaticParams` を参照して初期HP、吸収エネルギー等を決定

---

## 6. 永続データ設計（SaveData / JSON）

### 6.1 保存要件

* JSONで管理
* 保存タイミング：

  * 各ステージクリア時
  * 設定ウィンドウを閉じたとき
* 保存内容：

  * ステージのアンロック状況
  * 各ステージのクリアランク
  * 設定情報

### 6.2 SaveData スキーマ（推奨）

`SaveData`

* `int version`
* `List<StageProgress> stageProgresses`
* `GameSettings settings`

`StageProgress`

* `string stageId`
* `bool isUnlocked`
* `StageRank? clearRank`

`GameSettings`

* `Volume bgmVolume`
* `Volume seVolume`
* `GraphicsSettings graphicsSettings`

`Volume`

* `float value`（0〜1）
* `bool isEnabled`（チェックボックス）

`GraphicsSettings`（exe版のみ）

* `Resolution resolution`
* `WindowMode windowMode`

### 6.3 バージョン管理

* `version` を必須とする。
* 破壊的変更（フィールド削除・意味変更）時に+1。
* ロード時に version差がある場合：

  * 軽微（追加のみ）：デフォルト値で補完
  * 破壊的：移行（Migration）処理を実装、困難なら初期化＋バックアップ保持

### 6.4 例外/破損時ポリシー

* JSONが読めない：

  * 既存ファイルをバックアップ名で退避
  * デフォルトSaveDataを生成し続行

---

## 7. Repository / 保存・供給インタフェース（Contracts）

### 7.1 IMasterDataRepository（読み取り専用）

最低限、ユースケースとドメインが必要とする取得APIを提供する。

* `StageDefinitionModel GetStage(StageId id)`
* `PlayerParamsModel GetPlayerParams()`
* `EnemyParamsModel GetEnemyParams(EnemyId id)`
* `BossParamsModel GetBossParams(BossId id)`
* `AttackSequenceModel GetAttackSequence(AttackSequenceId id)`
* `ItemModel GetItem(ItemId id)`
* `StorySequenceModel GetStory(StoryId id)`

※ `*Model` は **純C#の読み取りモデル**（Unity型を含まない）

### 7.2 ISaveRepository

* `Save(SaveData data)`
* `LoadOrCreateDefault()`

### 7.3 ISettingsApplier

* `ApplySettings(GameSettings settings)`

---

## 8. 保存形式・ファイル配置（Infrastructure）

### 8.1 JSON実装（推奨）

* 書式：UTF-8 JSON
* 保存先：`Application.persistentDataPath` 配下
* ファイル名例：`save.json`

### 8.2 セーブトリガ（Application側）

* `StageClearUseCase`：StageResult → SaveData更新 → `ISaveRepository.Save`
* `OptionUseCase`：設定変更 → `ISettingsApplier.ApplySettings` → ウィンドウ閉じでSave

---

## 9. 編集・運用フロー

### 9.1 当面の方針（小規模）

* マスター：ScriptableObjectをInspector編集
* 大量データ化したくなったら、後からスプレッドシート→CSV/JSON導入（ハイブリッド移行）

### 9.2 Git運用の注意

* SOアセットはバイナリになりがち：

  * 変更競合が起きやすいデータ（大量の表）は将来的にCSV化を検討

---

## 10. フォルダ構成（推奨）

```
Assets/
  MasterData/
    Stages/
    Player/
    Enemies/
    Bosses/
    AttackSequences/
    Bullets/
    Items/
    Stories/
    UI/
  Prefabs/
  Scenes/
  Scripts/
    Game/
      Domain/
      Application/
      Contracts/
      Infrastructure/
      Presentation/
```

---

## 11. 検証（事故防止チェックリスト）

* [ ] すべてのMasterDataに `id` があり一意（重複チェック）
* [ ] SaveDataにUnityEngine型が含まれていない
* [ ] Domain/Applicationが `UnityEngine` に依存していない（asmdefで防止）
* [ ] セーブのversionが存在し、ロード破損時の復旧ができる
* [ ] アイテム上限（回復3/無敵2/エネルギー200）がデータで表現でき、Domain側で強制できる

---

## 12. 未確定値（unknown）の扱い

要件に `{unknown}` が残る項目（歩行速度、ダッシュ減速率、クールタイム等）は、

* PlayerStaticParams / ItemSpawnParams 等の **マスターデータ側に寄せる**
* Domainは「値を受け取って動く」設計を維持する

これにより、数値確定のたびにコード変更を最小化できる。
