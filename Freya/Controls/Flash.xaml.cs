using Freya.Util;
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
using static SharpOdinClient.util.utils;

namespace Freya.Controls
{
    /// <summary>
    /// Interaction logic for Flash.xaml
    /// </summary>
    public partial class Flash : UserControl
    {
        public FlashField BLPackage = new FlashField("BL");
        public FlashField APPackage = new FlashField("AP");
        public FlashField CPPackage = new FlashField("CP");
        public FlashField CSCPackage = new FlashField("CSC");
        public Odin Odin = new Odin();
        public event ProgressChangedDelegate ProgressChanged;
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
            Odin.Log += Odin_Log;
            Odin.ProgressChanged += Odin_ProgressChanged; 

        }
        private void Odin_ProgressChanged(string filename, long max, long value, long WritenSize)
        {
            ProgressChanged?.Invoke(filename, max, value, WritenSize);
        }

        private void Odin_Log(string Text, SharpOdinClient.util.utils.MsgType Color)
        {
            throw new NotImplementedException();
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
                    log?.Invoke($"File Curreped : {pitname}", true, RichColor.Yellow, RichFont.Bold);
                    return;
                }
            }
            else
            {
                var pit = await Odin.tar.ExtractFileFromTar(path, pitname);
                if (pit.Length == 0 || !Odin.PitTool.UNPACK_PIT(pit))
                {
                    log?.Invoke(Cls.GetKey("FileCurreped") + $" : {pitname}", true, RichColor.Yellow, RichFont.Bold);
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
            log?.Invoke("Checking Download Mode : ", true, RichColor.Lime, RichFont.Bold);
            if (await Odin.IsOdin())
            {
                log?.Invoke("ODIN", false, RichColor.Cyan, RichFont.Bold);
                log?.Invoke($"{Cls.GetKey("InitializingDevice")} : ", true, RichColor.Lime, RichFont.Bold);
                if (await Odin.LOKE_Initialize(Size))
                {
                    log?.Invoke(Cls.GetKey("Initialized"), false, RichColor.Cyan, RichFont.Bold);
                    if (!string.IsNullOrEmpty(TxtBxPit.Text) && RepartitionCheckBx.IsChecked == true )
                    {
                        log?.Invoke(Cls.GetKey("RepartitionDevice"), true, RichColor.Lime, RichFont.Bold);
                        var Repartition = await Odin.Write_Pit(TxtBxPit.Text);
                        if (Repartition.status)
                        {
                            log?.Invoke(Cls.GetKey("Ok"), false, RichColor.Cyan, RichFont.Bold);
                        }
                        else
                        {
                            log?.Invoke(Repartition.error, false, RichColor.Red, RichFont.Bold);
                            return;
                        }
                    }

                    log?.Invoke(Cls.GetKey("ReadingPit"), true, RichColor.Lime, RichFont.Bold);
                    var GetPit = await Odin.Read_Pit();
                    if (GetPit.Result)
                    {
                        log?.Invoke(Cls.GetKey("Ok"), false, RichColor.Cyan, RichFont.Bold);
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
                                log?.Invoke(Cls.GetKey("RebootingDeviceToNormalMode"), true, RichColor.Lime, RichFont.Bold);
                                if (await Odin.PDAToNormal())
                                {
                                    log?.Invoke(Cls.GetKey("Ok"), false, RichColor.Cyan, RichFont.Bold);
                                }
                                else
                                {
                                    log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                                }
                            }
                            else
                            {
                                log?.Invoke(Cls.GetKey("AutoRebootDisabledTryManual"), true, RichColor.Yellow, RichFont.Bold);
                            }

                        }
                    }
                    else
                    {
                        log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                    }
                }
                else
                {
                    log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                }
            }
            else
            {
                log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
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
                    log?.Invoke(Cls.GetKey("CalculatedSize"), true, RichColor.Lime, RichFont.Regulator);
                    var Size = 0L;
                    foreach (var item in ListFlash)
                    {
                        Size += item.RawSize;
                    }
                    if (Size > 0)
                    {
                        log?.Invoke(Cls.GetBytesReadable(Size), false, RichColor.Cyan, RichFont.Bold);
                        await DoFlash(Size, ListFlash);
                    }
                    else
                    {
                        log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Yellow, RichFont.Bold);
                    }
                }
                else if (!string.IsNullOrEmpty(TxtBxPit.Text) && RepartitionCheckBx.IsChecked == true)
                {
                    await DoFlash(0, ListFlash);
                }
                else
                {
                    log?.Invoke(Cls.GetKey("PleaseSelectFirmwarePackage"), true, RichColor.Yellow, RichFont.Bold);
                }

            }
            catch (Exception ee)
            {
                log?.Invoke($"{Cls.GetKey("SystemError")} : ", true, RichColor.Lime, RichFont.Bold);
                log?.Invoke(ee.Message, false, RichColor.Red, RichFont.Bold);

            }
            finally
            {
                IsRunning?.Invoke(false, Cls.GetKey("samsung_features_Flash"));
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
                IsRunning?.Invoke(true, Cls.GetKey("samsung_features_ReadPit"));
                await Odin.PrintInfo();
                log?.Invoke(Cls.GetKey("CheckingDownloadMode"), true, RichColor.Lime, RichFont.Bold);
                if (await Odin.IsOdin())
                {
                    log?.Invoke("ODIN", false, RichColor.Cyan, RichFont.Bold);
                    log?.Invoke(Cls.GetKey("InitializingDevice"), true, RichColor.Lime, RichFont.Bold);
                    if (await Odin.LOKE_Initialize(0))
                    {
                        log?.Invoke(Cls.GetKey("Initialized"), false, RichColor.Cyan, RichFont.Bold);
                        log?.Invoke(Cls.GetKey("ReadingPit"), true, RichColor.Lime, RichFont.Bold);
                        var GetPit = await Odin.Read_Pit();
                        if (GetPit.Result)
                        {
                            log?.Invoke(Cls.GetKey("Ok"), false, RichColor.Cyan, RichFont.Bold);
                            log?.Invoke($"{Cls.GetKey("SavedPath")} : ", true, RichColor.Lime, RichFont.Bold);
                            var fpath = $"{Cls.MyPath}\\backup\\samsung\\{ServiceData.SamsungDevice.model_name}\\pit\\{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.pit";
                            Cls.CreatFolder(System.IO.Path.GetDirectoryName(fpath));
                            System.IO.File.WriteAllBytes(fpath, GetPit.data);
                            log?.Invoke(fpath, false, RichColor.Cyan, RichFont.Bold);
                            if (AutoBoot.IsChecked == true)
                            {
                                log?.Invoke(Cls.GetKey("RebootingDeviceToNormalMode"), true, RichColor.Lime, RichFont.Bold);
                                if (await Odin.PDAToNormal())
                                {
                                    log?.Invoke(Cls.GetKey("Ok"), false, RichColor.Cyan, RichFont.Bold);
                                }
                                else
                                {
                                    log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                                }
                            }
                            else
                            {
                                log?.Invoke(Cls.GetKey("AutoRebootDisabledTryManual"), true, RichColor.Yellow, RichFont.Bold);
                            }
                        }
                        else
                        {
                            log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                        }
                    }
                    else
                    {
                        log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                    }
                }
                else
                {
                    log?.Invoke(Cls.GetKey("Failed"), false, RichColor.Red, RichFont.Bold);
                }
            }
            catch (Exception ee)
            {
                log?.Invoke($"{Cls.GetKey("SystemError")} : ", true, RichColor.Lime, RichFont.Bold);
                log?.Invoke(ee.Message, false, RichColor.Red, RichFont.Bold);

            }
            finally
            {
                IsRunning?.Invoke(false, Cls.GetKey("samsung_features_ReadPit"));
            }

        }


    }
}
