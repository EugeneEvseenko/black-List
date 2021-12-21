using Black_List.Properties;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WPFCustomMessageBox;

namespace Black_List
{
    /// <summary>
    /// Логика взаимодействия для Editor.xaml
    /// </summary>
    public partial class Editor : Window
    {
        public Editor()
        {
            InitializeComponent();
            logger = LogManager.GetCurrentClassLogger();
        }
        Human editItem;
        Logger logger;
        ObservableCollection<Human> list = new ObservableCollection<Human>();
        ObservableCollection<Human> ClearList = new ObservableCollection<Human>();
        Helper helper = new Helper();
        SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
        SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
        SolidColorBrush normalBrush = new SolidColorBrush(Colors.Gray);
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ListBoxItems.ItemsSource = list;
            if (Settings.Default.sortType.Equals("DESC"))
            {
                SetSortType(true);
            }
            else
            {
                SetSortType(false);
            }
        }
        public void UpdateList(string command)
        {
            try
            {
                SQLiteCommand comm = ((MainWindow)this.Tag).SQLconnection.CreateCommand();
                comm.CommandText = command;
                SQLiteDataReader reader = comm.ExecuteReader();
                list.Clear();
                ClearList.Clear();
                foreach (DbDataRecord record in reader)
                {
                    string name, iin = "ИИН не заполнен.", date = "Дата рождения не заполнена.", note = "Примечание не заполнено.";
                    name = record["HumanName"].ToString();
                    if (!record["IIN"].ToString().Equals(string.Empty))
                    {
                        iin = "ИИН: " + record["IIN"].ToString();
                    }
                    if (!record["DateOfBorn"].ToString().Equals(string.Empty))
                    {
                        DateTime birthdate = DateTime.Parse(record["DateOfBorn"].ToString());
                        var today = DateTime.Today;
                        var age = today.Year - birthdate.Year;
                        if (birthdate > today.AddYears(-age)) age--;
                        date = "Дата рождения: " + record["DateOfBorn"].ToString() + " (" + age.ToString() + helper.getEnding(age, "лет", "год", "года") + ")";
                    }
                    if (!record["Note"].ToString().Equals(string.Empty))
                    {
                        note = "Примечание: " + record["Note"].ToString();
                    }

                    Human item = new Human
                    {
                        HumanName = name,
                        IIN = iin,
                        DateOfBorn = date,
                        Note = note,
                        ID = int.Parse(record["ID"].ToString())
                    };
                    Human ClearItem = new Human
                    {
                        HumanName = record["HumanName"].ToString(),
                        IIN = record["IIN"].ToString(),
                        DateOfBorn = record["DateOfBorn"].ToString(),
                        Note = record["Note"].ToString(),
                        ID = int.Parse(record["ID"].ToString())
                    };
                    list.Add(item);
                    ClearList.Add(ClearItem);
                    Title = "Редактор чёрного списка [" + ListBoxItems.Items.Count.ToString() + "]";
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
        private void OpenContextMenu(FrameworkElement element)
        {
            if (element.ContextMenu != null)
            {
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.IsOpen = true;
            }
        }
        public void DeleteSelectedItem()
        {
            try
            {
                if (CustomMessageBox.ShowYesNo("Вы действительно хотите удалить '" + list[ListBoxItems.SelectedIndex].HumanName + "'?",
                    "Подтвердите удаление", "Да, хочу!", "Нет",
                    MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    SQLiteCommand comm = ((MainWindow)this.Tag).SQLconnection.CreateCommand();
                    comm.CommandText = @"
                DELETE FROM 'BlackList' WHERE ID ='" + list[ListBoxItems.SelectedIndex].ID + "';";
                    comm.ExecuteNonQuery();
                    ClearList.RemoveAt(ListBoxItems.SelectedIndex);
                    list.RemoveAt(ListBoxItems.SelectedIndex);
                    Title = "Редактор чёрного списка [" + ListBoxItems.Items.Count.ToString() + "]";
                    if(list.Count == 0)
                    {
                        ListBoxItems.IsEnabled = false;
                        InfoLabel.Content = "Необходимо заполнить базу данных.";
                    }
                    else
                    {
                        ListBoxItems.IsEnabled = true;
                        InfoLabel.Content = string.Empty;
                    }
                    ((MainWindow)Tag).GetCountRows();
                }
            } catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            DeleteSelectedItem();
        }

        private void SortBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenContextMenu(SortBlock);
        }
        public void SetSortMode(int mode)
        {
            try
            {
                Settings.Default.sortMode = mode;
                string sort;
                switch (mode)
                {
                    case 0:
                        {
                            NAMEsort.IsChecked = true;
                            IINsort.IsChecked = false;
                            INDEXsort.IsChecked = false;
                            SortBlock.Text = "Сортировка по имени";
                            sort = "HumanName";
                        }
                        break;
                    case 1:
                        {
                            NAMEsort.IsChecked = false;
                            IINsort.IsChecked = true;
                            INDEXsort.IsChecked = false;
                            SortBlock.Text = "Сортировка по ИИН";
                            sort = "IIN";
                        }
                        break;
                    case 2:
                        {
                            NAMEsort.IsChecked = false;
                            IINsort.IsChecked = false;
                            INDEXsort.IsChecked = true;
                            SortBlock.Text = "Сортировка по индексу";
                            sort = "ID";
                        }
                        break;
                    default:
                        {
                            NAMEsort.IsChecked = true;
                            IINsort.IsChecked = false;
                            INDEXsort.IsChecked = false;
                            SortBlock.Text = "Сортировка по имени";
                            sort = "HumanName";
                        }
                        break;
                }
                UpdateList("SELECT * FROM 'BlackList' ORDER BY " + sort + " " + Settings.Default.sortType);
                Settings.Default.Save();
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
        private void NAMEsort_Click(object sender, RoutedEventArgs e)
        {
            SetSortMode(0);
        }

        private void IINsort_Click(object sender, RoutedEventArgs e)
        {
            SetSortMode(1);
        }

        private void INDEXsort_Click(object sender, RoutedEventArgs e)
        {
            SetSortMode(2);
        }
        public void SetSortType(bool desc)
        {
            if (desc)
            {
                Settings.Default.sortType = "DESC";
                SortAscending.Visibility = Visibility.Visible;
                SortDescending.Visibility = Visibility.Hidden;
            }
            else
            {
                Settings.Default.sortType = string.Empty;
                SortAscending.Visibility = Visibility.Hidden;
                SortDescending.Visibility = Visibility.Visible;
            }
            Settings.Default.Save();
            SetSortMode(Settings.Default.sortMode);
        }
        private void SortDescending_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SetSortType(true);
        }

        private void SortAscending_MouseUp(object sender, MouseButtonEventArgs e)
        {
            SetSortType(false);
        }
        int editIndex = 0;
        public void SetEditMode(bool mode)
        {
            try
            {
                NameTextbox.IsEnabled = mode;
                DateBox.IsEnabled = mode;
                IINBox.IsEnabled = mode;
                NoteBox.IsEnabled = mode;
                SaveButton.IsEnabled = mode;
                CancelButton.IsEnabled = mode;
                SortAscending.IsEnabled = !mode;
                SortDescending.IsEnabled = !mode;
                SortBlock.IsEnabled = !mode;
                DeleteMenuItem.IsEnabled = !mode;
                AddItemButton.IsEnabled = !mode;
                SortBlock.Foreground = SortAscending.Foreground;
                if (mode)
                {
                    editIndex = ListBoxItems.SelectedIndex;
                    editItem = ClearList[editIndex];
                    NameTextbox.Text = editItem.HumanName;
                    if (!editItem.DateOfBorn.Equals(string.Empty))
                    {
                        DateBox.SelectedDate = DateTime.Parse(editItem.DateOfBorn);
                    }
                    else
                    {
                        DateBox.Text = string.Empty;
                    }
                    IINBox.Text = editItem.IIN;
                    NoteBox.Text = editItem.Note;
                    InfoLabel.Content = "[ Режим редактирования ]";
                }
                else
                {

                    SQLiteCommand comm = ((MainWindow)this.Tag).SQLconnection.CreateCommand();
                    string iin = "ИИН не заполнен.", date = "Дата рождения не заполнена.", note = "Примечание не заполнено.";
                    if (!IINBox.Text.Equals(string.Empty))
                    {
                        iin = "ИИН: " + IINBox.Text;
                    }
                    if (DateBox.SelectedDate != null)
                    {
                        DateTime birthdate = DateTime.Parse(DateBox.SelectedDate.Value.ToShortDateString());
                        var today = DateTime.Today;
                        var age = today.Year - birthdate.Year;
                        if (birthdate > today.AddYears(-age)) age--;
                        date = "Дата рождения: " + DateBox.SelectedDate.Value.ToShortDateString() + " (" + age.ToString() + helper.getEnding(age, "лет", "год", "года") + ")";
                    }
                    if (!NoteBox.Text.Equals(string.Empty))
                    {
                        note = "Примечание: " + NoteBox.Text;
                    }
                    ClearList[editIndex].HumanName = NameTextbox.Text;
                    string clearDate = string.Empty;
                    if (DateBox.SelectedDate != null)
                    {
                        clearDate = DateBox.SelectedDate.Value.ToShortDateString();
                    }
                    ClearList[editIndex].DateOfBorn = clearDate;
                    ClearList[editIndex].IIN = IINBox.Text;
                    ClearList[editIndex].Note = NoteBox.Text;

                    list[editIndex].HumanName = NameTextbox.Text;
                    list[editIndex].DateOfBorn = date;
                    list[editIndex].IIN = iin;
                    list[editIndex].Note = note;
                    comm.CommandText = @"UPDATE 'BlackList' SET HumanName = '" + NameTextbox.Text + "', DateOfBorn = '" + clearDate + "', IIN = '" + IINBox.Text + "', Note = '" + NoteBox.Text + "', FindString = '" + NameTextbox.Text.ToLower() + "' WHERE ID = " + list[ListBoxItems.SelectedIndex].ID + ";";
                    comm.ExecuteNonQuery();
                    ListBoxItems.Items.Refresh();
                    NameTextbox.Clear();
                    DateBox.Text = string.Empty;
                    IINBox.Clear();
                    NoteBox.Clear();
                    InfoLabel.Content = string.Empty;
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
        private void EditMenuItem_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(true);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            SetEditMode(false);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            editItem = null;
            NameTextbox.Clear();
            DateBox.Text = string.Empty;
            IINBox.Clear();
            NoteBox.Clear();
            NameTextbox.IsEnabled = false;
            DateBox.IsEnabled = false;
            IINBox.IsEnabled = false;
            NoteBox.IsEnabled = false;
            SaveButton.IsEnabled = false;
            CancelButton.IsEnabled = false;
            SortAscending.IsEnabled = true;
            SortDescending.IsEnabled = true;
            SortBlock.IsEnabled = true;
            AddItemButton.IsEnabled = true;
            SortBlock.Foreground = SortAscending.Foreground;
            InfoLabel.Content = string.Empty;
            DeleteMenuItem.IsEnabled = true;
        }

        private void NameTextbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataUpdated();
        }
        private void DataUpdated()
        {
            if (NameTextbox.IsEnabled)
            {
                if (NameTextbox.Text.Length > 0)
                {
                    if (!string.IsNullOrWhiteSpace(NameTextbox.Text))
                    {
                        SaveButton.IsEnabled = true;
                        InfoLabel.Foreground = normalBrush;
                        InfoLabel.Content = string.Empty;
                    }
                    else
                    {
                        SaveButton.IsEnabled = false;
                        InfoLabel.Foreground = redBrush;
                        InfoLabel.Content = "Поле 'ФИО' не может состоять только из символов-разделителей.";
                    }
                }
                else
                {
                    SaveButton.IsEnabled = false;
                    InfoLabel.Foreground = redBrush;
                    InfoLabel.Content = "Поле 'ФИО' не может быть пустым.";
                }
            }
        }
        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void IINBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsTextAllowed(e.Text))
            {
                e.Handled = true;
                InfoLabel.Foreground = redBrush;
                InfoLabel.Content = "В поле 'ИИН' используются только цифры.";
            }
            else
            {
                DataUpdated();
            }
        }

        private void IINBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IINBox.Text = IINBox.Text.Replace(" ", string.Empty);
        }

        private void AddItemButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddWindow addWindow = new AddWindow(new Human());
                if (addWindow.ShowDialog() == true)
                {
                    Human human = addWindow.Humand;
                    using (SQLiteCommand command = new SQLiteCommand(((MainWindow)this.Tag).SQLconnection))
                    {
                        command.CommandText = "INSERT INTO 'BlackList" + @"' ('HumanName', 'DateOfBorn', 'IIN', 'Note', 'FindString') VALUES ('" + human.HumanName + @"', '" + human.DateOfBorn + "', '" + human.IIN + "', '" + human.Note + "', '" + human.FindString + "');";
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    if (Settings.Default.sortType.Equals("DESC"))
                    {
                        SetSortType(true);
                    }
                    else
                    {
                        SetSortType(false);
                    }
                    if (list.Count > 0)
                    {
                        ListBoxItems.IsEnabled = true;
                        InfoLabel.Content = string.Empty;
                    }
                    ((MainWindow)Tag).GetCountRows();
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
