using System;
using System.Collections.Generic;
using System.IO;
using Terminal.Gui;
using Xease;

namespace PureGameEnv
{
    /// <summary>
    /// 交互宿主。单线程里用 Terminal.Gui 超时泵逻辑，战场、事件、日志、指令分行显示。
    /// </summary>
    internal sealed class TuiSession
    {
        // 界面刷新间隔。逻辑步长仍由 TimeService_Console 决定
        const int FrameMs = 50;
        // 文本区保留的最近行数上限
        const int MaxPaneLines = 400;
        // 上带占内容区的比例，使下方日志带约为战场带的三分之一
        const int UpperBandPercent = 75;

        readonly LinePane _logPane = new LinePane(MaxPaneLines);
        readonly LinePane _eventPane = new LinePane(MaxPaneLines);
        bool _started;
        GameEntry _entry;
        TextView _info;
        TextView _canvas;
        TextField _command;

        /// <summary>在 GameEntryInit 之前把日志和事件分收进两个文本区。</summary>
        public void Install()
        {
            ConsoleHostBridge.IsInputRedirected = false;
            ConsoleHostBridge.BindWrite(_logPane.Append);
            ConsoleHostBridge.BindEvent(_eventPane.Append);
            _logPane.Append("PureGameEnv tui");
        }

        /// <summary>进入备用屏幕并泵帧，直到 quit、Esc、Ctrl+Q 或 Ctrl+C。</summary>
        public void Run(GameEntry entry)
        {
            _entry = entry;
            GrowConsoleForLogBand();
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

        // 加高控制台，使战场带保持加高前的行数；失败则维持原高度
        static void GrowConsoleForLogBand()
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                var width = Console.WindowWidth;
                var height = Console.WindowHeight;
                if (width <= 0 || height <= 1)
                    return;

                var body = height - 1;
                var extra = Math.Max(1, body / 3);
                var next = height + extra;
                var largest = Console.LargestWindowHeight;
                if (largest > 0 && next > largest)
                    next = largest;
                if (next <= height)
                    return;

                var bufW = Math.Max(Console.BufferWidth, width);
                var bufH = Math.Max(Console.BufferHeight, next);
                Console.SetBufferSize(bufW, bufH);
                Console.SetWindowSize(width, next);
            }
            catch (IOException)
            {
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (PlatformNotSupportedException)
            {
            }
        }

        // 覆盖库默认蓝底；焦点只加亮前景
        static ColorScheme BlackBackgroundScheme()
        {
            var text = new Terminal.Gui.Attribute(Color.White, Color.Black);
            var focus = new Terminal.Gui.Attribute(Color.BrightCyan, Color.Black);
            var hot = new Terminal.Gui.Attribute(Color.BrightYellow, Color.Black);
            var disabled = new Terminal.Gui.Attribute(Color.Gray, Color.Black); //Color.DarkGray, Color.Black
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

            var body = new View
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Fill(1),
                CanFocus = false
            };
            var upper = new View
            {
                X = 0,
                Y = 0,
                Width = Dim.Fill(),
                Height = Dim.Percent(UpperBandPercent),
                CanFocus = false
            };

            var battle = new FrameView("Battle")
            {
                X = 0,
                Y = 0,
                Width = Dim.Percent(68),
                Height = Dim.Fill(),
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

            var eventFrame = _eventPane.CreateFrame("Event");
            eventFrame.X = Pos.Percent(68);
            eventFrame.Y = 0;
            eventFrame.Width = Dim.Fill();
            eventFrame.Height = Dim.Fill();

            var logFrame = _logPane.CreateFrame("Log");
            logFrame.X = 0;
            logFrame.Y = Pos.Bottom(upper);
            logFrame.Width = Dim.Fill();
            logFrame.Height = Dim.Fill();

            upper.Add(battle);
            upper.Add(eventFrame);
            body.Add(upper);
            body.Add(logFrame);

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

            top.Add(body);
            top.Add(prompt);
            top.Add(_command);
            UseScheme(scheme, top, body, upper, battle, _info, _canvas,
                eventFrame, _eventPane.Text, _eventPane.Scroll,
                logFrame, _logPane.Text, _logPane.Scroll,
                prompt, _command);

            RefreshBattle();
            _logPane.Flush();
            _eventPane.Flush();
        }

        // 超时回调在 UI 线程；返回 true 才会继续预约下一拍
        bool OnTick(MainLoop _)
        {
            // 滑块拖动时鼠标被抓住，不能把焦点抢回指令行
            if (Application.MouseGrabView == null && !_command.HasFocus)
                _command.SetFocus();

            GameSession.Tick(_entry);
            RefreshBattle();
            _logPane.Flush();
            _eventPane.Flush();
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

        /// <summary>
        /// 只读文本带。环形缓冲，滚到底跟随，并带竖向滑块。
        /// </summary>
        sealed class LinePane
        {
            readonly Queue<string> _lines = new Queue<string>();
            readonly int _maxLines; // 保留的最近行数上限
            bool _dirty = true;
            bool _stickToEnd = true; // 视口停在末尾时，新行继续滚到底
            TextView _view;
            ScrollBarView _scroll;

            public LinePane(int maxLines)
            {
                _maxLines = maxLines;
            }

            public TextView Text => _view;

            public ScrollBarView Scroll => _scroll;

            /// <summary>追加文本。按行拆开，并丢掉超出上限的旧行。</summary>
            public void Append(string message)
            {
                if (message == null)
                    return;

                var normalized = message.Replace("\r\n", "\n").Replace('\r', '\n');
                var parts = normalized.Split('\n');
                for (var i = 0; i < parts.Length; i++)
                {
                    _lines.Enqueue(parts[i]);
                    while (_lines.Count > _maxLines)
                        _lines.Dequeue();
                }

                _dirty = true;
            }

            /// <summary>建好带标题的框、只读文本和竖向滑块。位置由调用方设置。</summary>
            public FrameView CreateFrame(string title)
            {
                var frame = new FrameView(title);
                // 滑块在宿主不可聚焦时会直接丢掉鼠标；只读，避免把按键写进文本
                _view = new TextView
                {
                    X = 0,
                    Y = 0,
                    Width = Dim.Fill(),
                    Height = Dim.Fill(),
                    ReadOnly = true,
                    CanFocus = true,
                    WordWrap = true
                };
                frame.Add(_view);
                // 只挂竖向滑块；内容没超出可视高度时自动收起
                _scroll = new ScrollBarView(_view, true, false)
                {
                    AutoHideScrollBars = true,
                    KeepContentAlwaysInViewport = true,
                    CanFocus = false
                };
                _scroll.ChangedPosition += OnScrollChanged;
                _view.DrawContent += OnDrawContent;
                return frame;
            }

            /// <summary>缓冲有变化时写进文本区。</summary>
            public void Flush()
            {
                if (!_dirty || _view == null)
                    return;

                var keepRow = _view.TopRow;
                var stick = _stickToEnd;
                _view.Text = string.Join("\n", _lines);
                if (stick)
                    _view.MoveEnd();
                else
                    _view.TopRow = keepRow;

                if (_scroll != null)
                {
                    _scroll.Size = _view.Lines;
                    _scroll.Position = _view.TopRow;
                    _scroll.Refresh();
                }

                _dirty = false;
            }

            // 滑块拖动写回视口；滚回末尾后恢复跟随
            void OnScrollChanged()
            {
                if (_view == null || _scroll == null)
                    return;

                if (_view.TopRow != _scroll.Position)
                {
                    _view.TopRow = _scroll.Position;
                    if (_view.TopRow != _scroll.Position)
                        _scroll.Position = _view.TopRow;
                    _view.SetNeedsDisplay();
                }

                NoteStick();
            }

            // 绘制时把行数和当前位置交给滑块
            void OnDrawContent(Rect _)
            {
                if (_scroll == null || _view == null)
                    return;

                if (_scroll.Size != _view.Lines)
                    _scroll.Size = _view.Lines;
                if (_scroll.Position != _view.TopRow)
                    _scroll.Position = _view.TopRow;
                _scroll.Refresh();
            }

            void NoteStick()
            {
                var height = _view.Bounds.Height;
                if (height <= 0)
                    return;

                var maxTop = _view.Lines - height;
                if (maxTop < 0)
                    maxTop = 0;
                _stickToEnd = _view.TopRow >= maxTop;
            }
        }
    }
}
