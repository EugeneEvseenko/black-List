using Microsoft.Win32;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using WPFCustomMessageBox;

namespace Black_List
{
    /// <summary>
    /// Логика взаимодействия для RestoreBackup.xaml
    /// </summary>
    public partial class RestoreBackup : Window
    {
        public RestoreBackup()
        {
            InitializeComponent();
            logger = LogManager.GetCurrentClassLogger();
        }
        string ReserveCopyDir = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\Backups\";
        Logger logger;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(ReserveCopyDir))
                {
                    Directory.CreateDirectory(ReserveCopyDir);
                }
                foreach (string file in Directory.GetFiles(ReserveCopyDir))
                {
                    if (file.Substring(file.LastIndexOf(".")).Equals(".db"))
                    {
                        ListBases.Items.Add(file.Substring(file.LastIndexOf(@"\") + 1));
                        listPaths.Add(file);
                    }
                }
                if (ListBases.Items.Count == 0)
                {
                    InfoLabel.Content = "Резервные копии отсутствуют.";
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
        List<string> listPaths = new List<string>();
        public string OnRestoreDataBasePath = string.Empty;
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void ListBases_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if(ListBases.SelectedIndex != -1)
            {
                RestoreButton.IsEnabled = true;
                OnRestoreDataBasePath = listPaths[ListBases.SelectedIndex];
                InfoLabel.Content = OnRestoreDataBasePath;
                InfoLabel.ToolTip = OnRestoreDataBasePath;
            }
            else
            {
                OnRestoreDataBasePath = string.Empty;
                InfoLabel.Content = string.Empty;
                InfoLabel.ToolTip = string.Empty;
                RestoreButton.IsEnabled = false;
            }
        }
        string localDB = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName) + @"\BlackList.db";
        private void CustomSelectBase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.DefaultExt = ".db";
                openFileDialog.Filter = "База данных|*.db";
                Nullable<bool> result = openFileDialog.ShowDialog();
                if (result == true)
                {
                    string filename = openFileDialog.FileName;
                    if (filename.Equals(localDB))
                    {
                        CustomMessageBox.ShowOK("Эта база уже используется.", "Неудача!", "ОК", MessageBoxImage.Hand);
                    }
                    else
                    {
                        if (filename.Substring(filename.LastIndexOf(".")).Equals(".db"))
                        {
                            ListBases.Items.Add(filename.Substring(filename.LastIndexOf(@"\") + 1));
                            listPaths.Add(filename);
                        }
                    }
                }
            }catch(Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
