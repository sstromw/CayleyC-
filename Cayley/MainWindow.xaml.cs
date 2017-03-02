using System;
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
#if SAVE_DATA
        private DataReader data;
#endif

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();

            m = n = 1;
            txt_m.Text = m.ToString();
            txt_n.Text = n.ToString();

#if SAVE_DATA
            data = new DataReader();

            List<FoundItem> items = new List<FoundItem>();
            for (int i = 1; i <= Graph.MAX_VERTICES; i++)
            {
                items.Add(new FoundItem() { Order = i, Count = data.GetCount(i), Maximum = DataReader.GroupCounts[i-1] });
            }

            foundItemsControl.ItemsSource = items;
#endif
        }

        private void PointsButton_Click(object sender, RoutedEventArgs e)
        {
            canvas.AddShape(m, n);
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Group G = canvas.Graph.ToCayleyGraph();
            Storyboard story;
            ColorAnimation fade;

            if (G != null)
            {
                button.Background = Brushes.Green;
                display.Text = G.GroupDescription;

#if SAVE_DATA
                if (G.GroupID != -1 && !data.IsGroupFound(G.Order, G.GroupID))
                {
                    foundButton.Background = Brushes.Green;

                    story = new Storyboard();
                    fade = new ColorAnimation() { To = Colors.Transparent, Duration = TimeSpan.FromSeconds(2) };
                    Storyboard.SetTarget(fade, foundButton);
                    Storyboard.SetTargetProperty(fade, new PropertyPath("Background.Color"));
                    story.Children.Add(fade);
                    story.Begin();

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
#endif
            }
            else
            {
                button.Background = Brushes.Red;
                display.Text = string.Empty;
            }

            story = new Storyboard();
            fade = new ColorAnimation() { To = Colors.Transparent, Duration = TimeSpan.FromSeconds(2) };
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
#if SAVE_DATA
            foundGroupsViewer.Visibility = Visibility.Visible;
#endif
        }

        private void FoundButton_Leave(object sender, RoutedEventArgs e)
        {
#if SAVE_DATA
            foundGroupsViewer.Visibility = Visibility.Hidden;
#endif
        }

        private void FoundButton_MouseWheel(object sender, MouseWheelEventArgs e)
        {
#if SAVE_DATA
            foundGroupsViewer.ScrollToVerticalOffset(foundGroupsViewer.VerticalOffset - e.Delta);
            e.Handled = true;
#endif
        }

        private void SetColor(object sender, RoutedEventArgs e) { canvas.EdgeColor = ((Button)sender).Background; }
        private void RemoveColor(object sender, RoutedEventArgs e) { canvas.RemoveColor(((Button)sender).Background); }

#region Number selector

        private int m, n;

        private void cmdUp_Click_m(object sender, RoutedEventArgs e)
        {
            if (m < 10) m++;
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
