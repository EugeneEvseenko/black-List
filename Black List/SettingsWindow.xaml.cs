using Black_List.Properties;
using Microsoft.Win32;
using NLog;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using WPFCustomMessageBox;

namespace Black_List
{
    /// <summary>
    /// Логика взаимодействия для Settings.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            logger = LogManager.GetCurrentClassLogger();
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings.Default.Autorun = (bool)AutorunCheckbox.IsChecked;
            Settings.Default.HideStart = (bool)HideOnStartCheckbox.IsChecked;
            Settings.Default.HideOnClosing = (bool)HideOnClosingCheckbox.IsChecked;
            Settings.Default.Save();
            SetAutorun(Settings.Default.Autorun);
            
            Close();
        }
        private void OpenContextMenu(FrameworkElement element)
        {
            if (element.ContextMenu != null)
            {
                element.ContextMenu.PlacementTarget = element;
                element.ContextMenu.IsOpen = true;
            }
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
        string ReserveCopyDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Backups\";
        string localDB = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\BlackList.db";
        Logger logger;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            AutorunCheckbox.IsChecked = Settings.Default.Autorun;
            HideOnStartCheckbox.IsChecked = Settings.Default.HideStart;
            HideOnClosingCheckbox.IsChecked = Settings.Default.HideOnClosing;
        }
        
        

        private void backupBlock_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OpenContextMenu(backupBlock);
        }

        private void CreateDB_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(ReserveCopyDir))
                {
                    Directory.CreateDirectory(ReserveCopyDir);
                }
                if (File.Exists(localDB))
                {
                    string backupname = ReserveCopyDir + "BlackList_" + DateTime.Now.ToShortDateString() + ".db";
                    if (File.Exists(backupname))
                    {
                        switch(CustomMessageBox.ShowYesNoCancel("База данных с именем '" + backupname.Substring(backupname.LastIndexOf(@"\") + 1) + "' уже существует. Перезаписать ее или сохранить обе копии?",
                            "База данных уже существует!", "Перезаписать", "Оставить обе", "Отмена", MessageBoxImage.Question))
                        {
                            case MessageBoxResult.Yes:
                                {
                                    File.Copy(localDB, backupname, true);
                                }
                                break;
                            case MessageBoxResult.No:
                                {
                                    int number = 1;
                                    string newName = backupname.Insert(backupname.LastIndexOf("."), "_" + number.ToString());
                                    while (File.Exists(newName))
                                    {
                                        number++;
                                        if (number != 10)
                                        {
                                            newName = backupname.Insert(backupname.LastIndexOf("."), "_" + number.ToString());
                                        }
                                        else
                                        {
                                            CustomMessageBox.ShowOK("Вы превысили лимит создания резервных копий на день, удалите лишние резервные копии за сегодня.", "Лимит превышен!", "OK", MessageBoxImage.Exclamation);
                                            break;
                                        }
                                    }
                                    if (number != 10)
                                    {
                                        File.Copy(localDB, newName, true);
                                        if (File.Exists(newName))
                                        {
                                            CustomMessageBox.ShowOK("Резервная копия базы данных " + newName.Substring(newName.LastIndexOf(@"\") + 1) + " успешно создана.", "Копия создана", "Хорошо", MessageBoxImage.Information);
                                        }
                                        else
                                        {
                                            CustomMessageBox.ShowOK("Резервная копия базы данных " + newName.Substring(newName.LastIndexOf(@"\") + 1) + " не была создана.", "Копия не создана", "Хорошо", MessageBoxImage.Error);
                                        }
                                    }
                                }
                                break;
                        }
                    }
                    else
                    {
                        File.Copy(localDB, backupname, true);
                        if (File.Exists(backupname))
                        {
                            CustomMessageBox.ShowOK("Резервная копия базы данных " + backupname.Substring(backupname.LastIndexOf(@"\") + 1) + " успешно создана.", "Копия создана", "Хорошо", MessageBoxImage.Information);
                        }
                        else
                        {
                            CustomMessageBox.ShowOK("Резервная копия базы данных " + backupname.Substring(backupname.LastIndexOf(@"\") + 1) + " не была создана.", "Копия не создана", "Хорошо", MessageBoxImage.Error);
                        }
                    }
                }
                else
                {
                    CustomMessageBox.ShowOK("База данных не найдена!", "Для создания резервной копии базы данных её необходимо сначала создать.", "Хорошо", MessageBoxImage.Exclamation);
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void OpenDir_Click(object sender, RoutedEventArgs e)
        {
            if (!Directory.Exists(ReserveCopyDir))
            {
                Directory.CreateDirectory(ReserveCopyDir);
            }
            Process.Start(ReserveCopyDir);
        }
        Helper helper = new Helper();
        private void RemoveAll_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(ReserveCopyDir) && Directory.GetFiles(ReserveCopyDir).Length == 0)
                {
                    Directory.CreateDirectory(ReserveCopyDir);
                }
                else
                {
                    if (Directory.GetFiles(ReserveCopyDir).Length > 0)
                    {
                        if (CustomMessageBox.ShowYesNo("Вы хотите безвозвратно удалить " +
                            Directory.GetFiles(ReserveCopyDir).Length.ToString() +
                            helper.getEnding(Directory.GetFiles(ReserveCopyDir).Length, "файлов", "файл", "файла")
                            + "?",
                            "Подтвердите удаление",
                            "Да",
                            "Нет",
                            MessageBoxImage.Question) == MessageBoxResult.Yes)
                        {
                            foreach (string file in Directory.GetFiles(ReserveCopyDir))
                            {
                                File.Delete(file);
                            }
                        }
                    }
                    else
                    {

                    }
                }
            } catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void RestoreDase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                RestoreBackup restoreBackup = new RestoreBackup();
                if (restoreBackup.ShowDialog() == true)
                {
                    string restorePath = restoreBackup.OnRestoreDataBasePath;
                    restoreBackup = null;
                    if (restorePath != string.Empty)
                    {
                        ((MainWindow)this.Tag).SQLconnection.Close();
                        File.Copy(restorePath, localDB, true);
                        ((MainWindow)this.Tag).ConnectBase();
                        CustomMessageBox.ShowOK("База " + restorePath.Substring(restorePath.LastIndexOf(@"\") + 1) + " успешно восстановлена", "", "Отлично!", MessageBoxImage.Information);
                    }
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
