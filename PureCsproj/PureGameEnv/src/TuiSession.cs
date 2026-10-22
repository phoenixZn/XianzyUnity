using System;
using System.Collections.Generic;
using Terminal.Gui;
using Xease;

namespace PureGameEnv
{
    /// <summary>
    /// 交互宿主。单线程里用 Terminal.Gui 超时泵逻辑，战场、日志、指令分行显示。
    /// </summary>
    internal sealed class TuiSession
    {
        // 界面刷新间隔。逻辑步长仍由 TimeService_Console 决定
        const int FrameMs = 50;
        // 日志区保留的最近行数上限
        const int MaxLogLines = 400;

        readonly Queue<string> _logs = new Queue<string>();
        bool _logDirty = true;
        bool _started;
        GameEntry _entry;
        TextView _info;
        TextView _canvas;
        TextView _logView;
        ScrollBarView _logScroll; // 日志区竖向滑块，位置与 TopRow 同步
        bool _stickToLogEnd = true; // 视口停在末尾时，新日志继续滚到底
        TextField _command;

        /// <summary>在 GameEntryInit 之前把日志收进环形缓冲。</summary>
        public void Install()
        {
            ConsoleHostBridge.IsInputRedirected = false;
            ConsoleHostBridge.BindWrite(AppendLog);
            AppendLog("PureGameEnv tui");
        }

        /// <summary>进入备用屏幕并泵帧，直到 quit、Esc、Ctrl+Q 或 Ctrl+C。</summary>
        public void Run(GameEntry entry)
        {
            _entry = entry;
            Application.Init();
            _started = true;
            try
            {
                Build();
                Console.CancelKeyPress += OnCancel;
                Application.RootKeyEvent = OnRootKey;
                Application.MainLoop.AddTimeout(TimeSpan.FromMilliseconds(FrameMs), OnTick);
                Application.Run();
            }
            finally
            {
                if (_started)
                {
                    Application.Shutdown();
                    _started = false;
                }
            }
        }

        // 覆盖库默认蓝底；焦点只加亮前景
        static ColorScheme BlackBackgroundScheme()
        {
            var text = new Terminal.Gui.Attribute(Color.White, Color.Black);
            var focus = new Terminal.Gui.Attribute(Color.BrightCyan, Color.Black);
            var hot = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black);
            var disabled = new Terminal.Gui.Attribute(Color.DarkGray, Color.Black);
            return new ColorScheme
            {
                Normal = text,
                Focus = focus,
                HotNormal = hot,
                HotFocus = hot,
                Disabled = disabled
            };
        }

        static void UseScheme(ColorScheme scheme, params View[] views)
        {
            for (var i = 0; i < views.Length; i++)
                views[i].ColorScheme = scheme;
        }

        void Build()
        {
            var top = Application.Top;
            var scheme = BlackBackgroundScheme();
            Colors.Base = scheme;
            Colors.TopLevel = scheme;

            var battle = new FrameView("Battle")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(68),
                Height = Dim.Fill(1),
                CanFocus = false
            };
            _info = new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = 3,
                ReadOnly = true,
                CanFocus = false,
                WordWrap = false
            };
            _canvas = new TextView
            {
                X = 0,
                Y = 3,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                CanFocus = false,
                WordWrap = false
            };
            battle.Add(_info);
            battle.Add(_canvas);

            var logFrame = new FrameView("Log")
            {
                X = Pos.Percent(68),
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1)
            };
            // 滑块在宿主不可聚焦时会直接丢掉鼠标；只读，避免把按键写进日志
            _logView = new TextView
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(),
                ReadOnly = true,
                CanFocus = true,
                WordWrap = true
            };
            logFrame.Add(_logView);
            // 只挂竖向滑块；内容没超出可视高度时自动收起
            _logScroll = new ScrollBarView(_logView, true, false)
            {
                AutoHideScrollBars = true,
                KeepContentAlwaysInViewport = true,
                CanFocus = false
            };
            _logScroll.ChangedPosition += OnLogScrollChanged;
            _logView.DrawContent += OnLogDrawContent;

            var prompt = new Label("cmd>")
            {
                X = 0,
                Y = Pos.AnchorEnd(1),
                Width = 5,
                CanFocus = false
            };
            _command = new TextField("")
            {
                X = 5,
                Y = Pos.AnchorEnd(1),
                Width = Dim.Fill()
            };
            _command.KeyPress += OnCommandKey;

            top.Add(battle);
            top.Add(logFrame);
            top.Add(prompt);
            top.Add(_command);
            UseScheme(scheme, top, battle, _info, _canvas, logFrame, _logView, _logScroll, prompt, _command);

            RefreshBattle();
            FlushLog();
        }

        // 超时回调在 UI 线程；返回 true 才会继续预约下一拍
        bool OnTick(MainLoop _)
        {
            // 滑块拖动时鼠标被抓住，不能把焦点抢回指令行
            if (Application.MouseGrabView == null && !_command.HasFocus)
                _command.SetFocus();

            GameSession.Tick(_entry);
            RefreshBattle();
            FlushLog();
            if (!GameSession.QuitRequested)
                return true;

            Application.RequestStop();
            return false;
        }

        void OnCommandKey(View.KeyEventEventArgs args)
        {
            if (args.KeyEvent.Key != Key.Enter)
                return;

            var line = _command.Text == null ? "" : _command.Text.ToString();
            _command.Text = "";
            HostCommands.Submit(line);
            args.Handled = true;
        }

        // 空格与退出在控件收到按键之前处理，避免空格写进指令行
        bool OnRootKey(KeyEvent keyEvent)
        {
            var key = keyEvent.Key;
            if (key == Key.Space)
            {
                ConsoleHostBridge.MarkKeyDown(ConsoleKey.Spacebar);
                GameSession.NoteCommand("space");
                return true;
            }

            if (key == Key.Esc || key == (Key.q | Key.CtrlMask))
            {
                GameSession.RequestQuit();
                Application.RequestStop();
                return true;
            }

            return false;
        }

        void OnCancel(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            GameSession.RequestQuit();
            if (_started)
                Application.RequestStop();
        }

        void RefreshBattle()
        {
            if (_canvas == null)
                return;

            var grid = EntityCanvas.Build(out var entityCount);
            var state = GEnv.Inst?.EnvStateMng?.CurrentStateID;
            if (string.IsNullOrEmpty(state))
                state = "-";
            var last = GameSession.LastCommand;
            if (string.IsNullOrEmpty(last))
                last = "-";

            _info.Text = "状态 " + state + "\n实体 " + entityCount + "\n指令 " + last;
            _canvas.Text = grid;
        }

        void AppendLog(string message)
        {
            if (message == null)
                return;

            var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
            var parts = normalized.Split('\n');
            for (var i = 0; i < parts.Length; i++)
            {
                _logs.Enqueue(parts[i]);
                while (_logs.Count > MaxLogLines)
                    _logs.Dequeue();
            }

            _logDirty = true;
        }

        // 滑块拖动写回视口；滚回末尾后恢复跟随
        void OnLogScrollChanged()
        {
            if (_logView == null || _logScroll == null)
                return;

            if (_logView.TopRow != _logScroll.Position)
            {
                _logView.TopRow = _logScroll.Position;
                if (_logView.TopRow != _logScroll.Position)
                    _logScroll.Position = _logView.TopRow;
                _logView.SetNeedsDisplay();
            }

            NoteLogStick();
        }

        // 绘制时把行数和当前位置交给滑块
        void OnLogDrawContent(Rect _)
        {
            if (_logScroll == null || _logView == null)
                return;

            if (_logScroll.Size != _logView.Lines)
                _logScroll.Size = _logView.Lines;
            if (_logScroll.Position != _logView.TopRow)
                _logScroll.Position = _logView.TopRow;
            _logScroll.Refresh();
        }

        void NoteLogStick()
        {
            var height = _logView.Bounds.Height;
            if (height <= 0)
                return;

            var maxTop = _logView.Lines - height;
            if (maxTop < 0)
                maxTop = 0;
            _stickToLogEnd = _logView.TopRow >= maxTop;
        }

        void FlushLog()
        {
            if (!_logDirty || _logView == null)
                return;

            var keepRow = _logView.TopRow;
            var stick = _stickToLogEnd;
            _logView.Text = string.Join("\n", _logs);
            if (stick)
                _logView.MoveEnd();
            else
                _logView.TopRow = keepRow;

            if (_logScroll != null)
            {
                _logScroll.Size = _logView.Lines;
                _logScroll.Position = _logView.TopRow;
                _logScroll.Refresh();
            }

            _logDirty = false;
        }
    }
}
