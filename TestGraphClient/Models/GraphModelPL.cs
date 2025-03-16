using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Shapes;
using QuikGraph;
using TestGraphClient.Mappers;
using TestGraphModel;

namespace TestGraphClient.Models
{
    public class GraphPL : BidirectionalGraph<NodePL, EdgePL>, INotifyPropertyChanged
    {
        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(nameof(Id)); }
        }

        private string _graphName;
        public string GraphName
        {
            get => _graphName;
            set { _graphName = value; OnPropertyChanged(nameof(GraphName)); }
        }

        public Dictionary<int, NodePL> NodeMap { get; set; } = new Dictionary<int, NodePL>();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class EdgePL : IEdge<NodePL>
    {
        public int Id { get; set; }
        public NodePL Source { get; set; }
        public NodePL Target { get; set; }
        public PortPL PortSource { get; set; }
        public PortPL PortTarget { get; set; }

        public EdgePL(NodePL source, NodePL target)
        {
            Source = source;
            Target = target;
        }
        public EdgePL(NodePL source, NodePL target, PortPL portSource, PortPL portTarget)
        {
            Source = source;
            Target = target;
            PortSource = portSource;
            PortTarget = portTarget;
        }
    }

    public class NodePL : BaseNotify
    {
        public int Id { get; set; }
        public int PortsNumber { get; set; }

        private string _nodeName;
        public string NodeNamePL
        {
            get => _nodeName;
            set => SetField(ref _nodeName, value);
        }

        public NodeDataPL SimpleDataPL { get; set; }
        public ObservableCollection<PortPL> LeftPorts { get; set; }
        public ObservableCollection<PortPL> RightPorts { get; set; }

        private double _x;
        public double X
        {
            get => _x;
            set => SetField(ref _x, value);
        }

        private double _y;
        public double Y
        {
            get => _y;
            set => SetField(ref _y, value);
        }

        public NodePL(string name, List<Port> _ports, double node_X=0, double node_Y = 0, int startY = 10, int stepY = 20)
        {
            NodeNamePL = name;
            LeftPorts = new ObservableCollection<PortPL>();
            RightPorts = new ObservableCollection<PortPL>();
            X = node_X;
            Y = node_Y;

            for (int i = 0; i < _ports.Count; i++)
            {
                PortPL tempPortPL = BLL_to_PL_mapper.MapPort(_ports[i]);
                tempPortPL.NodeOwner = this;
                tempPortPL.LocalId = i;
                tempPortPL.X = (_ports[i].IsLeftSidePort ? 0 : 180);
                //X1 = nodeOwner.X + (port.IsLeftSidePort ? 0 : 180),
                //Y1 = nodeOwner.Y + port.Y + ellipse.Height / 2,

                if (!_ports[i].IsLeftSidePort)
                {

                    tempPortPL.Y =  startY + RightPorts.Count * stepY;
                    RightPorts.Add(tempPortPL);
                }
                else
                {
                    tempPortPL.Y =  startY + LeftPorts.Count * stepY;
                    LeftPorts.Add(tempPortPL);
                }
            }
        }
    }

    public class PortPL
    {
        public int Id { get; set; }
        public int LocalId { get; set; }
        public int InputPortNumber { get; set; }
        public NodePL NodeOwner { get; set; }
        public string? InputNodeName { get; set; }
        public bool IsLeftSidePort { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
    }

    public class NodeDataPL : BaseNotify
    {
        private string _someText;
        public string SomeText
        {
            get => _someText;
            set => SetField(ref _someText, value);
        }

        private int _someValue;
        public int SomeValue
        {
            get => _someValue;
            set => SetField(ref _someValue, value);
        }
    }
}
