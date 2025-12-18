using Ai_Project.DTO;
using Get_Pc_Info;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace Ai_Project.Content.Controls
{
    /// <summary>
    /// Interaction logic for FileControl.xaml
    /// </summary>
    public partial class FileControl : UserControl
    {
        public FileControl()
        {
            InitializeComponent();
            DataContextChanged += FileControl_DataContextChanged;

        }

        private void FileControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Regex regex = new Regex(@"(Q\d(_[A-Za-z0-1](?:_[A-Za-z])?)?|imatrix|F\d{2})", RegexOptions.IgnoreCase);

            SiblingDTO? sibling = this.DataContext as SiblingDTO;
            if (sibling != null)
            {
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

            string text = $"This model file uses {precision} and is designed to provide a balanced combination of performance, processing speed, and response quality. " +
              "It contributes to the overall behavior of the model, ensuring efficient inference while maintaining accurate and detailed outputs.";

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
            if (string.IsNullOrEmpty(Dtype)) return 50;

            // 1. Map dtype to numeric value
            int dtypeValue = Dtype.Contains("matrix") ? 0 :
                             Dtype.Contains("1") ? 1 :
                             Dtype.Contains("2") ? 2 :
                             Dtype.Contains("3") ? 3 :
                             Dtype.Contains("4") ? 4 :
                             Dtype.Contains("5") ? 5 :
                             Dtype.Contains("6") ? 6 :
                             Dtype.Contains("7") ? 7 :
                             Dtype.Contains("8") ? 8 :
                             Dtype.Contains("16") || Dtype.Contains("f16") ? 16 :
                             Dtype.Contains("32") || Dtype.Contains("f32") ? 32 : 4;

            // 2. Base speed per dtype (lower = faster)
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

            // 3. Scale by CPU cores
            int cores = GetPcInfo.CpuList.Sum(c => c.LogicalCores);
            double speed = baseSpeed * cores / 8.0; // 8-core baseline

            // 4. Scale by RAM bandwidth
            double ramBandwidth = GetPcInfo.GetRamBandwidth(); // GB/s
            speed *= Math.Min(1.0, ramBandwidth / 25.0); // 25 GB/s baseline

            // 5. Clamp to 100%
            speed = Math.Min(100, speed);

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

            int bits = CalculateSpeed(dtype);
            // Base speed depending on bits (user-friendly estimate)
            double baseSpeed = bits switch
            {
                1 => 100,
                2 => 95,  // Q2 fastest
                3 => 90,
                4 => 85,
                5 => 80,
                6 => 75,
                8 => 70,
                16 => 35,  // FP16
                32 => 30,  // F32
                0 => 65,   // matrix/imatrix
                _ => 50
            };

            // Scale by logical cores relative to 8-core baseline
            double speedValue = baseSpeed * totalCores / 8.0;

            // Clamp to 100%
            speedValue = Math.Min(100, speedValue);

            SpeedProgressBar.Value = speedValue;

        }
    }
}
