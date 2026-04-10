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
using System.Windows.Shapes;
using Microsoft.Win32;

namespace LotterySystem
{
    public partial class EditPrizeWindow : Window
    {
        public List<Prize> Prizes { get; set; }

        public EditPrizeWindow(List<Prize> prizes)
        {
            InitializeComponent();
            Prizes = prizes;
            RefreshPrizeList();
        }

        // 刷新 ListBox 显示
        private void RefreshPrizeList()
        {
            PrizeListBox.ItemsSource = null;
            PrizeListBox.ItemsSource = Prizes;
        }

        // 添加奖项
        private void AddPrize_Click(object sender, RoutedEventArgs e)
        {
            string name = NameTextBox.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("请输入奖项名称！");
                return;
            }

            if (!int.TryParse(CountTextBox.Text.Trim(), out int count) || count <= 0)
            {
                MessageBox.Show("请输入有效的正整数人数！");
                return;
            }

            Prizes.Add(new Prize { Name = name, Count = count });
            RefreshPrizeList();

            // 清空输入框
            NameTextBox.Text = "";
            CountTextBox.Text = "";
        }

        //修改奖项按钮
        private void UpdatePrize_Click(object sender, RoutedEventArgs e)
        {
            if (PrizeListBox.SelectedItem is Prize prize)
            {
                string name = NameTextBox.Text.Trim();

                if (string.IsNullOrEmpty(name))
                {
                    MessageBox.Show("奖项名称不能为空！");
                    return;
                }

                if (!int.TryParse(CountTextBox.Text.Trim(), out int count) || count <= 0)
                {
                    MessageBox.Show("人数必须是正整数！");
                    return;
                }

                // ⭐ 同时更新
                prize.Name = name;
                prize.Count = count;

                RefreshPrizeList();
            }
            else
            {
                MessageBox.Show("请先选择一个奖项！");
            }
        }

        // 删除奖项
        private void DeletePrize_Click(object sender, RoutedEventArgs e)
        {
            if (PrizeListBox.SelectedItem is Prize prize)
            {
                Prizes.Remove(prize);
                RefreshPrizeList();
            }
            else
            {
                MessageBox.Show("请先选择一个奖项！");
            }
        }

        // 确认
        private void OK_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
            this.Close();
        }

        //自动填充
        private void PrizeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PrizeListBox.SelectedItem is Prize prize)
            {
                NameTextBox.Text = prize.Name;
                CountTextBox.Text = prize.Count.ToString();
            }
        }
    }
}

