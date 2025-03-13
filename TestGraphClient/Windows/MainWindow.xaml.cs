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
using TestGraphClient.Mappers;
using TestGraphClient.Models;
using TestGraphClientLogic;
using TestGraphModel;

namespace TestGraphClient.Windows;

public partial class MainWindow : Window
{
    const string _uri = "http://localhost:3000/api/graph";
    GraphClientLogic logic = new GraphClientLogic(_uri);
    GraphPL graph = new GraphPL();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void ConnTest(object sender, RoutedEventArgs e)
    {
        SendMessage("Попытка подключения к серверу");
        bool responce = await logic.ConnectionTest();
        if (responce)
        {
            SendMessage("Подключение к серверу OK");
        }
        else SendMessage("Ответ от сервера не получен");
    }




    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    void SendMessage(string _message)
    {
        if (!string.IsNullOrWhiteSpace(_message))
        {
            ConsoleListBox.Items.Add(DateTime.Now.ToString("hh:mm:ss") + "\t" + _message);
        }
    }

    private async void GetGraph(object sender, RoutedEventArgs e)
    {
        SendMessage("Попытка получения графа");
        Graph temp = await logic.GetGraph();
        if (temp != null)
        {
            try
            {
                graph = BLL_to_PL_mapper.MapGraph(temp);
                SendMessage("Граф получен");
            }
            catch (Exception ex)
            {
                SendMessage($"Ошибка получения графа: {ex}");
            }

        }
        else { SendMessage("Ошибка получения графа: не получен ответ от сервера"); }
    }

    private async void CreateNewNode(object sender, RoutedEventArgs e)
    {
        SendMessage("Попытка создания нового узла");
        NodePL ppl = new NodePL("test", 3);
        Graph temp = await logic.CreateNode(PL_to_BLL_mapper.MapNode(ppl));
        if (temp != null)
        {
            try
            {
                graph = BLL_to_PL_mapper.MapGraph(temp);
                SendMessage("Узел создан");
            }
            catch (Exception ex)
            {
                SendMessage($"Узел не создан: {ex}");
            }
        }
        else { SendMessage("Ошибка создания нового узла"); }
    }
}