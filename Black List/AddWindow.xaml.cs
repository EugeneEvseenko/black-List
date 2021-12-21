using NLog;
using System;
using System.Collections.Generic;
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

namespace Black_List
{
    
    public partial class AddWindow : Window
    {
        public Human Humand { get; set; }
        public AddWindow(Human h)
        {
            InitializeComponent();
            logger = LogManager.GetCurrentClassLogger();
            Humand = h;
            this.DataContext = Humand;

        }
        Logger logger;
        private static readonly Regex _regex = new Regex("[^0-9]+"); //regex that matches disallowed text
        private static bool IsTextAllowed(string text)
        {
            return !_regex.IsMatch(text);
        }
        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void DataUpdated()
        {
            Humand.FindString = FIObox.Text.ToLower();
            Humand.Note = NoteBox.Text;
            if(FIObox.Text.Length > 0)
            {
                if (!string.IsNullOrWhiteSpace(FIObox.Text))
                {
                    acceptButton.IsEnabled = true;
                    NotifyBlock.Text = string.Empty;
                }
                else
                {
                    acceptButton.IsEnabled = false;
                    NotifyBlock.Text = "Поле 'ФИО' не может состоять только из пробелов";
                }
            }
            else
            {
                acceptButton.IsEnabled = false;
                NotifyBlock.Text = "Поле 'ФИО' не может быть пустым.";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            DataUpdated();
        }

        

        private void clearButton_Click(object sender, RoutedEventArgs e)
        {
            Smoker.IsChecked = false;
            Notpay.IsChecked = false;
            Thief.IsChecked = false;
            Oralo.IsChecked = false;
            Fury.IsChecked = false;
            FIObox.Text = string.Empty;
            IINbox.Text = string.Empty;
            NoteBox.Text = string.Empty;
            DateBox.Text = string.Empty;

        }

        private void DateBox_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DateBox.SelectedDate != null)
            {
                Humand.DateOfBorn = DateBox.SelectedDate.Value.ToShortDateString();
            }
            else
            {
                Humand.DateOfBorn = string.Empty;
            }
        }

        private void IINbox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsTextAllowed(e.Text))
            {
                e.Handled = true;
                NotifyBlock.Text = "В поле 'ИИН' используются только цифры.";
            }
            else
            {
                DataUpdated();
            }
        }

        private void IINbox_TextChanged(object sender, TextChangedEventArgs e)
        {
            IINbox.Text = IINbox.Text.Replace(" ", string.Empty);
            IINbox.Select(IINbox.Text.Length, 0); 
        }
        
        public void CheckNotePresets()
        {
            if (NoteBox.Text.Length != 0)
            {
                if (Smoker.IsChecked == true && !NoteBox.Text.Contains(", курильщик") && !NoteBox.Text.Contains("Курильщик"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, ", курильщик");
                }
                if (Notpay.IsChecked == true && !NoteBox.Text.Contains(", неплательщик") && !NoteBox.Text.Contains("Неплательщик"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, ", неплательщик");
                }
                if (Thief.IsChecked == true && !NoteBox.Text.Contains(", вор") && !NoteBox.Text.Contains("Вор"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, ", вор");
                }
                if (Oralo.IsChecked == true && !NoteBox.Text.Contains(", скандалит") && !NoteBox.Text.Contains("Скандалит"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, ", скандалит");
                }
                if (Fury.IsChecked == true && !NoteBox.Text.Contains(", агрессивен") && !NoteBox.Text.Contains("Агрессивен"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, ", агрессивен");
                }
            }
            else
            {
                if (Smoker.IsChecked == true && !NoteBox.Text.Contains("Курильщик"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, "Курильщик");
                }
                if (Notpay.IsChecked == true && !NoteBox.Text.Contains("Неплательщик"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, "Неплательщик");
                }
                if (Thief.IsChecked == true && !NoteBox.Text.Contains("Вор"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, "Вор");
                }
                if (Oralo.IsChecked == true && !NoteBox.Text.Contains("Скандалит"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, "Скандалит");
                }
                if (Fury.IsChecked == true && !NoteBox.Text.Contains("Агрессивен"))
                {
                    NoteBox.Text = NoteBox.Text.Insert(NoteBox.Text.Length, "Агрессивен");
                }
            }
        }
        public void UncheckNotePresets()
        {
            if (Smoker.IsChecked == false && NoteBox.Text.Contains(", курильщик"))
            {
                NoteBox.Text = NoteBox.Text.Replace(", курильщик", string.Empty);
            }
            if (Smoker.IsChecked == false && NoteBox.Text.Contains("Курильщик"))
            {
                NoteBox.Text = NoteBox.Text.Replace("Курильщик", string.Empty);
            }
            if (Notpay.IsChecked == false && NoteBox.Text.Contains(", неплательщик"))
            {
                NoteBox.Text = NoteBox.Text.Replace(", неплательщик", string.Empty);
            }
            if (Notpay.IsChecked == false && NoteBox.Text.Contains("Неплательщик"))
            {
                NoteBox.Text = NoteBox.Text.Replace("Неплательщик", string.Empty);
            }
            if (Thief.IsChecked == false && NoteBox.Text.Contains(", вор"))
            {
                NoteBox.Text = NoteBox.Text.Replace(", вор", string.Empty);
            }
            if (Thief.IsChecked == false && NoteBox.Text.Contains("Вор"))
            {
                NoteBox.Text = NoteBox.Text.Replace("Вор", string.Empty);
            }
            if (Oralo.IsChecked == false && NoteBox.Text.Contains(", скандалит"))
            {
                NoteBox.Text = NoteBox.Text.Replace(", скандалит", string.Empty);
            }
            if (Oralo.IsChecked == false && NoteBox.Text.Contains("Скандалит"))
            {
                NoteBox.Text = NoteBox.Text.Replace("Скандалит", string.Empty);
            }
            if (Fury.IsChecked == false && NoteBox.Text.Contains(", агрессивен"))
            {
                NoteBox.Text = NoteBox.Text.Replace(", агрессивен", string.Empty);
            }
            if (Fury.IsChecked == false && NoteBox.Text.Contains("Агрессивен"))
            {
                NoteBox.Text = NoteBox.Text.Replace("Агрессивен", string.Empty);
            }
        }
        private void Smoker_Checked(object sender, RoutedEventArgs e)
        {
            CheckNotePresets();
        }

        private void Smoker_Unchecked(object sender, RoutedEventArgs e)
        {
            UncheckNotePresets();
        }

        private void NoteBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            DataUpdated();
        }

        private void DataUpdated(object sender, TextChangedEventArgs e)
        {
            DataUpdated();
        }
    }
}
