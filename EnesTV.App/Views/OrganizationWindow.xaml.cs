using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using EnesTV.App.ViewModels;
using EnesTV.Core.Models;
using TextSearch = EnesTV.Core.Services.TextSearch; // not WPF's System.Windows.Controls.TextSearch

namespace EnesTV.App.Views;

public partial class OrganizationWindow : Window
{
    public sealed class ChannelRow(ChannelItem item) : Observable
    {
        public string Id { get; } = item.Channel.Id;
        public string OriginalName { get; } = item.Channel.Name;
        public string Category { get; } = item.Category;
        public string Name { get; set; } = item.Name;
        public bool Visible { get; set; } = !item.Hidden;
        private bool _inList;
        public bool InList { get => _inList; set => Set(ref _inList, value); }
    }
    public sealed class CategoryRow(string name, bool visible) : Observable
    {
        public string Name { get; } = name;
        private bool _visible = visible;
        public bool Visible { get => _visible; set => Set(ref _visible, value); }
    }
    private readonly MainViewModel _vm;
    private readonly ObservableCollection<ChannelRow> _rows;
    private readonly ObservableCollection<FavoriteList> _lists;
    private readonly CategoryRow[] _categories;
    private readonly ICollectionView _view;
    private bool _saving;
    public OrganizationWindow(MainViewModel vm)
    {
        InitializeComponent(); _vm = vm;
        _rows = new(vm.AllChannels.OrderBy(x => x.SortOrder).Select(x => new ChannelRow(x)));
        _lists = new(vm.Layout.FavoriteLists.Select(x => x with { Channels = new(x.Channels) }));
        _categories = vm.AllChannels.Select(x => x.Category).Distinct().Order().Select(x => new CategoryRow(x, !vm.Layout.HiddenCategories.Contains(x))).ToArray();
        _view = CollectionViewSource.GetDefaultView(_rows);
        _view.Filter = item => item is ChannelRow row && (TextSearch.Contains(row.Name, Search.Text) || TextSearch.Contains(row.Category, Search.Text) || TextSearch.Contains(row.OriginalName, Search.Text));
        RowsGrid.ItemsSource = _view; CategoryList.ItemsSource = _categories; Lists.ItemsSource = _lists; Lists.SelectedIndex = 0;
        Closing += (_, e) => { if (_saving) e.Cancel = true; };
    }
    private void Search_Changed(object sender, TextChangedEventArgs e) => _view?.Refresh();
    private void ShowAll_Click(object sender, RoutedEventArgs e) { foreach (var category in _categories) category.Visible = true; }
    private void HideAll_Click(object sender, RoutedEventArgs e) { foreach (var category in _categories) category.Visible = false; }
    private void Up_Click(object sender, RoutedEventArgs e) => Move(-1);
    private void Down_Click(object sender, RoutedEventArgs e) => Move(1);
    private void Move(int step)
    {
        if (RowsGrid.SelectedItems.Count != 1) { Result.Text = "Taşımak için bir kanal seçin."; return; }
        RowsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var row = (ChannelRow)RowsGrid.SelectedItem; var visible = _view.Cast<ChannelRow>().ToList(); var index = visible.IndexOf(row); var next = index + step;
        if (next < 0 || next >= visible.Count) return;
        _rows.Move(_rows.IndexOf(row), _rows.IndexOf(visible[next])); RowsGrid.ScrollIntoView(row);
    }
    private void Sort_Click(object sender, RoutedEventArgs e)
    {
        RowsGrid.CommitEdit(DataGridEditingUnit.Row, true);
        var sorted = _rows.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToArray();
        using (_view.DeferRefresh()) { _rows.Clear(); foreach (var row in sorted) _rows.Add(row); }
    }
    private string? AskName(string title, string initial)
    {
        var panel = new StackPanel { Margin = new Thickness(24) };
        var text = new TextBox { Text = initial, MaxLength = 80 }; panel.Children.Add(text);
        var button = new Button { Content = "Tamam", IsDefault = true, Margin = new Thickness(0, 14, 0, 0) }; panel.Children.Add(button);
        var dialog = new Window { Title = title, Owner = this, Width = 400, SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner, Content = panel };
        button.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(text.Text)) dialog.DialogResult = true; };
        dialog.Loaded += (_, _) => { text.Focus(); text.SelectAll(); };
        return dialog.ShowDialog() == true ? text.Text.Trim() : null;
    }
    private bool UniqueName(string name, string? id = null)
    {
        if (_lists.Any(x => x.Id != id && TextSearch.Equals(x.Name, name))) { Result.Text = "Bu isimde bir liste zaten var."; return false; }
        return true;
    }
    private void NewList_Click(object sender, RoutedEventArgs e)
    {
        var name = AskName("Yeni favori listesi", ""); if (name == null || !UniqueName(name)) return;
        var list = new FavoriteList(Guid.NewGuid().ToString("N"), name, []); _lists.Add(list); Lists.SelectedItem = list;
    }
    private void RenameList_Click(object sender, RoutedEventArgs e)
    {
        if (Lists.SelectedItem is not FavoriteList list) return;
        var name = AskName("Liste adı", list.Name); if (name == null || !UniqueName(name, list.Id)) return;
        var index = _lists.IndexOf(list); _lists[index] = list with { Name = name }; Lists.SelectedIndex = index;
    }
    private void DeleteList_Click(object sender, RoutedEventArgs e)
    {
        if (Lists.SelectedItem is not FavoriteList list) return;
        _lists.Remove(list); Lists.SelectedIndex = _lists.Count > 0 ? 0 : -1; UpdateMembership();
    }
    private void Lists_Changed(object sender, SelectionChangedEventArgs e) => UpdateMembership();
    private void UpdateMembership()
    {
        if (_rows == null) return;
        var list = Lists.SelectedItem as FavoriteList;
        foreach (var row in _rows) row.InList = list?.Channels.Contains(row.Id) == true;
        Membership.Text = list == null ? "Önce bir liste oluşturun." : $"{_rows.Count(x => x.InList)} kanal";
    }
    private void AddMembers_Click(object sender, RoutedEventArgs e) => SetMembers(true);
    private void RemoveMembers_Click(object sender, RoutedEventArgs e) => SetMembers(false);
    private void SetMembers(bool add)
    {
        if (Lists.SelectedItem is not FavoriteList list) return;
        foreach (ChannelRow row in RowsGrid.SelectedItems) { if (add) list.Channels.Add(row.Id); else list.Channels.Remove(row.Id); }
        UpdateMembership();
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        if (!RowsGrid.CommitEdit(DataGridEditingUnit.Cell, true) || !RowsGrid.CommitEdit(DataGridEditingUnit.Row, true)) return;
        _saving = true; IsEnabled = false;
        try
        {
            var rules = new Dictionary<string, ChannelLayout>(_vm.Layout.Channels);
            for (var i = 0; i < _rows.Count; i++) { var row = _rows[i]; rules[row.Id] = new(row.Name.Trim() == row.OriginalName ? "" : row.Name.Trim(), i, !row.Visible); }
            var hidden = new HashSet<string>(_vm.Layout.HiddenCategories);
            foreach (var category in _categories) { if (category.Visible) hidden.Remove(category.Name); else hidden.Add(category.Name); }
            await _vm.SaveOrganizationAsync(_vm.Layout with { Channels = rules, HiddenCategories = hidden, FavoriteLists = _lists.ToList() });
            _saving = false; Close();
        }
        catch (Exception ex) { _vm.Error(ex); Result.Text = _vm.Status; }
        finally { _saving = false; IsEnabled = true; }
    }
}
