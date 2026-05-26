using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Windows.Forms;

namespace Banqi
{
    public partial class Form1 : Form
    {
        // === 系統常數設定 ===
        private const int BOARD_ROWS = 4;
        private const int BOARD_COLS = 8;
        private const int PIECE_SIZE = 70;
        private const int MARGIN = 20;
        private const string BACK_IMAGE_RESOURCE_NAME = "暗";

        // 音效常數
        private const string DROP_SOUND_NAME = "落子";
        private const string DROP_SOUND_FILE = "落子.wav";

        private const string RED_WIN_SOUND_NAME = "紅方獲勝";
        private const string RED_WIN_SOUND_FILE = "紅方獲勝.wav";

        private const string BLUE_WIN_SOUND_NAME = "黑方獲勝";
        private const string BLUE_WIN_SOUND_FILE = "黑方獲勝.wav";

        private const string DRAW_SOUND_NAME = "平手";
        private const string DRAW_SOUND_FILE = "平手.wav";

        // === 遊戲狀態列舉 ===
        public enum PieceType { Pawn = 1, Cannon = 2, Horse = 3, Chariot = 4, Elephant = 5, Advisor = 6, General = 7 }
        public enum PieceColor { Red, Black, None }
        public enum GamePhase { Idle, PieceSelected, ConsecutiveEating }

        // === 棋子類別 ===
        public class Piece
        {
            public PieceType Type { get; set; }
            public PieceColor Color { get; set; }
            public bool IsFaceUp { get; set; }

            public string Name
            {
                get { return GetPieceName(); }
            }

            public string ResourceName
            {
                get { return GetResourceName(); }
            }

            private string GetPieceName()
            {
                if (Color == PieceColor.Red)
                {
                    switch (Type)
                    {
                        case PieceType.General: return "帥";
                        case PieceType.Advisor: return "仕";
                        case PieceType.Elephant: return "相";
                        case PieceType.Chariot: return "俥";
                        case PieceType.Horse: return "傌";
                        case PieceType.Cannon: return "炮";
                        case PieceType.Pawn: return "兵";
                        default: return "";
                    }
                }
                else
                {
                    switch (Type)
                    {
                        case PieceType.General: return "將";
                        case PieceType.Advisor: return "士";
                        case PieceType.Elephant: return "象";
                        case PieceType.Chariot: return "車";
                        case PieceType.Horse: return "馬";
                        case PieceType.Cannon: return "包";
                        case PieceType.Pawn: return "卒";
                        default: return "";
                    }
                }
            }

            private string GetResourceName()
            {
                if (Color == PieceColor.Red)
                {
                    switch (Type)
                    {
                        case PieceType.General: return "帥";
                        case PieceType.Advisor: return "仕";
                        case PieceType.Elephant: return "相";
                        case PieceType.Chariot: return "俥";
                        case PieceType.Horse: return "傌";
                        case PieceType.Cannon: return "炮";
                        case PieceType.Pawn: return "兵";
                        default: return "";
                    }
                }
                else
                {
                    switch (Type)
                    {
                        case PieceType.General: return "將";
                        case PieceType.Advisor: return "士";
                        case PieceType.Elephant: return "象";
                        case PieceType.Chariot: return "車";
                        case PieceType.Horse: return "馬";
                        case PieceType.Cannon: return "包";
                        case PieceType.Pawn: return "卒";
                        default: return "";
                    }
                }
            }
        }

        // === 遊戲變數 ===
        private Button[,] boardButtons = new Button[BOARD_ROWS, BOARD_COLS];
        private Piece[,] boardState = new Piece[BOARD_ROWS, BOARD_COLS];
        private PieceColor player1Color = PieceColor.None;
        private PieceColor currentPlayerColor = PieceColor.None;
        private GamePhase currentPhase = GamePhase.Idle;
        private Point selectedPoint = new Point(-1, -1);
        private Label statusLabel;

        private Button endTurnButton;
        private Button drawButton; // 新增：申請平手按鈕

        private FlowLayoutPanel redDeadPanel;
        private FlowLayoutPanel blackDeadPanel;

        private SoundPlayer actionSoundPlayer;

        private Dictionary<PieceType, int> InitialPieceCounts = new Dictionary<PieceType, int>()
        {
            { PieceType.General, 1 }, { PieceType.Advisor, 2 }, { PieceType.Elephant, 2 },
            { PieceType.Chariot, 2 }, { PieceType.Horse, 2 }, { PieceType.Cannon, 2 }, { PieceType.Pawn, 5 }
        };

        public Form1()
        {
            InitializeComponent();
            InitializeGameUI();
            StartNewGame();
        }

        private void InitializeGameUI()
        {
            this.Text = "暗棋遊戲 - 雙人對戰";
            this.Size = new Size(BOARD_COLS * PIECE_SIZE + MARGIN * 4 + 250, BOARD_ROWS * PIECE_SIZE + MARGIN * 6 + 60);
            this.BackColor = Color.BurlyWood;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.DoubleBuffered = true;
            this.Paint += new PaintEventHandler(Form1_Paint);

            // 預先載入落子音效
            try
            {
                System.IO.Stream soundStream = Properties.Resources.ResourceManager.GetStream(DROP_SOUND_NAME);
                if (soundStream != null)
                {
                    actionSoundPlayer = new SoundPlayer(soundStream);
                }
                else
                {
                    actionSoundPlayer = new SoundPlayer(DROP_SOUND_FILE);
                }
                actionSoundPlayer.LoadAsync();
            }
            catch { }

            for (int r = 0; r < BOARD_ROWS; r++)
            {
                for (int c = 0; c < BOARD_COLS; c++)
                {
                    Button btn = new Button();
                    btn.Width = PIECE_SIZE - 4;
                    btn.Height = PIECE_SIZE - 4;
                    btn.Location = new Point(MARGIN + c * PIECE_SIZE + 2, MARGIN + r * PIECE_SIZE + 2);
                    btn.FlatStyle = FlatStyle.Flat;
                    btn.FlatAppearance.BorderSize = 0;
                    btn.FlatAppearance.MouseDownBackColor = Color.Transparent;
                    btn.FlatAppearance.MouseOverBackColor = Color.Transparent;
                    btn.Cursor = Cursors.Hand;
                    btn.Tag = new Point(r, c);
                    btn.Click += new EventHandler(BoardButton_Click);
                    boardButtons[r, c] = btn;
                    this.Controls.Add(btn);
                }
            }

            statusLabel = new Label();
            statusLabel.Location = new Point(MARGIN, BOARD_ROWS * PIECE_SIZE + MARGIN * 2);
            statusLabel.AutoSize = true;
            statusLabel.MaximumSize = new Size(BOARD_COLS * PIECE_SIZE - 280, 0);
            statusLabel.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            statusLabel.ForeColor = Color.DarkSlateGray;
            this.Controls.Add(statusLabel);

            // 申請平手按鈕 (配置於結束回合按鈕的左側)
            drawButton = new Button();
            drawButton.Text = "申請平手";
            drawButton.Location = new Point(MARGIN + BOARD_COLS * PIECE_SIZE - 250, BOARD_ROWS * PIECE_SIZE + MARGIN * 2);
            drawButton.Size = new Size(120, 45);
            drawButton.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            drawButton.Click += new EventHandler(DrawButton_Click);
            this.Controls.Add(drawButton);

            // 結束回合按鈕
            endTurnButton = new Button();
            endTurnButton.Text = "結束回合";
            endTurnButton.Location = new Point(MARGIN + BOARD_COLS * PIECE_SIZE - 120, BOARD_ROWS * PIECE_SIZE + MARGIN * 2);
            endTurnButton.Size = new Size(120, 45);
            endTurnButton.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            endTurnButton.Enabled = false;
            endTurnButton.Click += new EventHandler(EndTurnButton_Click);
            this.Controls.Add(endTurnButton);

            int rightPanelX = BOARD_COLS * PIECE_SIZE + MARGIN * 3;

            Label redDeadLabel = new Label();
            redDeadLabel.Text = "【紅方陣亡棋子】";
            redDeadLabel.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            redDeadLabel.ForeColor = Color.DarkRed;
            redDeadLabel.AutoSize = true;
            redDeadLabel.Location = new Point(rightPanelX, MARGIN);
            this.Controls.Add(redDeadLabel);

            redDeadPanel = new FlowLayoutPanel();
            redDeadPanel.Location = new Point(rightPanelX, MARGIN + 25);
            redDeadPanel.Size = new Size(220, 110);
            redDeadPanel.AutoScroll = true;
            this.Controls.Add(redDeadPanel);

            Label blackDeadLabel = new Label();
            blackDeadLabel.Text = "【黑方陣亡棋子】";
            blackDeadLabel.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
            blackDeadLabel.ForeColor = Color.Black;
            blackDeadLabel.AutoSize = true;
            blackDeadLabel.Location = new Point(rightPanelX, MARGIN + 145);
            this.Controls.Add(blackDeadLabel);

            blackDeadPanel = new FlowLayoutPanel();
            blackDeadPanel.Location = new Point(rightPanelX, MARGIN + 170);
            blackDeadPanel.Size = new Size(220, 110);
            blackDeadPanel.AutoScroll = true;
            this.Controls.Add(blackDeadPanel);
        }

        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            using (Pen boardPen = new Pen(Color.Black, 2))
            {
                int totalWidth = BOARD_COLS * PIECE_SIZE;
                int totalHeight = BOARD_ROWS * PIECE_SIZE;

                for (int r = 0; r <= BOARD_ROWS; r++)
                {
                    int y = MARGIN + r * PIECE_SIZE;
                    g.DrawLine(boardPen, MARGIN, y, MARGIN + totalWidth, y);
                }

                for (int c = 0; c <= BOARD_COLS; c++)
                {
                    int x = MARGIN + c * PIECE_SIZE;
                    g.DrawLine(boardPen, x, MARGIN, x, MARGIN + totalHeight);
                }
            }
        }

        private void EndTurnButton_Click(object sender, EventArgs e)
        {
            SwitchTurn();
        }

        // 新增：申請平手按鈕的點擊事件
        private void DrawButton_Click(object sender, EventArgs e)
        {
            string currentPlayerName = currentPlayerColor == PieceColor.Red ? "紅方" : (currentPlayerColor == PieceColor.Black ? "黑方" : "目前玩家");

            DialogResult result = MessageBox.Show(
                $"【{currentPlayerName}】提出了平手申請！\n請問對方是否同意平手？",
                "申請平手",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question
            );

            if (result == DialogResult.Yes)
            {
                // 對方同意平手，播放平手音效並重新開始
                PlaySpecificSound(DRAW_SOUND_NAME, DRAW_SOUND_FILE);
                MessageBox.Show("雙方同意平手，這是一場精彩的對局！", "遊戲結束");
                StartNewGame();
            }
            else
            {
                // 對方拒絕平手，遊戲繼續
                MessageBox.Show("對方拒絕了平手申請，戰鬥繼續！", "申請拒絕");
            }
        }

        private void StartNewGame()
        {
            List<Piece> deck = new List<Piece>();
            PieceColor[] colors = new PieceColor[] { PieceColor.Red, PieceColor.Black };

            foreach (PieceColor color in colors)
            {
                foreach (KeyValuePair<PieceType, int> kvp in InitialPieceCounts)
                {
                    for (int i = 0; i < kvp.Value; i++)
                    {
                        deck.Add(new Piece { Type = kvp.Key, Color = color, IsFaceUp = false });
                    }
                }
            }

            Random rnd = new Random();
            deck = deck.OrderBy(x => rnd.Next()).ToList();

            int index = 0;
            for (int r = 0; r < BOARD_ROWS; r++)
            {
                for (int c = 0; c < BOARD_COLS; c++)
                {
                    boardState[r, c] = deck[index++];
                }
            }

            player1Color = PieceColor.None;
            currentPlayerColor = PieceColor.None;
            currentPhase = GamePhase.Idle;
            selectedPoint = new Point(-1, -1);
            endTurnButton.Enabled = false;

            UpdateBoardUI();
            UpdateStatus("遊戲開始！請翻開任意棋子以決定雙方陣營。");
        }

        private void BoardButton_Click(object sender, EventArgs e)
        {
            Button clickedBtn = (Button)sender;
            Point pt = (Point)clickedBtn.Tag;
            Piece clickedPiece = boardState[pt.X, pt.Y];

            if (currentPhase == GamePhase.Idle)
            {
                if (clickedPiece != null && !clickedPiece.IsFaceUp)
                {
                    PlayActionSound();
                    FlipPiece(pt.X, pt.Y);
                    SwitchTurn();
                }
                else if (clickedPiece != null && clickedPiece.IsFaceUp && clickedPiece.Color == currentPlayerColor)
                {
                    selectedPoint = pt;
                    currentPhase = GamePhase.PieceSelected;
                    UpdateBoardUI();
                    UpdateStatus("已選擇【" + clickedPiece.Name + "】，請點擊目標格子移動或吃子。");
                }
            }
            else if (currentPhase == GamePhase.PieceSelected || currentPhase == GamePhase.ConsecutiveEating)
            {
                if (pt == selectedPoint && currentPhase != GamePhase.ConsecutiveEating)
                {
                    selectedPoint = new Point(-1, -1);
                    currentPhase = GamePhase.Idle;
                    UpdateBoardUI();
                    UpdateStatus("已取消選取。");
                    return;
                }

                ProcessAction(selectedPoint, pt);
            }
        }

        private void FlipPiece(int r, int c)
        {
            boardState[r, c].IsFaceUp = true;

            if (player1Color == PieceColor.None)
            {
                player1Color = boardState[r, c].Color;
                currentPlayerColor = player1Color;
            }
            UpdateBoardUI();
        }

        private void ProcessAction(Point from, Point to)
        {
            Piece attacker = boardState[from.X, from.Y];
            Piece target = boardState[to.X, to.Y];

            bool isAdjacent = Math.Abs(from.X - to.X) + Math.Abs(from.Y - to.Y) == 1;
            bool isCannonStrike = false;

            if (attacker.Type == PieceType.Cannon && target != null)
            {
                isCannonStrike = CheckCannonPath(from, to);
            }

            bool isValidAttackPath = false;
            if (attacker.Type == PieceType.Cannon)
            {
                isValidAttackPath = isCannonStrike;
            }
            else
            {
                isValidAttackPath = isAdjacent;
            }

            if (target == null && isAdjacent && currentPhase != GamePhase.ConsecutiveEating)
            {
                boardState[to.X, to.Y] = attacker;
                boardState[from.X, from.Y] = null;
                PlayActionSound();
                SwitchTurn();
            }
            else if (target != null && isValidAttackPath)
            {
                bool isBlindEat = !target.IsFaceUp;

                if (!isBlindEat && target.Color == attacker.Color)
                {
                    UpdateStatus("明吃不能吃同色的子！請重新選擇目標。");
                    return;
                }

                if (isBlindEat) target.IsFaceUp = true;

                bool canEat = CanEat(attacker, target);

                if (canEat)
                {
                    boardState[to.X, to.Y] = attacker;
                    boardState[from.X, from.Y] = null;
                    selectedPoint = to;

                    PlayActionSound();

                    currentPhase = GamePhase.ConsecutiveEating;
                    endTurnButton.Enabled = true;
                    UpdateBoardUI();
                    UpdateStatus("吃子成功！【" + attacker.Name + "】可選擇繼續連吃，或點擊「結束回合」。");
                    CheckWinCondition();
                }
                else if (isBlindEat)
                {
                    UpdateBoardUI();
                    MessageBox.Show("暗吃失敗！被翻開的棋子是【" + target.Name + "】，順位較大，原地不動。");
                    SwitchTurn();
                }
                else
                {
                    UpdateStatus("不可越級吃子，請重新選擇。");
                }
            }
            else
            {
                if (attacker.Type == PieceType.Cannon && isAdjacent)
                {
                    UpdateStatus("炮不能直接吃相鄰的棋子，必須隔一子才能攻擊！");
                }
                else
                {
                    UpdateStatus("不合法的移動或攻擊範圍！");
                }
            }
        }

        private bool CanEat(Piece attacker, Piece target)
        {
            if (attacker.Type == PieceType.Cannon) return true;
            if (attacker.Type == PieceType.Pawn && target.Type == PieceType.General) return true;
            if (attacker.Type == PieceType.General && target.Type == PieceType.Pawn) return false;

            return (int)attacker.Type >= (int)target.Type;
        }

        private bool CheckCannonPath(Point from, Point to)
        {
            if (from.X != to.X && from.Y != to.Y) return false;

            int count = 0;
            if (from.X == to.X)
            {
                int min = Math.Min(from.Y, to.Y);
                int max = Math.Max(from.Y, to.Y);
                for (int y = min + 1; y < max; y++)
                    if (boardState[from.X, y] != null) count++;
            }
            else
            {
                int min = Math.Min(from.X, to.X);
                int max = Math.Max(from.X, to.X);
                for (int x = min + 1; x < max; x++)
                    if (boardState[x, from.Y] != null) count++;
            }
            return count == 1;
        }

        private void SwitchTurn()
        {
            currentPlayerColor = currentPlayerColor == PieceColor.Red ? PieceColor.Black : PieceColor.Red;
            currentPhase = GamePhase.Idle;
            selectedPoint = new Point(-1, -1);
            endTurnButton.Enabled = false;
            UpdateBoardUI();
            UpdateStatus("目前回合：" + (currentPlayerColor == PieceColor.Red ? "紅方" : "黑方"));
        }

        private void UpdateBoardUI()
        {
            System.Resources.ResourceManager resManager = Properties.Resources.ResourceManager;

            for (int r = 0; r < BOARD_ROWS; r++)
            {
                for (int c = 0; c < BOARD_COLS; c++)
                {
                    Button btn = boardButtons[r, c];
                    Piece p = boardState[r, c];

                    btn.BackColor = (selectedPoint == new Point(r, c)) ? Color.Yellow : Color.Transparent;
                    btn.BackgroundImage = null;
                    btn.Text = "";
                    btn.Enabled = true;

                    if (p != null)
                    {
                        if (!p.IsFaceUp)
                        {
                            Image backImg = (Image)resManager.GetObject(BACK_IMAGE_RESOURCE_NAME);
                            if (backImg != null)
                            {
                                btn.BackgroundImage = backImg;
                                btn.BackgroundImageLayout = ImageLayout.Stretch;
                            }
                            else
                            {
                                btn.Text = "暗";
                                btn.BackColor = Color.LightGray;
                                btn.Font = new Font("微軟正黑體", 14, FontStyle.Bold);
                                btn.ForeColor = Color.DimGray;
                            }
                        }
                        else
                        {
                            Image pieceImg = (Image)resManager.GetObject(p.ResourceName);
                            if (pieceImg != null)
                            {
                                btn.BackgroundImage = pieceImg;
                                btn.BackgroundImageLayout = ImageLayout.Stretch;
                            }
                            else
                            {
                                btn.Text = p.Name;
                                btn.Font = new Font("微軟正黑體", 18, FontStyle.Bold);
                                btn.ForeColor = p.Color == PieceColor.Red ? Color.Red : Color.Black;
                            }
                        }
                    }
                }
            }

            UpdateCapturedUI(resManager);
        }

        private void UpdateCapturedUI(System.Resources.ResourceManager resManager)
        {
            redDeadPanel.Controls.Clear();
            blackDeadPanel.Controls.Clear();

            Dictionary<PieceType, int> currentRed = new Dictionary<PieceType, int>();
            Dictionary<PieceType, int> currentBlack = new Dictionary<PieceType, int>();

            foreach (PieceType pt in InitialPieceCounts.Keys)
            {
                currentRed[pt] = 0;
                currentBlack[pt] = 0;
            }

            foreach (Piece p in boardState)
            {
                if (p != null)
                {
                    if (p.Color == PieceColor.Red) currentRed[p.Type]++;
                    else if (p.Color == PieceColor.Black) currentBlack[p.Type]++;
                }
            }

            foreach (KeyValuePair<PieceType, int> kvp in InitialPieceCounts)
            {
                int deadRed = kvp.Value - currentRed[kvp.Key];
                for (int i = 0; i < deadRed; i++)
                {
                    Piece dummy = new Piece { Type = kvp.Key, Color = PieceColor.Red, IsFaceUp = true };
                    AddCapturedPieceIcon(redDeadPanel, dummy, resManager);
                }

                int deadBlack = kvp.Value - currentBlack[kvp.Key];
                for (int i = 0; i < deadBlack; i++)
                {
                    Piece dummy = new Piece { Type = kvp.Key, Color = PieceColor.Black, IsFaceUp = true };
                    AddCapturedPieceIcon(blackDeadPanel, dummy, resManager);
                }
            }
        }

        private void AddCapturedPieceIcon(FlowLayoutPanel panel, Piece p, System.Resources.ResourceManager resManager)
        {
            PictureBox pb = new PictureBox();
            pb.Size = new Size(35, 35);
            pb.SizeMode = PictureBoxSizeMode.StretchImage;
            pb.Margin = new Padding(2);

            Image img = (Image)resManager.GetObject(p.ResourceName);
            if (img != null)
            {
                pb.Image = img;
            }
            else
            {
                Label lbl = new Label();
                lbl.Text = p.Name;
                lbl.Size = new Size(35, 35);
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.Font = new Font("微軟正黑體", 12, FontStyle.Bold);
                lbl.ForeColor = p.Color == PieceColor.Red ? Color.Red : Color.Black;
                lbl.BackColor = Color.LightGray;
                lbl.Margin = new Padding(2);
                panel.Controls.Add(lbl);
                return;
            }
            panel.Controls.Add(pb);
        }

        private void UpdateStatus(string msg)
        {
            string sideInfo = currentPlayerColor == PieceColor.None ? "未定陣營" : (currentPlayerColor == PieceColor.Red ? "紅方" : "黑方");
            statusLabel.Text = "【目前輪到：" + sideInfo + "】\n" + msg;
        }

        private void CheckWinCondition()
        {
            int redCount = 0, blackCount = 0;
            foreach (Piece p in boardState)
            {
                if (p != null)
                {
                    if (p.Color == PieceColor.Red) redCount++;
                    else if (p.Color == PieceColor.Black) blackCount++;
                }
            }

            // 新增：勝負判定與專屬音效播放
            if (redCount == 0)
            {
                PlaySpecificSound(BLUE_WIN_SOUND_NAME, BLUE_WIN_SOUND_FILE);
                MessageBox.Show("黑方將紅方全數吃光！黑方(或藍方)獲勝！", "遊戲結束");
                StartNewGame();
            }
            else if (blackCount == 0)
            {
                PlaySpecificSound(RED_WIN_SOUND_NAME, RED_WIN_SOUND_FILE);
                MessageBox.Show("紅方將黑方全數吃光！紅方獲勝！", "遊戲結束");
                StartNewGame();
            }
        }

        // 一般行動播放的落子音效
        private void PlayActionSound()
        {
            try
            {
                actionSoundPlayer?.Play();
            }
            catch { }
        }

        // 新增：用於播放指定事件（勝利、平手）的單次音效播放器
        private void PlaySpecificSound(string resourceName, string fileName)
        {
            try
            {
                SoundPlayer player;
                System.IO.Stream stream = Properties.Resources.ResourceManager.GetStream(resourceName);
                if (stream != null)
                {
                    player = new SoundPlayer(stream);
                }
                else
                {
                    player = new SoundPlayer(fileName);
                }
                player.Play();
            }
            catch
            {
                // 若找不到音效檔則不報錯，不影響遊戲進行
            }
        }
    }
}