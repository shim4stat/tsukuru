# ボス・弾幕詳細設計書

## 0. 参照ドキュメント

* 要件定義書：ボス戦、複数HPゲージ、ダッシュ吸収、ボス接触ダメージ、ドロップ、攻撃アイコン
* アーキテクチャ設計書：Domain/Presentation 分離、SpawnRequest、コリジョン橋渡し、ObjectPool
* ドメインモデル設計書：`Boss` / `BattleFlowService` / `BossStateMachine` / `EnemyBullet`
* データ設計書：`BossParamsContract` / `BossStateDefinition` / `BossActionDefinition`
* 実装計画書：BOSS-01〜13、PL-04、PL-05

本書は、今後の正規設計を `BossActionDefinition + Timeline(Command/Window)` 基準で定義し、2026-05-07 時点の現行実装との差分も明記する。
以後の実装は本書の「正規設計」を優先し、現状コードが未達の箇所は「未実装」または「部分実装」として扱う。

---

## 1. 目的

* ボス固有の進行を、バトル全体進行から分離して安全に拡張できるようにする。
* ボスの下位実行部を弾幕専用実行器ではなく、フレーム基準の汎用行動タイムライン実行器へ置き換える。
* 「8f で弾を撃つ」「8f〜12f だけ斬撃判定を出す」を同じ仕組みで表現できるようにする。
* 低レベル弾幕は `SingleShot` / `NWayShot` / `BurstShot` を再利用しつつ、将来的な近接、突進、召喚、SE/VFX、部位連動へ拡張可能な構造にする。
* 複数HPゲージ、イントロ演出、撃破演出、ダッシュ吸収、ボス接触ダメージ、ドロップ、特殊攻撃による弾消しと自然に接続できるようにする。

---

## 2. 位置付けと整合方針

### 2.1 正規設計の立場

* `BattleFlowService` はバトル全体の外周進行を担う。
  * `BattleStart`、`BossBoot`、`Combat`、`BossDefeated`、`BattleEnd` の真実はここに置く。
* `Boss` は Domain 上の真実の状態とし、現在ゲージ、現在HP、撃破判定、ゲージ跨ぎダメージ適用を持つ。
* `BossStateMachine` はボス固有進行の中心とし、`Intro`、`Phase`、`Dead` の遷移を管理する。
* `Phase` 状態の下位実行は `BossActionController` が担い、1 つずつ `BossActionDefinition` を選択・開始・更新・終了する。
* 各 `BossActionDefinition` は `ConfiguredBossAction` により実行時オブジェクト化され、その内部で `BossActionTimelineExecutor` がフレーム基準の timeline を進める。
* timeline は `Command` と `Window` を処理する。
  * `Command` は瞬間イベント。
  * `Window` は区間イベント。
* `SpawnBulletPattern` は timeline command の 1 種であり、弾幕そのものは下位の `IBossAttackPattern` が担当する。
* `IBossAttackPattern` は低レベルな「弾の撃ち方」の共通契約であり、上位の攻撃単位契約ではない。
* `EnemyBulletSpawnRequest` は引き続き Domain から Presentation への弾生成要求 DTO として使う。
* `EnemyBulletService` は引き続き弾の生成、移動、寿命管理、消滅管理を担う。
* ボス定義の正規入力は `BossParamsContract.InitialStateId`、`States`、`Actions` とする。
* 現行 `BossParamsContract` の正規入力は `InitialStateId`、`States`、`Actions` のみとし、旧 `PhasePatterns` は contract へ載せない。

### 2.2 本書の読み方

* 「正規設計」
  * 今後の実装が従うべき構造と意味。
* 「現状実装」
  * 2026-05-07 時点でコード上に確認できる範囲。
* 「未実装」
  * 契約のみ存在する、または契約自体も未整備だが本書上は今後必要とする内容。

正規設計と現状実装が異なる場合、本書では正規設計を本文の基準とし、差分は `8. 実装状況と未実装項目` で明示する。

### 2.3 用語

| 用語 | 役割 | 補足 |
| --- | --- | --- |
| `BossStateMachine` | ボス固有状態機械 | `Intro` / `Phase` / `Dead` を管理する |
| `IBossState` | 状態機械の状態契約 | `IntroBossState` / `PhaseBossState` / `DeadBossState` の共通契約 |
| `BossActionController` | フェーズ内 action オーケストレータ | `BossActionPlan` に従って次 action を選ぶ |
| `IBossAction` | 1 action 単位の共通契約 | `ConfiguredBossAction` が主実装 |
| `ConfiguredBossAction` | `BossActionDefinition` の実行体 | timeline と完了判定を持つ |
| `BossActionTimelineExecutor` | action の timeline 実行器 | command 発火、window 集計、子 emitter 更新を行う |
| `BossActionCommand` | 1 フレームで発火するイベント | `PlayAnimation` / `SpawnBulletPattern` / `EmitSignal` など |
| `BossActionWindow` | 一定区間だけ有効な状態 | `HitboxWindow` / `InvincibleWindow` など |
| `BossBulletPatternDefinition` | 低レベル弾幕 emitter の設定 | 発射周期、Way 数、弾速などを持つ |
| `BossBulletPatternEmitterRuntime` | 継続発火する弾幕 emitter 実行体 | timeline command から起動される |
| `IBossAttackPattern` | 下位弾幕パターン契約 | `SingleShot` / `NWayShot` / `BurstShot` の共通契約 |
| `BossActionExecutionResult` | action 1 update 分の内部結果 | `SpawnRequests` / `EmittedSignals` / `FrameState` を持つ |
| `BossBehaviorUpdateResult` | ボス更新結果 DTO | `SpawnRequests` / `BossBehaviorSignal` / `BossActionFrameState` を返す |
| `BossActionFrameState` | Window の外部公開状態 | active hitbox / hurtbox / invincible / move velocity / cancel tag を持つ |
| `BossBrain` | 将来の Presentation/Animation 連携候補 | 現行の真実の状態機械ではない |

### 2.4 要件解釈の固定

* ボスは基本的にロボット外かつ画面右側に存在する前提で設計する。
* 攻撃アイコンは既存要件の「通常4 + 特殊1」を前提とし、四隅や右中央などの具体配置は Stage/Robot レイアウト側の責務とする。
* 本書でいう「右中央の大砲」は、既存要件の特殊攻撃アイコンから発動される強攻撃シーケンスの一形態として扱う。
* 近接、体当たり、突進、召喚、SE/VFX も timeline command / window に乗せる対象とする。弾幕だけを特別扱いしない。

---

## 3. レイヤ別責務

### 3.1 Domain

* `Boss`
  * 現在ゲージ、現在HP、撃破判定、ゲージ跨ぎダメージ適用を保持する。
* `BattleFlowService`
  * バトル全体のフェーズ進行を管理する。
  * ボス固有の内部状態遷移は持たない。
* `BossStateMachine`
  * ボス固有の `Intro`、`Phase`、`Dead` を管理する。
  * state 定義と action 定義の妥当性検証を担う。
  * `Phase` では `BossActionController` を用いて action を進行させる。
* `BossActionController`
  * `OpeningSequenceActionIds` を先に消化し、その後 `RandomActionIds` から履歴除外付きで次 action を選ぶ。
  * action 完了の検知と履歴更新を担う。
* `ConfiguredBossAction`
  * 1 つの `BossActionDefinition` を実行する。
  * `deltaTime` を 60fps 仮想 frame に変換し、timeline を進める。
  * `Manual` / `DurationElapsed` の終了条件を扱う。
* `BossActionTimelineExecutor`
  * command 発火、active window 集計、弾幕 emitter 更新、signal 収集を担う。
* `BossBulletPatternEmitterRuntime`
  * `SpawnBulletPattern` command で起動され、下位 `IBossAttackPattern` を継続更新する。
* `BossAttackPatternFactory`
  * `BossBulletPatternDefinition` から `IBossAttackPattern` を生成する。
  * 下位弾幕定義の妥当性検証を担う。
* `IBossAttackPattern`
  * 低レベルな弾の撃ち方のみを表す。
  * 状態遷移、近接判定、SE/VFX、部位制御は持たない。
* `EnemyBulletService`
  * 弾の生成、移動、寿命管理、消滅管理を担う。
* `BossDamageService`
  * ボスへのダメージ適用のみを担う。
* `DropService` / `ItemSpawnService`
  * ダッシュ吸収、被弾時ドロップ、撃破ドロップ、個数上限を担う。

### 3.2 Presentation

* `GameSceneEntryPoint`
  * `BattleFlowService` と `BossStateMachine` を接続する。
  * `BossBoot` と `Combat` の両方で `BossStateMachine.Update` を呼び、戻り値の `SpawnRequests`、`BossBehaviorSignal`、`BossActionFrameState` を処理する。
* `BossTitleOverlayPresenter`
  * イントロ演出完了時に `BossStateSignalIds.IntroFinished` を `BossStateMachine` へ通知する。
* `BossBattleRuntime`
  * `EnemyBullet` と View の同期、ボス View の反映、当たり判定橋渡しを担う。
* `BossRoot`、`BossHealth`、`BossAssembler`、`BossAnimatorBridge`
  * Prefab 構造、見た目、アニメ同期、位置反映、被弾入口の橋渡しを担う将来拡張ポイント。
* `BossHurtbox`、`BossAttackHitbox`、`BulletHitbox`
  * Unity の Trigger/Collision を受け、Domain サービスへ橋渡しする。

### 3.3 MasterData / ScriptableObject

* `BossParamsContract`
  * 複数ゲージ HP、ドロップ量、初期状態 ID、状態定義、action 定義を持つ。
* `BossStateDefinition`
  * 状態 ID、`StateType`、`ActionPlan`、`Transitions` を持つ。
* `BossActionPlan`
  * `OpeningSequenceActionIds`、`RandomActionIds`、`HistoryWindow` を持つ。
* `BossActionDefinition`
  * `AnimationStateName`、`EndConditionType`、`TotalDurationFrames`、`Commands`、`Windows` を持つ。
* `BossActionCommand`
  * `TriggerFrame`、`CommandType` と command ごとの payload を持つ。
* `BossActionWindow`
  * `Id`、`WindowType`、`StartFrameInclusive`、`EndFrameExclusive` を持つ。
* `BossBulletPatternDefinition`
  * 下位弾幕 emitter の発射設定を持つ。
* 旧 `phasePatterns`
  * 現行 contract 外の旧 authoring field。
* `BossLayout`
  * 部位 Prefab、差し込みスロット、ローカル座標、初期有効状態を持つ Presentation 向けレイアウト定義の将来拡張候補。

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
* 将来的には `HurtboxWindow` により有効・無効を切り替える。

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
* `BossSpawner` は Stage 開始やイベント開始時に、出現位置、向き、`BossLayout`、使用ボス定義をまとめて割り当てる。

### 4.5 判定分離

* `VisualRoot` は見た目のみ。
* `BossHurtbox` は被弾判定のみ。
* `BossAttackHitbox` は近接攻撃や体当たり攻撃の当たり判定のみ。
* `BodyCollision` は通常接触用。
* `BossAttackHitbox` は常時有効にせず、将来的には `HitboxWindow` で明示制御する。
* `InvincibleWindow` はボス被弾入口側で吸収し、見た目だけでは無敵判定を決めない。

### 4.6 高レベル状態

ボス固有状態は Domain 側の `BossStateMachine` で次を管理する。

* `Intro`
* `Phase`
* `Dead`

補足：

* これはボス行動の真実の状態であり、`BattleFlowService` の外周フェーズと分離する。
* `BossBrain` 側で `Idle`、`Move`、`Recovery`、`Stunned` などを持つ構成は将来拡張案であり、現行の真実の状態機械ではない。
* フェーズごとの差分はクラス増殖ではなく `BossStateDefinition` と `BossActionPlan` のデータ差分で表現する。

---

## 5. Action / Timeline 実行設計

### 5.1 基本モデル

ボスの下位実行は次の階層で分離する。

* `BattleFlowService`
* `BossStateMachine`
* `IBossState`
* `BossActionController`
* `IBossAction`
* `BossActionTimelineExecutor`
* `BossBulletPatternEmitterRuntime`
* `IBossAttackPattern`
* `EnemyBulletSpawnRequest`
* `EnemyBulletService`

ボス本体は「どの状態にいるか」「今どの action を実行するか」を決め、timeline 層は「どの frame で何を起こすか」を決め、低レベル弾幕層は「どういう弾をどう撃つか」を担う。

### 5.2 状態定義と遷移条件

`BossStateDefinition` は次を持つ。

* `Id`
* `StateType`
* `ActionPlan`
* `Transitions`

`BossStateType` は次の 3 種とする。

* `Intro`
* `Phase`
* `Dead`

`BossStateTransition.ConditionType` は次を持つ。

* `ExternalSignal`
* `ElapsedTime`
* `CurrentHpRateAtOrBelow`
* `CurrentAttackCompleted`
* `CurrentGaugeIndexAtOrAbove`

運用方針：

* 正規用途は `ExternalSignal`、`ElapsedTime`、`CurrentHpRateAtOrBelow`、`CurrentAttackCompleted` とする。
* `CurrentGaugeIndexAtOrAbove` はゲージ番号を条件にした手動 authoring や将来の legacy migration 用に残す。
* `CurrentAttackCompleted` は enum 名だけ旧名が残っているが、意味は「現在 action が完了した」で扱う。
* 遷移は宣言順に評価し、同フレームで複数条件が成立しても先頭のみ採用する。
* 終端遷移は `Dead` からのみ許可する。
* `Intro` 完了は Presentation から渡される外部シグナルで決定する。
* HP 0 到達後も即座に `BattleFlowService` を終端させず、`Dead` 状態の完了後に `DeadCompleted` を返して撃破進行へ入る。

### 5.3 Action 定義と選択

`BossActionDefinition` は次を持つ。

* `Id`
* `AnimationStateName`
* `EndConditionType`
* `TotalDurationFrames`
* `Commands`
* `Windows`

`BossActionEndConditionType` は次を持つ。

* `Manual`
* `DurationElapsed`

`BossActionPlan` は次を持つ。

* `OpeningSequenceActionIds`
* `RandomActionIds`
* `HistoryWindow`

運用方針：

* 各フェーズはまず `OpeningSequenceActionIds` を順番に消化する。
* その後は `RandomActionIds` からランダム選択する。
* `RandomActionIds` が空なら `OpeningSequenceActionIds` をランダム候補として再利用できる。
* `HistoryWindow` で指定した直近履歴を除外する。
* 除外により候補がなくなった場合は、除外幅を 1 ずつ緩めて最終的に選択可能な候補を作る。
* `AnimationStateName` は action 開始時の基本アニメ状態を表す予約フィールドとし、将来的に `BossAnimatorBridge` が参照する。
* `Manual` action は自動完了しない。
  * state 遷移または外部条件で action ごと終了させる。
* `DurationElapsed` action は `TotalDurationFrames` 到達で自動完了する。
* `DurationElapsed` action が 1 update 中に終了した場合、余剰 frame は次 action に持ち越して同じ update 内で処理できる構造を正とする。

### 5.4 Command 設計

すべての command は少なくとも次を持つ。

* `TriggerFrame`
* `CommandType`

`BossActionCommandType` は次を持つ。

* `PlayAnimation`
* `SpawnBulletPattern`
* `SpawnEnemy`
* `PlayEffect`
* `PlaySound`
* `EmitSignal`

#### `PlayAnimation`

* 指定 frame でアニメ状態を切り替える。
* 最低限の payload は `AnimationStateName`。
* 将来的には layer、blend、cross-fade 秒数、上書きモードを追加できる。
* 現状実装では enum と文字列フィールドのみ存在し、runtime 実装は未接続。

#### `SpawnBulletPattern`

* 指定 frame で低レベル弾幕 emitter を起動する。
* payload は `BossBulletPatternDefinition` と `EmitterDurationFrames`。
* `EmitterDurationFrames = null` は「action 終了まで継続」を意味する。
* この command 自体は「弾幕 emitter を開始する」ものであり、低レベルパターンは emitter 側が継続更新する。
* `BossBulletPatternDefinition.InitialDelayFrames` で初弾タイミングを制御する。
  * `-1` は低レベルパターン既定の初回待ち時間に従う。
  * `0` は command 発火 frame から即時発射可能にする。
  * 正数は指定 frame だけ遅延してから発射を開始する。

#### `SpawnEnemy`

* 指定 frame で使い魔、砲台、子機、補助ユニットを生成する。
* 将来的な payload 例：
  * `EnemyDefinitionId`
  * `SpawnAnchorId`
  * `SpawnOffset`
  * `InitialSignalId`
* 現状 contract は `EnemyDefinitionId` と `SpawnOffset` を持つが、runtime 実行は未接続である。

#### `PlayEffect`

* 指定 frame で VFX を再生する。
* 将来的な payload 例：
  * `EffectId`
  * `AttachTarget`
  * `LocalOffset`
  * `LifetimePolicy`
* 現状 contract は `EffectId` と `EffectLocalOffset` を持つが、runtime 実行は未接続である。

#### `PlaySound`

* 指定 frame で SE を再生する。
* 将来的な payload 例：
  * `SoundId`
  * `MixerRoute`
  * `VolumeScale`
* 現状 contract は `SoundId` と `VolumeScale` を持つが、runtime 実行は未接続である。

#### `EmitSignal`

* 指定 frame で action 内部シグナルを発火する。
* シグナルは同 update 内で state 遷移判定へマージされる。
* これにより「action の特定 frame 到達で phase を切り替える」を表現できる。

#### 共通ルール

* command は `TriggerFrame` 昇順で実行する。
* 同一 frame に複数 command がある場合、authoring 上の定義順に実行する。
* 未対応 command は沈黙して無視せず、runtime で明示的に失敗させる。

### 5.5 Window 設計

すべての window は少なくとも次を持つ。

* `Id`
* `WindowType`
* `StartFrameInclusive`
* `EndFrameExclusive`

`BossActionWindowType` は次を持つ。

* `MoveWindow`
* `HitboxWindow`
* `HurtboxWindow`
* `InvincibleWindow`
* `CancelWindow`

#### `MoveWindow`

* 特定区間だけ移動制御を有効化する。
* 現状 payload は `VelocityPerSecond` を持つ。
* 突進、回り込み、後退、ふわり移動などをこの window で表現する。

#### `HitboxWindow`

* 特定区間だけ近接攻撃判定や体当たり判定を有効化する。
* 現状 payload は `Offset`、`Radius`、`Damage` を持つ。

#### `HurtboxWindow`

* 特定区間だけ被弾部位を有効化する。
* 現状 payload は `Offset`、`Radius` を持つ。

#### `InvincibleWindow`

* 特定区間だけボス被弾を無効化する。
* 現状は追加 payload を持たず、window type の有効区間だけで無敵を表す。

#### `CancelWindow`

* 特定区間だけ action の割り込みを許可する。
* 現状 payload は `CancelTag` を持つ。

#### 共通ルール

* `StartFrameInclusive` は開始 frame を含む。
* `EndFrameExclusive` は終了 frame を含まない。
* 同時に複数 window が重なってもよい。
* `Window` は「状態」なので、1 度発火して終わる command と混同しない。
* 現行 runtime は active window を `BossActionFrameState` に集計して外部公開する。
* `BossActionFrameState` は `ActiveHitboxes`、`ActiveHurtboxes`、`IsInvincible`、`MoveVelocityPerSecond`、`ActiveCancelTags`、`UsesExplicitHurtboxWindows` を持つ。

### 5.6 BulletPattern 定義

`BossBulletPatternDefinition` は次を持つ。

* `PatternType`
* `InitialDelayFrames`
* `FireIntervalFrames`
* `ShotCount`
* `SpreadDegrees`
* `BurstShotCount`
* `BurstShotIntervalFrames`
* `BulletSpeed`
* `BulletLifetimeSeconds`
* `BulletDamage`
* `AbsorbableEnergyAmount`
* `BulletBehaviorType`
* `SpawnOffset`
* `FireDirection`

`BossAttackPatternType` は次を持つ。

* `SingleShot`
* `NWayShot`
* `BurstShot`

低レベル弾幕は次の責務分離で構成する。

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

* timeline は frame 基準、低レベル弾幕の update は `deltaTime` 秒基準という 2 層構造を取る。
* `BulletLifetimeSeconds` は現状どおり秒基準で管理する。
* `EnemyBulletBehaviorType` は設定上 `Straight` / `Wave` / `Homing` を許容するが、現状運用は実質 `Straight` が中心である。
* 将来的に詳細弾幕へ進む場合も、弾 1 発ごとに複雑性を持たせず、timeline と emitter の合成で表現する。

### 5.7 1 update 内の実行順

`ConfiguredBossAction.Update` は次の順序で動くことを正とする。

1. `deltaTime` を 60fps 仮想 frame に変換し、蓄積する。
2. 跨いだ frame 数だけ loop する。
3. 現在 frame に到達した command を発火する。
4. 起動済み bullet emitter を 1 frame 分更新する。
5. その frame で発生した `EnemyBulletSpawnRequest` と `EmittedSignals` を収集する。
6. frame を進める。
7. 最新処理 frame に対する `BossActionFrameState` を返す。

補足：

* `SpawnBulletPattern` は command 発火と同 frame に emitter が更新される。
* emitter 内部パターンは `InitialDelayFrames` と `FireIntervalFrames` を持つ。
* `InitialDelayFrames = 0` なら command 発火 frame から即時発射できる。
* `InitialDelayFrames = -1` は低レベルパターン既定の初回待ち時間に従う。
* `DurationElapsed` action が途中で終わった場合の余剰 frame は、同じ update 内で次 action に持ち越す。

### 5.8 現行最小構成の例

`stage_01` のテストボスは次の構成を取る。

* `intro`
  * `ExternalSignal(intro_finished)` を待つ。
* `phase_01`
  * `phase_01_single` を固定 action として繰り返す。
* `phase_01_single`
  * `TriggerFrame = 0` に `SpawnBulletPattern` を 1 回発火する。
  * その emitter は `FireIntervalFrames = 60` で真下へ単発弾を継続発射する。
  * `EndConditionType = Manual` のため、自動完了しない。
* `dead`
  * `ElapsedTime = 0.5` 秒後に終端完了する。

この構成により、ボスタイトル演出中は攻撃せず、演出完了後に戦闘へ入り、HP 0 到達後は短い撃破完了待ちを経てバトル終端へ進む。

---

## 6. データ設計と互換方針

### 6.1 正規データ

`BossParamsContract` は次を持つ。

* `Id`
* `GaugeMaxHps`
* `BaseDropEnergyAmount`
* `MinDropIntervalSeconds`
* `InitialStateId`
* `States`
* `Actions`

補足：

* 正規入力は `InitialStateId`、`States`、`Actions` である。
* `ActionIntervalSeconds` と `PhasePatterns` は現行 `BossParamsContract` の正規 field ではない。
* 正規 action timeline の実行単位は frame である。

### 6.2 ScriptableObject 側

ScriptableObject は次を持つ。

* `BossActionDefinitionAsset`
* `BossActionPlanAsset`
* `BossActionCommandAsset`
* `BossActionWindowAsset`
* `BossBulletPatternDefinitionAsset`

移行方針：

* `BossStateDefinitionAsset.attackPlan` は `actionPlan` へ rename し、旧 field 名は `FormerlySerializedAs` で吸収する。
* `BossParamsAsset.attacks` は `actions` へ rename し、旧 field 名は `FormerlySerializedAs` で吸収する。
* ただし、旧 flat `BossAttackDefinitionAsset` の内部構造をそのまま新 `commands/windows` 構造へ自動変換する機能は現状未整備である。
* 旧 flat `attacks` データを使う資産が残る場合は、将来的に importer または一括 migration を別途用意する。

### 6.3 互換入力

旧 `PhasePatterns` は正規入力ではなく、現行 `BossParamsContract` には存在しない。

* `BossParamsAsset` には旧 authoring field として `actionIntervalSeconds` と `phasePatterns` が残っている。
* 2026-05-07 時点の `MasterDataMapper` はこれらを `BossParamsContract` へ返していない。
* 旧 `phasePatterns` から legacy state/action を自動生成する機能は未実装として扱う。
* 旧資産を残す場合は importer または一括 migration で `InitialStateId + States + Actions` へ移す。

### 6.4 正規データとコードの責務分離

データに寄せるもの：

* `InitialStateId`
* `BossStateDefinition`
* `BossActionPlan`
* `BossActionDefinition`
* `BossActionCommand`
* `BossActionWindow`
* `BossBulletPatternDefinition`
* 発射間隔、Way 数、拡散角、弾速、寿命、威力、吸収量、挙動種別
* 各種アニメ名、SE 名、VFX 名、将来の summon 定義
* 部位レイアウト

コードに寄せるもの：

* 状態遷移の実行
* action 選択の履歴管理
* timeline の frame 進行
* command 発火順制御
* window 有効判定
* emit signal と state transition の merge
* 下位弾幕パターン生成
* 妥当性検証
* legacy migration
* ダメージ適用、ドロップ、一括弾消し

---

## 7. ゲーム固有要素との接続

### 7.1 ダッシュ吸収

敵弾は少なくとも次の値を持つ。

* `canBeAbsorbed`
* `energyValue`
* `absorbEffectId`

ダッシュ中に敵弾へ接触した場合：

* `EnemyBulletService` または上位管理層が対象弾を中央管理リストから外す。
* `ItemSpawnService` または対応する加算処理へ吸収量を通知する。
* 弾 View は Pool または破棄経路へ返却する。

### 7.2 ボス接触ダメージ

* ダッシュ中のプレイヤーが `BodyCollision` に触れた場合、Presentation は Domain の `DamageService` へ「Boss 接触ダメージ」を通知する。
* 近接攻撃判定と通常接触判定は分離し、常時有効な攻撃 Hitbox にはしない。
* 将来的に近接攻撃そのものは `HitboxWindow` と `PlayAnimation` の組み合わせで同期する。

### 7.3 特殊攻撃による弾消し

* 特殊攻撃アイコンに紐づく強攻撃シーケンスは、必要に応じて「大ダメージ + 敵弾一括消去」を持てる。
* 一括消去はボス固有コードから個別弾参照しない。
* 敵弾管理サービスに対する中央命令で実行する。

想定例：

* `ClearEnemyBullets(ClearReason.SpecialAttack)`
* `ClearEnemyBulletsOwnedBy(bossId, ClearReason.BossPhaseEnd)`

### 7.4 ドロップと高リスク・高リターン

* ボス本体は画面右側の圧力源として設計する。
* プレイヤーが右側に踏み込むほど、被弾、接触、高密度弾幕のリスクが高まる。
* その代わり、ボス被弾時ドロップや撃破ドロップは `DropService` と連携して高いリターンに繋げる。
* ドロップ量は既存要件どおり、基礎量と攻撃側のドロップ倍率で算出する。

### 7.5 Animation / SE / VFX 連携

* `AnimationStateName` と `PlayAnimation` は `BossAnimatorBridge` を通じて Unity Animator へ橋渡しする。
* `PlayEffect` は `VFXRoot` または部位 attach 点へ出す。
* `PlaySound` は action の frame に同期して SE を鳴らす。
* 現状は契約のみで、runtime 接続は未実装。

---

## 8. 実装状況と未実装項目

### 8.1 現状整理

| 項目 | 正規設計 | 現状 | 備考 |
| --- | --- | --- | --- |
| `BossStateMachine` による `Intro` / `Phase` / `Dead` 管理 | 実装する | 実装済み | state 遷移と validation あり |
| `BossActionPlan` による固定順 + 履歴付きランダム選択 | 実装する | 実装済み | `HistoryWindow` の除外緩和あり |
| `ConfiguredBossAction` による frame 基準 timeline 実行 | 実装する | 実装済み | 60fps 仮想 frame で更新し、action 跨ぎの frame budget も消費する |
| `SpawnBulletPattern` command | 実装する | 実装済み | bullet emitter 起動と `InitialDelayFrames` による初弾制御まで接続済み |
| `EmitSignal` command | 実装する | 実装済み | 同 update 内で state transition 判定へ反映済み |
| `Manual` end condition | 実装する | 実装済み | 自動完了しない |
| `DurationElapsed` end condition | 実装する | 実装済み | 終了判定と余剰 frame 持ち越し済み |
| `BossBulletPatternDefinition` -> `SingleShot/NWay/BurstShot` | 実装する | 実装済み | 下位弾幕再利用済み |
| 旧 `phasePatterns` 互換変換 | 必要なら migration で対応する | 未実装 | `BossParamsAsset` に旧 field は残るが contract / mapper には未接続 |
| `Action.AnimationStateName` | Animator 連携に使う | 契約済み / runtime 未接続 | field は asset / contract に存在 |
| `PlayAnimation` command | 中途アニメ切替 | 契約済み / runtime 未接続 | `AnimationStateName` / `CrossFadeFrames` payload あり |
| `SpawnEnemy` command | 子機/使い魔/砲台生成 | 契約済み / runtime 未接続 | `EnemyDefinitionId` / `SpawnOffset` payload あり |
| `PlayEffect` command | VFX 再生 | 契約済み / runtime 未接続 | `EffectId` / `EffectLocalOffset` payload あり |
| `PlaySound` command | SE 再生 | 契約済み / runtime 未接続 | `SoundId` / `VolumeScale` payload あり |
| `BossActionWindow` の active 判定 | 実装する | 実装済み | `BossActionFrameState` に集計済み |
| `BossActionFrameState` の外部公開 | 実装する | 実装済み | `BossBehaviorUpdateResult.FrameState` 経由で `BattleContext` へ反映済み |
| `MoveWindow` の実戦闘適用 | 実装する | 実装済み | `VelocityPerSecond` を `Boss.Position` へ適用済み |
| `HitboxWindow` の実戦闘適用 | 実装する | 実装済み | `BossBattleRuntime` が player damage 判定に使用済み |
| `HurtboxWindow` の実戦闘適用 | 実装する | 部分実装 | 明示 hurtbox window がある間だけ被弾受付を許可する。個別 hurtbox の空間判定は未接続 |
| `InvincibleWindow` の実戦闘適用 | 実装する | 実装済み | `BossDamageService` がボス被弾を無効化 |
| `CancelWindow` の実戦闘適用 | 実装する | 部分実装 | window 中の割り込み許可に使用済み。cancel tag 別の分岐は未拡張 |
| 即発射可能な `SpawnBulletPattern` | 実装する | 実装済み | `InitialDelayFrames = 0` で表現する |
| 同 frame 複数 command の定義順保証 | 実装する | 実装済み | `TriggerFrame` 昇順、同 frame は定義順 |
| `Wave` / `Homing` の実挙動 | 実装する | 部分実装 | 設定列挙はあるが運用はほぼ `Straight` |
| 旧 flat `attacks` asset の自動移行 | 実装する | 未実装 | field rename のみで内部形状変換なし |

### 8.2 直近で必要な未実装

#### Animation / SE / VFX / SpawnEnemy

次段階で最低限必要なこと：

* `PlayAnimation` と `AnimationStateName` を `BossAnimatorBridge` に接続する。
* `PlayEffect` と `PlaySound` を Presentation 側の再生口へ接続する。
* `SpawnEnemy` の spawn 先責務と生成要求 DTO を確定し、runtime command として実行する。

#### Window 適用の精度向上

次段階で最低限必要なこと：

* `HurtboxWindow` の `Offset` / `Radius` をボス被弾判定へ空間的に反映する。
* `CancelWindow.ActiveCancelTags` を、割り込み先 action や優先度へ拡張するか決める。
* `MoveWindow` に境界制限、curve、anchor 移動が必要になった時点で payload を増やす。

#### 互換資産の整理

次段階で最低限必要なこと：

* 旧 flat `attacks` 資産の migration ルートを用意する。
* 旧 `actionIntervalSeconds` / `phasePatterns` を削除するか、importer で `InitialStateId + States + Actions` へ変換する。

#### 回帰テストの追加

次段階で最低限必要なこと：

* `DurationElapsed` が action を跨いで frame budget を消費することを検証する。
* 同 frame 複数 command が定義順に実行されることを検証する。
* `BossActionFrameState` の hitbox / hurtbox / invincible / cancel / move 集計を検証する。

---

## 9. 段階導入方針

### Phase 1：最小ボス基盤

* `BossRoot`
* `BossHealth`
* `BossSpawnPoint`
* `BossSpawner`
* 見た目と Hurtbox の分離

### Phase 2：状態機械基盤

* `BossStateMachine`
* `BossBehaviorUpdateResult`
* `Intro` / `Phase` / `Dead`
* イントロ signal と撃破完了 signal の接続

### Phase 3：Action / Timeline 基盤

* `BossActionController`
* `ConfiguredBossAction`
* `BossActionTimelineExecutor`
* `BossActionDefinition`
* `BossActionPlan`
* `SpawnBulletPattern`
* `EmitSignal`
* `SingleShotPattern` / `NWayShotPattern` / `BurstShotPattern`
* `BossBulletPatternEmitterRuntime`

### Phase 4：Window / FrameState 接続

* `BossBehaviorUpdateResult.FrameState` による `BossActionFrameState` 公開
* `HitboxWindow`
* `HurtboxWindow`
* `InvincibleWindow`
* `MoveWindow`
* `CancelWindow`

### Phase 5：演出 command 接続

* `PlayAnimation`
* `PlayEffect`
* `PlaySound`
* `SpawnEnemy`
* `BossAnimatorBridge`
* `VFXRoot`
* summon / familiar / turret 系の橋渡し

### Phase 6：複合ボス

* `BossPart`
* `BossAssembler`
* `BossLayout`
* 左右砲台、回転ユニット、補助部位

### Phase 7：複雑弾幕

* Spiral
* AlternatingSpiral
* RandomSpread
* Familiar を用いた空間分担
* 分裂弾
* フェーズ別スペル切り替え
* 詳細 emitter 制御

### Phase 8：ゲーム固有要素接続

* ダッシュ吸収によるエネルギー化
* 特殊攻撃による弾消し
* 撃破ドロップ
* UI、SE、VFX 連携

---

## 10. 非目標

* 全敵に共通する完全汎用 AI フレームワーク
* ノーコードで全弾幕を記述する DSL の完成
* あらゆる部位関係を自動解決する超汎用アセンブラ
* 物理ベースの複雑な破壊シミュレーション
* 弾 1 発ごとに個別 coroutine や個別 AI を持たせる構造

---

## 11. アンチパターン

* バトル全体進行とボス固有進行を 1 クラスに混在させる
* フェーズ選択、action 選択、timeline 実行、弾幕生成を 1 クラスに詰め込む
* `BossActionDefinition` を再び弾幕専用の flat 定義へ戻す
* `Command` と `Window` を区別せず、すべてを animation event のみで表現する
* `Window` を contract に置きながら外部へ一切公開しないまま運用し続ける
* 未対応 command を silently ignore する
* 弾を 1 発ずつ `Instantiate / Destroy` し続ける
* 弾 1 発ごとに coroutine を持たせる
* 見た目と被弾判定を完全一致前提で設計する
* ScriptableObject に実行時状態を持たせる
* ボス固有コードが敵弾インスタンスを個別に握り続ける

---

## 12. 最終方針

> ボス固有進行は `BossStateMachine` を中心に `Intro` / `Phase` / `Dead` で管理し、フェーズ内 action は `BossActionController` が選択し、下位実行は `ConfiguredBossAction + BossActionTimelineExecutor` が frame 基準で処理する。`Command` は瞬間イベント、`Window` は区間イベントとし、弾幕は `SpawnBulletPattern` command から起動される下位 `IBossAttackPattern` 群へ分離する。外周のバトル進行は `BattleFlowService` に残し、両者は `BossBehaviorUpdateResult`、`BossActionFrameState`、signal で接続する。正規入力は `InitialStateId + States + Actions` とし、旧 authoring データは必要に応じて migration で正規入力へ変換する。Animation、SE、VFX、召喚、近接判定、無敵、移動はすべて同じ timeline 基盤へ統合し、ボス実装を弾幕専用構造へ戻さない。
