using Black_List.Properties;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.Common;
using System.Data.SQLite;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WPFCustomMessageBox;

namespace Black_List
{

    public partial class MainWindow : Window
    {
        string localAppDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr handle, int cmdShow);
        [DllImport("user32.dll")]
        private static extern int SetForegroundWindow(IntPtr handle);
        readonly Mutex mutex = new Mutex(false, "9C771DFB-23A1-4039-BDE6-6855880A25A8");
        public class MemoryManagement
        {
            [DllImport("kernel32.dll")]
            public static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

            public void FlushMemory()
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
        }
        public MainWindow()
        {
            if (!mutex.WaitOne(500, false))
            {
                //MessageBox.Show("Приложение уже запущено!", "Ошибочка!");
                string processName = Process.GetCurrentProcess().ProcessName;
                Process process = Process.GetProcesses().Where(p => p.ProcessName == processName).FirstOrDefault();
                if (process != null)
                {
                    IntPtr handle = process.MainWindowHandle;
                    ShowWindow(handle, 1);
                    SetForegroundWindow(handle);
                }
                this.Close();
                return;
            }
            InitializeComponent();
            Height = Settings.Default.HeightWindow;
            Width = Settings.Default.WidthWindow;
            if (Settings.Default.Maximized)
            {
                WindowState = WindowState.Maximized;
            }
            LogManager.Configuration = helper.GetLoggingConfiguration();
            logger = LogManager.GetCurrentClassLogger();
            logger.Info("Старт программы");
        }
        public void SetFindMode(int mode)
        {
            Settings.Default.findMode = mode;
            if (mode == 0)
            {
                NAMEmode.IsChecked = true;
                IINmode.IsChecked = false;
                modeBlock.Text = "Поиск по имени";
            }
            else
            {
                NAMEmode.IsChecked = false;
                IINmode.IsChecked = true;
                modeBlock.Text = "Поиск по ИИН";
            }
            Settings.Default.Save();
        }
        public void SetAutorun(bool mode)
        {
            try
            {
                RegistryKey reg = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run\\");
                if (mode)
                {
                    reg.SetValue("BlackList", "\"" + System.Reflection.Assembly.GetExecutingAssembly().Location + "\"");
                }
                else
                {
                    reg.DeleteValue("BlackList");
                }
                reg.Close();
            }
            catch { }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            MM.FlushMemory();
            if (Settings.Default.HideStart)
            {
                WindowState = WindowState.Minimized;
                Hide();
            }
            SetAutorun(Settings.Default.Autorun);
            SetFindMode(Settings.Default.findMode);
            
            tbi.TrayLeftMouseDown += OnClickTrayIcon;
            ConnectBase();
            FindView.ItemsSource = findedList;
            
        }
        private void OnClickTrayIcon(object sender, RoutedEventArgs e)
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                WindowState = WindowState.Normal;
                Activate();
            }
        }
        public SQLiteConnection SQLconnection;
        Helper helper = new Helper();
        SolidColorBrush greenBrush = new SolidColorBrush(Colors.Green);
        SolidColorBrush redBrush = new SolidColorBrush(Colors.Red);
        SolidColorBrush normalBrush = new SolidColorBrush(Colors.Gray);
        MemoryManagement MM = new MemoryManagement();
        ObservableCollection<Human> findedList = new ObservableCollection<Human>();
        Logger logger;
        
        public void ConnectBase()
        {
            try
            {
                string baseName = localAppDir + @"\BlackList.db";
                if (!File.Exists(baseName))
                {
                    SQLiteConnection.CreateFile(baseName);

                    SQLconnection = new SQLiteConnection("Data Source = " + baseName + "; Version=3;");
                    SQLconnection.Open();
                    using (SQLiteCommand command = new SQLiteCommand(SQLconnection))
                    {
                        command.CommandText = @"CREATE TABLE [BlackList] (
                    [id] integer NOT NULL PRIMARY KEY AUTOINCREMENT UNIQUE,
                    [HumanName] text NOT NULL,
                    [DateOfBorn] text,
                    [IIN] text,
                    [Note] text,
                    [FindString] text NOT NULL
                    );";
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                }
                else
                {
                    SQLconnection = new SQLiteConnection("Data Source = " + baseName + "; Version=3;");
                    SQLconnection.Open();
                }
            } catch (Exception ex)
            {
                logger.Error(ex);
            }
            GetCountRows();

        }
        public void GetCountRows()
        {
            int n = 0;
            try
            {
                SQLiteCommand comm = SQLconnection.CreateCommand();
                comm.CommandText = "SELECT * FROM 'BlackList'";
                SQLiteDataReader reader = comm.ExecuteReader();
                foreach (DbDataRecord record in reader)
                {
                    n++;
                }

                if (n != 0)
                {
                    FindBox.IsEnabled = true;
                    SearchButton.IsEnabled = true;
                    InfoLabel.Content = "Введите данные для поиска.";
                    EditButton.IsEnabled = true;
                    EditButton.Source = ImageSourceForBitmap(Properties.Resources.edit);
                    CountLabel.Content = "В списке " + n.ToString() + helper.getEnding(n, "человек.", "человек.", "человека.");
                }
                else
                {
                    FindBox.IsEnabled = false;
                    SearchButton.IsEnabled = false;
                    EditButton.IsEnabled = false;
                    EditButton.Source = ImageSourceForBitmap(Properties.Resources.edit_not_enabled);
                    InfoLabel.Content = "Поиск недоступен. Заполните базу данных.";
                    CountLabel.Content = "В списке нет людей.";
                }
                MM.FlushMemory();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void FindBox_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter && FindBox.Text.Length > 0)
            {
                Find();
            }
        }
        public void Find()
        {
            try
            {
                SQLiteCommand command;
                if (Settings.Default.findMode == 0)
                {
                    command = new SQLiteCommand("SELECT * FROM BlackList WHERE FindString LIKE '%" + FindBox.Text.ToLower() + "%';", SQLconnection);
                }
                else
                {
                    command = new SQLiteCommand("SELECT * FROM BlackList WHERE IIN LIKE '%" + FindBox.Text + "%';", SQLconnection);
                }
                SQLiteDataReader reader = command.ExecuteReader();
                int count = 0;
                findedList.Clear();
                foreach (DbDataRecord record in reader)
                {
                    Human item = new Human
                    {
                        HumanName = record["HumanName"].ToString(),
                        IIN = record["IIN"].ToString(),
                        DateOfBorn = record["DateOfBorn"].ToString(),
                        Note = record["Note"].ToString(),
                    };
                    findedList.Add(item);
                    count++;
                }
                if (count == 0)
                {
                    InfoLabel.Content = "Совпадений не найдено!";
                    InfoLabel.Foreground = greenBrush;
                    FindView.Visibility = Visibility.Hidden;
                }
                else
                {
                    InfoLabel.Foreground = redBrush;
                    FindView.Visibility = Visibility.Visible;
                    InfoLabel.Content = "Найдено " + count.ToString() + helper.getEnding(count, "совпадений!", "совпадение!", "совпадения!");
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
}
        private void FindBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            if (Settings.Default.findMode == 0)
            {
                InfoLabel.Foreground = normalBrush;
                if (FindBox.Text.Length > 0)
                {
                    InfoLabel.Content = "Для поиска нажмите Enter или на кнопку поиска.";
                    ClearButton.Source = ImageSourceForBitmap(Properties.Resources.clear);
                    ClearButton.IsEnabled = true;
                }
                else
                {
                    InfoLabel.Content = "Введите данные для поиска.";
                    ClearButton.Source = ImageSourceForBitmap(Properties.Resources.clear_not_enabled);
                    ClearButton.IsEnabled = false;
                }
            }
            else
            {
                FindBox.Text = FindBox.Text.Replace(" ", string.Empty);
                InfoLabel.Foreground = normalBrush;
                if (FindBox.Text.Length > 0)
                {
                    InfoLabel.Content = "Для поиска нажмите Enter или на кнопку поиска.";
                    ClearButton.Source = ImageSourceForBitmap(Properties.Resources.clear);
                    ClearButton.IsEnabled = true;
                }
                else
                {
                    InfoLabel.Content = "Введите данные для поиска.";
                    ClearButton.Source = ImageSourceForBitmap(Properties.Resources.clear_not_enabled);
                    ClearButton.IsEnabled = false;
                }
            }
        }
        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void FindBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            if (Settings.Default.findMode == 1)
            {
                if (!IsTextAllowed(e.Text))
                {
                    e.Handled = true;
                    InfoLabel.Content = "Для поиска по ИИН используются только цифры.";
                }

            }
        }
        public void AddItem()
        {
            try
            {
                AddWindow addWindow = new AddWindow(new Human());
                if (addWindow.ShowDialog() == true)
                {
                    Human human = addWindow.Humand;
                    using (SQLiteCommand command = new SQLiteCommand(SQLconnection))
                    {
                        command.CommandText = "INSERT INTO 'BlackList" + @"' ('HumanName', 'DateOfBorn', 'IIN', 'Note', 'FindString') VALUES ('" + human.HumanName + @"', '" + human.DateOfBorn + "', '" + human.IIN + "', '" + human.Note + "', '" + human.FindString + "');";
                        command.CommandType = CommandType.Text;
                        command.ExecuteNonQuery();
                    }
                    GetCountRows();
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
        private void AddButton_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            AddItem();
        }
        [DllImport("gdi32.dll", EntryPoint = "DeleteObject")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject([In] IntPtr hObject);

        public ImageSource ImageSourceForBitmap(Bitmap bmp)
        {
            var handle = bmp.GetHbitmap();
            try
            {
                return Imaging.CreateBitmapSourceFromHBitmap(handle, IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            finally { DeleteObject(handle); }
        }
        private void addButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            addButton.Source = ImageSourceForBitmap(Properties.Resources.add_hover);
        }

        private void addButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            addButton.Source = ImageSourceForBitmap(Properties.Resources.add);
        }

        private void SettingsButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SettingsButton.Source = ImageSourceForBitmap(Properties.Resources.settings_hover);
        }

        private void SettingsButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SettingsButton.Source = ImageSourceForBitmap(Properties.Resources.settings);
        }

        private void SearchButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SearchButton.Source = ImageSourceForBitmap(Properties.Resources.search_hover);
        }

        private void SearchButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            SearchButton.Source = ImageSourceForBitmap(Properties.Resources.search);
        }

        private void OpenContextMenu(FrameworkElement element)
        {
            if (element.ContextMenu != null)
            {
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.IsOpen = true;
            }
        }

        private void TextBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenContextMenu(modeBlock);
        }

        private void IINmode_Click(object sender, RoutedEventArgs e)
        {
            SetFindMode(1);
            FindBox.Text = string.Empty;
            FindView.Visibility = Visibility.Hidden;
        }

        private void NAMEmode_Click(object sender, RoutedEventArgs e)
        {
            SetFindMode(0);
        }

        private void Window_KeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Escape)
            {
                if (FindBox.Text.Length != 0)
                {
                    FindBox.Text = string.Empty;
                    FindView.Visibility = Visibility.Hidden;
                }
                else
                {
                    Hide();
                }
            }
        }

        private void ClearButton_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ClearButton.IsEnabled)
            {
                ClearButton.Source = ImageSourceForBitmap(Properties.Resources.clear_hover);
            }
        }

        private void ClearButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (ClearButton.IsEnabled)
            {
                ClearButton.Source = ImageSourceForBitmap(Properties.Resources.clear);
            }
        }

        private void ClearButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            FindBox.Text = string.Empty;
            FindView.Visibility = Visibility.Hidden;
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
        }


        private void Window_Closed(object sender, EventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                Hide();
            }
        }

        private void EditButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (EditButton.IsEnabled)
            {
                EditButton.Source = ImageSourceForBitmap(Properties.Resources.edit_hover);
            }
        }

        private void EditButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (EditButton.IsEnabled)
            {
                EditButton.Source = ImageSourceForBitmap(Properties.Resources.edit);
            }
        }
        Editor editor;
        private void EditButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (editor == null)
            {
                editor = new Editor
                {
                    Tag = this
                };
                editor.Closed += Editor_Closed;
                editor.Show();
            }
            else
            {
                editor.Activate();
            }
        }

        private void Editor_Closed(object sender, EventArgs e)
        {
            editor = null;
            MM.FlushMemory();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (Settings.Default.HideOnClosing)
            {
                e.Cancel = true;
                Hide();
            }
            else
            {
                Application.Current.Shutdown();
            }
            if (WindowState == WindowState.Maximized)
            {
                Settings.Default.HeightWindow = RestoreBounds.Height;
                Settings.Default.WidthWindow = RestoreBounds.Width;
                Settings.Default.Maximized = true;
            }
            else
            {
                Settings.Default.HeightWindow = Height;
                Settings.Default.WidthWindow = Width;
                Settings.Default.Maximized = false;
            }

            Settings.Default.Save();
        }

        private void SearchButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if(FindBox.Text.Length > 0)
            {
                Find();
            }
        }

        private void ExitContext_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void OpenEditorContext_Click(object sender, RoutedEventArgs e)
        {
            if (editor == null)
            {
                editor = new Editor
                {
                    Tag = this
                };
                editor.Closed += Editor_Closed;
                editor.Show();
            }
            else
            {
                editor.Activate();
            }
        }

        private void AddItemContext_Click(object sender, RoutedEventArgs e)
        {
            AddItem();
        }
        SettingsWindow settingsWindow;
        private void SettingsButton_MouseUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow
                {
                    Tag = this
                };
                settingsWindow.Closed += SettingsWindow_Closed;
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Activate();
            }
        }

        private void SettingsWindow_Closed(object sender, EventArgs e)
        {
            settingsWindow = null;
            MM.FlushMemory();
        }

        private void OpenSettingsContext_Click(object sender, RoutedEventArgs e)
        {
            if (settingsWindow == null)
            {
                settingsWindow = new SettingsWindow
                {
                    Tag = this
                };
                settingsWindow.Closed += SettingsWindow_Closed;
                settingsWindow.Show();
            }
            else
            {
                settingsWindow.Activate();
            }
        }
        private void FindWindow_Activated(object sender, EventArgs e)
        {
            
        }
    }
}
