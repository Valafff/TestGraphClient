using Newtonsoft.Json;
using QuikGraph;
using System;
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
    //Отслеживает координаты узлов
    private NodePositionPool nodePositionPool = new NodePositionPool();

    //Работа с ребрами
    private PortPL tempSourcePort;
    private PortPL tempTargetPort;
    private PortPL hoverPort;
    private Line _tempLine;
    private bool isDragging = false; // Флаг для отслеживания перетаскивания
    private bool isDraggingStartPoint = false; // Флаг для определения, какой конец линии перетаскивается
    private Dictionary<EdgePL, Line> _edgeLines = new Dictionary<EdgePL, Line>();

    //Работа с визуальной частью
    private NodePL _draggedNode; // Узел, который перемещается
    private Point _offset; // Смещение курсора относительно верхнего левого угла узла

    //Для обработки двойного клика по ноде
    DateTime _lastClickTime;
    const int DoubleClickTimeout = 500;


    public MainWindow()
    {
        InitializeComponent();
        GetGraph(null, null);
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
            string selectedText = createNodeWindow.SomeText;
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
        var ellipse = sender as Ellipse;
        if (ellipse == null) return;

        var port = ellipse.DataContext as PortPL;
        if (port == null) return;

        var nodeOwner = port.NodeOwner;
        if (nodeOwner == null) return;

        if (tempSourcePort == null)
        {
            // Выбираем пор-источник
            tempSourcePort = port;
            SendMessage($"Выбран первый порт: Узел-владелец: {nodeOwner.NodeNamePL}, Локальный ID порта: {port.LocalId}");

            _tempLine = new Line
            {
                Stroke = Brushes.Black,
                StrokeThickness = 3,
                X1 = nodeOwner.X + (port.IsLeftSidePort ? 0 : 180),
                Y1 = nodeOwner.Y + port.Y + ellipse.Height / 2,
                X2 = e.GetPosition(GraphCanvas).X,
                Y2 = e.GetPosition(GraphCanvas).Y
            };
            GraphCanvas.Children.Add(_tempLine);
            GraphCanvas.MouseMove += GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp += GraphCanvas_MouseLeftButtonUp;
            GraphCanvas.CaptureMouse();
            isDragging = true;
        }
        //else
        //{
        //    // Проверяем, что второй порт принадлежит другому узлу
        //    if (port.NodeOwner == tempSourcePort.NodeOwner)
        //    {
        //        SendMessage("Нельзя соединить порты одного узла!");
        //        return;
        //    }

        //    // Выбираем второй порт
        //    tempTargetPort = port;
        //    SendMessage($"Выбран второй порт: Узел-владелец: {nodeOwner.NodeNamePL}, Локальный ID порта: {port.LocalId}");

        //    // Удаляем временную линию
        //    GraphCanvas.Children.Remove(_tempLine);

        //    // Отписываемся от событий мыши
        //    GraphCanvas.MouseMove -= GraphCanvas_MouseMove;
        //    GraphCanvas.MouseLeftButtonUp -= GraphCanvas_MouseLeftButtonUp;

        //    // Освобождаем мышь
        //    GraphCanvas.ReleaseMouseCapture();

        //    // Создаем ребро между портами
        //    //CreateEdge(_selectedPort1, _selectedPort2);

        //    // Сбрасываем выбранные порты
        //    tempSourcePort = null;
        //    tempTargetPort = null;
        //    isDragging = false;
        //}
    }

    private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging && _tempLine != null)
        {
            // Получаем текущую позицию курсора относительно Canvas
            var position = e.GetPosition(GraphCanvas);

            // Обновляем координаты временной линии
            _tempLine.X2 = position.X;
            _tempLine.Y2 = position.Y;
        }
    }

    //Установка второго порта - порта назначения осуществляется тут
    private async void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isDragging)
        {
            // Отписываемся от событий мыши
            GraphCanvas.MouseMove -= GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp -= GraphCanvas_MouseLeftButtonUp;

            // Удаляем временную линию
            GraphCanvas.Children.Remove(_tempLine);

            // Освобождаем мышь
            GraphCanvas.ReleaseMouseCapture();
            isDragging = false;

            //Запись ребра
            if (hoverPort != null)
            {
                tempTargetPort = hoverPort;

                if (tempSourcePort != null && tempTargetPort != null)
                {
                    SendMessage("Попытка создания ребра");
                    EdgePL tempEpdge = new EdgePL(tempSourcePort.NodeOwner, tempTargetPort.NodeOwner, tempSourcePort, tempTargetPort);
                    Graph temp = await logic.CreateOrEditEdge(PL_to_BLL_mapper.MapEdge(tempEpdge.Id, tempEpdge, tempSourcePort.NodeOwner, tempTargetPort.NodeOwner));
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


            // Сбрасываем состояние
            tempSourcePort = null;
            tempTargetPort = null;
        }
    }

    private void Ellipse_MouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is Ellipse ellipse)
        {
            if (ellipse.DataContext is PortPL)
            {
                hoverPort = ellipse.DataContext as PortPL;
                return;
            }
        }
        hoverPort = null;
    }


    private void Node_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {

        //Проверка нажатия на порт
        if (e.OriginalSource is Ellipse)
        {
            // Если клик был на порту событие еще не обработано, едем дальше
            e.Handled = false;
            return;
        }

        //Нода, на которую нажали
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

    private async void Node_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        // Двойной клик - редактирование ноды
        DateTime currentTime = DateTime.Now;
        if ((currentTime - _lastClickTime).TotalMilliseconds <= DoubleClickTimeout)
        {
            var contentPresenter = sender as ContentPresenter;
            var node = contentPresenter?.DataContext as NodePL;
            if (node == null) return;

            contentPresenter?.ReleaseMouseCapture();
            _draggedNode = null;
            RefreshNodePositions();

            var editNodeWindow = new EditNodeWindow(node.NodeNamePL, node.SimpleDataPL.SomeText, node.SimpleDataPL.SomeValue);
            if (editNodeWindow.ShowDialog() == true)
            {
                node.NodeNamePL = editNodeWindow.NodeName;
                node.SimpleDataPL.SomeText = editNodeWindow.SomeText;
                node.SimpleDataPL.SomeValue = editNodeWindow.Number;
                Graph temp = await logic.EditNode(PL_to_BLL_mapper.MapNode(node));
                if (temp != null)
                {
                    try
                    {
                        graph = BLL_to_PL_mapper.MapGraph(temp);
                        GetSavedNodePositions();
                        DataContext = graph;
                        SendMessage("Узел отредактирован");
                    }
                    catch (Exception ex)
                    {
                        SendMessage($"Узел не отредактирован: {ex}");
                    }
                }
            }
            else { SendMessage("Узел не редактировался"); }
        }


        if (_draggedNode != null)
        {
            // Освобождаем мышь
            var contentPresenter = sender as ContentPresenter;
            contentPresenter?.ReleaseMouseCapture();

            // Сброс состояние перемещения
            _draggedNode = null;
            RefreshNodePositions();
        }
        //Время последнего клика
        _lastClickTime = DateTime.Now;
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

    private void Line_MouseDown(object sender, MouseButtonEventArgs e)
    {
        // Снимаем выделение с предыдущей линии
        if (_tempLine != null)
        {
            _tempLine.Stroke = Brushes.Black;
        }

        // Выделяем новую линию
        _tempLine = sender as Line;
        if (_tempLine != null)
        {
            _tempLine.Stroke = Brushes.Red;

            // Определяем, какой конец линии был нажат
            Point mousePosition = e.GetPosition(GraphCanvas);
            double startDistance = Distance(mousePosition, new Point(_tempLine.X1, _tempLine.Y1));
            double endDistance = Distance(mousePosition, new Point(_tempLine.X2, _tempLine.Y2));

            // Если расстояние до начальной точки меньше, перетаскиваем её
            isDraggingStartPoint = startDistance < endDistance;
            isDragging = true; // Начинаем перетаскивание

            // Захватываем мышь, чтобы события MouseMove обрабатывались даже за пределами линии
            _tempLine.CaptureMouse();
        }
    }

    private void Line_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDragging && _tempLine != null)
        {
            // Получаем текущие координаты курсора
            Point mousePosition = e.GetPosition(GraphCanvas);

            // Обновляем координаты выбранного конца линии
            if (isDraggingStartPoint)
            {
                _tempLine.X1 = mousePosition.X;
                _tempLine.Y1 = mousePosition.Y;
            }
            else
            {
                _tempLine.X2 = mousePosition.X;
                _tempLine.Y2 = mousePosition.Y;
            }
        }
    }

    private void Line_MouseUp(object sender, MouseButtonEventArgs e)
    {
        // Завершаем перетаскивание
        isDragging = false;

        // Освобождаем захват мыши
        if (_tempLine != null)
        {
            _tempLine.ReleaseMouseCapture();
        }
    }

    // Вспомогательная функция для вычисления расстояния между двумя точками
    private double Distance(Point p1, Point p2)
    {
        return Math.Sqrt(Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2));
    }
}