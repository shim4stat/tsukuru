# ボス・弾幕詳細設計書

## 0. 参照ドキュメント

* 要件定義書：ボス戦、複数HPゲージ、ダッシュ吸収、ボス接触ダメージ、ドロップ、攻撃アイコン
* アーキテクチャ設計書：Domain/Presentation 分離、SpawnRequest、コリジョン橋渡し、ObjectPool
* ドメインモデル設計書：`Boss` / `BattleFlowService` / `BossStateMachine` / `EnemyBullet`
* データ設計書：`BossParamsContract` / `BossStateDefinition` / `BossActionDefinition`
* 実装計画書：BOSS-01〜13、PL-04、PL-05

本書は、2026-05-07 時点の現行実装を踏まえたうえで、今後の正規設計を `BossActionBase + BossActionContext + BossActionFactory/Registry` に切り替える。
旧 `ConfiguredBossAction + Timeline(Command/Window)` は既存実装および移行期間の legacy/互換ルートとして扱い、新規の複雑なボス行動は C# 専用アクションクラスで記述する。

---

## 1. 目的

* ボス固有の進行を、バトル全体進行から分離して安全に拡張できるようにする。
* ボスの各攻撃、移動、演出、判定制御を「原則 1 行動 = 1 C# スクリプト」で記述できるようにする。
* `phase_01` のような内部状態を持つ行動を、データ列ではなく読みやすい手続き型コードで表現する。
* 弾の発射だけでなく、移動、アニメーション、SE/VFX、召喚、hitbox/hurtbox、無敵、cancel、signal を同じ action スクリプト内で扱えるようにする。
* 低レベル弾幕パターンはヘルパーとして再利用しつつ、上位の「攻撃/行動」そのものは C# action に寄せる。

---

## 2. 位置付けと整合方針

### 2.1 正規設計の立場

* `BattleFlowService` はバトル全体の外周進行を担う。
  * `BattleStart`、`BossBoot`、`Combat`、`BossDefeated`、`BattleEnd` の真実はここに置く。
* `Boss` は Domain 上の真実の状態とし、現在ゲージ、現在HP、撃破判定、ゲージ跨ぎダメージ適用を持つ。
* `BossStateMachine` はボス固有進行の中心とし、`Intro`、`Phase`、`Dead` の遷移を管理する。
* `Phase` 状態の下位実行は `BossActionController` が担い、`BossActionPlan` に従って action id を選ぶ。
* 選ばれた action id は `BossActionFactory` または `BossActionRegistry` に渡し、対応する C# 専用 action インスタンスを生成する。
* 正規ルートは `BossStateMachine` → `BossActionController` → `IBossAction` → `BossActionBase` 派生クラス → `BossActionContext` とする。
* 各 action は `BossActionBase` を継承し、`OnEnter`、`OnFrame`、`OnExit` で行動を記述する。
* `BossActionContext` は action から `BattleContext` を直接壊さずに操作するための小さな便利APIを提供する。
* `EnemyBulletSpawnRequest` は引き続き Domain から Presentation への弾生成要求 DTO として使う。
* `EnemyBulletService` は引き続き弾の生成、移動、寿命管理、消滅管理を担う。
* `BossBehaviorUpdateResult`、`BossActionFrameState`、`BossActionCommandEvent` は C# action の出力を Presentation へ渡す公開経路として維持する。
* ボス定義の正規入力は `BossParamsContract.InitialStateId`、`States`、`ActionPlan`、action 参照情報とする。
* 現行 `BossParamsContract.Actions` の `Commands` / `Windows` payload は legacy timeline 用データであり、今後の正規実行データではない。

### 2.2 Legacy timeline の扱い

* `ConfiguredBossAction` は既存の `BossActionDefinition` を実行する legacy/互換 action として残せる。
* `BossActionTimelineExecutor` は legacy action 内部でのみ使う実行器として扱う。
* `BossActionCommand` と `BossActionWindow` は、既存 asset や簡単な検証行動を動かすための互換 payload として扱う。
* 新規の複雑な行動で `Command` / `Window` のデータ列を増やすことは避ける。
* 移行期間は、`IBossAction` の実装として C# action と legacy timeline action が混在してよい。
* 移行完了後に legacy timeline 資産が不要になった段階で、contract / asset / mapper からの削除を検討する。

### 2.3 本書の読み方

* 「正規設計」は今後の実装が従うべき構造と意味を表す。
* 「現状実装」は 2026-05-07 時点でコード上に確認できる範囲を表す。
* 「legacy/互換」は既存実装や既存資産を動かすために残すが、新規設計の主経路ではないものを表す。
* 正規設計と現状実装が異なる場合、本書では正規設計を本文の基準とし、差分は `8. 実装状況と移行項目` で明示する。

### 2.4 用語

| 用語 | 役割 | 補足 |
| --- | --- | --- |
| `BossStateMachine` | ボス固有状態機械 | `Intro` / `Phase` / `Dead` を管理する |
| `IBossState` | 状態機械の状態契約 | `IntroBossState` / `PhaseBossState` / `DeadBossState` の共通契約 |
| `BossActionController` | フェーズ内 action オーケストレータ | `BossActionPlan` に従って次 action を選ぶ |
| `IBossAction` | 1 action 単位の共通契約 | C# action と legacy timeline action の共通口 |
| `BossActionBase` | C# 専用 action の基底 | `OnEnter` / `OnFrame` / `OnExit` を提供する |
| `BossActionContext` | action 用便利API | 移動、弾、演出、判定、signal を扱う |
| `BossActionFactory` | action 生成責務 | action id / type id から `IBossAction` を生成する |
| `BossActionRegistry` | action 登録責務 | ボス固有 action クラスを登録する |
| `BossActionExecutionResult` | action 1 update 分の内部結果 | `SpawnRequests` / `EmittedSignals` / `CommandEvents` / `FrameState` を持つ |
| `BossBehaviorUpdateResult` | ボス更新結果 DTO | Domain から Presentation へ action 結果を返す |
| `BossActionFrameState` | 現フレームの公開状態 | active hitbox / hurtbox / invincible / move velocity / cancel tag を持つ |
| `BossActionCommandEvent` | Presentation 向け瞬間イベント | animation、SE、VFX、spawn などを渡す |
| `IBossAttackPattern` | 低レベル弾幕ヘルパー | `SingleShot` / `NWayShot` / `BurstShot` など |
| `ConfiguredBossAction` | legacy timeline action | 既存 `Commands` / `Windows` を実行する互換実装 |
| `BossActionTimelineExecutor` | legacy timeline 実行器 | legacy action 内部のみに閉じる |
| `BossBrain` | 将来の Presentation/Animation 連携候補 | 現行の真実の状態機械ではない |

### 2.5 要件解釈の固定

* ボスは基本的にロボット外かつ画面右側に存在する前提で設計する。
* 攻撃アイコンは既存要件の「通常4 + 特殊1」を前提とし、具体配置は Stage/Robot レイアウト側の責務とする。
* 本書でいう「右中央の大砲」は、既存要件の特殊攻撃アイコンから発動される強攻撃シーケンスの一形態として扱う。
* 近接、体当たり、突進、召喚、SE/VFX は C# action 内の処理として表現する。

---

## 3. レイヤ別責務

### 3.1 Domain

* `Boss`
  * 現在ゲージ、現在HP、撃破判定、ゲージ跨ぎダメージ適用、位置を保持する。
* `BattleFlowService`
  * バトル全体のフェーズ進行を管理する。
  * ボス固有の内部状態遷移は持たない。
* `BossStateMachine`
  * ボス固有の `Intro`、`Phase`、`Dead` を管理する。
  * state 定義、action 参照、遷移条件の妥当性検証を担う。
  * `Phase` では `BossActionController` を用いて action を進行させる。
* `BossActionController`
  * `OpeningSequenceActionIds` を先に消化し、その後 `RandomActionIds` から履歴除外付きで次 action を選ぶ。
  * action の開始、更新、完了検知、履歴更新を担う。
  * action の具体処理には関与しない。
* `BossActionFactory` / `BossActionRegistry`
  * action id または action type id を C# action クラスへ解決する。
  * 必要な config を渡して `IBossAction` インスタンスを生成する。
* `BossActionBase`
  * `IBossAction` の定型処理を持つ基底クラス。
  * duration、cancel policy、elapsed frame、完了状態、snapshot を管理する。
* `BossActionContext`
  * action が使う操作APIを集約する。
  * `BattleContext` への直接変更、出力 DTO 生成、FrameState 構築を安全に包む。
* `IBossAttackPattern`
  * 低レベルな弾の撃ち方のみを表す。
  * 状態遷移、近接判定、SE/VFX、部位制御は持たない。
* `EnemyBulletService`
  * 弾の生成、移動、寿命管理、消滅管理を担う。
* `BossDamageService`
  * ボスへのダメージ適用のみを担う。
  * `BossActionFrameState` の invincible / hurtbox 状態を参照する。
* `DropService` / `ItemSpawnService`
  * ダッシュ吸収、被弾時ドロップ、撃破ドロップ、個数上限を担う。

### 3.2 Presentation

* `GameSceneEntryPoint`
  * `BattleFlowService` と `BossStateMachine` を接続する。
  * `BossBoot` と `Combat` の両方で `BossStateMachine.Update` を呼び、戻り値の `SpawnRequests`、`CommandEvents`、`BossBehaviorSignal`、`BossActionFrameState` を処理する。
* `BossTitleOverlayPresenter`
  * イントロ演出完了時に `BossStateSignalIds.IntroFinished` を `BossStateMachine` へ通知する。
* `BossBattleRuntime`
  * `EnemyBullet` と View の同期、ボス View の反映、当たり判定橋渡しを担う。
  * `BossActionCommandEvent` のうち `PlayAnimation` は Animator へ橋渡し済み。
  * `SpawnEnemy`、`PlayEffect`、`PlaySound` は Presentation 側の再生/生成口を今後追加する。
* `BossRoot`、`BossHealth`、`BossAssembler`、`BossAnimatorBridge`
  * Prefab 構造、見た目、アニメ同期、位置反映、被弾入口の橋渡しを担う将来拡張ポイント。
* `BossHurtbox`、`BossAttackHitbox`、`BulletHitbox`
  * Unity の Trigger/Collision を受け、Domain サービスへ橋渡しする。

### 3.3 MasterData / ScriptableObject

* `BossParamsContract`
  * 複数ゲージ HP、ドロップ量、初期状態 ID、状態定義、action 参照情報を持つ。
* `BossStateDefinition`
  * 状態 ID、`StateType`、`ActionPlan`、`Transitions` を持つ。
* `BossActionPlan`
  * `OpeningSequenceActionIds`、`RandomActionIds`、`HistoryWindow` を持つ。
* `BossActionDefinition`
  * 正規設計では「C# action を参照する metadata」として扱う。
  * 最低限 `Id`、`ActionTypeId`、`EndConditionType`、`CancelPolicy`、`TotalDurationFrames`、任意 config 参照を持つ。
* legacy `Commands` / `Windows`
  * 現行 contract / asset に残る互換 payload。
  * 新規 action の正規表現としては使わない。
* `BossBulletPatternDefinition`
  * 下位弾幕ヘルパーの発射設定を持つ。
  * 上位 action そのものの正規表現ではない。
* 旧 `phasePatterns`
  * 現行 contract 外の旧 authoring field。

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

* `BossRoot` はボス全体の親で、ステージ上の位置、生成、破棄、撃破時の演出起点をまとめる。
* `VisualRoot` は SpriteRenderer、Animator、装飾オブジェクトを持つ見た目専用ノードとし、当たり判定を持たない。
* `Hurtboxes` は被弾判定群を管理し、C# action から `SetHurtbox` などで現フレームの有効状態を指定できる。
* `BodyCollision` はプレイヤーとの接触判定、押し戻し判定、ダッシュ接触ダメージの判定対象を担う。
* `PartSlots` は部位差し込み Transform 群を持つ。
* `EmittersRoot` は発射器、使い魔、補助ノードの配置親とする。
* `VFXRoot` は被弾、発射、破壊、フェーズ遷移などの VFX 親とする。

### 4.3 判定分離

* `VisualRoot` は見た目のみ。
* `BossHurtbox` は被弾判定のみ。
* `BossAttackHitbox` は近接攻撃や体当たり攻撃の当たり判定のみ。
* `BodyCollision` は通常接触用。
* C# action は `BossActionContext.SetHitbox`、`SetHurtbox`、`SetInvincible` を通じてフレーム状態を公開する。
* 見た目だけで無敵や被弾可否を決めない。

### 4.4 高レベル状態

ボス固有状態は Domain 側の `BossStateMachine` で次を管理する。

* `Intro`
* `Phase`
* `Dead`

補足：

* これはボス行動の真実の状態であり、`BattleFlowService` の外周フェーズと分離する。
* `BossBrain` 側で `Idle`、`Move`、`Recovery`、`Stunned` などを持つ構成は将来拡張案であり、現行の真実の状態機械ではない。
* フェーズごとの差分は `BossStateDefinition` と `BossActionPlan` による action 選択で表現する。

---

## 5. C# Action 実行設計

### 5.1 基本モデル

ボスの下位実行は次の階層で分離する。

* `BattleFlowService`
* `BossStateMachine`
* `IBossState`
* `BossActionController`
* `IBossAction`
* `BossActionBase` 派生クラス
* `BossActionContext`
* `EnemyBulletSpawnRequest`
* `EnemyBulletService`

ボス本体は「どの状態にいるか」「今どの action を実行するか」を決める。
action クラスは「毎フレームどう動くか」「いつ弾を撃つか」「いつ演出を出すか」「どの判定を有効にするか」をコードで記述する。

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
* `CurrentActionCompleted`
* `CurrentGaugeIndexAtOrAbove`

運用方針：

* 正規用途は `ExternalSignal`、`ElapsedTime`、`CurrentHpRateAtOrBelow`、`CurrentActionCompleted` とする。
* `CurrentGaugeIndexAtOrAbove` はゲージ番号を条件にした手動 authoring や将来の legacy migration 用に残す。
* `CurrentActionCompleted` は「現在 action が完了した」で扱う。
* 遷移は宣言順に評価し、同フレームで複数条件が成立しても先頭のみ採用する。
* 終端遷移は `Dead` からのみ許可する。
* `Intro` 完了は Presentation から渡される外部シグナルで決定する。
* HP 0 到達後も即座に `BattleFlowService` を終端させず、`Dead` 状態の完了後に `DeadCompleted` を返して撃破進行へ入る。

### 5.3 Action 定義と選択

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
* `BossActionController` は action id の選択とライフサイクル管理だけを担当し、行動内容を知らない。

### 5.4 `BossActionBase`

`BossActionBase` は `IBossAction` を実装する手続き型 action の基底とする。

公開・保護メンバーの想定：

* `Id`
* `DurationFrames`
* `CancelPolicy`
* `IsCompleted`
* `Enter(BattleContext context)`
* `Update(BattleContext context, int availableFrames)`
* `Snapshot()`
* `Exit()`
* `protected virtual OnEnter(BossActionContext action)`
* `protected abstract OnFrame(BossActionContext action)`
* `protected virtual OnExit(BossActionContext action)`
* `protected Complete()`

運用方針：

* `BossActionBase` は 60fps 仮想 frame を基準に `OnFrame` を呼ぶ。
* `DurationFrames` 到達で自動完了できる。
* `Manual` 相当の行動は `Complete()` を呼ぶまで完了しない。
* 1 update 内で action が完了した場合、余剰 frame は次 action に持ち越せる構造を維持する。
* `BossActionBase` は action の出力を `BossActionExecutionResult` として集約する。

### 5.5 `BossActionContext`

`BossActionContext` は action から使う小さな便利APIを提供する。

基本情報：

* `BattleContext Battle`
* `Boss Boss`
* `int ElapsedFrames`
* `int DurationFrames`
* `float Progress01`
* `float DeltaSeconds`

時間・制御：

* `bool IsEvery(int intervalFrames)`
* `bool IsAt(int frame)`
* `void Complete()`
* `void EmitSignal(string signalId)`

移動：

* `void MoveBossTo(Vector3 position)`
* `void MoveBossBy(Vector3 delta)`
* `void SetBossVelocity(Vector3 velocityPerSecond)`

弾：

* `void FireBullet(Vector3 direction, BossBulletSpec bullet)`
* `void FireBullet(Vector3 originOffset, Vector3 direction, BossBulletSpec bullet)`
* `void FirePattern(IBossAttackPattern pattern)`

演出：

* `void PlayAnimation(string stateName, int crossFadeFrames = 0)`
* `void PlayEffect(string effectId, Vector3 localOffset)`
* `void PlaySound(string soundId, float volumeScale = 1f)`
* `void SpawnEnemy(string enemyDefinitionId, Vector3 offset)`

判定：

* `void SetHitbox(Vector3 offset, float radius, int damage)`
* `void SetHurtbox(Vector3 offset, float radius)`
* `void SetInvincible(bool isInvincible = true)`
* `void AddCancelTag(string cancelTag)`

補足：

* `BossActionContext` は `BossActionFrameState` を構築し、Presentation と damage service へ公開する。
* `PlayAnimation` は `BossActionCommandEvent` として Presentation へ渡し、現行 `BossBattleRuntime` で Animator へ橋渡し済み。
* `PlayEffect`、`PlaySound`、`SpawnEnemy` は同じ event 経路で渡し、Presentation 側の実行口を今後追加する。

### 5.6 Action Factory / Registry

`BossActionFactory` は action id または action type id から `IBossAction` を生成する。

想定方針：

* `BossActionRegistry` に action type id と生成関数を登録する。
* `BossActionDefinition` は action type id と config 参照を持つ。
* config が小さい場合は `BossActionDefinition` 内の scalar 値で渡してよい。
* config が大きい場合は専用 config class または ScriptableObject から読み取り model へ変換して渡す。
* factory は `ConfiguredBossAction` を legacy action として生成してもよい。

例：

```csharp
registry.Register("vertical_sweep_shot", definition =>
    new VerticalSweepShotAction(new VerticalSweepShotConfig(definition)));
```

### 5.7 phase_01 の正規例

`phase_01` は上下に反復運動しながら、発射角を徐々に回転させる行動として実装する。

```csharp
internal sealed class VerticalSweepShotAction : BossActionBase
{
    private readonly VerticalSweepShotConfig _config;
    private Vector3 _startPosition;
    private float _currentAngleDegrees;

    public VerticalSweepShotAction(VerticalSweepShotConfig config)
        : base(config.Id, config.DurationFrames, config.CancelPolicy)
    {
        _config = config;
    }

    protected override void OnEnter(BossActionContext action)
    {
        _startPosition = action.Boss.Position;
        _currentAngleDegrees = _config.StartAngleDegrees;
        action.PlayAnimation(_config.AnimationStateName, _config.CrossFadeFrames);
    }

    protected override void OnFrame(BossActionContext action)
    {
        float progress = action.Progress01;
        float y = MathF.Sin(progress * MathF.Tau * _config.MoveCycles) * _config.MoveAmplitude;
        action.MoveBossTo(_startPosition + new Vector3(0f, y, 0f));

        if (action.IsEvery(_config.FireIntervalFrames))
        {
            Vector3 direction = BossActionMath.DirectionFromDegrees(_currentAngleDegrees);
            action.FireBullet(direction, _config.Bullet);
            action.PlaySound(_config.FireSoundId);
            _currentAngleDegrees += _config.AngleStepDegrees;
        }

        action.SetHurtbox(Vector3.Zero, _config.HurtboxRadius);
    }
}
```

この例では、移動、弾発射、アニメ、SE、hurtbox を 1 つの action スクリプトにまとめる。
`BossActionPlan` 側は `phase_01` で `vertical_sweep_shot` の action id を選ぶだけでよい。

### 5.8 低レベル弾幕ヘルパー

`BossBulletPatternDefinition` と `IBossAttackPattern` は低レベル弾幕ヘルパーとして残す。

* `SingleShot`
* `NWayShot`
* `BurstShot`
* 将来の `Spiral`
* 将来の `AlternatingSpiral`

運用方針：

* `IBossAttackPattern` は「弾の撃ち方」だけを表す。
* 上位の行動単位は C# action で表す。
* action 内で必要に応じて `FireBullet` または `FirePattern` を呼ぶ。
* 弾 1 発ごとに coroutine や個別 AI を持たせない。

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

* 正規入力は `InitialStateId`、`States`、`ActionPlan`、action 参照情報である。
* `ActionIntervalSeconds` と `PhasePatterns` は現行 `BossParamsContract` の正規 field ではない。
* `Commands` / `Windows` payload は legacy timeline 用であり、新規の複雑行動の正規入力ではない。
* action の実行単位は C# class であり、調整値は config として C# action に渡す。

### 6.2 ScriptableObject 側

ScriptableObject は次のどちらかを段階的に扱う。

* 正規：C# action 参照 metadata
* 互換：legacy timeline payload

正規 `BossActionDefinitionAsset` の想定：

* `id`
* `actionTypeId`
* `animationStateName`
* `endConditionType`
* `cancelPolicy`
* `totalDurationFrames`
* `configKey` または専用 config

互換 payload：

* `BossActionCommandAsset`
* `BossActionWindowAsset`
* `BossBulletPatternDefinitionAsset`

移行方針：

* 現行 `BossParamsAsset` / `BossParamsContract` に残る `commands` / `windows` は legacy timeline 用として維持する。
* 新規の複雑行動では `actionTypeId` と専用 config を使う。
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
* action id
* action type id
* action config 参照
* 基本調整値
* 各種アニメ名、SE 名、VFX 名、将来の summon 定義
* 部位レイアウト

コードに寄せるもの：

* action の具体的な毎フレーム処理
* 正弦波、円運動、角度回転、条件分岐などの手続き
* 状態遷移の実行
* action 選択の履歴管理
* action 生成
* `BossActionContext` による出力集約
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
* 近接攻撃は C# action の `SetHitbox` と `PlayAnimation` の組み合わせで同期する。

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

* `BossActionContext.PlayAnimation` は `BossActionCommandEvent` を通じて Unity Animator へ橋渡しする。
* 2026-05-07 時点で `PlayAnimation` は `BossBattleRuntime` と `IBossBattleAnimatableView` 経由で runtime 接続済み。
* `PlayEffect` は `VFXRoot` または部位 attach 点へ出す。
* `PlaySound` は action の frame に同期して SE を鳴らす。
* `SpawnEnemy` は子機、使い魔、砲台などの生成要求へ変換する。
* `PlayEffect`、`PlaySound`、`SpawnEnemy` の Presentation 実行口は今後追加する。

---

## 8. 実装状況と移行項目

### 8.1 現状整理

| 項目 | 正規設計 | 現状 | 備考 |
| --- | --- | --- | --- |
| `BossStateMachine` による `Intro` / `Phase` / `Dead` 管理 | 維持する | 実装済み | state 遷移と validation あり |
| `BossActionPlan` による固定順 + 履歴付きランダム選択 | 維持する | 実装済み | `HistoryWindow` の除外緩和あり |
| `BossActionController` による action ライフサイクル | 維持する | 実装済み | `IBossAction` を扱える |
| `BossActionBase` | 新規正規実装にする | 未実装 | C# 専用 action の基底 |
| `BossActionContext` | 新規正規実装にする | 未実装 | action 用便利API |
| `BossActionFactory` / `BossActionRegistry` | 新規正規実装にする | 未実装 | action id から C# action を生成 |
| C# 専用 action | 新規正規実装にする | 未実装 | `phase_01` から導入する |
| `BossBehaviorUpdateResult.CommandEvents` | 維持する | 実装済み | C# action の Presentation event 経路として使う |
| `BossActionFrameState` の外部公開 | 維持する | 実装済み | `BattleContext` へ反映済み |
| `PlayAnimation` の runtime 橋渡し | 維持する | 実装済み | `BossBattleRuntime` / `IBossBattleAnimatableView` 経由 |
| `SpawnEnemy` / `PlayEffect` / `PlaySound` event | 維持する | 部分実装 | event は出せるが Presentation 実行口は今後追加 |
| Hurtbox 空間判定API | 維持する | 実装済み | `BossDamageService` に hit position / radius 判定口あり |
| `ConfiguredBossAction` | legacy/互換として扱う | 実装済み | 新規正規ルートではない |
| `BossActionTimelineExecutor` | legacy/互換として扱う | 実装済み | legacy action 内部に閉じる |
| `BossActionCommand` / `BossActionWindow` | legacy/互換 payload として扱う | 実装済み | 新規の複雑行動では増やさない |
| `BossBulletPatternDefinition` -> `SingleShot/NWay/BurstShot` | 低レベルヘルパーとして扱う | 実装済み | 上位 action の正規表現ではない |
| 旧 `phasePatterns` 互換変換 | 必要なら migration で対応する | 未実装 | `BossParamsAsset` に旧 field は残るが contract / mapper には未接続 |

### 8.2 直近で必要な実装

#### Procedural Boss Action API

次段階で最低限必要なこと：

* `BossActionBase` を追加する。
* `BossActionContext` を追加する。
* `BossActionContext` から弾生成、アニメ event、FrameState、signal を集約できるようにする。
* `BossActionController` の action 跨ぎ frame budget 処理を C# action でも維持する。

#### Action Factory / Registry

次段階で最低限必要なこと：

* `BossActionFactory` または `BossActionRegistry` を追加する。
* `BossStateMachine.CreateActionController()` で `new ConfiguredBossAction(...)` 固定にしない。
* action id / type id から C# action または legacy action を生成できるようにする。

#### phase_01 専用 action

次段階で最低限必要なこと：

* `VerticalSweepShotAction` を追加する。
* 上下反復移動、回転角ショット、hurtbox、アニメ/SE event を action 内で記述する。
* `TestBossDefinitionProvider` の `phase_01` は C# action を参照する定義へ移行する。

#### Presentation event 接続

次段階で最低限必要なこと：

* `PlayEffect` と `PlaySound` を Presentation 側の再生口へ接続する。
* `SpawnEnemy` の spawn 先責務と生成要求 DTO を確定する。

#### 互換資産の整理

次段階で最低限必要なこと：

* legacy timeline 資産を残す範囲を決める。
* 旧 flat `attacks` 資産の migration ルートを用意する。
* 旧 `actionIntervalSeconds` / `phasePatterns` を削除するか、importer で正規入力へ変換する。

#### 回帰テストの追加

次段階で最低限必要なこと：

* `BossActionBase` の duration 完了と action 跨ぎ frame budget を検証する。
* `BossActionContext.FireBullet` が `EnemyBulletSpawnRequest` を生成することを検証する。
* `BossActionContext.SetHitbox` / `SetHurtbox` / `SetInvincible` が `BossActionFrameState` に反映されることを検証する。
* `BossActionFactory` が C# action と legacy action を解決できることを検証する。

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

### Phase 3：C# Action 基盤

* `BossActionController`
* `BossActionBase`
* `BossActionContext`
* `BossActionFactory`
* `BossActionRegistry`
* `BossActionDefinition` の action 参照 metadata 化
* legacy `ConfiguredBossAction` の factory 経由化

### Phase 4：phase_01 専用 action

* `VerticalSweepShotAction`
* 上下反復移動
* 回転ショット
* hurtbox / hitbox / invincible / cancel の context API 適用
* アニメ / SE event の context API 適用

### Phase 5：演出・生成接続

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
* 複雑 action を `Commands` / `Windows` の大量データ列で表現し続けること

---

## 11. アンチパターン

* バトル全体進行とボス固有進行を 1 クラスに混在させる
* フェーズ選択、action 選択、弾幕生成、Presentation event を 1 クラスに詰め込む
* 新規の複雑行動を legacy timeline payload の肥大化で表現する
* C# action から UnityEngine の View や Prefab を直接操作する
* action が `BattleContext` の内部コレクションを無秩序に直接変更する
* 未対応 event を silently ignore する
* 弾を 1 発ずつ `Instantiate / Destroy` し続ける
* 弾 1 発ごとに coroutine を持たせる
* 見た目と被弾判定を完全一致前提で設計する
* ScriptableObject に実行時状態を持たせる
* ボス固有コードが敵弾インスタンスを個別に握り続ける

---

## 12. 最終方針

> ボス固有進行は `BossStateMachine` を中心に `Intro` / `Phase` / `Dead` で管理し、フェーズ内 action は `BossActionController` が選択する。下位実行の正規ルートは `IBossAction` を実装する `BossActionBase` 派生クラスとし、各攻撃/行動は原則 1 つの C# スクリプトとして記述する。action は `BossActionContext` を通じて移動、弾発射、アニメ、SE/VFX、召喚、hitbox/hurtbox、無敵、cancel、signal を出力し、`BossBehaviorUpdateResult`、`BossActionFrameState`、`BossActionCommandEvent` で Presentation と Domain サービスへ接続する。
>
> `ConfiguredBossAction`、`BossActionTimelineExecutor`、`BossActionCommand`、`BossActionWindow` は legacy/互換ルートとして扱い、新規の複雑なボス行動の正規表現にはしない。
