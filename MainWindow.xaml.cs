using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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
using System.Xml;
using ZsfbApp;

namespace ZsfbApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public CDistributionBox m_cDistributionBox { get; set; }
        public XmlDocument m_xlProductData { get; set; }


        public MainWindow()
        {
            InitializeComponent();

            //初始化元器件树列表

            m_xlProductData = new XmlDocument();
            m_xlProductData.Load(".\\ProductsInfo.xml");


            //初始化“防爆配电箱”板块
            XmlNode xnProductSeriesNode = m_xlProductData.SelectSingleNode("/Products/Series[1]");

            while (xnProductSeriesNode != null)
            {
                //如果有新系列(Series)，创建新标签页
                TabItem TabItem_New = new TabItem();
                TabItem_New.Header = xnProductSeriesNode.Attributes["name"].Value;
                TabControl_Produce.Items.Add(TabItem_New);

                //如果新系列(Series)里面有数据，那么就导入数据
                if (xnProductSeriesNode.HasChildNodes)
                {
                    //在标签页中增加树控件
                    TreeView TreeView_New = new TreeView();
                    TabItem_New.Content = TreeView_New;

                    XmlNode xnProductNode = xnProductSeriesNode.FirstChild;
                    while (xnProductNode != null)
                    {
                        TreeViewItem tvItem = new TreeViewItem();
                        tvItem.Header = xnProductNode.Attributes["name"].Value;
                        tvItem.Tag = xnProductNode.Attributes["ID"].Value;
                        TreeView_New.Items.Add(tvItem);
                        if (xnProductNode.HasChildNodes)
                        {
                            tvItem.Items.Add("*");
                            tvItem.IsExpanded = true;
                        }
                        xnProductNode = xnProductNode.NextSibling;
                    }
                }
                xnProductSeriesNode = xnProductSeriesNode.NextSibling;
            }




            //初始化防爆配电箱类
            m_cDistributionBox = new CDistributionBox();
            m_cDistributionBox.Name = "";

            //设置输出结果文本框的数据源
            TextBlock_Result.DataContext = m_cDistributionBox;

            //设置元器件已选列表控件的数据源
            DataGrid_Product.ItemsSource = m_cDistributionBox.Parts;
        }



        //对元件列表里面的所有元件价格求和
        private void MenuItem_Sum_Click(object sender, RoutedEventArgs e)
        {
            m_cDistributionBox.Price = m_cDistributionBox.GetTotalPrice();
        }

        //清空元件列表里面的所有元件
        private void MenuItem_DeleteAll_Click(object sender, RoutedEventArgs e)
        {
            m_cDistributionBox.Parts.Clear();
            m_cDistributionBox.Price = 0;
        }

        //删除元件列表中选中的元件
        private void MenuItem_DeleteSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Product.SelectedIndex >= 0 && DataGrid_Product.SelectedIndex < DataGrid_Product.Items.Count)
            {
                m_cDistributionBox.Parts.RemoveAt(DataGrid_Product.SelectedIndex);
                m_cDistributionBox.GetTotalPrice();
            }

        }

        //增加元件列表中选中元件的数量
        private void MenuItem_IncreaseSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Product.SelectedIndex >= 0 && DataGrid_Product.SelectedIndex < DataGrid_Product.Items.Count)
            {
                m_cDistributionBox.Parts[DataGrid_Product.SelectedIndex].Quantity++;
                m_cDistributionBox.GetTotalPrice();
            }
        }

        /*添加所选的元件到元件列表:
         *    说明：
         * 添加的元件和当前列表中的所选元件是同一元件时，所选元件加1；
         * 添加的元件和当前列表中的所选元件不是同一元件时，在所选元件的下行添加；
         * 元件列表自动设置当前添加的元件为所选元件
         */
        private void TreeView_AllProducts_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            string strControlName = e.OriginalSource.GetType().Name;

            if (strControlName == "TextBlock")
            {
                TreeViewItem tviSelectedTreeViewItem = (TreeViewItem)((TreeView)sender).SelectedItem;

                if (!tviSelectedTreeViewItem.HasItems)
                {
                    string strXmlPath = "//*[@ID=" + tviSelectedTreeViewItem.Tag.ToString() + "]";
                    XmlNode xnProductNode = m_xlProductData.SelectSingleNode(strXmlPath);
                    if (xnProductNode.Name == "Product")
                    {
                        CProductPart cpProductItem = new CProductPart() { ID = "", Name = "", Quantity = 0, Price = 0, Description = "" };

                        cpProductItem.ID = xnProductNode.Attributes["ID"].Value;
                        cpProductItem.Name = xnProductNode.Attributes["name"].Value;
                        cpProductItem.Description = xnProductNode.Attributes["Description"].Value;
                        cpProductItem.Price = Convert.ToInt32(xnProductNode.Attributes["price"].Value);
                        cpProductItem.Quantity++;

                        //没有选择行，直接添加产品，
                        if (DataGrid_Product.SelectedIndex < 0)
                        {
                            m_cDistributionBox.Parts.Add(cpProductItem);
                            DataGrid_Product.SelectedIndex = m_cDistributionBox.Parts.Count - 1;
                        }
                        //选择的是空行，直接添加产品，表格的选择行不变
                        else if (DataGrid_Product.SelectedIndex >= DataGrid_Product.Items.Count)
                        {
                            m_cDistributionBox.Parts.Add(cpProductItem);
                            DataGrid_Product.SelectedIndex = m_cDistributionBox.Parts.Count - 1;
                        }
                        //或者选中的行和添加的行内容相同, 者选中的行增加1
                        else if (m_cDistributionBox.Parts[DataGrid_Product.SelectedIndex].ID == cpProductItem.ID)
                        {
                            m_cDistributionBox.Parts[DataGrid_Product.SelectedIndex].Quantity++;
                        }
                        //否者在选中的行后面增加
                        else
                        {
                            m_cDistributionBox.Parts.Insert(DataGrid_Product.SelectedIndex + 1, cpProductItem);
                            DataGrid_Product.SelectedIndex++;
                        }

                        m_cDistributionBox.GetTotalPrice();
                    }
                }

            }

        }


        //树控件展开节点
        private void TreeView_AllProducts_Expanded(object sender, RoutedEventArgs e)
        {
            TreeViewItem tvItem = (TreeViewItem)e.OriginalSource;
            if (tvItem.HasItems)
            {
                //获取第1个子节点的值，如果是*号，就清除当前所有子节点，重新加载子节点
                string strTreeViewItemHeader = tvItem.Items[0].ToString();
                if (strTreeViewItemHeader == "*")
                {
                    tvItem.Items.Clear();  //清除所有子节点
                    string strXmlXpath = "//*[@ID=" + tvItem.Tag.ToString() + "]";
                    XmlNode xnProductNode = m_xlProductData.SelectSingleNode(strXmlXpath);
                    //当前节点是否有子节点
                    if (xnProductNode.HasChildNodes)
                    {
                        xnProductNode = xnProductNode.FirstChild;

                        while (xnProductNode != null)
                        {
                            TreeViewItem tvNewTreeViewItem = new TreeViewItem();
                            tvNewTreeViewItem.Header = xnProductNode.Attributes["name"].Value;
                            tvNewTreeViewItem.Tag = xnProductNode.Attributes["ID"].Value;
                            tvItem.Items.Add(tvNewTreeViewItem);
                            if (xnProductNode.HasChildNodes)
                            {
                                tvNewTreeViewItem.Items.Add("*");
                            }
                            xnProductNode = xnProductNode.NextSibling;
                        }
                    }

                }
            }
        }

        //减去选中行的数量
        private void MenuItem_ReduceSelectedItem_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid_Product.SelectedIndex >= 0 && DataGrid_Product.SelectedIndex < DataGrid_Product.Items.Count)
            {
                m_cDistributionBox.Parts[DataGrid_Product.SelectedIndex].Quantity--;
                if (m_cDistributionBox.Parts[DataGrid_Product.SelectedIndex].Quantity == 0)
                    m_cDistributionBox.Parts.RemoveAt(DataGrid_Product.SelectedIndex);
                m_cDistributionBox.GetTotalPrice();
            }
        }

        private void gridProducts_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            m_cDistributionBox.GetTotalPrice();
        }

        private void MenuItem_Price_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Price.IsChecked)
                DataGridTextColumn_Price.Visibility = Visibility.Visible;
            else
                DataGridTextColumn_Price.Visibility = Visibility.Hidden;
        }

        private void MenuItem_Amount_Click(object sender, RoutedEventArgs e)
        {
            if (MenuItem_Amount.IsChecked)
                DataGridTextColumn_Amount.Visibility = Visibility.Visible;
            else
                DataGridTextColumn_Amount.Visibility = Visibility.Hidden;
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            _ = MessageBox.Show("郑高环@中沈防爆", "关于程序", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    //已选的元件
    public class CProductPart : INotifyPropertyChanged
    {
        public string ID { get; set; }  //产品编号
        public string Name { get; set; }  //产品名称
        public string Description { get; set; }  //产品描述
        public int Price { get; set; }  //产品价格

        //产品数量
        public int _quantity; //产品数量
        public int Quantity
        {
            get => _quantity;
            set
            {
                _quantity = value;
                if (PropertyChanged != null)
                {
                    PropertyChanged(this, new PropertyChangedEventArgs("Quantity"));
                    PropertyChanged(this, new PropertyChangedEventArgs("Amount"));
                }
            }
        }

        //产品总价
        public int _amount; //产品小计
        public int Amount
        {
            get
            {
                return Quantity * Price;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }


    //已选的元件的价格等的计算结果
    public class CDistributionBox : INotifyPropertyChanged
    {
        //产品部件
        private ObservableCollection<CProductPart> _parts = new ObservableCollection<CProductPart>();
        public ObservableCollection<CProductPart> Parts
        {
            get => _parts;
            set
            {
                _parts = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Summary"));
            }
        }

        //产品名称
        private string _name = "";
        public string Name { get; set; }


        //总价
        private int _price = 0;
        public int Price
        {
            get => _price;
            set
            {
                _price = value;
                PropertyChanged(this, new PropertyChangedEventArgs("Summary"));
            }
        }




        //定义属性
        public string Summary => "单价：" + _price.ToString() + " 元/台 (不含税不含运费)\r\n"
                       + "单价：" + ((int)(_price * 1.12)).ToString() + " 元/台 (含税不含运费)";


        public int GetTotalPrice()
        {
            int iTotalPrice = 0;
            for (int i = 0; i < Parts.Count; i++)
            {
                iTotalPrice += Parts[i].Amount;
            }
            Price = iTotalPrice;
            return iTotalPrice;
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }


}
