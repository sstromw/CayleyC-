﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Cayley
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DataReader data;

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();

            m = n = 1;
            txt_m.Text = m.ToString();
            txt_n.Text = n.ToString();

            data = new DataReader();

            List<FoundItem> items = new List<FoundItem>();
            for (int i = 1; i <= Graph.MAX_VERTICES; i++)
            {
                items.Add(new FoundItem() { Order = i, Count = data.GetCount(i), Maximum = DataReader.GroupCounts[i-1] });
            }

            foundItemsControl.ItemsSource = items;
        }

        private void PointsButton_Click(object sender, RoutedEventArgs e)
        {
            canvas.AddShape(m, n);
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Group G = canvas.Graph.ToCayleyGraph();

            if (G != null)
            {
                button.Background = Brushes.Green;
                display.Text = G.GroupName;
                
                if (G.GroupID != -1 && !data.IsGroupFound(G.Order, G.GroupID))
                {
                    data.AddGroup(G.Order, G.GroupID);
                    foreach (FoundItem f in foundItemsControl.ItemsSource)
                    {
                        if (f.Order == G.Order)
                        {
                            f.Count += 1;
                            break;
                        }
                    }
                }
            }
            else
            {
                button.Background = Brushes.Red;
                display.Text = string.Empty;
            }

            Storyboard story = new Storyboard();
            ColorAnimation fade = new ColorAnimation() { To = Colors.Transparent, Duration = TimeSpan.FromSeconds(2) };
            Storyboard.SetTarget(fade, button);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Background.Color"));
            story.Children.Add(fade);
            story.Begin();
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            canvas.ClearAll();
            display.Text = string.Empty;
        }

        private void QuitButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void FoundButton_Enter (object sender, RoutedEventArgs e)
        {
            foundGroupsViewer.Visibility = Visibility.Visible;
        }

        private void FoundButton_Leave(object sender, RoutedEventArgs e)
        {
            foundGroupsViewer.Visibility = Visibility.Hidden;
        }

        private void FoundButton_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            foundGroupsViewer.ScrollToVerticalOffset(foundGroupsViewer.VerticalOffset - e.Delta);
            e.Handled = true;
        }

        private void SetBlack(object sender, RoutedEventArgs e) { canvas.EdgeColor = Brushes.Black; }
        private void SetRed(object sender, RoutedEventArgs e) { canvas.EdgeColor = Brushes.Red; }
        private void SetBlue(object sender, RoutedEventArgs e) { canvas.EdgeColor = Brushes.Blue; }
        private void SetGreen(object sender, RoutedEventArgs e) { canvas.EdgeColor = Brushes.Green; }
        private void SetOrange(object sender, RoutedEventArgs e) { canvas.EdgeColor = Brushes.Orange; }

        #region Number picker

        private int m, n;

        private void cmdUp_Click_m(object sender, RoutedEventArgs e)
        {
            if (m < 8) m++;
            txt_m.Text = m.ToString();
        }

        private void cmdDown_Click_m(object sender, RoutedEventArgs e)
        {
            if (m > 1) m--;
            txt_m.Text = m.ToString();
        }

        private void cmdUp_Click_n(object sender, RoutedEventArgs e)
        {
            if (n < Graph.MAX_VERTICES) n++;
            txt_n.Text = n.ToString();

        }

        private void cmdDown_Click_n(object sender, RoutedEventArgs e)
        {
            if (n > 1) n--;
            txt_n.Text = n.ToString();
        }

        #endregion
    }

    public class FoundItem : INotifyPropertyChanged
    {
        int count;

        public int Order { get; set; }
        public int Maximum { get; set; }
        public double Width { get { return 10.38 * Maximum; } }
        public int Count
        {
            get { return count; }
            set { if (count != value) { count = value; OnPropertyChanged(new PropertyChangedEventArgs("Count")); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }
    }
}