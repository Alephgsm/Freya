using Freya.Util;
using Microsoft.Win32;
using SharpOdinClient;
using SharpOdinClient.util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Freya.Util.Util;

namespace Freya.Controls
{
    /// <summary>
    /// Interaction logic for FlashField.xaml
    /// </summary>
    public partial class FlashField : UserControl
    {
        public List<FileFlash> FlashFile = new List<FileFlash>();
        public List<FileFlash> Files
        {
            get
            {
                if (FlashFile.Count <= 0)
                {
                    return FlashFile;
                }
                var items = CmbBxListFile.Items.OfType<FileFlash>().ToList();
                return items;
            }
        }

        public ListCollectionView view;
        public string Package;
        public FlashField(string package)
        {
            InitializeComponent();
            this.Package = package;
            view = new ListCollectionView(FlashFile);
            view.IsLiveFiltering = true;
            view.IsLiveSorting = true;
            CmbBxListFile.ItemsSource = view;
            BtnClear.Visibility = Visibility.Collapsed;
            txtSelectTeam.Text = $"{Package} ";
            view.Refresh();
        }

        public bool Exist(cListFileData File)
        {
            foreach (var item in FlashFile)
            {
                if (item.FileName == File.Filename)
                {
                    return true;
                }
            }
            return false;
        }
        public event PitDetectDelegate PitDetect;
        private void BtnChooseFile_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".tar",
                Filter = "samsung firmware|*.tar;*.md5;*.limra"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                BtnClear_Click(sender, e);
                string filename = dlg.FileName;
                var odin = new Odin();
                var item = odin.tar.TarInformation(filename);
                if (item.Count > 0)
                {
                    foreach (var Tiem in item)
                    {
                        if (!Exist(Tiem))
                        {
                            var Extension = System.IO.Path.GetExtension(Tiem.Filename);
                            var file = new FileFlash
                            {
                                Enable = true,
                                FileName = Tiem.Filename,
                                FilePath = filename
                            };

                            if (Extension == ".pit")
                            {
                                PitDetect?.Invoke(Tiem.Filename, filename);
                                continue;
                            }
                            else if (Extension == ".lz4")
                            {
                                file.RawSize = odin.CalculateLz4SizeFromTar(filename, Tiem.Filename);
                            }
                            else
                            {
                                file.RawSize = Tiem.Filesize;
                            }
                            FlashFile.Add(file);
                        }
                    }
                    if (CmbBxListFile.Items.Count > 0)
                    {
                        BtnClear.Visibility = Visibility.Visible;
                        txtSelectTeam.Text = filename;
                    }
                    view.Refresh();

                }

            }
        }

        private void BtnClear_Click(object sender, RoutedEventArgs e)
        {
            FlashFile.Clear();
            view.Refresh();
            txtSelectTeam.Text = $"Select {Package} Package";
            BtnClear.Visibility = Visibility.Collapsed;

        }

        private void CmbBxListFile_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CmbBxListFile.SelectedItem = null;

        }
    }
}
