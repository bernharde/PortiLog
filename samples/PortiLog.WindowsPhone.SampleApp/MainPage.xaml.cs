using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;
using PortiLog.WindowsPhone.SampleApp.Resources;

namespace PortiLog.WindowsPhone.SampleApp
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();

            this.DataContext = ListenerViewModel.Current;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        void LogSingleEntryWithCategoryButton_Click(object sender, RoutedEventArgs e)
        {
            ListenerViewModel.Current.LogSingleEntryWithCategory();
        }

        void Log150EntriesButton_Click(object sender, RoutedEventArgs e)
        {
            ListenerViewModel.Current.LogWrite150Entries();
        }

        void NavigateToDumpServiceUrlButton_Click(object sender, RoutedEventArgs e)
        {
            ListenerViewModel.Current.StartNavigateToDumpServiceUrl();
        }

        void TestDumpServiceButton_Click(object sender, RoutedEventArgs e)
        {
            ListenerViewModel.Current.StartTestDumpService();
        }

        void ViewLogFileButton_Click(object sender, RoutedEventArgs e)
        {
            ListenerViewModel.Current.ViewLogFileAsync();
        }
    }
}