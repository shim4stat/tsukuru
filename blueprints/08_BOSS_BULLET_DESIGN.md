# ボス・弾幕詳細設計書

## 0. 参照ドキュメント

* 要件定義書：ボス戦、複数HPゲージ、ダッシュ吸収、ボス接触ダメージ、ドロップ、攻撃アイコン
* アーキテクチャ設計書：Domain/Presentation 分離、SpawnRequest、コリジョン橋渡し、ObjectPool
* ドメインモデル設計書：`Boss` / `BossActionService` / `EnemySpawnService` / `EnemyBullet`
* データ設計書：`BossStaticParams` / `BossPhaseDefinition` / `EnemyBulletDefinition`
* 実装計画書：BOSS-01〜13、PL-04、PL-05

本書は上記と矛盾しない形で、**ボスPrefab構造** と **弾幕実装の詳細方針** を定める。
既存文書のボス/弾定義は「初期実装の最小構成」として有効なままとし、本書はその先の拡張詳細を補う。

---

## 1. 目的

* ボスを Prefab ベースで再利用可能に実装できるようにする。
* 単一スプライトのボスだけでなく、複数 Prefab を組み合わせたボスにも対応できるようにする。
* スプライトと当たり判定を分離し、調整・拡張しやすくする。
* ステージごとにボスの初期位置や構成を柔軟に切り替えられるようにする。
* 複雑な弾幕を、責務分離された破綻しにくい構造で実装できるようにする。
* 要件にあるダッシュ吸収、ボス接触ダメージ、ドロップ、複数ゲージと自然に接続できるようにする。

---

## 2. 位置付けと整合方針

### 2.1 既存設計との関係

* `Boss` は引き続き Domain 上の真実の状態として扱う。現在ゲージ、現在HP、撃破状態の責務は維持する。
* `BossActionService` は引き続き Combat 中の攻撃進行責務を持つ。本書の `BossPhaseController` / `PatternRunner` は、その内部実装または委譲先の詳細化候補として位置付ける。
* `BossPhaseDefinition` は現行の最小攻撃定義として維持する。本書の `SpellDefinition` は将来の詳細定義であり、直ちに既存定義を置き換える前提にはしない。
* `EnemyBulletSpawnRequest` は引き続き Domain から Presentation への弾生成要求DTOとして用いる。
* `EnemyBullet` は現行の実行時弾状態として有効なままとし、本書の `BulletRuntime` はその詳細化または内部再編成の候補として扱う。

### 2.2 追加用語の扱い

| 本書の用語 | 位置付け | 既存設計との接続 |
| --- | --- | --- |
| `BossRoot` | Unity上のボス親Prefab | Domain の `Boss` を可視化する Presentation ルート |
| `BossBrain` | ボスの高レベル挙動制御 | `BossActionService` と `Boss` 状態を参照して演出・移動・攻撃開始を橋渡し |
| `BossHealth` | Unity側の被弾入口とHP表示同期 | Domain の `Boss.TakeDamage()` への窓口 |
| `BossLayout` | 部位差し込み用レイアウト定義 | `BossStaticParams` を補助する Presentation 向けデータ |
| `SpellDefinition` | 弾幕の詳細定義 | `BossPhaseDefinition` の拡張先。初期段階では片方のみでも可 |
| `BulletManager` | 敵弾の中央管理 | SpawnRequest / Pool / 衝突通知の集約先 |

### 2.3 要件解釈の固定

* ボスは要件通り、基本的にロボット外かつ画面右側に存在する前提で設計する。
* 攻撃アイコンは既存要件の「通常4 + 特殊1」を前提とし、四隅や右中央などの具体的な配置は Stage/Robot レイアウト側の責務とする。
* 本書でいう「右中央の大砲」は、既存要件の**特殊攻撃アイコンから発動される強攻撃シーケンスの一形態**として扱う。別ルールの独立システムにはしない。

---

## 3. レイヤ別責務

### 3.1 Domain

* `Boss`
  * 現在ゲージ、現在HP、撃破判定、フェーズ進行に必要な最小状態を保持する。
* `BossActionService`
  * Combat 中のみ攻撃進行を更新し、`EnemyBulletSpawnRequest` と必要なイベントを返す。
  * 詳細弾幕を入れる場合は `BossPhaseController` / `PatternRunner` を内包または委譲する。
* `EnemySpawnService`
  * ボスフェーズや攻撃中状態に応じた雑魚スポーン要求を返す。
* `DropService` / `ItemSpawnService`
  * ダッシュ吸収、被弾時ドロップ、撃破ドロップ、個数上限を担う。

### 3.2 Presentation

* `BossRoot`, `BossBrain`, `BossHealth`, `BossAssembler`, `BossAnimatorBridge`
  * Prefab構造、見た目、アニメ同期、位置反映、被弾入口の橋渡しを担う。
* `BossSpawnPoint`, `BossSpawner`
  * ステージ上の出現位置、登場演出位置、フェーズ遷移時の再配置位置を定義する。
* `BulletManager`, `BulletPool`
  * 弾Viewの生成・再利用・画面反映・弾消し・衝突通知を中央管理する。
* `BossHurtbox`, `BossAttackHitbox`, `BulletHitbox`
  * Unity の Trigger/Collision を受け、Domain サービスへ橋渡しする。

### 3.3 MasterData / ScriptableObject

* `BossStaticParams`
  * 複数ゲージHP、被弾ドロップ量、フェーズ最小定義など、ゲームルールに必要な静的値を持つ。
* `BossLayout`
  * 部位Prefab、差し込みスロット、ローカル座標、初期有効状態を持つ Presentation 向けレイアウト定義。
* `SpellDefinition`
  * スペル名、ループ位置、タイムラインコマンド列、難易度差分などの弾幕詳細定義。
* `EmitterPreset`, `BulletSpec`
  * 発射器と弾種の再利用設定を持つ。

---

## 4. ボス構造層設計

### 4.1 基本Prefab構成

```text
BossRoot
├─ VisualRoot
├─ Hurtboxes
├─ BodyCollision
├─ PartSlots
├─ EmittersRoot
└─ VFXRoot
```

`BossBrain`, `BossHealth`, `BossAssembler`, `BossAnimatorBridge` は `BossRoot` に付与するコンポーネントとして扱う。

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

* `BodySlot`、`LeftWeaponSlot`、`RightWeaponSlot`、`OrbitSlotA`、`OrbitSlotB`、`TailSlot` などの部位差し込みTransform群。

#### `EmittersRoot`

* 発射器、使い魔、補助ノードの配置親。
* 見た目と発射原点を分離しやすくするため、`VisualRoot` から独立させる。

#### `VFXRoot`

* 被弾、発射、破壊、フェーズ遷移などのVFX親。

### 4.3 複合Prefab対応

* ボスは単一スプライト前提で設計しない。
* 各部位は `BossPart` Prefab として分離し、`BossAssembler` が `BossLayout` に従って `PartSlots` に差し込む。
* `BossPart` は見た目、部位用 Hurtbox、必要なら `EmitterMounts` を持つ。
* 破壊可能部位は Domain では「追加HP/追加状態」、Presentation では「差し替え可能な部位」として扱う。

### 4.4 出現位置とレイアウト

* ボスの初期位置は Prefab に固定しない。
* `BossSpawnPoint` は少なくとも `introAnchor` と `battleAnchor` を持つ。
* 必要に応じて `phaseTransitionAnchor` を持ち、フェーズ遷移演出時の再配置先に使う。
* `BossSpawner` は Stage 開始やイベント開始時に、出現位置・向き・`BossLayout`・使用スペル定義をまとめて割り当てる。

### 4.5 判定分離

* `VisualRoot` は見た目のみ。
* `BossHurtbox` は被弾判定のみ。
* `BossAttackHitbox` は近接攻撃や体当たり攻撃の当たり判定のみ。
* `BodyCollision` は通常接触用。
* `BossAttackHitbox` は常時有効にせず、Animation Event または攻撃シーケンスから明示制御する。

### 4.6 高レベル状態

`BossBrain` の内部状態は次を基本とする。

* `Intro`
* `Idle`
* `Move`
* `SelectAttack`
* `Attacking`
* `Recovery`
* `Stunned`
* `Dead`

補足：

* これは Unity 側の高レベル挙動状態であり、バトル全体の真実は引き続き `BattleContext.phase` に置く。
* 攻撃選択は「候補列挙 → 使用不可除外 → 重み付き選択」を基本とする。
* 判定条件には距離、プレイヤー位置、現在フェーズ、直前攻撃、クールダウン、雑魚数、部位生存状態を使う。

---

## 5. 弾幕システム層設計

### 5.1 基本モデル

弾幕は次の階層で分離する。

* Boss / Phase
* Spell / Attack Pattern
* Emitter / Familiar
* Bullet

ボス本体は「いつ何を使うか」を決め、弾幕層は「どう発射し、どう更新するか」を担う。

### 5.2 `BossPhaseController`

* `Boss` のゲージ、経過時間、部位状態、フェーズ移行条件を見て現在のスペルを選ぶ。
* HPしきい値による切り替えを初期実装の主軸とし、部位破壊や時間条件は段階的に追加する。
* 現行の `BossPhaseDefinition` を使う最小実装と、`SpellDefinition` を使う詳細実装の両方を許容する。

### 5.3 `PatternRunner`

* タイムライン進行器。
* 「何フレーム目または何秒目に何をするか」を順番に実行する。
* 弾1発ごとに賢い判断を持たせず、複雑さは `PatternRunner` 側の時間制御で作る。

想定コマンド例：

* Emitter生成
* Emitter移動
* 発射開始 / 発射停止
* 基準角変更
* 自機狙い補正
* Familiar生成 / 退場
* ループ開始位置ジャンプ
* スペル終了

### 5.4 `SpellDefinition`

* スペル名
* 対応フェーズ条件
* ループ位置
* 終了条件
* 使用する `EmitterPreset`
* タイムラインコマンド列
* 難易度差分

`BossPhaseDefinition` との使い分け：

* 初期段階：`BossPhaseDefinition` のみで最小発射
* 詳細弾幕段階：`BossPhaseDefinition` から `SpellDefinition` を参照する、または `SpellDefinition` へ段階移行

### 5.5 `EmitterController`

* 発射位置
* 基準角
* 発射方向
* 発射方式
* Way数
* 発射間隔
* 回転量
* 自機狙い補正
* 使用弾種

Emitter は発射に専念し、攻撃選択やフェーズ判断は持たない。

### 5.6 `FamiliarController`

* 移動しながら弾幕を撃つ補助ノード。
* ボス本体の弾幕空間を分担し、左右挟み込みや移動発射を実現する。
* Familiar 自体の見た目は Presentation で持ち、発射規則は弾幕層の設定に従う。

### 5.7 `BulletManager` / `BulletRuntime` / `BulletPool`

* `BulletManager`
  * 敵弾の一覧、更新、寿命切れ、画面外処理、吸収、弾消し、Pool 返却を中央管理する。
* `BulletRuntime`
  * 位置、速度、角度、加速度、角速度、寿命、表示情報、判定情報、吸収可否、分裂予約などの最小状態のみを持つ。
* `BulletPool`
  * 弾Viewの再利用を担い、`Instantiate / Destroy` の連打を避ける。

補足：

* 1発ごとの通常移動に Coroutine は使わない。
* `BulletRuntime` の責務は、既存 `EnemyBullet` の詳細化または内部実装への置換候補として扱う。

### 5.8 予約イベント型の弾変化

弾が持つ追加挙動は次に限定する。

* 一定時間後に分裂
* 一定時間後に加速
* 一定時間後に進行方向変更

これらも「弾の意思決定」ではなく、「予約済みイベントの適用」として表現する。

---

## 6. ゲーム固有要素との接続

### 6.1 ダッシュ吸収

敵弾は少なくとも次の値を持つ。

* `canBeAbsorbed`
* `energyValue`
* `absorbEffectId`

ダッシュ中に敵弾へ接触した場合：

* `BulletManager` が対象弾を中央管理リストから外す。
* `ItemSpawnService` または対応する加算処理へ吸収量を通知する。
* 弾Viewは Pool に返却する。

### 6.2 ボス接触ダメージ

* ダッシュ中のプレイヤーが `BodyCollision` に触れた場合、Presentation は Domain の `DamageService` へ「Boss 接触ダメージ」を通知する。
* 近接攻撃判定と通常接触判定は分離し、常時有効な攻撃Hitboxにはしない。

### 6.3 特殊攻撃による弾消し

* 特殊攻撃アイコンに紐づく強攻撃シーケンスは、必要に応じて「大ダメージ + 敵弾一括消去」を持てる。
* 一括消去はボス固有コードから個別弾参照しない。
* `BulletManager` に対する中央命令で実行する。

想定例：

* `ClearEnemyBullets(ClearReason.SpecialAttack)`
* `ClearEnemyBulletsOwnedBy(bossId, ClearReason.BossPhaseEnd)`

### 6.4 ドロップと高リスク・高リターン

* ボス本体は画面右側の圧力源として設計する。
* プレイヤーが右側に踏み込むほど、被弾・接触・高密度弾幕のリスクが高まる。
* その代わり、ボス被弾時ドロップや撃破ドロップは既存 `DropService` と連携して高いリターンに繋げる。
* ドロップ量は既存要件どおり、基礎量と攻撃側のドロップ倍率で算出する。

---

## 7. データとコードの分担

### 7.1 データに寄せるもの

* スペル名
* 発射間隔
* Way数
* 角度オフセット
* ループ位置
* フェーズ条件
* 部位レイアウト
* 弾の見た目、寿命、速度、吸収量

### 7.2 コードに寄せるもの

* 状態遷移
* タイムライン進行
* 条件分岐
* 重み付き攻撃選択
* 弾の一括更新
* 吸収処理
* 一括弾消し
* フェーズ移行トリガー

### 7.3 判断基準

* 数値調整と構成差分はデータで扱う。
* 実行時状態と分岐が絡むものはコードで扱う。
* ScriptableObject に実行時状態を持たせない。

---

## 8. 段階導入方針

### Phase 1：最小ボス基盤

* `BossRoot`
* `BossBrain`
* `BossHealth`
* `BossSpawnPoint`
* `BossSpawner`
* 見た目と Hurtbox の分離

### Phase 2：基本攻撃

* 近接攻撃1種
* 直線弾1種
* 突進1種
* Animation Event による近接同期

### Phase 3：弾幕基盤

* `BulletManager`
* `BulletPool`
* `EmitterController`
* Ring / NWay / Aim の基本発射
* `PatternRunner`

### Phase 4：複合ボス

* `BossPart`
* `BossAssembler`
* `BossLayout`
* 左右砲台、回転ユニット、補助部位

### Phase 5：複雑弾幕

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

* 全敵に共通する完全汎用AIフレームワーク
* ノーコードで全弾幕を記述するDSLの完成
* あらゆる部位関係を自動解決する超汎用アセンブラ
* 物理ベースの複雑な破壊シミュレーション

---

## 10. アンチパターン

* `BossController` 1本に全ロジックを書く
* 弾を 1 発ずつ `Instantiate / Destroy` し続ける
* 弾 1 発ごとに Coroutine を持たせる
* 見た目と被弾判定を完全一致前提で設計する
* 弾幕ロジックを Animator に埋め込みすぎる
* ScriptableObject に実行時状態を持たせる
* ボス固有コードが敵弾インスタンスを個別に握り続ける

---

## 11. 最終方針

> ボスは `BossRoot` を中心とした Prefab 構造で実装し、必要に応じて複数の `BossPart` を組み合わせられるようにする。高レベル制御は既存の `Boss` / `BossActionService` を軸に保ちつつ、Unity 側では `BossBrain` と `BossAssembler` で見た目・部位・アニメを橋渡しする。弾幕は `PatternRunner` + `EmitterController` + `BulletManager` に分離し、弾自体は単純に保ち、複雑さは時間制御と発射規則の合成で表現する。

この方針により、既存設計で定義済みの複数ゲージ、ダッシュ吸収、ボス接触ダメージ、ドロップ、特殊攻撃との整合を保ったまま、将来的な複合ボスと高密度弾幕へ拡張できる。
