# ボス・弾幕詳細設計書

## 0. 参照ドキュメント

* 要件定義書：ボス戦、複数HPゲージ、ダッシュ吸収、ボス接触ダメージ、ドロップ、攻撃アイコン
* アーキテクチャ設計書：Domain/Presentation 分離、SpawnRequest、コリジョン橋渡し、ObjectPool
* ドメインモデル設計書：`Boss` / `BattleFlowService` / `BossStateMachine` / `EnemyBullet`
* データ設計書：`BossParamsContract` / `BossStateDefinition` / `BossAttackDefinition`
* 実装計画書：BOSS-01〜13、PL-04、PL-05

本書は上記と矛盾しない形で、**現行のボス行動状態機械** と **弾幕実装の詳細方針** を定める。
将来拡張案は残すが、現行実装の真実は `BossStateMachine` を中心とした構成とする。

---

## 1. 目的

* ボス固有の進行を、バトル全体進行から分離して安全に拡張できるようにする。
* フェーズ制御、攻撃選択、弾の撃ち方を責務分離し、複雑化しても破綻しにくい構造にする。
* 複数HPゲージ、イントロ演出、撃破演出、ダッシュ吸収、ボス接触ダメージ、ドロップと自然に接続できるようにする。
* 低レベル弾幕は既存 `SingleShot` / `NWayShot` / `BurstShot` を再利用しつつ、将来的な詳細弾幕へ移行可能にする。

---

## 2. 位置付けと整合方針

### 2.1 現行実装との関係

* `BattleFlowService` は引き続きバトル全体の外周進行を担う。`BattleStart`、`BossBoot`、`Combat`、`BossDefeated`、`BattleEnd` の真実はここに置く。
* `Boss` は引き続き Domain 上の真実の状態とし、現在ゲージ、現在HP、撃破判定の責務を持つ。
* `BossStateMachine` はボス固有進行の中心とし、`Intro`、`Phase`、`Dead` の状態遷移を管理する。
* `BossAttackController` はフェーズ内の攻撃選択と攻撃進行を担う。各フェーズの固定シーケンスを消化した後、履歴を考慮したランダム選択へ移る。
* `IBossAttack` は「1攻撃単位」の共通契約であり、現行では `ConfiguredBossAttack` が `BossAttackDefinition` から構築される。
* `IBossAttackPattern` は低レベルな「弾の撃ち方」の共通契約として残す。`SingleShotPattern`、`NWayShotPattern`、`BurstShotPattern` はこの層に属する。
* `EnemyBulletSpawnRequest` は引き続き Domain から Presentation への弾生成要求 DTO として用いる。
* `EnemyBullet` は現行の実行時弾状態として有効なままとし、`EnemyBulletService` が生成、更新、寿命切れ管理を担う。
* ボス定義の正規入力は `BossParamsContract.InitialStateId`、`States`、`Attacks` とする。
* 旧 `PhasePatterns` は互換入力としてのみ残し、`MasterDataMapper` が legacy state/attack へ自動変換する。

### 2.2 追加用語の扱い

| 本書の用語 | 位置付け | 現行実装との接続 |
| --- | --- | --- |
| `BossRoot` | Unity 上のボス親 Prefab | Domain の `Boss` を可視化する Presentation ルート |
| `BossStateMachine` | ボス固有状態機械 | `Intro` / `Phase` / `Dead` の進行と遷移を管理 |
| `IBossState` | 状態機械の状態契約 | `IntroBossState` / `PhaseBossState` / `DeadBossState` の共通契約 |
| `BossAttackController` | フェーズ内攻撃オーケストレータ | `IBossAttack` の選択、開始、更新、終了を管理 |
| `IBossAttack` | 1攻撃単位の契約 | `ConfiguredBossAttack` が `BossAttackDefinition` を実行に変換 |
| `IBossAttackPattern` | 低レベル弾幕パターン契約 | `SingleShot` / `NWayShot` / `BurstShot` の共通契約 |
| `BossBehaviorUpdateResult` | ボス更新結果 DTO | 弾生成要求と `IntroCompleted` / `DeadCompleted` を返す |
| `BossStateDefinition` | 状態定義データ | `StateType`、`AttackPlan`、`Transitions` を持つ |
| `BossAttackDefinition` | 攻撃定義データ | パターン種別、弾設定、`ActiveDurationSeconds` を持つ |
| `BossAttackPlan` | フェーズ内攻撃選択定義 | 固定シーケンス、ランダム候補、履歴除外幅を持つ |
| `BossStateTransition` | 状態遷移定義 | 遷移条件と遷移先状態を持つ |
| `BossBrain` | Unity 側の将来拡張候補 | 見た目演出や移動の橋渡し候補であり、現行の真実の状態機械ではない |

### 2.3 要件解釈の固定

* ボスは要件通り、基本的にロボット外かつ画面右側に存在する前提で設計する。
* 攻撃アイコンは既存要件の「通常4 + 特殊1」を前提とし、四隅や右中央などの具体的な配置は Stage/Robot レイアウト側の責務とする。
* 本書でいう「右中央の大砲」は、既存要件の**特殊攻撃アイコンから発動される強攻撃シーケンスの一形態**として扱う。別ルールの独立システムにはしない。

---

## 3. レイヤ別責務

### 3.1 Domain

* `Boss`
  * 現在ゲージ、現在HP、撃破判定、ゲージ跨ぎダメージ適用を保持する。
* `BattleFlowService`
  * バトル全体のフェーズ進行を管理する。
  * `BossBoot` 完了と撃破完了の受け口を持つが、ボス固有の内部状態遷移は持たない。
* `BossStateMachine`
  * ボス固有の `Intro`、`Phase`、`Dead` を管理する。
  * 状態更新結果として `EnemyBulletSpawnRequest` 群と `BossBehaviorSignal` を返す。
* `BossAttackController`
  * フェーズ内の攻撃選択と攻撃進行を担う。
  * `OpeningSequenceAttackIds` を先に消化し、その後 `RandomAttackIds` から履歴除外付きで攻撃を選ぶ。
* `ConfiguredBossAttack`
  * `BossAttackDefinition` を 1 攻撃単位の実行オブジェクトへ変換する。
  * `ActiveDurationSeconds` の範囲で低レベルパターンを更新し、完了判定を持つ。
* `BossAttackPatternFactory`
  * `BossAttackDefinition` から `IBossAttackPattern` を構築する。
  * パターン種別ごとの最小妥当性検証を担う。
* `EnemyBulletService`
  * 弾の生成、移動、寿命管理、消滅管理を担う。
* `BossDamageService`
  * ボスへのダメージ適用のみを担う。
  * バトル進行通知は行わず、撃破遷移は `BossStateMachine` 側が HP 状態を見て判断する。
* `DropService` / `ItemSpawnService`
  * ダッシュ吸収、被弾時ドロップ、撃破ドロップ、個数上限を担う。

### 3.2 Presentation

* `BossRoot`、`BossHealth`、`BossAssembler`、`BossAnimatorBridge`
  * Prefab 構造、見た目、アニメ同期、位置反映、被弾入口の橋渡しを担う。
* `GameSceneEntryPoint`
  * `BattleFlowService` と `BossStateMachine` を接続する。
  * `BossBoot` と `Combat` の両方で `BossStateMachine.Update` を呼び、戻り値の `SpawnRequests` と `BossBehaviorSignal` を処理する。
* `BossTitleOverlayPresenter`
  * イントロ演出完了時に `BossStateSignalIds.IntroFinished` を `BossStateMachine` へ通知する。
* `BossBattleRuntime`
  * `EnemyBullet` と View の同期、当たり判定橋渡し、ボス View の反映を担う。
* `BossSpawnPoint`、`BossSpawner`
  * ステージ上の出現位置、登場演出位置、フェーズ遷移時の再配置位置を定義する将来拡張ポイントとする。
* `BossHurtbox`、`BossAttackHitbox`、`BulletHitbox`
  * Unity の Trigger/Collision を受け、Domain サービスへ橋渡しする。

### 3.3 MasterData / ScriptableObject

* `BossParamsContract`
  * 複数ゲージ HP、ドロップ量、初期状態 ID、状態定義、攻撃定義を持つ。
* `BossStateDefinition`
  * 状態 ID、`StateType`、`AttackPlan`、`Transitions` を持つ。
* `BossAttackDefinition`
  * `PatternType`、発射パラメータ、弾パラメータ、`ActiveDurationSeconds` を持つ。
* `BossAttackPlan`
  * `OpeningSequenceAttackIds`、`RandomAttackIds`、`HistoryWindow` を持つ。
* `BossStateTransition`
  * `ConditionType`、`Threshold`、`SignalId`、`NextStateId` を持つ。
* `BossLayout`
  * 部位 Prefab、差し込みスロット、ローカル座標、初期有効状態を持つ Presentation 向けレイアウト定義とする将来拡張候補。
* `SpellDefinition`、`EmitterPreset`、`BulletSpec`
  * 詳細弾幕段階で導入する将来拡張候補とする。

---

## 4. ボス構造層設計

### 4.1 基本 Prefab 構成

```text
BossRoot
├─ VisualRoot
├─ Hurtboxes
├─ BodyCollision
├─ PartSlots
├─ EmittersRoot
└─ VFXRoot
```

`BossHealth`、`BossAssembler`、`BossAnimatorBridge` は `BossRoot` に付与するコンポーネントとして扱う。

### 4.2 各責務

#### `BossRoot`

* ボス全体の親。
* ステージ上の位置、生成、破棄、撃破時の演出起点をまとめる。

#### `VisualRoot`

* SpriteRenderer、Animator、装飾オブジェクトを持つ見た目専用ノード。
* 当たり判定を持たない。

#### `Hurtboxes`

* 被弾判定群を管理する。
* `CoreHurtbox`、`WeakPointHurtbox`、`PartHurtbox` を必要に応じてぶら下げる。

#### `BodyCollision`

* プレイヤーとの接触判定、押し戻し判定を担う。
* ダッシュ接触ダメージの判定対象にもなる。

#### `PartSlots`

* `BodySlot`、`LeftWeaponSlot`、`RightWeaponSlot`、`OrbitSlotA`、`OrbitSlotB`、`TailSlot` などの部位差し込み Transform 群。

#### `EmittersRoot`

* 発射器、使い魔、補助ノードの配置親。
* 見た目と発射原点を分離しやすくするため、`VisualRoot` から独立させる。

#### `VFXRoot`

* 被弾、発射、破壊、フェーズ遷移などの VFX 親。

### 4.3 複合 Prefab 対応

* ボスは単一スプライト前提で設計しない。
* 各部位は `BossPart` Prefab として分離し、`BossAssembler` が `BossLayout` に従って `PartSlots` に差し込む。
* `BossPart` は見た目、部位用 Hurtbox、必要なら `EmitterMounts` を持つ。
* 破壊可能部位は Domain では「追加 HP/追加状態」、Presentation では「差し替え可能な部位」として扱う。

### 4.4 出現位置とレイアウト

* ボスの初期位置は Prefab に固定しない。
* `BossSpawnPoint` は少なくとも `introAnchor` と `battleAnchor` を持つ想定とする。
* 必要に応じて `phaseTransitionAnchor` を持ち、フェーズ遷移演出時の再配置先に使う。
* `BossSpawner` は Stage 開始やイベント開始時に、出現位置、向き、`BossLayout`、使用ボス定義をまとめて割り当てる将来拡張ポイントとする。

### 4.5 判定分離

* `VisualRoot` は見た目のみ。
* `BossHurtbox` は被弾判定のみ。
* `BossAttackHitbox` は近接攻撃や体当たり攻撃の当たり判定のみ。
* `BodyCollision` は通常接触用。
* `BossAttackHitbox` は常時有効にせず、Animation Event または攻撃シーケンスから明示制御する。

### 4.6 高レベル状態

現行のボス固有状態は Domain 側の `BossStateMachine` で次を管理する。

* `Intro`
* `Phase`
* `Dead`

補足：

* これはボス行動の真実の状態であり、`BattleFlowService` の外周フェーズと分離する。
* `BossBrain` 側で `Idle`、`Move`、`Recovery`、`Stunned` などを持つ構成は将来拡張案とし、現行実装の前提にはしない。
* フェーズごとの差分はクラス増殖ではなく `BossStateDefinition` と `BossAttackPlan` のデータ差分で表現する。

---

## 5. 弾幕システム層設計

### 5.1 基本モデル

現行の弾幕制御は次の階層で分離する。

* `BattleFlowService`
* `BossStateMachine`
* `IBossState`
* `BossAttackController`
* `IBossAttack`
* `IBossAttackPattern`
* `EnemyBulletSpawnRequest`
* `EnemyBulletService`

ボス本体は「どの状態にいるか」「今どの攻撃を実行するか」を決め、低レベル弾幕層は「どの方向にどう撃つか」を担う。

### 5.2 現行実装の構成

* `BossStateMachine`
  * `Intro`、`Phase`、`Dead` の遷移を管理する。
  * 各フレームの更新結果として `BossBehaviorUpdateResult` を返す。
* `BossBehaviorUpdateResult`
  * `SpawnRequests` と `BossBehaviorSignal` を持つ。
  * `BossBehaviorSignal` は `None`、`IntroCompleted`、`DeadCompleted` を持つ。
* `PhaseBossState`
  * `BossAttackController` を 1 つ保持し、フェーズ中の攻撃進行を集約する。
* `BossAttackController`
  * フェーズ開始時に攻撃履歴と現在攻撃を初期化する。
  * 固定シーケンスを先に消化し、その後ランダム候補から攻撃を選ぶ。
  * `HistoryWindow` 分だけ直近攻撃を除外し、候補が不足した場合は除外幅を 1 ずつ緩める。
* `ConfiguredBossAttack`
  * `BossAttackDefinition` から `IBossAttackPattern` を生成し、`ActiveDurationSeconds` の範囲で更新する。
  * 期間終了時に `IsCompleted` を立て、`BossAttackController` に完了を通知する。
* `BossAttackPatternFactory`
  * 低レベル弾幕 `SingleShot` / `NWayShot` / `BurstShot` を生成する。
* `EnemyBulletService`
  * `SpawnRequests` を `EnemyBullet` 実体へ変換し、移動・寿命更新を行う。

### 5.3 状態定義と遷移条件

`BossStateDefinition` は次を持つ。

* `Id`
* `StateType`
* `AttackPlan`
* `Transitions`

`BossStateType` は次の 3 種とする。

* `Intro`
* `Phase`
* `Dead`

`BossStateTransition` の `ConditionType` は次を持つ。

* `ExternalSignal`
* `ElapsedTime`
* `CurrentHpRateAtOrBelow`
* `CurrentAttackCompleted`
* `CurrentGaugeIndexAtOrAbove`

運用方針：

* 正規用途は `ExternalSignal`、`ElapsedTime`、`CurrentHpRateAtOrBelow`、`CurrentAttackCompleted` とする。
* `CurrentGaugeIndexAtOrAbove` は旧 `PhasePatterns` からの互換変換用条件として主に用いる。
* 遷移は宣言順に評価し、同フレームで複数条件が成立しても先頭のみ採用する。
* 終端遷移は `Dead` からのみ許可する。
* `Intro` 完了は Presentation から渡される外部シグナルで決定する。
* HP 0 到達後も即座に `BattleFlowService` を終端させず、`Dead` 状態の完了後に `DeadCompleted` を返して撃破進行へ入る。

### 5.4 攻撃定義と攻撃選択

`BossAttackDefinition` は次を持つ。

* `Id`
* `PatternType`
* `FireIntervalSeconds`
* `ShotCount`
* `SpreadDegrees`
* `BurstShotCount`
* `BurstShotIntervalSeconds`
* `BulletSpeed`
* `BulletLifetimeSeconds`
* `BulletDamage`
* `AbsorbableEnergyAmount`
* `BulletBehaviorType`
* `SpawnOffset`
* `FireDirection`
* `ActiveDurationSeconds`

`BossAttackPlan` は次を持つ。

* `OpeningSequenceAttackIds`
* `RandomAttackIds`
* `HistoryWindow`

運用方針：

* 各フェーズはまず `OpeningSequenceAttackIds` を順番に消化する。
* その後は `RandomAttackIds` からランダム選択する。
* `RandomAttackIds` が空なら `OpeningSequenceAttackIds` をランダム候補として再利用できる。
* `HistoryWindow` で指定した直近履歴を除外する。
* 除外により候補がなくなった場合は、直近除外幅を 1 ずつ緩め、最終的に選択可能な候補を作る。

### 5.5 低レベル弾幕パターン

現行の低レベル弾幕は次の責務分離で構成する。

* `IBossAttackPattern`
  * `Reset()` と `Update(BattleContext, deltaTime)` を共通契約とする。
* `IntervalBossAttackPatternBase`
  * 一定間隔発火型パターンの共通クールダウン制御を担う。
* `SingleShotPattern`
  * 一定間隔で単発を撃つ。
* `NWayShotPattern`
  * 一定間隔で扇状に複数弾を撃つ。
* `BurstShotPattern`
  * バースト間隔とバースト内間隔を分けて連射する。
* `BossBulletPatternConfig`
  * 発射原点、発射方向、弾速、寿命、威力、吸収量、挙動種別をまとめて `EnemyBulletSpawnRequest` を生成する。

補足：

* 現行実装では **1 パターン = 1 クラス = 1 ファイル** を基本方針とする。
* `EnemyBulletBehaviorType` は設定上は複数種を許容するが、現行運用は主に `Straight` を前提とする。
* 低レベルパターンは「どの弾をどう撃つか」だけを担い、状態遷移や攻撃選択は持たない。

### 5.6 互換入力

旧 `BossParamsContract.PhasePatterns` は正規入力ではないが、互換入力として維持する。

* `MasterDataMapper` は `PhasePatterns` しか存在しない場合、legacy attack 群と legacy state 群を自動生成する。
* legacy state は `legacy_intro`、`legacy_phase_i`、`legacy_dead` を基本形とする。
* legacy phase の遷移には `CurrentGaugeIndexAtOrAbove` を使い、旧「ゲージ番号と攻撃番号が直結する」構造を再現する。
* 互換変換後の攻撃は `ActiveDurationSeconds = Infinity` とし、旧パターン挙動を維持する。

### 5.7 現行最小構成の例

`stage_01` のテストボスは次の構成を取る。

* `intro`
  * `ExternalSignal(intro_finished)` を待つ。
* `phase_01`
  * `phase_01_single` を固定攻撃として繰り返す。
  * 攻撃内容は「1 秒ごとに真下へ単発弾を 1 発」。
  * `CurrentHpRateAtOrBelow = 0` で `dead` へ遷移する。
* `dead`
  * `ElapsedTime = 0.5` 秒後に終端完了する。

この構成により、ボスタイトル演出中は攻撃せず、演出完了後に戦闘へ入り、HP 0 到達後は短い撃破完了待ちを経てバトル終端へ進む。

### 5.8 将来拡張

* `SpellDefinition`
  * スペル名、ループ位置、終了条件、タイムラインコマンド列、難易度差分を持つ詳細弾幕定義。
* `PatternRunner`
  * 詳細弾幕段階で「何秒目に何をするか」を順番に実行するタイムライン進行器。
* `EmitterController`
  * 発射位置、基準角、発射方式、Way 数、回転量、自機狙い補正などを持つ発射器制御。
* `FamiliarController`
  * 移動しながら弾幕を撃つ補助ノード。

将来の詳細弾幕では、複雑さを個々の弾に持たせず、時間制御と発射規則の合成で表現する。

---

## 6. ゲーム固有要素との接続

### 6.1 ダッシュ吸収

敵弾は少なくとも次の値を持つ。

* `canBeAbsorbed`
* `energyValue`
* `absorbEffectId`

ダッシュ中に敵弾へ接触した場合：

* `EnemyBulletService` または上位管理層が対象弾を中央管理リストから外す。
* `ItemSpawnService` または対応する加算処理へ吸収量を通知する。
* 弾 View は Pool または破棄経路へ返却する。

### 6.2 ボス接触ダメージ

* ダッシュ中のプレイヤーが `BodyCollision` に触れた場合、Presentation は Domain の `DamageService` へ「Boss 接触ダメージ」を通知する。
* 近接攻撃判定と通常接触判定は分離し、常時有効な攻撃 Hitbox にはしない。

### 6.3 特殊攻撃による弾消し

* 特殊攻撃アイコンに紐づく強攻撃シーケンスは、必要に応じて「大ダメージ + 敵弾一括消去」を持てる。
* 一括消去はボス固有コードから個別弾参照しない。
* 敵弾管理サービスに対する中央命令で実行する。

想定例：

* `ClearEnemyBullets(ClearReason.SpecialAttack)`
* `ClearEnemyBulletsOwnedBy(bossId, ClearReason.BossPhaseEnd)`

### 6.4 ドロップと高リスク・高リターン

* ボス本体は画面右側の圧力源として設計する。
* プレイヤーが右側に踏み込むほど、被弾、接触、高密度弾幕のリスクが高まる。
* その代わり、ボス被弾時ドロップや撃破ドロップは `DropService` と連携して高いリターンに繋げる。
* ドロップ量は既存要件どおり、基礎量と攻撃側のドロップ倍率で算出する。

---

## 7. データとコードの分担

### 7.1 データに寄せるもの

* `InitialStateId`
* `BossStateDefinition` の `StateType`
* `BossStateTransition` の条件種別、しきい値、シグナル ID、遷移先
* `BossAttackPlan` の固定シーケンス、ランダム候補、履歴除外幅
* `BossAttackDefinition` の `patternType`
* 発射間隔
* Way 数
* 拡散角
* バースト数
* バースト内間隔
* 発射方向
* 発射原点オフセット
* 弾の見た目、寿命、速度、吸収量、挙動種別
* 攻撃の継続時間
* 部位レイアウト

### 7.2 コードに寄せるもの

* 状態遷移の実行
* 攻撃選択の履歴管理
* `BossAttackPatternFactory` による低レベルパターン生成
* パターン別の妥当性検証
* `PhasePatterns` からの互換変換
* `BossBehaviorSignal` の生成と外周フロー接続
* 弾の一括更新
* 吸収処理
* 一括弾消し
* フェーズ移行トリガーの評価
* 将来のタイムライン進行

### 7.3 判断基準

* 数値調整と構成差分はデータで扱う。
* 実行時状態、履歴、分岐、シグナル連携はコードで扱う。
* ScriptableObject に実行時状態を持たせない。

---

## 8. 段階導入方針

### Phase 1：最小ボス基盤

* `BossRoot`
* `BossHealth`
* `BossSpawnPoint`
* `BossSpawner`
* 見た目と Hurtbox の分離

### Phase 2：基本攻撃

* 近接攻撃 1 種
* 直線弾 1 種
* 突進 1 種
* Animation Event による近接同期

### Phase 3：現行弾幕基盤

* `BossStateMachine`
* `BossAttackController`
* `ConfiguredBossAttack`
* `BossAttackPatternFactory`
* `SingleShotPattern` / `NWayShotPattern` / `BurstShotPattern`
* `BossBulletPatternConfig`
* `EnemyBulletService`
* `BossBehaviorUpdateResult`

補足：

* 現行実装で整理済みなのは、状態機械、攻撃選択、個別パターンクラス分割、Factory 分離まで。
* `PatternRunner`、`EmitterController`、詳細 `SpellDefinition` はこの次段階で導入する。

### Phase 4：複合ボス

* `BossPart`
* `BossAssembler`
* `BossLayout`
* 左右砲台、回転ユニット、補助部位

### Phase 5：複雑弾幕

* `PatternRunner`
* `SpellDefinition`
* `EmitterController`
* Spiral
* AlternatingSpiral
* RandomSpread
* Familiar を用いた空間分担
* 分裂弾
* フェーズ別スペル切り替え

### Phase 6：ゲーム固有要素接続

* ダッシュ吸収によるエネルギー化
* 特殊攻撃による弾消し
* 撃破ドロップ
* UI、SE、VFX 連携

---

## 9. 非目標

* 全敵に共通する完全汎用 AI フレームワーク
* ノーコードで全弾幕を記述する DSL の完成
* あらゆる部位関係を自動解決する超汎用アセンブラ
* 物理ベースの複雑な破壊シミュレーション

---

## 10. アンチパターン

* バトル全体進行とボス固有進行を 1 クラスに混在させる
* フェーズ選択、攻撃選択、発射パターン生成を 1 クラスに詰め込む
* 弾を 1 発ずつ `Instantiate / Destroy` し続ける
* 弾 1 発ごとに Coroutine を持たせる
* 見た目と被弾判定を完全一致前提で設計する
* 弾幕ロジックを Animator に埋め込みすぎる
* ScriptableObject に実行時状態を持たせる
* ボス固有コードが敵弾インスタンスを個別に握り続ける

---

## 11. 最終方針

> ボス固有進行は `BossStateMachine` を中心に `Intro` / `Phase` / `Dead` で管理し、フェーズ内攻撃は `BossAttackController`、低レベル弾幕は `IBossAttackPattern` 群へ分離する。外周のバトル進行は `BattleFlowService` に残し、両者は `BossBehaviorUpdateResult` のシグナルで接続する。ボス定義の正規形は `InitialStateId` + `States` + `Attacks` とし、旧 `PhasePatterns` は互換入力としてのみ扱う。将来的に詳細弾幕が必要になった段階で `PatternRunner`、`EmitterController`、`SpellDefinition` を追加し、複雑さは時間制御と発射規則の合成で表現する。

この方針により、複数ゲージ、イントロ演出、撃破演出、ダッシュ吸収、ボス接触ダメージ、ドロップ、特殊攻撃との整合を保ったまま、将来的な複合ボスと高密度弾幕へ拡張できる。
