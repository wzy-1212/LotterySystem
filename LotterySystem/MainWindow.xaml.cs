using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
//导入EXCEL所需要的库
using OfficeOpenXml;
//using OfficeOpenXml.License;
using Microsoft.Win32;
using System.IO;


//缓存transform（使旋转流畅）////////////////////////////////////////
class ItemWrapper
{
    public Border Border;
    public ScaleTransform Scale;
    public int Row;
    public double BaseAngle;
    //初始位置
    public double OriginalAngle;
    public int OriginalRow;

    public bool IsWinner = false;
}

public class User
{
    public string Id;
    public string Name;
    public string Dept;
}

public class Prize
{
    public string Name { get; set; }
    public int Count { get; set; }
}

namespace LotterySystem
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer rotateTimer = new DispatcherTimer();

        public MainWindow()
        {
            currentCardBrush = normalCardBrush;

            InitializeComponent();

            InitCylinder();
            
            UpdatePositions(); //初始化位置

            BgCanvas.Loaded += (s, e) =>
            {
                InitParticles();
                StartParticleAnimation();
            };

            this.SizeChanged += (s, e) => UpdatePositions();
            angleOffset = new Random().NextDouble() * 2 * Math.PI;
            rotateTimer.Interval = TimeSpan.FromMilliseconds(16);
            rotateTimer.Tick += RotateTimer_Tick;
            RefreshPrizeUI();
            speed = 0.02;
            rotateTimer.Start();
            //winnerPanels = new List<Panel>
            //{
            //    WinnerPanel0,
            //    WinnerPanel1,
            //    WinnerPanel2
            //};
        }

        // 所有参与抽奖的人/////////////////////////////////////////
        private List<User> users = new List<User>
        {
            new User{Id="E001", Name="张三", Dept="技术部"},
            new User{Id="E002", Name="李四", Dept="销售部"},
            new User{Id="E003", Name="张三", Dept="技术部"},
            new User{Id="E004", Name="李四", Dept="销售部"},
            new User{Id="E005", Name="张三", Dept="技术部"},
            new User{Id="E006", Name="张三", Dept="技术部"},
            new User{Id="E007", Name="李四", Dept="销售部"},
            new User{Id="E008", Name="李四", Dept="销售部"},
        };

        // 已中奖的人
        private List<string> winners = new List<string>();

        private Random rand = new Random();
        private DispatcherTimer timer = new DispatcherTimer();

        //创建名字卡片，初始化圆柱////////////////////////////////////
        private List<ItemWrapper> items = new List<ItemWrapper>();

        private int rows = 3; // 3~4行都可以
        private double rowSpacing = 80;

        private void InitCylinder() 
        {
            CylinderCanvas.Children.Clear();
            items.Clear();

            int count = users.Count;

            for (int i = 0; i < count; i++)
            {
                var user = users[i];

                int row = i % rows;

                double baseAngle = 2 * Math.PI * i / count;

                var border = new Border
                {
                    //Width = 120,
                    //Height = 140,
                    Width = 100,
                    Height = 80,
                    //Background = new SolidColorBrush(Color.FromRgb(50, 200, 120)), //卡片颜色
                    Background = currentCardBrush,
                    CornerRadius = new CornerRadius(10),
                    Padding = new Thickness(5)
                };
                //卡片内容
                border.Child = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = user.Id,
                            FontSize = 12,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = user.Name,
                            FontSize = 20,
                            FontWeight = FontWeights.Bold,
                            HorizontalAlignment = HorizontalAlignment.Center
                        },
                        new TextBlock
                        {
                            Text = user.Dept,
                            FontSize = 12,
                            HorizontalAlignment = HorizontalAlignment.Center
                        }
                    }
                };

                var scale = new ScaleTransform(1, 1);
                border.RenderTransform = scale;
                border.RenderTransformOrigin = new Point(0.5, 0.5);

                items.Add(new ItemWrapper
                {
                    Border = border,
                    Scale = scale,
                    Row = row,
                    BaseAngle = 2 * Math.PI * i / count,
                    //保存初始状态
                    OriginalAngle = 2 * Math.PI * i / count,
                    OriginalRow = row
                });

                CylinderCanvas.Children.Add(border);
            }
        }

        //更新圆柱位置/////////////////////////////////////////////
        private double angleOffset = 0;
        private double radius = 250;
        private double centerX;
        private double centerY;

        private void UpdatePositions()
        {
            centerX = CylinderCanvas.ActualWidth / 2;
            centerY = CylinderCanvas.ActualHeight / 2;

            foreach (var item in items)
            {
                if (item.IsWinner) continue; // ⭐⭐⭐ 跳过中奖人
                // ⭐ 每一行错开
                double rowOffset = item.Row * 0.5;

                double angle = item.BaseAngle + angleOffset + rowOffset;

                double x = centerX + radius * Math.Cos(angle);
                double z = Math.Sin(angle);

                double y = centerY + (item.Row - rows / 2.0) * rowSpacing;

                double scale = 0.5 + (z + 1) / 2;

                // 位置
                Canvas.SetLeft(item.Border, x);
                Canvas.SetTop(item.Border, y);

                // 复用 transform（不卡的关键）
                item.Scale.ScaleX = scale;
                item.Scale.ScaleY = scale;

                item.Border.Opacity = 0.3 + 0.7 * (z + 1) / 2;

                Panel.SetZIndex(item.Border, (int)(scale * 100));
            }
        }

        //旋转动画/////////////////////////////////////////////////
        private double speed = 0.1;

        private void RotateTimer_Tick(object sender, EventArgs e)
        {

            angleOffset += speed;

            UpdatePositions();
        }

        //选中奖人在前方///////////////////////////////////////////
        //private HashSet<string> selectedUsers = new HashSet<string>();

        //private List<ItemWrapper> GetFrontItems(int count)
        //{
        //    return items
        //        .Where(i =>
        //        {
        //            var userName = ((TextBlock)((StackPanel)i.Border.Child).Children[1]).Text;
        //            return !selectedUsers.Contains(userName);
        //        })
        //        .OrderByDescending(i => i.Scale.ScaleX)
        //        .Take(count)
        //        .ToList();
        //}
        private HashSet<ItemWrapper> selectedItems = new HashSet<ItemWrapper>();
        
        private List<ItemWrapper> DrawWinners(int count)
        {
            var available = items
                .Where(i => !selectedItems.Contains(i))
                .ToList();

            if (available.Count == 0)
                return new List<ItemWrapper>();

            // 随机打乱
            var shuffled = available.OrderBy(x => rand.Next()).ToList();

            var winners = shuffled.Take(Math.Min(count, available.Count)).ToList();

            foreach (var w in winners)
                selectedItems.Add(w);

            return winners;
        }

        //中奖人动画///////////////////////////////////////////////
        private void AnimateWinner(ItemWrapper item, double targetX, double targetY)
        {
            // 位置动画
            var animX = new DoubleAnimation(targetX, TimeSpan.FromMilliseconds(500));
            var animY = new DoubleAnimation(targetY, TimeSpan.FromMilliseconds(500));

            item.Border.BeginAnimation(Canvas.LeftProperty, animX);
            item.Border.BeginAnimation(Canvas.TopProperty, animY);

            // 放大动画
            var scaleAnim = new DoubleAnimation(1.8, TimeSpan.FromMilliseconds(500));
            item.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
            item.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

            // 发光效果
            item.Border.Background = Brushes.Gold;
        }

        //奖项列表/////////////////////////////////////////////////
        private List<Prize> prizes = new()
        {
            new Prize { Name = "一等奖", Count = 1 },
            new Prize { Name = "二等奖", Count = 2 },
            new Prize { Name = "三等奖", Count = 3 }
        };

        private int currentPrizeIndex = 0;

        private int currentPrizeCount => prizes[currentPrizeIndex].Count;

        //中奖人展示////////////////////////////////////////////////
        private async Task ShowWinners(List<ItemWrapper> winners)
        {
            double canvasWidth = CylinderCanvas.ActualWidth;
            // ⭐ 每行最多几个（你可以调）
            int maxPerRow = 3;

            // 自动计算行数
            int rowCount = (int)Math.Ceiling((double)winners.Count / maxPerRow);

            // 卡片间距
            double spacingX = 200;
            double spacingY = 200;

            // 总宽度（用于居中）
            double totalWidth = (Math.Min(winners.Count, maxPerRow) - 1) * spacingX;

            // 起点（水平居中）
            double startX = (canvasWidth - totalWidth) / 2;

            // 起点Y（垂直稍微往上）
            //double startY = 60;
            double cardHeight = winners[0].Border.Height;

            // ⭐ 正确总高度
            double totalHeight = rowCount * cardHeight + (rowCount - 1) * (spacingY - cardHeight);

            // ⭐ 居中
            double startY = (CylinderCanvas.ActualHeight - totalHeight) / 2;

            for (int i = 0; i < winners.Count; i++)
            {
                int row = i / maxPerRow;
                int col = i % maxPerRow;

                double x = startX + col * spacingX;
                double y = startY + row * spacingY;

                var item = winners[i];

                Panel.SetZIndex(item.Border, 1000);

                item.IsWinner = true;
                item.Border.Opacity = 1;

                double centerX = x - item.Border.Width / 2;
                double centerY = y - item.Border.Height / 2;

                AnimateWinner(item, centerX, centerY);
                //AnimateWinner(item, x, y);
            }

            // ⭐ 等动画结束（关键）
            await Task.Delay(600);

            lastRoundWinners = winners.ToList();

            // 中奖人显示到左侧
            var panel = winnerPanels[currentPrizeIndex];

            foreach (var item in winners)
            {
                var userName = ((TextBlock)((StackPanel)item.Border.Child).Children[1]).Text;

                panel.Children.Add(new Border
                {
                    Background = Brushes.Gold,
                    CornerRadius = new CornerRadius(5),
                    Margin = new Thickness(5, 0, 0, 0),
                    Padding = new Thickness(5),
                    Child = new TextBlock
                    {
                        Text = userName,
                        FontSize = 15,
                        FontWeight = FontWeights.Bold,
                        Foreground = Brushes.Black
                    }
                });
            }

            currentPrizeIndex++;
        }
        //记录中奖人///////////////////////////////////////////
        private List<ItemWrapper> lastRoundWinners = new List<ItemWrapper>();

        //回位动画//////////////////////////////////////////////
        private async Task AnimateBackToOriginal(List<ItemWrapper> winners)
        {
            List<Task> tasks = new List<Task>();

            foreach (var item in winners)
            {
                item.IsWinner = false;

                // 恢复角度（让它回到圆柱）
                item.BaseAngle = item.OriginalAngle;
                item.Row = item.OriginalRow;

                // 计算目标位置
                double centerX = CylinderCanvas.ActualWidth / 2;
                double centerY = CylinderCanvas.ActualHeight / 2;

                double rowOffset = item.Row * 0.5;
                double angle = item.BaseAngle + angleOffset + rowOffset;

                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + (item.Row - rows / 2.0) * rowSpacing;

                // 动画
                var animX = new DoubleAnimation(x, TimeSpan.FromMilliseconds(500));
                var animY = new DoubleAnimation(y, TimeSpan.FromMilliseconds(500));

                item.Border.BeginAnimation(Canvas.LeftProperty, animX);
                item.Border.BeginAnimation(Canvas.TopProperty, animY);

                // 缩回去
                var scaleAnim = new DoubleAnimation(1.0, TimeSpan.FromMilliseconds(500));
                item.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                item.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

                // 颜色恢复
                item.Border.Background = currentCardBrush;
            }

            await Task.Delay(600);
        }

        //旋转到中奖人///////////////////////////////////////////////
        private async Task RotateToWinner(ItemWrapper winner)
        {
            double target = -winner.BaseAngle;

            // ⭐ 保证顺时针（关键）
            while (target <= angleOffset)
            {
                target += 2 * Math.PI;
            }

            double totalDistance = target - angleOffset;

            int steps = 60; // 帧数（越大越丝滑）
            double current = 0;

            for (int i = 0; i < steps; i++)
            {
                // ⭐ 使用缓动（ease out）
                double t = (double)i / steps;
                double ease = 1 - Math.Pow(1 - t, 3); // cubic ease-out

                double next = totalDistance * ease;
                double delta = next - current;

                current = next;
                angleOffset += delta;

                UpdatePositions();
                await Task.Delay(16);
            }

            // ⭐ 精确对齐
            angleOffset = target;
            UpdatePositions();
        }

        //重置中奖人颜色/////////////////////////////////////////////
        private void ResetItems()
        {
            foreach (var item in items)
            {
                //item.Border.Background = new SolidColorBrush(Color.FromRgb(50, 200, 120));
                item.Border.Background = currentCardBrush;

                item.Scale.ScaleX = 1;
                item.Scale.ScaleY = 1;

                item.Border.Opacity = 1;

                item.IsWinner = false;//恢复透明度

                // ⭐ 恢复位置（关键！！！）
                item.BaseAngle = item.OriginalAngle;
                item.Row = item.OriginalRow;

                // 清除动画
                item.Border.BeginAnimation(Canvas.LeftProperty, null);
                item.Border.BeginAnimation(Canvas.TopProperty, null);
                item.Scale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
                item.Scale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            }

            UpdatePositions();
        }

        //重置函数
        private void ResetLottery()
        {
            // 清空已抽中奖人
            selectedItems.Clear();

            // 重置圆柱
            ResetItems();

            // 重置奖项索引
            currentPrizeIndex = 0;

            // 重置按钮
            btnStart.Content = "开始抽奖";

            // 重置左上角奖项中奖人显示（如果需要清空）
            foreach (var panel in winnerPanels)
                panel.Children.Clear();
        }

        //抽奖按钮/////////////////////////////////////////////
        private bool isFinished = false;
        private bool isAnimating = false;

        private bool isAllDrawFinished = false; // 是否抽完
        private bool isShowingFinal = false;   // 是否已点击“结束抽奖”

        private async void StartDraw_Click(object sender, RoutedEventArgs e)
        {
            //PrizeList.SelectedIndex = currentPrizeIndex;

            if (isAnimating) return; // ⭐ 防止连点

            // 如果按钮显示“重置抽奖”，就执行重置逻辑
            if (btnStart.Content.ToString() == "重置抽奖")
            {
                isShowingFinal = true;
                // 清空左侧中奖人
                foreach (var panel in winnerPanels)
                {
                    panel.Children.Clear();
                }
                selectedItems.Clear();
                ResetItems();          // 圆柱回到原位
                currentPrizeIndex = 0; // 重置奖项索引
                btnStart.Content = "开始抽奖";
                return;
            }

            // ⭐ 如果已经抽完 → 重置
            //if (currentPrizeIndex >= prizes.Count)
            //{
            //    selectedItems.Clear(); // ⭐ 必加！！！
            //    if (lastRoundWinners.Count > 0)
            //    {
            //        await AnimateBackToOriginal(lastRoundWinners);
            //        lastRoundWinners.Clear();
            //    }
            //    foreach (var panel in winnerPanels)
            //    {
            //        panel.Children.Clear();
            //    }
            //    // 再彻底重置
            //    ResetItems();
            //    btnStart.Content = "开始抽奖";
            //    currentPrizeIndex = 0;
            //    return;
            //}

            // ⭐ 第一步：抽完后第一次点击 → “结束抽奖”
            if (isAllDrawFinished && !isShowingFinal)
            {
                btnStart.Content = "重置抽奖";

                // ⭐ 中间中奖人回去
                if (lastRoundWinners.Count > 0)
                {
                    await AnimateBackToOriginal(lastRoundWinners);
                    lastRoundWinners.Clear();
                }

                isShowingFinal = true;
                return;
            }

            // ⭐ 第二步：点击“重置抽奖”
            if (isAllDrawFinished && isShowingFinal)
            {
                // 清空左侧中奖人
                foreach (var panel in winnerPanels)
                {
                    panel.Children.Clear();
                }

                // 重置数据
                selectedItems.Clear();
                currentPrizeIndex = 0;
                isAllDrawFinished = false;
                isShowingFinal = false;

                btnStart.Content = "开始抽奖";

                ResetItems();
                return;
            }

            isAnimating = true;

            btnStart.Content = "正在抽取...";

            //ResetItems(); // 先恢复
            if (lastRoundWinners.Count > 0)
            {
                await AnimateBackToOriginal(lastRoundWinners);
                lastRoundWinners.Clear();
            }

            // 再彻底重置
            ResetItems();

            speed = 0.2;
            rotateTimer.Start();

            await Task.Delay(2000);

            rotateTimer.Stop();

            //抽取人数 = 当前奖项人数，但不超过剩余人
            int prizeCount = prizes[currentPrizeIndex].Count;
            var winners = DrawWinners(prizeCount);

            if (winners.Count > 0)
            {
                await RotateToWinner(winners[0]);
                await ShowWinners(winners);
            }
            

            // 判断是否所有人已经抽完
            if (selectedItems.Count >= users.Count || currentPrizeIndex >= prizes.Count)
            {
                // 所有人抽完
                MessageBox.Show("所有人已抽完");
                btnStart.Content = "重置抽奖";

                // 中间圆柱回到原位（不影响左上角中奖人显示）
                ResetItems();

                speed = 0.02;
                rotateTimer.Start();

                isAnimating = false;
                return;
            }

            //currentPrizeIndex++;

            // 如果还有奖项剩余，按钮显示“继续抽奖”
            btnStart.Content = "继续抽奖";
            isAnimating = false;
        }

        //EXCEL按钮
        private void ImportExcel_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "Excel文件|*.xlsx"
            };

            if (dlg.ShowDialog() == true)
            {
                LoadUsersFromExcel(dlg.FileName);
            }
            ResetLottery(); // 导入完成后自动重置抽奖
        }

        //读取Excel的函数
        private void LoadUsersFromExcel(string path)
        {
            try
            {
                ExcelPackage.License.SetNonCommercialOrganization("Your Org");

                using (var package = new ExcelPackage(new FileInfo(path)))
                {
                    var sheet = package.Workbook.Worksheets[0];

                    users.Clear();

                    int row = 2; // 跳过表头

                    while (sheet.Cells[row, 1].Value != null)
                    {
                        users.Add(new User
                        {
                            Id = sheet.Cells[row, 1].Text,
                            Name = sheet.Cells[row, 2].Text,
                            Dept = sheet.Cells[row, 3].Text
                        });

                        row++;
                    }
                }

                // ⭐ 重新初始化
                selectedItems.Clear();
                currentPrizeIndex = 0;

                InitCylinder();
                UpdatePositions();

                //MessageBox.Show("导入成功！");
                MessageBox.Show($"成功导入 {users.Count} 人！");
            }
            catch (Exception ex)
            {
                MessageBox.Show("导入失败：" + ex.Message);
            }
        }

        //编辑奖项///////////////////////////////////////////////////////
        // 保存每个奖项对应的 WrapPanel，用于显示中奖人
        private List<WrapPanel> winnerPanels = new List<WrapPanel>();
        //private List<Panel> winnerPanels = new List<Panel>();
        private void EditPrize_Click(object sender, RoutedEventArgs e)
        {
            var win = new EditPrizeWindow(prizes);

            win.Owner = this; // 可选：保证弹窗在主窗口前
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (win.ShowDialog() == true)
            {
                RefreshPrizeUI(); // ⭐ 刷新左侧
                                  
                ResetLottery(); // ⭐ 自动重置抽奖
            }
        }
        //动态生成左侧奖项///////////////////////////////////////////////////
        private void RefreshPrizeUI()
        {
            PrizeContainer.Children.Clear();
            winnerPanels.Clear();

            var header = new TextBlock
            {
                Text = "奖品列表",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.Black
            };
            PrizeContainer.Children.Add(header);

            foreach (var prize in prizes)
            {
                var grid = new Grid { Margin = new Thickness(0, 10, 0, 0) };
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                // 左侧文字：奖项名称 + 人数
                var text = new TextBlock
                {
                    Text = $"{prize.Name}（{prize.Count}名）",
                    FontSize = 18,
                    FontWeight = FontWeights.Bold,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(text, 0);

                // 右侧 WrapPanel 显示中奖人
                var wrap = new WrapPanel();
                Grid.SetColumn(wrap, 1);

                grid.Children.Add(text);
                grid.Children.Add(wrap);

                PrizeContainer.Children.Add(grid);

                winnerPanels.Add(wrap);
            }
        }

        //主题切换
        private Brush normalCardBrush = new SolidColorBrush(Color.FromRgb(50, 200, 120));
        private Brush redCardBrush = new SolidColorBrush(Color.FromRgb(225, 50, 50));

        private Brush currentCardBrush;

        private bool isRedTheme = false;

        private void ToggleTheme_Click(object sender, RoutedEventArgs e)
        {
            isRedTheme = !isRedTheme;

            if (isRedTheme)
            {
                ApplyRedTheme();
            }
            else
            {
                ApplyDefaultTheme();
            }
        }

        private void ApplyRedTheme()
        {
            // 背景
            currentCardBrush = redCardBrush;
            this.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            // 按钮颜色
            btnStart.Background = new SolidColorBrush(Color.FromRgb(225, 50, 50));
            btnStart.Foreground = Brushes.White;
            btnEdit.Background = new SolidColorBrush(Color.FromRgb(225, 50, 50));
            btnExcel.Background = new SolidColorBrush(Color.FromRgb(225, 50, 50));

            btnTheme.Content = "红色";
            btnTheme.Background = new SolidColorBrush(Color.FromRgb(225, 50, 50));

            // 左侧奖项区
            PrizeContainer.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            // 圆柱卡片颜色
            foreach (var item in items)
            {
                if (!item.IsWinner)
                {
                    item.Border.Background = new SolidColorBrush(Color.FromRgb(225, 50, 50));
                }
            }
        }

        private void ApplyDefaultTheme()
        {
            currentCardBrush = normalCardBrush;
            this.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            btnStart.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));

            btnTheme.Content = "绿色";
            btnTheme.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));

            PrizeContainer.Background = new SolidColorBrush(Color.FromRgb(255, 255, 255));

            btnEdit.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));
            btnExcel.Background = new SolidColorBrush(Color.FromRgb(76, 175, 80));

            foreach (var item in items)
            {
                if (!item.IsWinner)
                {
                    item.Border.Background = new SolidColorBrush(Color.FromRgb(50, 200, 120));
                }
            }
        }

        //初始化背景粒子：
        private List<Ellipse> particles = new();
        private List<(double dx, double dy)> velocities = new();

        private void InitParticles()
        {
            particles.Clear();
            velocities.Clear();
            BgCanvas.Children.Clear();

            double canvasWidth = BgCanvas.ActualWidth;
            double canvasHeight = BgCanvas.ActualHeight;

            for (int i = 0; i < 100; i++)
            {
                var dot = new Ellipse
                {
                    Width = 2 + rand.NextDouble() * 3,
                    Height = 2 + rand.NextDouble() * 3,
                    Fill = Brushes.MediumPurple,
                    Opacity = 0.3 + 0.7 * rand.NextDouble()
                };

                // X 随机在整个顶部宽度
                double x = rand.NextDouble() * canvasWidth;
                double y = rand.NextDouble() * 10; // Y 在顶部 0~10px 内随机

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);

                BgCanvas.Children.Add(dot);
                particles.Add(dot);

                // 每个粒子 Y 方向向下速度随机，X 方向微飘动
                double dx = rand.NextDouble() * 0.5 - 0.25; // 左右微动
                double dy = 0.5 + rand.NextDouble() * 1.0;  // 向下速度
                velocities.Add((dx, dy));
            }
        }

        private DispatcherTimer particleTimer = new DispatcherTimer();

        private void StartParticleAnimation()
        {
            particleTimer.Interval = TimeSpan.FromMilliseconds(16); // 60 FPS
            particleTimer.Tick += ParticleTimer_Tick;
            particleTimer.Start();
        }

        // 粒子动画每帧更新
        private void ParticleTimer_Tick(object sender, EventArgs e)
        {
            double canvasWidth = BgCanvas.ActualWidth;
            double canvasHeight = BgCanvas.ActualHeight;

            for (int i = 0; i < particles.Count; i++)
            {
                var dot = particles[i];
                var (dx, dy) = velocities[i];

                double x = Canvas.GetLeft(dot) + dx;
                double y = Canvas.GetTop(dot) + dy;

                // 如果掉到底部，从顶部随机 X 重生
                if (y > canvasHeight)
                {
                    y = 0;
                    x = rand.NextDouble() * canvasWidth;
                }

                // 左右边界限制
                if (x < 0) x = 0;
                if (x > canvasWidth) x = canvasWidth;

                Canvas.SetLeft(dot, x);
                Canvas.SetTop(dot, y);

                velocities[i] = (dx, dy);
            }
        }

    }
}