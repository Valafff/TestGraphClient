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

namespace TestGraphClient.Windows
{
    public partial class CreateNodeWindow : Window
    {
        public string NodeName { get; private set; }
        public int PortCount { get; private set; }
        public string SelectedText { get; private set; }
        public int Number { get; private set; }

        public CreateNodeWindow()
        {
            InitializeComponent();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация введенных данных
            if (string.IsNullOrWhiteSpace(NodeNameTextBox.Text))
            {
                MessageBox.Show("Введите название узла.");
                return;
            }

            if (!int.TryParse(PortCountTextBox.Text, out int portCount) || portCount < 0 || portCount > 10)
            {
                MessageBox.Show("Количество портов должно быть числом от 0 до 10.");
                return;
            }

            if (TextComboBox.SelectedItem == null)
            {
                MessageBox.Show("Выберите текст из списка.");
                return;
            }

            if (!int.TryParse(NumberTextBox.Text, out int number))
            {
                MessageBox.Show("Введите корректное число.");
                return;
            }

            NodeName = NodeNameTextBox.Text;
            PortCount = portCount;
            SelectedText = (TextComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            Number = number;

            //Условие записи данных в основную форму
            DialogResult = true;

            Close();
        }
    }
}
