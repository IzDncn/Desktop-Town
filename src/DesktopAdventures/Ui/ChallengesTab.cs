using DesktopAdventures.Challenges;
using static DesktopAdventures.Ui.BookTheme;

namespace DesktopAdventures.Ui;

/// <summary>
/// Reiter "Challenges". Untermenü: Aktiv / Abgeschlossen. Links die Liste mit Fortschrittsbalken (mit Blättern),
/// rechts Beschreibung, Fortschritt und Belohnung der gewählten Challenge.
/// </summary>
public sealed class ChallengesTab : BookTab
{
    private const int RowH = 40, RowGap = 6, PerPage = 4, CardW = 44, CardH = 44;

    private static readonly string[] SubNames = { "Aktiv", "Abgeschlossen" };

    private readonly ChallengeManager _manager;
    private readonly GameState _state;

    private int _sub;
    private int _page;
    private string? _selectedId;

    public ChallengesTab(ChallengeManager manager, GameState state)
    {
        _manager = manager;
        _state = state;
    }

    public override string Title => "Challenges";
    public override uint Color => 0xFFD9706A;
    public override PixelIcon Icon => PixelIcon.Star;

    public override void Draw(UiContext ui, Rect left, Rect right)
    {
        var list = (_sub == 0 ? _manager.Active : _manager.Done).ToList();
        var selected = list.FirstOrDefault(c => c.Id == _selectedId) ?? list.FirstOrDefault();

        DrawList(ui, left, list, selected);
        DrawDetails(ui, right, selected);
    }

    // ---------- Linke Seite: Liste ----------

    private void DrawList(UiContext ui, Rect page, List<Challenge> list, Challenge? selected)
    {
        var (x0, w) = ContentSpan(page, rightSide: false);
        int y = PageParts.Header(ui, page, false, "Challenges", $"{_manager.Done.Count()}/{_manager.All.Count}");
        y += SubTabBar.Draw(ui, x0, y, SubNames, _sub, SwitchSub) + 8;

        if (list.Count == 0)
        {
            string message = _sub == 0 ? "Gerade ist keine Challenge aktiv." : "Du hast noch keine Challenge abgeschlossen.";
            PageParts.Paragraph(ui, message, x0, y + 20, w, 3, InkSoft);
            return;
        }

        int pages = Math.Max(1, (list.Count + PerPage - 1) / PerPage);
        _page = Math.Clamp(_page, 0, pages - 1);
        int first = _page * PerPage;

        for (int i = 0; i < PerPage && first + i < list.Count; i++)
            DrawRow(ui, new Rect(x0, y + i * (RowH + RowGap), w, RowH), list[first + i], list[first + i].Id == selected?.Id);

        PageParts.Pager(ui, page, x0, w, _page, pages, p => _page = p);
    }

    private void DrawRow(UiContext ui, Rect rect, Challenge challenge, bool selected)
    {
        var b = ui.Buffer;
        var font = ui.Font;
        bool done = _manager.IsCompleted(challenge);
        bool hover = ui.Button(rect, () => Select(challenge.Id));

        b.FillRect(rect.X, rect.Y, rect.W, rect.H, CardFill);
        b.StrokeRect(rect.X, rect.Y, rect.W, rect.H, selected || hover ? CardBorderHover : CardBorder);
        if (selected) b.StrokeRect(rect.X + 1, rect.Y + 1, rect.W - 2, rect.H - 2, CardBorderHover);

        double progress = done ? challenge.Target : _manager.Progress(challenge);
        string count = ChallengeText.Progress(challenge, progress, withUnit: false);
        int countW = done ? PixelIcon.Check.Width : font.Measure(count);

        font.Draw(b, font.Truncate(challenge.Name, rect.W - countW - 22), rect.X + 6, rect.Y + 3, Ink);
        if (done) PixelIcon.Check.Draw(b, rect.Right - 8 - countW, rect.Y + 9, 0xFF2F7A2B);
        else font.DrawRight(b, count, rect.Right - 7, rect.Y + 3, InkSoft);

        PageParts.ProgressBar(ui, new Rect(rect.X + 6, rect.Y + 27, rect.W - 12, 8), progress / challenge.Target);
    }

    // ---------- Rechte Seite: Details ----------

    private void DrawDetails(UiContext ui, Rect page, Challenge? challenge)
    {
        var font = ui.Font;
        var (x0, w) = ContentSpan(page, rightSide: true);

        if (challenge == null)
        {
            PageParts.Header(ui, page, true, "Details");
            PageParts.Paragraph(ui, "Wähle links eine Challenge, um Ziel und Belohnung zu sehen.", x0, page.Y + 46, w, 4, InkSoft);
            return;
        }

        bool done = _manager.IsCompleted(challenge);
        string side = done ? "Geschafft" : "Aktiv";
        int y = PageParts.Header(ui, page, true, font.Truncate(challenge.Name, w - font.Measure(side) - 10), side: side);

        PageParts.Paragraph(ui, challenge.Description, x0, y, w, 3);
        y += 3 * font.LineStep + 6;

        double progress = done ? challenge.Target : _manager.Progress(challenge);
        PageParts.ProgressBar(ui, new Rect(x0, y, w, 10), progress / challenge.Target);
        y += 14;
        font.Draw(ui.Buffer, ChallengeText.Progress(challenge, progress), x0, y, Ink);
        y += font.LineStep + 6;

        DrawRewards(ui, challenge, done, x0, y, w);
    }

    private void DrawRewards(UiContext ui, Challenge challenge, bool done, int x0, int y, int w)
    {
        var characters = challenge.Rewards.Where(r => r.Character != null).Select(r => r.Character!).ToList();
        if (characters.Count == 0) return;

        string label = done
            ? "Belohnung: " + string.Join(", ", characters.Select(c => c.Name))
            : characters.Count == 1 ? "Belohnung: Neuer Charakter" : $"Belohnung: {characters.Count} neue Charaktere";
        ui.Font.Draw(ui.Buffer, ui.Font.Truncate(label, w), x0, y, InkSoft);
        y += ui.Font.LineStep + 2;

        for (int i = 0; i < characters.Count && i < 3; i++)
            PageParts.CharacterCard(ui, new Rect(x0 + i * (CardW + 6), y, CardW, CardH), characters[i], _state.IsUnlocked(characters[i]), selected: false);
    }

    // ---------- Aktionen ----------

    private void SwitchSub(int index)
    {
        _sub = index;
        _page = 0;
        _selectedId = null;
    }

    private void Select(string id) => _selectedId = id;
}
