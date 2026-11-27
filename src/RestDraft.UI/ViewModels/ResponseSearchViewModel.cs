using System.Windows.Input;
using RestDraft.Application.Search;
using RestDraft.UI.Commands;

namespace RestDraft.UI.ViewModels;

// The find panel of one response. Each tab has its own, so the panel, the search text and the options stay with the tab.
// Matches are looked for only while the panel is open.
public sealed class ResponseSearchViewModel : ViewModelBase
{
    private const int NoMatch = -1;
    private const string TruncatedNote = "Only the preview is searched.";

    private readonly RelayCommand _nextCommand;
    private readonly RelayCommand _previousCommand;
    private string _text = string.Empty;
    private string _query = string.Empty;
    private bool _isOpen;
    private bool _matchCase;
    private bool _wholeWord;
    private string _truncationNote = string.Empty;
    private IReadOnlyList<TextMatch> _matches = [];
    private int _currentIndex = NoMatch;
    // The current match is the first one at or after this place. It follows the current match, so a longer search text
    // stays where the shorter one was found.
    private int _anchor;

    public ResponseSearchViewModel()
    {
        _nextCommand = new RelayCommand(SelectNext, () => _matches.Count > 0);
        _previousCommand = new RelayCommand(SelectPrevious, () => _matches.Count > 0);
        CloseCommand = new RelayCommand(Close, () => true);
    }

    public bool IsOpen
    {
        get => _isOpen;
        private set => SetProperty(ref _isOpen, value);
    }

    public string Query
    {
        get => _query;
        set
        {
            if (SetProperty(ref _query, value ?? string.Empty))
            {
                Search();
            }
        }
    }

    public bool MatchCase
    {
        get => _matchCase;
        set
        {
            if (SetProperty(ref _matchCase, value))
            {
                Search();
            }
        }
    }

    public bool WholeWord
    {
        get => _wholeWord;
        set
        {
            if (SetProperty(ref _wholeWord, value))
            {
                Search();
            }
        }
    }

    public IReadOnlyList<TextMatch> Matches => _matches;

    public TextMatch? CurrentMatch => _currentIndex == NoMatch ? null : _matches[_currentIndex];

    public string CounterText => _query.Length == 0 ? string.Empty : $"{_currentIndex + 1} of {_matches.Count}";

    public bool HasNoMatch => _query.Length > 0 && _matches.Count == 0;

    public string TruncationNote
    {
        get => _truncationNote;
        private set => SetProperty(ref _truncationNote, value);
    }

    public ICommand NextCommand => _nextCommand;

    public ICommand PreviousCommand => _previousCommand;

    public ICommand CloseCommand { get; }

    // Raised when the matches or the current one change, so the editor can show them.
    public event EventHandler? ResultsChanged;

    public event EventHandler<SearchClosedEventArgs>? Closed;

    // A selection of one line becomes the search text. Anything else keeps the text searched last.
    public void Open(string selectedText, int cursorOffset)
    {
        _anchor = cursorOffset;
        if (IsOneLine(selectedText))
        {
            SetProperty(ref _query, selectedText, nameof(Query));
        }

        IsOpen = true;
        Search();
    }

    // The text that is shown now. A new text is searched from its start.
    public void SetText(string text, bool isTruncated)
    {
        TruncationNote = isTruncated ? TruncatedNote : string.Empty;
        if (_text == text)
        {
            return;
        }

        _text = text;
        _anchor = 0;
        Search();
    }

    public void SelectNext()
    {
        Move(1);
    }

    public void SelectPrevious()
    {
        Move(-1);
    }

    public void Close()
    {
        if (!IsOpen)
        {
            return;
        }

        var current = CurrentMatch;
        IsOpen = false;
        Search();
        Closed?.Invoke(this, new SearchClosedEventArgs(current));
    }

    private static bool IsOneLine(string text)
    {
        return text.Length > 0 && text.AsSpan().IndexOfAny('\r', '\n') < 0;
    }

    private void Search()
    {
        _matches = IsOpen ? TextMatchFinder.Find(_text, _query, new TextSearchOptions(MatchCase, WholeWord)) : [];
        _nextCommand.RaiseCanExecuteChanged();
        _previousCommand.RaiseCanExecuteChanged();
        RaisePropertyChanged(nameof(Matches));
        RaisePropertyChanged(nameof(HasNoMatch));
        MakeCurrent(FindFromAnchor());
    }

    // The first match at or after the anchor, or the first one of all when there is none after it.
    private int FindFromAnchor()
    {
        if (_matches.Count == 0)
        {
            return NoMatch;
        }

        var index = TextMatches.FirstIndexFrom(_matches, _anchor);
        return index < _matches.Count ? index : 0;
    }

    private void Move(int step)
    {
        if (_matches.Count == 0)
        {
            return;
        }

        MakeCurrent((_currentIndex + step + _matches.Count) % _matches.Count);
    }

    private void MakeCurrent(int index)
    {
        _currentIndex = index;
        if (CurrentMatch is { } match)
        {
            _anchor = match.Start;
        }

        RaisePropertyChanged(nameof(CurrentMatch));
        RaisePropertyChanged(nameof(CounterText));
        ResultsChanged?.Invoke(this, EventArgs.Empty);
    }
}
