using System;
using System.Numerics;

namespace Game.Domain.Battle
{
    public class PlayerMoveManager
    {
        private Player player;

        // --- 状態管理 ---
        // 現在いるグリッド座標
        private Vector2 _logicalPosition;
        // 次に向かっているグリッド座標
        private Vector2 _targetPosition;

        // 現在の移動進行度 (0.0f ～ 1.0f)
        private float _moveProgress = 0f;

        // 現在の進行方向
        private Vector2 _currentDirection = Vector2.Zero;
        // 次に曲がりたい方向（先行入力バッファ）
        private Vector2 _nextInputDirection = Vector2.Zero;

        // ダッシュ先行入力
        private bool _isDashInputBuffered = false;

        // ダッシュ終了後の減速中の現在速度 (walkSpeed以下なら減速完了)
        private float _postDashSpeed;

        // 外部公開用の描画座標
        public Vector3 DisplayPosition { get; private set; }

        // コーナーリング中の位置補正ベクトル
        private Vector3 _cornerOffset = Vector3.Zero;

        // 90度ターンを許可する進行度のしきい値
        private const float TURN_THRESHOLD = 0.8f;
        // ターン時に元の移動方向に水平なベクトルにかける倍率
        private const float TURN_SMOOTHNESS = 0.5f;
        public PlayerMoveManager(Player player)
        {
            this.player = player;

            // 初期位置の設定
            _logicalPosition = player.StaticParams.InitialPosition; // 仮位置。本来はStageSettings等から取得
            _targetPosition = _logicalPosition;
            DisplayPosition = new Vector3(_logicalPosition.X, _logicalPosition.Y, 0);

            _postDashSpeed = player.StaticParams.WalkSpeed;

            // Playerへ初期位置を反映
            player.Position = DisplayPosition;
        }

        /// <summary>
        /// プレイヤーからの方向入力を受け付ける
        /// </summary>
        public void SetInputDirection(Vector2 inputDir)
        {
            // ゼロベクトル以外ならバッファに保存
            if (inputDir != Vector2.Zero)
            {
                _nextInputDirection = inputDir;
            }
        }

        /// <summary>
        /// ダッシュ入力を受け付ける
        /// </summary>
        public void SetInputDash()
        {
            _isDashInputBuffered = true;
        }

        /// <summary>
        /// 毎フレーム呼び出す更新処理
        /// </summary>
        public void Update(Robot robot, float deltaTime)
        {
            // ダッシュクールダウンの更新
            if (player.DashCooldownRemaining > 0)
            {
                player.DashCooldownRemaining -= deltaTime;
                if (player.DashCooldownRemaining < 0) player.DashCooldownRemaining = 0;
            }

            // ダッシュ状態の更新（持続時間）
            if (player.IsDashing)
            {
                player.DashTimeRemaining -= deltaTime;
                if (player.DashTimeRemaining <= 0)
                {
                    EndDash();
                }
            }
            else
            {
                // ダッシュ開始判定（入力があり、かつクールダウン中でない場合）
                if (_isDashInputBuffered && player.DashCooldownRemaining <= 0)
                {
                    StartDash();
                    _isDashInputBuffered = false; // 入力を消費
                }
            }

            // --- 速度決定 ---
            float currentSpeed;
            if (player.IsDashing)
            {
                currentSpeed = player.StaticParams.DashSpeed;
            }
            else if (_postDashSpeed > player.StaticParams.WalkSpeed)
            {
                // ダッシュ終了後の減速中: dashDeceleration/秒で walkSpeed に向かって減速
                _postDashSpeed -= player.StaticParams.DashDeceleration * deltaTime;
                if (_postDashSpeed < player.StaticParams.WalkSpeed)
                    _postDashSpeed = player.StaticParams.WalkSpeed;
                currentSpeed = _postDashSpeed;
            }
            else
            {
                currentSpeed = player.StaticParams.WalkSpeed;
            }

            // --- 移動処理 ---

            // 移動中かどうかの判定
            bool isMoving = _logicalPosition != _targetPosition;

            if (isMoving)
            {
                // 先行入力がある場合、方向転換の判定を行う
                if (_nextInputDirection != Vector2.Zero)
                {
                    // Uターン判定 (逆方向)
                    if (_nextInputDirection.X == -_currentDirection.X &&
                        _nextInputDirection.Y == -_currentDirection.Y)
                    {
                        // 進行方向を反転：ターゲットと現在地（論理）を入れ替える
                        var temp = _logicalPosition;
                        _logicalPosition = _targetPosition;
                        _targetPosition = temp;

                        // 進行度を反転
                        _moveProgress = 1.0f - _moveProgress;

                        // Offset計算: 逆向きに進むことになるので、Offsetも現在の進行度に合わせて再計算すべきだが、
                        // シンプルにUターンの場合はOffsetをリセットして直線移動に戻す、あるいは既存のOffsetを維持する。
                        // ここでは、視覚的なズレを防ぐため、Offsetを今の位置に合わせて再設定するアプローチをとる。
                        Vector3 newStartPos = new Vector3(_logicalPosition.X, _logicalPosition.Y, 0);
                        Vector3 newEndPos = new Vector3(_targetPosition.X, _targetPosition.Y, 0);
                        Vector3 expectedPos = Vector3.Lerp(newStartPos, newEndPos, _moveProgress);
                        _cornerOffset = DisplayPosition - expectedPos;

                        _currentDirection = _nextInputDirection;
                    }
                    // 90度ターン判定 (直交方向)
                    // 内積が0なら直交（成分が0,1,-1の組み合わせなので判定可能）
                    else if (_nextInputDirection.X * _currentDirection.X + _nextInputDirection.Y * _currentDirection.Y == 0)
                    {
                        HandleCornerTurn(robot);
                    }
                }

                // 進行度を進める
                _moveProgress += currentSpeed * deltaTime;

                // 目的地に到達したか
                if (_moveProgress >= 1.0f)
                {
                    // 到着処理：論理座標を更新
                    _logicalPosition = _targetPosition;
                    _moveProgress = 0f;
                    _cornerOffset = Vector3.Zero; // 到着したら補正は終了

                    // 到着時にダッシュ入力があれば即反映できるようトライ
                    if (!player.IsDashing && _isDashInputBuffered && player.DashCooldownRemaining <= 0)
                    {
                        StartDash();
                        _isDashInputBuffered = false;
                    }

                    // 次の移動を開始するか判定
                    TryStartNextMove(robot);
                }
            }
            else
            {
                // 停止中なら即座に移動判定
                TryStartNextMove(robot);
            }

            // 描画用座標の計算 (Linear Interpolation) with Corner Offset
            // Offsetは進行度に応じて減衰させる (1.0 - _moveProgress)
            Vector3 basePosition = Vector3.Lerp(
                new Vector3(_logicalPosition.X, _logicalPosition.Y, 0),
                new Vector3(_targetPosition.X, _targetPosition.Y, 0),
                _moveProgress
            );

            // 進行度が1を超えることもあるため、clampしておく (描画計算用)
            float dampFactor = Math.Max(0f, 1.0f - _moveProgress);
            DisplayPosition = basePosition + _cornerOffset * dampFactor;

            // Playerエンティティへ反映
            player.Position = DisplayPosition;

            // ダッシュ入力バッファはフレーム跨ぎすぎないようリセットするか、
            // あるいは一定時間保持する実装もあるが、ここではシンプルに入力を消費しなかった場合
            // 次のグリッド到達まで保持される仕様とする（_isDashInputBufferedの自動クリアはしない）
            // ただし無限に保持されるのを防ぐならここで false にする設計もアリ。
            // 今回は操作性を考慮し「次の移動開始タイミング」まで保持させる。
        }

        /// <summary>
        /// 移動中の90度ターン処理
        /// </summary>
        private void HandleCornerTurn(Robot robot)
        {
            // Case A: 進行度が一定以上 -> 次のマスへ到達したとみなして曲がる
            if (_moveProgress > TURN_THRESHOLD)
            {
                // 次のターゲット候補を計算 (今のターゲット + 入力方向)
                // 例: (1,0)に向かっている最中に上(0,-1)入力 -> (1,-1)へ行きたい
                Vector2 nextOfTarget = new Vector2(_targetPosition.X + _nextInputDirection.X, _targetPosition.Y + _nextInputDirection.Y);

                // 壁判定: 「今のターゲット位置」から「入力方向」へ行けるか？
                if (CanMove(robot, _targetPosition, _nextInputDirection))
                {
                    // 現在の表示位置を維持するためのOffset計算
                    Vector3 newStartVec = new Vector3(_targetPosition.X, _targetPosition.Y, 0);
                    Vector3 rawOffset = DisplayPosition - newStartVec;

                    // 元の移動方向に水平なベクトルはTURN_SMOOTHNESS倍する
                    Vector3 originalDirVec = new Vector3(_currentDirection.X, _currentDirection.Y, 0);
                    float projectedLength = Vector3.Dot(rawOffset, originalDirVec);
                    Vector3 parallelComponent = originalDirVec * projectedLength;
                    Vector3 perpendicularComponent = rawOffset - parallelComponent;

                    _cornerOffset = perpendicularComponent + parallelComponent * TURN_SMOOTHNESS;

                    // 状態更新
                    _logicalPosition = _targetPosition; // 論理的には次のマスへ到達済とする
                    _targetPosition = nextOfTarget;
                    _currentDirection = _nextInputDirection;
                    _moveProgress = 0f; // 新しい移動の開始
                }
            }
            // Case B: 進行度が一定未満 -> 元のマスに戻って曲がる
            else if (_moveProgress < (1.0f - TURN_THRESHOLD))
            {
                // 次のターゲット候補を計算 (今の論理位置 + 入力方向)
                // 例: (0,0)から出た直後に上(0,-1)入力 -> (0,-1)へ行きたい
                Vector2 nextOfStart = new Vector2(_logicalPosition.X + _nextInputDirection.X, _logicalPosition.Y + _nextInputDirection.Y);

                // 壁判定: 「今の論理位置」から「入力方向」へ行けるか？
                if (CanMove(robot, _logicalPosition, _nextInputDirection))
                {
                    // 現在の表示位置を維持するためのOffset計算
                    Vector3 newStartVec = new Vector3(_logicalPosition.X, _logicalPosition.Y, 0);
                    Vector3 rawOffset = DisplayPosition - newStartVec;

                    // 元の移動方向に水平なベクトルはTURN_SMOOTHNESS倍する
                    Vector3 originalDirVec = new Vector3(_currentDirection.X, _currentDirection.Y, 0);
                    float projectedLength = Vector3.Dot(rawOffset, originalDirVec);
                    Vector3 parallelComponent = originalDirVec * projectedLength;
                    Vector3 perpendicularComponent = rawOffset - parallelComponent;

                    _cornerOffset = perpendicularComponent + parallelComponent * TURN_SMOOTHNESS;

                    // 状態更新
                    // _logicalPosition はそのまま
                    _targetPosition = nextOfStart;
                    _currentDirection = _nextInputDirection;
                    _moveProgress = 0f; // 新しい移動の開始
                }
            }

            // ダッシュ入力バッファはフレーム跨ぎすぎないようリセットするか、
            // あるいは一定時間保持する実装もあるが、ここではシンプルに入力を消費しなかった場合
            // 次のグリッド到達まで保持される仕様とする（_isDashInputBufferedの自動クリアはしない）
            // ただし無限に保持されるのを防ぐならここで false にする設計もアリ。
            // 今回は操作性を考慮し「次の移動開始タイミング」まで保持させる。
        }

        private void StartDash()
        {
            player.IsDashing = true;
            player.DashTimeRemaining = player.StaticParams.DashDuration;
            player.DashCooldownRemaining = player.StaticParams.DashCooldown;
            _postDashSpeed = player.StaticParams.DashSpeed;
        }

        private void EndDash()
        {
            player.IsDashing = false;
            // _postDashSpeed は現在 dashSpeed のまま残っているので、
            // 次フレーム以降の速度決定で dashDeceleration により walkSpeed まで減速される
        }

        /// <summary>
        /// 次の移動が可能か判定し、開始する
        /// </summary>
        private void TryStartNextMove(Robot robot)
        {
            // 1. 先行入力（曲がりたい方向）へ行けるか？
            if (_nextInputDirection != Vector2.Zero && CanMove(robot, _logicalPosition, _nextInputDirection))
            {
                _currentDirection = _nextInputDirection;
                StartMove();
                return;
            }

            // 2. 現在の進行方向（直進）へ行けるか？
            if (_currentDirection != Vector2.Zero && CanMove(robot, _logicalPosition, _currentDirection))
            {
                // 方向はそのまま
                StartMove();
                return;
            }

            // どちらも無理なら停止
            // (次の入力が来るまで _logicalPosition で待機)
            // _currentDirection = Vector2.Zero; // ここでゼロにすると「壁押し」の挙動が消えるので好みで調整
        }

        private void StartMove()
        {
            _targetPosition = new Vector2(_logicalPosition.X + _currentDirection.X, _logicalPosition.Y + _currentDirection.Y);
            _moveProgress = 0f;
        }

        /// <summary>
        /// StageMapのインスタンスを参照して「壁判定」を行う
        /// </summary>
        private bool CanMove(Robot robot, Vector2 currentPos, Vector2 dir)
        {
            if (robot == null)
            {
                throw new ArgumentNullException(nameof(robot));
            }

            var stagemap = robot.StageMap ?? throw new InvalidOperationException("Robot.StageMap is not initialized.");
            int x = (int)currentPos.X;
            int y = (int)currentPos.Y;

            // --- 右移動 (+1, 0) ---
            if (dir.X == 1 && dir.Y == 0)
            {
                // マップ端チェック
                if (x >= stagemap.Width - 1) return false;
                // 壁チェック: VerticalWalls[x, y] が true なら壁あり
                return !stagemap.VerticalWalls[x, y];
            }

            // --- 左移動 (-1, 0) ---
            if (dir.X == -1 && dir.Y == 0)
            {
                if (x <= 0) return false;
                // 左に行くときは、自分の「左隣の壁」＝ (x-1, y) のVerticalWallを見る
                return !stagemap.VerticalWalls[x - 1, y];
            }

            // 下移動 (0, 1)
            if (dir.X == 0 && dir.Y == 1)
            {
                if (y >= stagemap.Height - 1) return false;
                return !stagemap.HorizontalWalls[x, y];
            }

            // 上移動 (0, -1)
            if (dir.X == 0 && dir.Y == -1)
            {
                if (y <= 0) return false;
                // 上に行くときは、自分の「上の壁」＝ (x, y-1) のHorizontalWallを見る
                return !stagemap.HorizontalWalls[x, y - 1];
            }

            return false;
        }
    }
}
