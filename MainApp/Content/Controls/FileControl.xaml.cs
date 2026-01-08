using Ai_Project.Content.Controls.Demo;
using Ai_Project.DTO;
using Ai_Project.Model_Manager;
using Get_Pc_Info;
using ModelsDTO;
using Run_LlamaSharp.DTOs;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using static LLama.Common.ChatHistory;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for FileControl.xaml
    /// </summary>
    public partial class FileControl : UserControl
    {
        HuggingFaceModelDTO model;
        ModelManager modelManager;
        RichTextBox textBox;
        ScrollViewer logScrollViewer;
        public FileControl(HuggingFaceModelDTO model, ModelManager modelManager, RichTextBox textBox, ScrollViewer logScrollViewer)
        {
            this.logScrollViewer = logScrollViewer;
            this.textBox = textBox;
            this.model = model;
            this.modelManager = modelManager;

            InitializeComponent();

            DataContextChanged += FileControl_DataContextChanged;
        }

        private void FileControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Regex regex = new Regex(@"(Q\d(_[A-Za-z0-1](?:_[A-Za-z])?)?|imatrix|F\d{2})", RegexOptions.IgnoreCase);

            SiblingDTO? sibling = this.DataContext as SiblingDTO;
            if (sibling != null)
            {
                HuggingFaceModelDTO? modelDTO = modelManager.AvailableModels.FirstOrDefault(m => m.id == model.id);
                if (modelManager.CheckFileExists(model, sibling.rfilename))
                {
                    if (modelDTO != null)
                    {
                        DownloadBorder.Visibility = Visibility.Collapsed;
                    }
                }
                else
                {
                    DownloadBorder.Visibility = Visibility.Visible;
                    LoadModelBorder.Visibility = Visibility.Collapsed;
                    DeleteModelBorder.Visibility = Visibility.Collapsed;
                }

                Match match = regex.Match(sibling.rfilename);
                if (match.Success)
                {
                    DataTypeTextBlock.Text = $"{match.ToString().ToUpper()}";
                }
                else
                    DataTypeTextBlock.Text = $"Unknown";
                FillProgressBar(match.Value);

                GenerateDescription(sibling);
            }
        }

        private void GenerateDescription(SiblingDTO sibling)
        {
            if (sibling == null) return;

            string dtype = sibling.rfilename.ToLower();
            string precision;

            if (dtype.Contains("matrix") || dtype.Contains("imatrix"))
                precision = "matrix-based precision";
            else if (dtype.Contains("q1"))
                precision = "very low precision";
            else if (dtype.Contains("q2"))
                precision = "low precision";
            else if (dtype.Contains("q3"))
                precision = "low-medium precision";
            else if (dtype.Contains("q4"))
                precision = "medium precision";
            else if (dtype.Contains("q5"))
                precision = "medium-high precision";
            else if (dtype.Contains("q6"))
                precision = "high-medium precision";
            else if (dtype.Contains("q7"))
                precision = "high precision";
            else if (dtype.Contains("q8"))
                precision = "very high precision";
            else if (dtype.Contains("q16") || dtype.Contains("f16"))
                precision = "half precision (FP16)";
            else if (dtype.Contains("q32") || dtype.Contains("f32"))
                precision = "full precision (FP32)";
            else
                precision = "optimized precision";

            string text = $"This model uses {precision} to balance performance, speed, and response quality, ensuring efficient inference and accurate outputs.";

            DescriptionTextBlock.Text = text;
        }

        private double CalculateEfficeny(string Dtype)
        {
            int type = Dtype.Contains("matrix") ? 0 :
                       Dtype.Contains("1") ? 1 :
                       Dtype.Contains("2") ? 2 :
                       Dtype.Contains("3") ? 3 :
                       Dtype.Contains("4") ? 4 :
                       Dtype.Contains("5") ? 5 :
                       Dtype.Contains("6") ? 6 :
                       Dtype.Contains("7") ? 7 :
                       Dtype.Contains("8") ? 8 :
                       Dtype.Contains("16") ? 16 :
                       Dtype.Contains("32") ? 32 : 4;
            switch (type)
            {
                case 0:
                    return 0;
                case 1:
                    return Math.Round((double)1 / 16 * 100, 0);
                case 2:
                    return Math.Round((double)2 / 16 * 100, 0);
                case 3:
                    return Math.Round((double)3 / 16 * 100, 0);
                case 4:
                    return Math.Round((double)4 / 16 * 100, 0);
                case 5:
                    return Math.Round((double)5 / 16 * 100, 0);
                case 6:
                    return Math.Round((double)6 / 16 * 100, 0);
                case 7:
                    return Math.Round((double)7 / 16 * 100, 0);
                case 8:
                    return Math.Round((double)8 / 16 * 100, 0);
                case 16:
                    return Math.Round((double)16 / 16 * 100, 0);
                case 32:
                    return Math.Round((double)16 / 16 * 100, 0);
                default:
                    return Math.Round((double)4 / 16 * 100, 0); ;
            }
        }

        private double CalculateQuality(string Dtype)
        {
            int type = Dtype.Contains("matrix") ? 0 :
                       Dtype.Contains("1") ? 1 :
                       Dtype.Contains("2") ? 2 :
                       Dtype.Contains("3") ? 3 :
                       Dtype.Contains("4") ? 4 :
                       Dtype.Contains("5") ? 5 :
                       Dtype.Contains("6") ? 6 :
                       Dtype.Contains("7") ? 7 :
                       Dtype.Contains("8") ? 8 :
                       Dtype.Contains("16") ? 16 :
                       Dtype.Contains("32") ? 32 : 4;
            switch (type)
            {
                case 0:
                    return 65;
                case 1:
                    return 55;
                case 2:
                    return 60;
                case 3:
                    return 65;
                case 4:
                    return 70;
                case 5:
                    return 75;
                case 6:
                    return 80;
                case 7:
                    return 85;
                case 8:
                    return 90;
                case 16:
                    return 100;
                case 32:
                    return 100;
                default:
                    return 70;
            }
        }

        private int CalculateSpeed(string Dtype)
        {
            if (string.IsNullOrEmpty(Dtype))
                return 50;

            // 1. Normalize dtype string
            string dt = Dtype.ToLowerInvariant();

            // 2. Map dtype to numeric value
            int dtypeValue = dt.Contains("matrix") ? 0 :
                             dt.Contains("1") && !dt.Contains("16") ? 1 :
                             dt.Contains("2") ? 2 :
                             dt.Contains("3") ? 3 :
                             dt.Contains("4") && !dt.Contains("16") ? 4 :
                             dt.Contains("5") ? 5 :
                             dt.Contains("6") && !dt.Contains("16") ? 6 :
                             dt.Contains("7") ? 7 :
                             dt.Contains("8") ? 8 :
                             dt.Contains("16") || dt.Contains("f16") ? 16 :
                             dt.Contains("32") || dt.Contains("f32") ? 32 : 4;

            // 3. Base speed per dtype (lower = faster)
            double baseSpeed = dtypeValue switch
            {
                0 => 65,   // matrix
                1 => 100,  // Q1 fastest
                2 => 95,
                3 => 90,
                4 => 85,
                5 => 80,
                6 => 75,
                7 => 70,
                8 => 65,
                16 => 50,  // FP16
                32 => 35,  // FP32
                _ => 70
            };

            // 4. CPU scaling
            int totalLogical = GetPcInfo.CpuList.Sum(c => c.LogicalCores);
            int maxLogical = GetPcInfo.CpuList.Max(c => c.LogicalCores);
            maxLogical = Math.Max(1, maxLogical); // avoid divide by zero
            double cpuFactor = (double)totalLogical / maxLogical;
            cpuFactor = Math.Max(0.5, Math.Min(2.0, cpuFactor));
            double speed = baseSpeed * cpuFactor;

            // 5. RAM scaling
            double ramBandwidth = GetPcInfo.GetRamBandwidth(); // GB/s
            double baselineRam = ramBandwidth > 0 ? ramBandwidth : 1; // treat own system as 100%
            double ramFactor = ramBandwidth > 0 ? ramBandwidth / ramBandwidth : 1.0; // relative to itself
            speed *= ramFactor;

            // 6. GPU scaling (dynamic, no hardcode)
            ulong totalVRAM = 0;
            ulong maxVRAM = 0;
            foreach (var gpu in GetPcInfo.Gpus)
            {
                totalVRAM += gpu.VRAM;
                if (gpu.VRAM > maxVRAM)
                    maxVRAM = gpu.VRAM;
            }

            double gpuFactor = maxVRAM > 0
                ? Math.Min(1.0, (double)totalVRAM / maxVRAM)
                : 1.0;

            speed *= gpuFactor;

            // 7. Clamp final speed
            speed = Math.Min(100, speed);
            speed = Math.Max(10, speed);

            return (int)Math.Round(speed);
        }


        private void FillProgressBar(string dtype)
        {
            double efficencyvalue = CalculateEfficeny(dtype);
            if (efficencyvalue == 0) efficencyvalue = Math.Round((double)6 / 16 * 100, 0);
            EfficiencyProgressBar.Value = efficencyvalue;

            double qualityvalue = CalculateQuality(dtype);
            QualityProgressBar.Value = qualityvalue;

            int totalCores = 0;
            foreach (var cpu in GetPcInfo.CpuList)
                totalCores += cpu.LogicalCores;

            int speedValue = CalculateSpeed(dtype);

            SpeedProgressBar.Value = speedValue;

        }

        private async void DownloadBorder_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not SiblingDTO sibling)
                return;

            if (DownloadModel.IsDownloading)
            {
                MessageBox.Show("Another download is already in progress. Please wait for it to finish before starting a new download.", "Download in Progress", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var download = new DownloadModel(ProgressGrid, DownloadProgressBar, DownloadBorder, StopBorder, modelManager))
            {
                download.sibling = sibling;
                download.model = model;
                download.DownloadingProgressTextBlock = DownloadingProgressTextBlock;
                ResultDTO result = await download.DownloadModelAsync();

                if (result.IsSuccess && !result.Data!.ToString()!.Equals("Download canceled by user"))
                {
                    LoadModelBorder.Visibility = Visibility.Visible;
                    DeleteModelBorder.Visibility = Visibility.Visible;
                }
            }
        }

        private void StopBorder_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            modelManager.DownloadCancelled?.Invoke(true);
        }

        private async void LoadBorder_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SiblingDTO? sibling = this.DataContext as SiblingDTO;
            if (sibling != null)
            {
                if (modelManager.CheckFileExists(model, sibling.rfilename))
                {
                    ModelDTO runModel = new ModelDTO
                    {
                        id = model.id,
                        _id = model._modelInfoDto!._id,
                        path = modelManager.GetModelPath(model, sibling.rfilename)!,
                    };
                    textBox.Document.Blocks.Clear();
                    if (textBox.Document.Blocks.FirstBlock == null)
                        textBox.Document.Blocks.Add(new Paragraph { Margin = new Thickness(0) });
                    var paragraph = (Paragraph)textBox.Document.Blocks.FirstBlock!;
                    Run run = new Run("Running the model, please wait..." + "\n" + "\n");
                    paragraph!.Inlines.Add(run);

                    modelManager.selectLlama.OnLog += (type, message) =>
                    {
                        textBox.Dispatcher.Invoke(() =>
                        {
                            string normalized = message.ToLowerInvariant();
                            run = new Run(message);
                            
                            if (normalized.Contains("error") || normalized.Contains("failed"))
                            {
                                run.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"));
                            }
                            else
                            {
                                run.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF464E6D"));
                            }
                            
                            paragraph!.Inlines.Add(run);
                            logScrollViewer.ScrollToEnd();
                        }, System.Windows.Threading.DispatcherPriority.Render);
                    };
                    
                    ResultDTO result = await Task.Run(() => modelManager.RunModel(runModel));

                    if (result.IsSuccess)
                    {
                        run = new Run("\n" + "✔ Model run successfully.")
                        {
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF006600"))
                        };
                        paragraph!.Inlines.Add(run);
                        LoadModelBorder.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        run = new Run("\n" + "X Failed to run the model.")
                        {
                            Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFFF0000"))
                        };
                        paragraph!.Inlines.Add(run);
                        
                    }
                }
            }
        }

        private void DeleteBorder_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            SiblingDTO? sibling = this.DataContext as SiblingDTO;
            if (sibling != null)
            {
                if (MessageBox.Show("Are you sure to delete this model?", "Delete model", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    if (modelManager.CheckFileExists(model, sibling.rfilename))
                    {
                        modelManager.DeleteModel(model, sibling.rfilename);

                        DeleteModelBorder.Visibility =
                            LoadModelBorder.Visibility = Visibility.Collapsed;
                        DownloadBorder.Visibility = Visibility.Visible;
                    }
                }
            }
        }
    }
}
