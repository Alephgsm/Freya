using Freya.Util;
using Microsoft.Win32;
using SharpOdinClient;
using System;
using System.Collections.Generic;
using System.IO;
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
using static SharpOdinClient.util.utils;

namespace Freya.Controls
{
    /// <summary>
    /// Interaction logic for Flash.xaml
    /// </summary>
    public partial class Flash : UserControl
    {
        public FlashField BLPackage = new FlashField("BL (bootloader) file package [tar,md5]");
        public FlashField APPackage = new FlashField("AP (PDA) file package [tar,md5]");
        public FlashField CPPackage = new FlashField("CP (Modem) file package [tar,md5]");
        public FlashField CSCPackage = new FlashField("CSC file package [tar,md5]");
        public Odin Odin = new Odin();

        public event ProgressChangedDelegate ProgressChanged;
        public event LogDelegate Log;
        public event IsRunningProcessDelegate IsRunning;

        public Flash()
        {
            InitializeComponent();

            Features.Children.Add(BLPackage);
            Features.Children.Add(APPackage);
            Features.Children.Add(CPPackage);
            Features.Children.Add(CSCPackage);
            BLPackage.PitDetect += BLPackage_PitDetect;
            APPackage.PitDetect += BLPackage_PitDetect;
            CPPackage.PitDetect += BLPackage_PitDetect;
            CSCPackage.PitDetect += BLPackage_PitDetect;

            RepartitionCheckBx.IsEnabled = false;
            BtnClearPit.Visibility = Visibility.Collapsed;
            Odin.Log += Odin_Log; ;
            Odin.ProgressChanged += Odin_ProgressChanged; 

        }


        public void ControlsManage(bool IsEnable)
        {
            BLPackage.IsEnabled = !IsEnable;
            APPackage.IsEnabled = !IsEnable;
            CPPackage.IsEnabled = !IsEnable;
            CSCPackage.IsEnabled = !IsEnable;
            AutoBoot.IsEnabled = !IsEnable;
            BootUpdate.IsEnabled = !IsEnable;
            EfsClear.IsEnabled = !IsEnable;
            TxtBxPit.IsEnabled = !IsEnable;
            BtnClearPit.IsEnabled = !IsEnable;
            BtnChoosePit.IsEnabled = !IsEnable;
            BtnFlash.IsEnabled = !IsEnable;
            BtnReadPit.IsEnabled = !IsEnable;
        }

        private void Odin_Log(string Text, MsgType Color, bool IsError = false)
        {
            Log?.Invoke(Text, Color, IsError);
        }

        private void Odin_ProgressChanged(string filename, long max, long value, long WritenSize)
        {
            ProgressChanged?.Invoke(filename, max, value, WritenSize);
        }
        private void BtnChoosePit_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                DefaultExt = ".tar",
                Filter = "Csc Or Pit file|*.tar;*.md5;*.pit"
            };
            bool? result = dlg.ShowDialog();
            if (result == true)
            {
                TxtBxPit.Clear();
                string filename = dlg.FileName;
                var ext = System.IO.Path.GetExtension(filename);
                if (ext == ".pit")
                {
                    BLPackage_PitDetect(System.IO.Path.GetFileName(filename), filename);
                }
                else
                {
                    var item = this.Odin.tar.TarInformation(filename);
                    if (item.Count > 0)
                    {
                        foreach (var Tiem in item)
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
                                BLPackage_PitDetect(Tiem.Filename, filename);
                                return;
                            }
                        }
                    }

                }


            }
        }
        private void BtnClearPit_Click(object sender, RoutedEventArgs e)
        {
            TxtBxPit.Clear();
            RepartitionCheckBx.IsChecked = false;
        }
        private async void BLPackage_PitDetect(string pitname, string path)
        {
            RepartitionCheckBx.IsChecked = false;
            var extension = System.IO.Path.GetExtension(path);
            if (extension.ToLower() == ".pit")
            {
                if (Odin.PitTool.UNPACK_PIT(File.ReadAllBytes(path)))
                {
                    TxtBxPit.Text = path;
                    RepartitionCheckBx.IsChecked = true;
                }
                else
                {
                    Log?.Invoke($"File Curreped : {pitname}", MsgType.Message , true);
                    return;
                }
            }
            else
            {
                var pit = await Odin.tar.ExtractFileFromTar(path, pitname);
                if (pit.Length == 0 || !Odin.PitTool.UNPACK_PIT(pit))
                {
                    Log?.Invoke($"File Curreped : {pitname}", MsgType.Message, true);
                    return;
                }
                else
                {
                    TxtBxPit.Text = path;
                    RepartitionCheckBx.IsChecked = true;
                }
            }
        }

        public async Task DoFlash(long Size, List<FileFlash> ListFlash)
        {
            var list = new List<SharpOdinClient.structs.FileFlash>();
            foreach(var i in ListFlash)
            {
                list.Add(new SharpOdinClient.structs.FileFlash
                {
                    Enable = i.Enable,
                    FileName = i.FileName,
                    FilePath = i.FilePath,
                    RawSize = i.RawSize
                });

            }
            if (!await Odin.FindAndSetDownloadMode())
            {
                return;
            }
            await Odin.PrintInfo();
            Log?.Invoke("Checking Download Mode : ", MsgType.Message);
            if (await Odin.IsOdin())
            {
                Log?.Invoke("ODIN", MsgType.Result);
                Log?.Invoke($"Initializing Device : ", MsgType.Message);
                if (await Odin.LOKE_Initialize(Size))
                {
                    Log?.Invoke("Initialized", MsgType.Result);
                    if (!string.IsNullOrEmpty(TxtBxPit.Text) && RepartitionCheckBx.IsChecked == true )
                    {
                        Log?.Invoke("Repartition Device : ", MsgType.Message);
                        var Repartition = await Odin.Write_Pit(TxtBxPit.Text);
                        if (Repartition.status)
                        {
                            Log?.Invoke("Ok", MsgType.Result);
                        }
                        else
                        {
                            Log?.Invoke(Repartition.error, MsgType.Result , true);
                            return;
                        }
                    }

                    Log?.Invoke("Reading Pit from device : ", MsgType.Message);
                    var GetPit = await Odin.Read_Pit();
                    if (GetPit.Result)
                    {
                        Log?.Invoke("Ok", MsgType.Result );
                        var EfsClearInt = 0;
                        var BootUpdateInt = 0;
                        if (EfsClear.IsChecked == true)
                        {
                            EfsClearInt++;
                        }
                        if (BootUpdate.IsChecked == true)
                        {
                            BootUpdateInt++;
                        }
                        if (await Odin.FlashFirmware(list, GetPit.Pit, EfsClearInt, BootUpdateInt, true))
                        {
                            if (AutoBoot.IsChecked == true)
                            {
                                Log?.Invoke("Rebooting Device To Normal Mode : ", MsgType.Message);
                                if (await Odin.PDAToNormal())
                                {
                                    Log?.Invoke("Ok", MsgType.Result );
                                }
                                else
                                {
                                    Log?.Invoke("Failed", MsgType.Result , true);
                                }
                            }
                            else
                            {
                                Log?.Invoke("Auto Reboot Disabled Try Manual", MsgType.Message);
                            }

                        }
                    }
                    else
                    {
                        Log?.Invoke("Failed", MsgType.Result , true);
                    }
                }
                else
                {
                    Log?.Invoke("Failed", MsgType.Result , true);
                }
            }
            else
            {
                Log?.Invoke("Failed", MsgType.Result , true);
            }
        }

        private async void BtnFlash_Click(object sender, RoutedEventArgs e)
        {
            try
            {
               
                IsRunning?.Invoke(true, "Flash");
                var ListFlash = new List<FileFlash>();
                ListFlash.AddRange(BLPackage.Files);
                ListFlash.AddRange(APPackage.Files);
                ListFlash.AddRange(CPPackage.Files);
                ListFlash.AddRange(CSCPackage.Files);
                if (ListFlash.Count > 0)
                {
                    Log?.Invoke("Calculated Size : ", MsgType.Message);
                    var Size = 0L;
                    foreach (var item in ListFlash)
                    {
                        Size += item.RawSize;
                    }
                    if (Size > 0)
                    {
                        Log?.Invoke(Util.Util.GetBytesReadable(Size),MsgType.Result );
                        await DoFlash(Size, ListFlash);
                    }
                    else
                    {
                        Log?.Invoke("Failed",MsgType.Result , true);
                    }
                }
                else if (!string.IsNullOrEmpty(TxtBxPit.Text) && RepartitionCheckBx.IsChecked == true)
                {
                    await DoFlash(0, ListFlash);
                }
                else
                {
                    Log?.Invoke("Please Select Firmware Package and try again", MsgType.Message);
                }

            }
            catch (Exception ee)
            {
                Log?.Invoke($"System Error : ", MsgType.Message);
                Log?.Invoke(ee.Message, MsgType.Result , true);

            }
            finally
            {
                IsRunning?.Invoke(false, "Flash");
            }
        }

        private void TxtBxPit_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(TxtBxPit.Text))
            {
                RepartitionCheckBx.IsEnabled = false;
                BtnClearPit.Visibility = Visibility.Collapsed;
            }
            else
            {
                RepartitionCheckBx.IsEnabled = true;
                BtnClearPit.Visibility = Visibility.Visible;
            }

        }

        private async void BtnReadPit_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await Odin.FindAndSetDownloadMode())
                {
                    return;
                }
                IsRunning?.Invoke(true, "ReadPit");
                await Odin.PrintInfo();
                Log?.Invoke("Checking Download Mode : ", MsgType.Message);
                if (await Odin.IsOdin())
                {
                    Log?.Invoke("ODIN",MsgType.Result );
                    Log?.Invoke("Initializing Device : ", MsgType.Message);
                    if (await Odin.LOKE_Initialize(0))
                    {
                        Log?.Invoke("Initialized",MsgType.Result );
                        Log?.Invoke("Reading Pit : ", MsgType.Message);
                        var GetPit = await Odin.Read_Pit();
                        if (GetPit.Result)
                        {
                            Log?.Invoke("Ok",MsgType.Result );
                            Log?.Invoke($"SavedPath : ", MsgType.Message);
                            var fpath = $"{Util.Util.MyPath}\\backup\\samsung\\pit\\{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.pit";
                            Util.Util.CreatFolder(System.IO.Path.GetDirectoryName(fpath));
                            System.IO.File.WriteAllBytes(fpath, GetPit.data);
                            Log?.Invoke(fpath,MsgType.Result );
                            if (AutoBoot.IsChecked == true)
                            {
                                Log?.Invoke("Rebooting Device To Normal Mode : ", MsgType.Message);
                                if (await Odin.PDAToNormal())
                                {
                                    Log?.Invoke("Ok",MsgType.Result );
                                }
                                else
                                {
                                    Log?.Invoke("Failed", MsgType.Result , true);
                                }
                            }
                            else
                            {
                                Log?.Invoke("Auto Reboot Disabled Try Manual", MsgType.Message);
                            }
                        }
                        else
                        {
                            Log?.Invoke("Failed", MsgType.Result , true);
                        }
                    }
                    else
                    {
                        Log?.Invoke("Failed", MsgType.Result , true);
                    }
                }
                else
                {
                    Log?.Invoke("Failed", MsgType.Result , true);
                }
            }
            catch (Exception ee)
            {
                Log?.Invoke($"System Error : ", MsgType.Message);
                Log?.Invoke(ee.Message, MsgType.Result , true);

            }
            finally
            {
                IsRunning?.Invoke(false, "ReadPit");
            }
        }
    }
}
