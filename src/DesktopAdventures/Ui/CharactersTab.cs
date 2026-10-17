using DesktopAdventures.Characters;
using DesktopAdventures.Engine;
using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>Reiter "Charaktere": links Übersicht aller Charaktere (mit Blättern), rechts Details zum gewählten.</summary>
public sealed class CharactersTab : BookTab
{
    private const int Columns = 4, Rows = 3, CardW = 38, CardH = 44, Gap = 5;
    private const int PerPage = Columns * Rows;
    private const int PreviewW = 64, PreviewH = 70, PreviewGround = 6, PreviewScale = 2;

    private readonly IReadOnlyList<CharacterProfile> _profiles;
    private readonly GameState _state;
    private readonly Random _random = new();

    private int _selected;
    private int _page;
    private ActionChoice? _previewAction;
    private double _previewStart, _previewEnd;

    public CharactersTab(IReadOnlyList<CharacterProfile> profiles, GameState state)
    {
        _profiles = profiles;
        _state = state;
    }

    public override string Title => "Charaktere";
    public override uint Color => 0xFFF0D9A8;
    public override PixelIcon Icon => PixelIcon.Person;

    public override void OnOpened() => ResetPreview();

    public override void Draw(UiContext ui, Rect left, Rect right)
    {
        DrawOverview(ui, left);
        DrawDetails(ui, right);
    }

    // ---------- Linke Seite: Übersicht ----------

    private void DrawOverview(UiContext ui, Rect page)
    {
        var b = ui.Buffer;
        var font = ui.Font;
        var (x0, w) = ContentSpan(page, rightSide: false);
        int y = page.Y + 10;

        font.Draw(b, "Charaktere", x0, y, Ink);
        font.DrawRight(b, $"{_state.UnlockedCount}/{_profiles.Count}", x0 + w, y, InkSoft);
        y += font.LineHeight + 1;
        b.FillRect(x0, y, w, 1, InkFaint);
        y += 7;

        int gridW = Columns * CardW + (Columns - 1) * Gap;
        int gridX = x0 + (w - gridW) / 2;
        int first = _page * PerPage;

        for (int i = 0; i < PerPage && first + i < _profiles.Count; i++)
        {
            int index = first + i;
            var rect = new Rect(gridX + i % Columns * (CardW + Gap), y + i / Columns * (CardH + Gap), CardW, CardH);
            DrawCard(ui, rect, _profiles[index], index == _selected, () => Select(index));
        }

        DrawPager(ui, page, x0, w);
    }

    private void DrawCard(UiContext ui, Rect rect, CharacterProfile profile, bool selected, Action onClick)
    {
        var b = ui.Buffer;
        bool hover = ui.Button(rect, onClick);
        bool unlocked = _state.IsUnlocked(profile);

        b.FillRect(rect.X, rect.Y, rect.W, rect.H, CardFill);
        b.StrokeRect(rect.X, rect.Y, rect.W, rect.H, selected || hover ? CardBorderHover : CardBorder);
        if (selected) b.StrokeRect(rect.X + 1, rect.Y + 1, rect.W - 2, rect.H - 2, CardBorderHover);

        var sheet = profile.Sheet;
        var idle = sheet.Get("idle");
        int frame = hover ? (int)(ui.Time * idle.Fps) % idle.Frames : 0;

        b.SetClip(rect.X + 2, rect.Y + 2, rect.W - 4, rect.H - 4);
        b.Draw(sheet.Image, frame * sheet.FrameWidth, idle.Row * sheet.FrameHeight, sheet.FrameWidth, sheet.FrameHeight,
            rect.X + (rect.W - sheet.FrameWidth) / 2, rect.Bottom - sheet.FrameHeight - 3,
            tint: unlocked ? (uint?)null : Silhouette);
        b.ResetClip();
    }

    private void DrawPager(UiContext ui, Rect page, int x0, int w)
    {
        var b = ui.Buffer;
        var font = ui.Font;
        int pages = Math.Max(1, (_profiles.Count + PerPage - 1) / PerPage);
        _page = Math.Clamp(_page, 0, pages - 1);

        int y = page.Bottom - 10 - font.LineHeight;
        string label = $"{_page + 1} / {pages}";
        font.DrawCentered(b, label, x0 + w / 2, y, InkSoft);

        DrawArrow(ui, new Rect(x0 - 2, y, 14, font.LineHeight), left: true, enabled: _page > 0, () => _page--);
        DrawArrow(ui, new Rect(x0 + w - 12, y, 14, font.LineHeight), left: false, enabled: _page < pages - 1, () => _page++);
    }

    private static void DrawArrow(UiContext ui, Rect area, bool left, bool enabled, Action onClick)
    {
        bool hover = enabled && ui.Button(area, onClick);
        uint color = !enabled ? InkFaint : hover ? Cream : Ink;
        int x = area.X + 4, y = area.Y + (area.H - 9) / 2;
        for (int j = 0; j < 9; j++)
        {
            int len = (j <= 4 ? j : 8 - j) + 1;
            ui.Buffer.FillRect(left ? x + 5 - len : x, y + j, len, 1, color);
        }
    }

    // ---------- Rechte Seite: Details ----------

    private void DrawDetails(UiContext ui, Rect page)
    {
        var b = ui.Buffer;
        var font = ui.Font;
        var (x0, w) = ContentSpan(page, rightSide: true);
        int y = page.Y + 10;

        if (_profiles.Count == 0)
        {
            DrawParagraph(ui, "Keine Charaktere gefunden. Siehe docs/CHARAKTERE.md.", x0, y, w, 3);
            return;
        }

        var profile = _profiles[Math.Clamp(_selected, 0, _profiles.Count - 1)];
        bool unlocked = _state.IsUnlocked(profile);

        font.Draw(b, unlocked ? profile.Name : "???", x0, y, Ink);
        string sub = unlocked ? profile.Info.Title : "Gesperrt";
        if (sub.Length > 0) font.DrawRight(b, sub, x0 + w, y, InkSoft);
        y += font.LineHeight + 1;
        b.FillRect(x0, y, w, 1, InkFaint);
        y += 7;

        var preview = new Rect(x0, y, PreviewW, PreviewH);
        DrawPreview(ui, preview, profile, unlocked);

        if (unlocked)
        {
            int fx = preview.Right + 8;
            DrawFacts(ui, profile.Info.Facts, fx, y, x0 + w - fx, PreviewH / font.LineStep);
        }
        y += PreviewH + 8;

        string text = unlocked ? profile.Info.Description : profile.Info.UnlockHint;
        int maxLines = (page.Bottom - 30 - y) / font.LineStep;
        DrawParagraph(ui, text, x0, y, w, maxLines);
    }

    private static void DrawFacts(UiContext ui, IReadOnlyList<string> facts, int x, int y, int width, int maxLines)
    {
        var lines = new List<string>();
        foreach (string fact in facts) lines.AddRange(ui.Font.Wrap(fact, width));
        DrawLines(ui, lines, x, y, width, maxLines, Ink);
    }

    private static void DrawParagraph(UiContext ui, string text, int x, int y, int width, int maxLines) =>
        DrawLines(ui, ui.Font.Wrap(text, width), x, y, width, maxLines, Ink);

    private static void DrawLines(UiContext ui, List<string> lines, int x, int y, int width, int maxLines, uint color)
    {
        if (maxLines <= 0) return;
        if (lines.Count > maxLines)
        {
            lines = lines.Take(maxLines).ToList();
            lines[^1] = ui.Font.Truncate(lines[^1] + "…", width);
        }
        foreach (string line in lines)
        {
            ui.Font.Draw(ui.Buffer, line, x, y, color);
            y += ui.Font.LineStep;
        }
    }

    private void DrawPreview(UiContext ui, Rect box, CharacterProfile profile, bool unlocked)
    {
        var b = ui.Buffer;
        bool canAct = unlocked && profile.OnClick.Count > 0;
        bool hover = canAct && ui.Button(box, () => StartPreviewAction(ui.Time, profile));

        // Kleine Szene: Himmel, Wiese
        b.FillRect(box.X, box.Y, box.W, box.H, Sky);
        b.FillRect(box.X, box.Y + box.H / 2, box.W, box.H / 2, SkyLow);
        int groundTop = box.Bottom - PreviewGround - 1;
        b.FillRect(box.X, groundTop, box.W, PreviewGround + 1, 0xFF5FB23F);
        b.FillRect(box.X, groundTop, box.W, 1, 0xFF9BD65A);
        b.StrokeRect(box.X - 1, box.Y - 1, box.W + 2, box.H + 2, hover ? CardBorderHover : CardBorder);

        var sheet = profile.Sheet;
        var clip = sheet.Get("idle");
        double t = ui.Time;
        if (_previewAction != null && ui.Time < _previewEnd)
        {
            clip = sheet.Get(_previewAction.Animation);
            t = ui.Time - _previewStart;
        }
        int frame = (int)(t * clip.Fps);
        frame = clip.Loop ? frame % clip.Frames : Math.Min(frame, clip.Frames - 1);

        b.SetClip(box.X, box.Y, box.W, box.H);
        b.Draw(sheet.Image, frame * sheet.FrameWidth, clip.Row * sheet.FrameHeight, sheet.FrameWidth, sheet.FrameHeight,
            box.X + (box.W - sheet.FrameWidth * PreviewScale) / 2,
            groundTop + PreviewScale - sheet.FrameHeight * PreviewScale,
            scale: PreviewScale, tint: unlocked ? (uint?)null : Silhouette);
        b.ResetClip();
    }

    // ---------- Aktionen ----------

    private void Select(int index)
    {
        _selected = index;
        ResetPreview();
    }

    private void ResetPreview() => _previewAction = null;

    private void StartPreviewAction(double time, CharacterProfile profile)
    {
        if (profile.OnClick.Count == 0) return;

        double roll = _random.NextDouble() * profile.OnClick.Sum(c => c.Weight);
        var choice = profile.OnClick[^1];
        foreach (var candidate in profile.OnClick)
        {
            roll -= candidate.Weight;
            if (roll > 0) continue;
            choice = candidate;
            break;
        }

        _previewAction = choice;
        _previewStart = time;
        _previewEnd = time + profile.Sheet.Get(choice.Animation).Duration * choice.Repeat;
    }
}
