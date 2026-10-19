using DesktopAdventures.Challenges;
using DesktopAdventures.Characters;
using DesktopAdventures.Engine;
using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>
/// Reiter "Charaktere". Untermenü: Freigeschaltet / Gesperrt. Links Übersicht (mit Blättern),
/// rechts Details zum gewählten Charakter.
/// </summary>
public sealed class CharactersTab : BookTab
{
    private const int Columns = 4, Rows = 3, CardW = 44, CardH = 44, Gap = 6;
    private const int PerPage = Columns * Rows;
    private const int PreviewW = 64, PreviewH = 70, PreviewGround = 6, PreviewScale = 2;

    private static readonly string[] SubNames = { "Freigeschaltet", "Gesperrt" };

    private readonly IReadOnlyList<CharacterProfile> _profiles;
    private readonly GameState _state;
    private readonly ChallengeManager _challenges;
    private readonly Random _random = new();

    private int _sub;
    private int _page;
    private string? _selectedId;
    private ActionChoice? _previewAction;
    private double _previewStart, _previewEnd;

    public CharactersTab(IReadOnlyList<CharacterProfile> profiles, GameState state, ChallengeManager challenges)
    {
        _profiles = profiles;
        _state = state;
        _challenges = challenges;
    }

    public override string Title => "Charaktere";
    public override uint Color => 0xFFF0D9A8;
    public override PixelIcon Icon => PixelIcon.Person;

    public override void OnOpened() => ResetPreview();

    public override void Draw(UiContext ui, Rect left, Rect right)
    {
        var visible = _profiles.Where(p => _state.IsUnlocked(p) == (_sub == 0)).ToList();
        var selected = visible.FirstOrDefault(p => p.Id == _selectedId) ?? visible.FirstOrDefault();

        DrawOverview(ui, left, visible, selected);
        DrawDetails(ui, right, selected);
    }

    // ---------- Linke Seite: Übersicht ----------

    private void DrawOverview(UiContext ui, Rect page, List<CharacterProfile> visible, CharacterProfile? selected)
    {
        var b = ui.Buffer;
        var (x0, w) = ContentSpan(page, rightSide: false);
        int y = PageParts.Header(ui, page, false, "Charaktere", $"{_state.UnlockedCount}/{_profiles.Count}");

        y += SubTabBar.Draw(ui, x0, y, SubNames, _sub, SwitchSub) + 8;

        if (visible.Count == 0)
        {
            string message = _sub == 0 ? "Noch kein Charakter freigeschaltet." : "Alle Charaktere sind freigeschaltet!";
            PageParts.Paragraph(ui, message, x0, y + 20, w, 3, InkSoft);
            return;
        }

        int gridW = Columns * CardW + (Columns - 1) * Gap;
        int gridX = x0 + (w - gridW) / 2;
        int pages = Math.Max(1, (visible.Count + PerPage - 1) / PerPage);
        _page = Math.Clamp(_page, 0, pages - 1);
        int first = _page * PerPage;

        for (int i = 0; i < PerPage && first + i < visible.Count; i++)
        {
            var profile = visible[first + i];
            var rect = new Rect(gridX + i % Columns * (CardW + Gap), y + i / Columns * (CardH + Gap), CardW, CardH);
            PageParts.CharacterCard(ui, rect, profile, _state.IsUnlocked(profile), profile.Id == selected?.Id, () => Select(profile.Id));
        }

        PageParts.Pager(ui, page, x0, w, _page, pages, p => _page = p);
    }

    // ---------- Rechte Seite: Details ----------

    private void DrawDetails(UiContext ui, Rect page, CharacterProfile? profile)
    {
        var b = ui.Buffer;
        var font = ui.Font;
        var (x0, w) = ContentSpan(page, rightSide: true);

        if (profile == null)
        {
            PageParts.Header(ui, page, true, "Details");
            PageParts.Paragraph(ui, "Wähle links einen Charakter, um mehr über ihn zu erfahren.", x0, page.Y + 46, w, 4, InkSoft);
            return;
        }

        bool unlocked = _state.IsUnlocked(profile);
        int y = PageParts.Header(ui, page, true, unlocked ? profile.Name : "???",
            side: unlocked ? profile.Info.Title : "Gesperrt");

        var preview = new Rect(x0, y, PreviewW, PreviewH);
        DrawPreview(ui, preview, profile, unlocked);

        if (unlocked)
        {
            int fx = preview.Right + 8;
            var lines = new List<string>();
            foreach (string fact in profile.Info.Facts) lines.AddRange(font.Wrap(fact, x0 + w - fx));
            PageParts.Lines(ui, lines, fx, y, x0 + w - fx, PreviewH / font.LineStep, Ink);
        }
        y += PreviewH + 8;

        string text = unlocked ? profile.Info.Description : LockedText(profile);
        PageParts.Paragraph(ui, text, x0, y, w, (page.Bottom - 34 - y) / font.LineStep);
    }

    /// <summary>Gesperrt: Hinweis aus der Challenge, die den Charakter freischaltet (mit Fortschritt), sonst der Freischalt-Hinweis aus der JSON.</summary>
    private string LockedText(CharacterProfile profile)
    {
        var challenge = _challenges.FindUnlocking(profile);
        if (challenge == null) return profile.Info.UnlockHint;
        if (!_challenges.IsActive(challenge)) return "Schließe zuerst andere Challenges ab.";

        double current = _state.GetStat(challenge.Stat);
        return $"Challenge „{challenge.Name}“\n{challenge.Description}\n{ChallengeText.Progress(challenge, current)}";
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

    private void SwitchSub(int index)
    {
        _sub = index;
        _page = 0;
        _selectedId = null;
        ResetPreview();
    }

    private void Select(string id)
    {
        _selectedId = id;
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
