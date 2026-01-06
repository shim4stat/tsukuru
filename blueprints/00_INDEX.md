# Project Knowledge Index（Unityゲーム開発）

このProjectは「仕様・設計・実装ルール」をChatGPTに渡して、質問や実装相談をブレなく進めるための知識ベースです。

---

## 0. まずここを見る（目的別ショートカット）

### 仕様を確認したい
- 要件: 01_REQUIREMENTS.md
- 画面フロー: 06_UI_SCREEN_FLOW.md
- 状態遷移（BattlePhase / Pause / GameOver）: 05_STATE_FLOW.md
- データ（SaveData / MasterData）: 04_DATA_DESIGN.md

### コードの組み立て方（層/依存/命名）を確認したい
- 02_ARCHITECTURE.md

### クラス責務やドメイン構造を確認したい
- 03_DOMAIN_MODEL.md
- 10_GLOSSARY.md（用語）

### 実装の進め方（順序/タスク分解）
- 07_IMPLEMENTATION_PLAN.md

---

## 1. Project内ファイル
- 00_INDEX.md（このファイル）
- 01_REQUIREMENTS.md（= 要件定義書）
- 02_ARCHITECTURE.md（= アーキテクチャ設計書）
- 03_DOMAIN_MODEL.md（= ドメインモデル設計書）
- 04_DATA_DESIGN.md（= データ設計書）
- 05_STATE_FLOW.md（= 状態遷移・フロー設計書）
- 06_UI_SCREEN_FLOW.md（= UI・画面フロー設計）
- 07_IMPLEMENTATION_PLAN.md（= 実装計画書）
- 10_GLOSSARY.md（=用語集）



## 2. このゲームの超概要（1画面で把握）

### 2.1 シーン
- Boot（任意）
- Title
- Game

### 2.2 画面（例）
- TitleRoot上に StageSelect / Option をモーダル表示
- InGameは HUD / Story / ConversationOverlay / BossTitleOverlay / Pause / GameOver を切替

### 2.3 保存タイミング
- ステージクリア時
- Optionを閉じたとき（即時反映＋保存）

---

## 2. 最重要ルール（実装判断の優先順位）

1) 要件定義と矛盾しない
2) 状態遷移（BattlePhase/Pause/GameOver）に矛盾しない
3) レイヤード依存ルールを破らない（Domainは純C#）
4) “生成はEntryPointで、利用はPresenter/Controller” を守る
5) 保存トリガを勝手に増やさない（必要なら設計書更新）

---
