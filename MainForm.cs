using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SS4SS {
  public partial class MainForm : Form {
    private const string SS4Title = "SS4 Stats";
    private const string SSSMTitle = "SSSM Stats";

    private static readonly Color SS4Colour = Color.FromArgb(0xFF, 0xAA, 0x00);
    private static readonly Color SSSMColour = Color.FromArgb(0x02, 0xAC, 0xFF);

    private static readonly int[] UPDATE_OPTIONS = new int[] {
      1, 5, 10, 17, 50, 100, 500, 1000
    };
    private int updateMS = UPDATE_OPTIONS[2];
    private readonly Timer updateTimer;

    private List<Label> colourUpdatingLabels = new List<Label>();

    public MainForm() {
      InitializeComponent();

      // This needs to be in the back in the visual editor, but at the front during runtime
      Controls.SetChildIndex(notHookedLabel, 0);

      FontFamily fira = FontManager.Load(Properties.Resources.FiraSans_Medium);
      foreach (Label l in statTable.Controls) {
        l.Font = new Font(fira, l.Font.Size, l.Font.Style);
        if (l.ForeColor != Color.White) {
          colourUpdatingLabels.Add(l);
        }
      }
      UpdateFontSize();

      ContextMenuStrip = new ContextMenuStrip();
      ToolStripMenuItem pollingDropDown = new ToolStripMenuItem("Polling Rate (ms)");
      foreach (int option in UPDATE_OPTIONS) {
        ToolStripMenuItem sub = new ToolStripMenuItem(option.ToString()) {
          CheckOnClick = true,
          Checked = option == updateMS
        };
        sub.Click += new EventHandler(ChangeUpdateRate);
        pollingDropDown.DropDownItems.Add(sub);
      }
      ContextMenuStrip.Items.Add(pollingDropDown);

      updateTimer = new Timer() {
        Interval = updateMS
      };
      updateTimer.Tick += new EventHandler(Update);
      updateTimer.Start();
    }

    private const string LONGEST_LEFT = "DIFFICULTY";
    private const string LONGEST_RIGHT = "234567 /\u00a0234567";
    private const float RIGHT_SIZE_MULT = 1.2f;
    private void UpdateFontSize() {
      Control ctrl = statTable.GetControlFromPosition(0, 0);
      int maxW = statTable.GetColumnWidths()[0] + statTable.GetColumnWidths()[1];
      int maxH = statTable.GetRowHeights()[0];
      maxW -= 2 * (ctrl.Margin.Left + ctrl.Margin.Right) + 6;
      maxH -= ctrl.Margin.Top + ctrl.Margin.Bottom;

      // Binary search for something that fits
      float minSize = 0;
      float maxSize = 256;
      using (Graphics g = CreateGraphics()) {
        while (maxSize - minSize > 0.01) {
          float avg = (minSize + maxSize) / 2;
          SizeF left = g.MeasureString(LONGEST_LEFT, new Font(ctrl.Font.FontFamily, avg));
          SizeF right = g.MeasureString(LONGEST_RIGHT, new Font(ctrl.Font.FontFamily, avg * RIGHT_SIZE_MULT));
          // To help make sure it doesn't add a linebreak
          SizeF limitedRight = g.MeasureString(LONGEST_RIGHT, new Font(ctrl.Font.FontFamily, avg * RIGHT_SIZE_MULT), (int) (maxW - left.Width - 8.25));

          if ((left.Width + right.Width) > maxW || left.Height > maxH || right.Height > maxH || limitedRight.Height > maxH) {
            maxSize = avg;
          } else {
            minSize = avg;
          }
        }
      }

      Font leftFont = new Font(ctrl.Font.FontFamily, (minSize + maxSize) / 2);
      Font rightFont = new Font(ctrl.Font.FontFamily, RIGHT_SIZE_MULT * (minSize + maxSize) / 2);
      foreach (Label l in statTable.Controls) {
        l.Font = statTable.GetColumn(l) == 0 ? leftFont : rightFont;
      }
      notHookedLabel.Font = rightFont;
    }

    private void ChangeUpdateRate(object sender, EventArgs args) {
      ToolStripMenuItem dropDown = (ToolStripMenuItem) ContextMenuStrip.Items[0];
      for (int i = 0; i < dropDown.DropDownItems.Count; i++) {
        ToolStripMenuItem item = (ToolStripMenuItem) dropDown.DropDownItems[i];
        if (item == sender) {
          updateMS = UPDATE_OPTIONS[i];
        } else {
          item.Checked = false;
        }
      }

      updateTimer.Stop();
      updateTimer.Interval = updateMS;
      updateTimer.Start();
    }

    private void Update(object sender, EventArgs args) {
      if (!GameHook.IsHooked) {
        if (GameHook.TryHook()) {
          notHookedLabel.Visible = false;
          colourUpdatingLabels.ForEach(l => l.ForeColor = GameHook.IsSS4 ? SS4Colour : SSSMColour);
          Text = GameHook.IsSS4 ? SS4Title : SSSMTitle;
        } else {
          notHookedLabel.Visible = true;
          return;
        }
      }

      PlayerStats stats = default;
      try {
        stats = GameHook.Stats;
      } catch (Win32Exception) { }

      scoreValue.Text = stats.Score.ToString();
      killsValue.Text = $"{stats.Kills} /\u00a0{stats.MaxKills}";
      secretsValue.Text = $"{stats.Secrets} /\u00a0{stats.MaxSecrets}";
      savesValue.Text = $"{stats.Saves} /\u00a0{stats.MaxSaves}";
      deathsValue.Text = stats.Deaths.ToString();
      timeValue.Text = TimeSpan.FromSeconds(stats.IgtSeconds).ToString(@"hh\:mm\:ss");

      Difficulty d = stats.GameDifficulty;
      if (d == Difficulty.None) {
        difficultyValue.Text = "N/A";
      } else if (Difficulty.Tourist <= d && d <= Difficulty.Serious) {
        difficultyValue.Text = d.ToString().ToUpperInvariant();
      } else {
        difficultyValue.Text = "UNKNOWN";
      }

      int cheatIdx = statTable.GetRow(cheatedLabel);
      if (stats.HasCheated) {
        statTable.RowStyles[cheatIdx].SizeType = SizeType.Percent;
        statTable.RowStyles[cheatIdx].Height = statTable.RowStyles[0].Height;
      } else {
        statTable.RowStyles[cheatIdx].SizeType = SizeType.Absolute;
        statTable.RowStyles[cheatIdx].Height = 0;
      }
    }

    private void MainForm_Resize(object sender, EventArgs e) => UpdateFontSize();
  }
}
