﻿using Newtonsoft.Json;
using QuikGraph;
using System.Net.Http;
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
    private NodePositionPool nodePositionPool = new NodePositionPool();

    //Работа с визуальной частью
    private NodePL _draggedNode; // Узел, который перемещается
    private Point _offset; // Смещение курсора относительно верхнего левого угла узла


    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        logic.SetGraph(PL_to_BLL_mapper.MapGraph(graph));
    }
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
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
                GetSavedNodePositions();

                DataContext = graph;
                SendMessage("Граф получен");
            }
            catch (Exception ex)
            {
                SendMessage($"Ошибка получения графа: {ex}");
            }
        }
        else
        {
            SendMessage("Ошибка получения графа: не получен ответ от сервера");
        }
    }

    private async void CreateNewNode(object sender, RoutedEventArgs e)
    {
        SendMessage("Попытка создания нового узла");
        var createNodeWindow = new CreateNodeWindow();
        if (createNodeWindow.ShowDialog() == true)
        {
            string nodeName = createNodeWindow.NodeName;
            int portCount = createNodeWindow.PortCount;
            string selectedText = createNodeWindow.SelectedText;
            int number = createNodeWindow.Number;

            var ports = new List<Port>();
            for (int i = 0; i < portCount; i++)
            {
                ports.Add(new Port { IsLeftSidePort = i % 2 != 0 });
            }
            var newNode = new NodePL(nodeName, ports)
            {
                SimpleDataPL = new NodeDataPL
                {
                    SomeText = selectedText,
                    SomeValue = number
                }
            };
            Graph temp = await logic.CreateNode(PL_to_BLL_mapper.MapNode(newNode));
            if (temp != null)
            {
                try
                {
                    graph = BLL_to_PL_mapper.MapGraph(temp);
                    GetSavedNodePositions();
                    DataContext = graph;
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

    private void RefreshNodePositions()
    {
        foreach (var node in graph.Vertices)
        {
            nodePositionPool.AddOrUpdateNodePosition(node.Id, new Point { X = node.X, Y = node.Y });
        }
    }

    private void GetSavedNodePositions()
    {
        foreach (var node in graph.Vertices)
        {
            Point? pos = nodePositionPool.GetNodePosition(node.Id);
            if (pos != null)
            {
                node.X = pos.Value.X;
                node.Y = pos.Value.Y;
            }
        }
    }


    private void ItemsControl_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (_draggedNode != null && e.LeftButton == MouseButtonState.Pressed)
        {
            // Получаем текущую позицию курсора относительно Canvas
            var canvas = sender as ItemsControl;
            var position = e.GetPosition(canvas);

            // Обновляем координаты узла с учетом смещения
            _draggedNode.X = position.X - _offset.X;
            _draggedNode.Y = position.Y - _offset.Y;
        }
    }

    private void Port_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {

    }
    private void Node_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Получаем узел, на который нажали
        var contentPresenter = sender as ContentPresenter;
        _draggedNode = contentPresenter?.DataContext as NodePL;

        if (_draggedNode != null)
        {
            // Запоминаем смещение курсора относительно верхнего левого угла узла
            var position = e.GetPosition(contentPresenter);
            _offset = new Point(position.X, position.Y);

            // Захватываем мышь, чтобы получать события даже за пределами узла
            contentPresenter.CaptureMouse();
        }
    }
    private void Node_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (_draggedNode != null)
        {
            // Освобождаем мышь
            var contentPresenter = sender as ContentPresenter;
            contentPresenter?.ReleaseMouseCapture();

            // Сбрасываем состояние перемещения
            _draggedNode = null;
            RefreshNodePositions();
        }
    }
}