using System;
using System.Windows;
using System.Windows.Controls;

namespace TestGraphClient.Windows
{
    public partial class EditNodeWindow : Window
    {
        public string NodeName { get; private set; }
        public string SomeText { get; private set; }
        public int Number { get; private set; }

        public EditNodeWindow(string nodeName, string selectedText, int number)
        {
            InitializeComponent();

            // Инициализация полей начальными значениями
            NodeNameTextBox.Text = nodeName;
            TextComboBox.SelectedItem = TextComboBox.Items.Cast<ComboBoxItem>()
                .FirstOrDefault(item => item.Content.ToString() == selectedText);
            NumberTextBox.Text = number.ToString();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // Валидация введенных данных
            if (string.IsNullOrWhiteSpace(NodeNameTextBox.Text))
            {
                MessageBox.Show("Введите название узла.");
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

            // Сохраняем отредактированные данные
            NodeName = NodeNameTextBox.Text;
            SomeText = (TextComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
            Number = number;

            // Закрываем окно с результатом DialogResult = true
            DialogResult = true;
            Close();
        }
    }
}