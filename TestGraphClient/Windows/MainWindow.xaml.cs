using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

    //ObjectPool(Подслушано на RadioDotNet, вероятно излишне, просто попытка реализовать) Отслеживает координаты узлов - при обновлении графа возвращает актуальные координаты узлам
    private NodePositionPool nodePositionPool = new NodePositionPool();

    //Работа с ребрами
    private PortPL tempSourcePort;
    private PortPL tempTargetPort;
    private PortPL hoverPort;
    private Line _tempLine;
    private bool isDragging = false; // Флаг для отслеживания перетаскивания
    private bool isDraggingStartPoint = false; // Флаг для определения, какой конец линии перетаскивается

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
        KeyDown += Edge_Delete;
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
            ConsoleListBox.Items.Add(DateTime.Now.ToString("hh:mm:ss") + ": " + _message);
            ConsoleScrollViewer.ScrollToEnd();
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
                DeleteAllLines();
                Line_Inicialization();
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
                ports.Add(new Port { IsLeftSidePort = i % 2 != 0, LocalId = i+1 });
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

    //Создание, редактирование ребра
    private async void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (isDragging)
        {
            // Отписываемся от событий мыши
            GraphCanvas.MouseMove -= GraphCanvas_MouseMove;
            GraphCanvas.MouseLeftButtonUp -= GraphCanvas_MouseLeftButtonUp;

            // Освобождаем мышь
            GraphCanvas.ReleaseMouseCapture();
            isDragging = false;

            //Запись ребра
            if (hoverPort != null)
            {
                tempTargetPort = hoverPort;

                if (tempSourcePort != null && tempTargetPort != null)
                {
                    var edg = graph.Edges.FirstOrDefault(e => e.PortSource.Id == tempSourcePort.Id);
                    if (edg != null)
                    {
                        tempSourcePort.InputPortNumber = edg.PortSource.InputPortNumber;
                        tempSourcePort.InputNodeName = edg.PortSource.InputNodeName;
                        tempTargetPort.InputPortNumber = edg.PortTarget.InputPortNumber;
                        tempTargetPort.InputNodeName = edg.PortTarget.InputNodeName;
                    }
                    SendMessage("Попытка создания ребра");
                    EdgePL tempEdge = new EdgePL(tempSourcePort.NodeOwner, tempTargetPort.NodeOwner, tempSourcePort, tempTargetPort);
                    Graph temp = await logic.CreateOrEditEdge(PL_to_BLL_mapper.MapEdge(tempEdge.Id, tempEdge, tempSourcePort.NodeOwner, tempTargetPort.NodeOwner));
                    if (temp != null)
                    {
                        try
                        {
                            graph = BLL_to_PL_mapper.MapGraph(temp);
                            GetSavedNodePositions();
                            DataContext = graph;
                            var te = graph.Edges.FirstOrDefault(e => e.Id == graph.Edges.Max(e => e.Id));
                            SendMessage($"Ребро создано! Id ребра: {te.Id}. Узел источник: {te.Source.NodeNamePL}. Локальный Id порта источника: {te.PortSource.LocalId}." +
                                $" Узел приемник: {te.Target.NodeNamePL}. Локальный Id порта приемника: {te.PortTarget.LocalId}");
                            Line tempLine = new Line
                            {
                                Stroke = Brushes.Black,
                                StrokeThickness = 3,
                                X1 = tempSourcePort.NodeOwner.X + tempSourcePort.X,
                                Y1 = tempSourcePort.NodeOwner.Y + tempSourcePort.Y + 5,
                                X2 = tempTargetPort.NodeOwner.X + tempTargetPort.X,
                                Y2 = tempTargetPort.NodeOwner.Y + tempTargetPort.Y + 5
                            };
                            GraphCanvas.Children.Remove(_tempLine);
                            GraphCanvas.Children.Add(tempLine);
                            DeleteAllLines();
                            Line_Inicialization();
                        }
                        catch (Exception ex)
                        {
                            GraphCanvas.Children.Remove(_tempLine);
                            SendMessage($"Ребро не создано: {ex}");
                        }
                    }
                    else { SendMessage("Ошибка создания ребра"); GraphCanvas.Children.Remove(_tempLine); }
                }
            }


            // Сбрасываем порт источник и порт приемник
            tempSourcePort = null;
            tempTargetPort = null;
        }
    }

    //Удаление ребра
    private async void Edge_Delete(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Delete && _tempLine != null)
        {
            //Удаление ребра
            EdgePL targetEdge = graph.Edges.FirstOrDefault(e => Math.Round(e.Source.X + e.PortSource.X) == Math.Round(_tempLine.X1)
            && Math.Round(e.Source.Y + e.PortSource.Y + 5) == Math.Round(_tempLine.Y1)
            && Math.Round(e.Target.X + e.PortTarget.X) == Math.Round(_tempLine.X2)
            && Math.Round(e.Target.Y + e.PortTarget.Y + 5) == Math.Round(_tempLine.Y2));

            if (targetEdge != null)
            {
                Graph temp = await logic.DeleteEdge(targetEdge.Id);
                if (temp != null)
                {
                    try
                    {
                        graph = BLL_to_PL_mapper.MapGraph(temp);
                        GetSavedNodePositions();
                        DataContext = graph;
                        SendMessage("Ребро удалено");
                    }
                    catch (Exception ex)
                    {
                        SendMessage($"Ребро не удалено: {ex}");
                    }
                }
                else { SendMessage("Ошибка удаления ребра"); }

                //Удаление линии канвы
                GraphCanvas.Children.Remove(_tempLine);
                _tempLine = null;
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

            DeleteAllLines();
            Line_Inicialization();
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
            SendMessage($"Выбран порт: Узел-владелец: {nodeOwner.NodeNamePL}, Локальный ID порта: {port.LocalId}");

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

    //Редактирование Узла
    private async void NodeEdit_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
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

            // Сброс состояния перемещения
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
            _tempLine.CaptureMouse();
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

    private void Line_Inicialization()
    {
        foreach (var edge in graph.Edges)
        {
            Point sourcePoint;
            sourcePoint.X = edge.Source.X;
            sourcePoint.Y = edge.Source.Y;
            Point targetPoint;
            targetPoint.X = edge.Target.X;
            targetPoint.Y = edge.Target.Y;

            Line tempLine = new Line
            {
                Stroke = Brushes.Black,
                StrokeThickness = 4,
                X1 = sourcePoint.X + edge.PortSource.X,
                Y1 = sourcePoint.Y + edge.PortSource.Y + 5,
                X2 = targetPoint.X + edge.PortTarget.X,
                Y2 = targetPoint.Y + edge.PortTarget.Y + 5
            };
            tempLine.MouseDown += Line_MouseDown;
            tempLine.MouseUp += Line_MouseUp;
            GraphCanvas.Children.Add(tempLine);
        }
    }

    private void DeleteAllLines()
    {
        var linesToRemove = GraphCanvas.Children.OfType<Line>().ToList();
        foreach (var line in linesToRemove)
        {
            GraphCanvas.Children.Remove(line);
        }
    }
}