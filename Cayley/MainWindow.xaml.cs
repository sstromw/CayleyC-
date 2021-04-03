using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private Menu mainMenu;

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();

            m = n = 1;
            txt_m.Text = m.ToString();
            txt_n.Text = n.ToString();
        }

        private void PointsButton_Click(object sender, RoutedEventArgs e)
        {
            if (m * n > Graph.MAX_VERTICES) ButtonColorFlash((Button)sender, Brushes.Red);
            canvas.AddShape(m, n);
        }

        private void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            Group G = canvas.Graph.ToCayleyGraph();
            
            if (G != null)
            {
                ButtonColorFlash(button, Brushes.Green);
                display.Text = G.ToString();
            }
            else
            {
                ButtonColorFlash(button, Brushes.Red);
                display.Text = string.Empty;
            }
        }

        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            canvas.ClearAll();
            display.Text = string.Empty;
        }

        /*
        private void MenuButton_Click(object sender, RoutedEventArgs e)
        {
            pop.IsOpen ^= true;
        }
        */

        private void QuitButton_Click(object sender, RoutedEventArgs e) { Close(); }

        private void SetColor(object sender, RoutedEventArgs e) { canvas.EdgeColor = ((Button)sender).Background; }
        private void RemoveColor(object sender, RoutedEventArgs e) { canvas.RemoveColor(((Button)sender).Background); }

        private void ButtonColorFlash(Button button, Brush brush)
        {
            button.Background = brush;
            Storyboard story = new Storyboard();
            ColorAnimation fade = new ColorAnimation() { To = Colors.Transparent, Duration = TimeSpan.FromSeconds(2) };
            Storyboard.SetTarget(fade, button);
            Storyboard.SetTargetProperty(fade, new PropertyPath("Background.Color"));
            story.Children.Add(fade);
            story.Begin();
        }

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
}
