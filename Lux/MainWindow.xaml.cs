﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Lux.Graphics;

namespace Lux
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private GraphicsEngine LuxGraphicEngine = null;

        public MainWindow()
        {
            InitializeComponent();

            Width = 1600;
            Height = 900;
            Title = "Lux Engine Editor";
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            this.Activated += InitializeVulkan;
        }

        private void InitializeVulkan(object sender, EventArgs eventArgs)
        {
            try
            {
                LuxGraphicEngine = new GraphicsEngine();
                LuxGraphicEngine.Run(new WindowInteropHelper(this).Handle);
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
            }
        }
    }
}