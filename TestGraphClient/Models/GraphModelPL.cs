using System.Collections.ObjectModel;
using System.ComponentModel;
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
        //public List<PortPL> LeftPorts { get; set; }
        //public List<PortPL> RightPorts { get; set; }
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

        public NodePL(string name, List<Port> _ports, int startY = 10, int stepY = 20)
        {
            NodeNamePL = name;
            LeftPorts = new ObservableCollection<PortPL>();
            RightPorts = new ObservableCollection<PortPL>();

            //for (int i = 0; i < _ports.Count; i++)
            //{
            //    if (i % 2 == 0)
            //    {
            //        RightPorts.Add(new PortPL { NodeOwner = this, LocalId = i, Y = startY + i * stepY });
            //    }
            //    else
            //    {
            //        LeftPorts.Add(new PortPL { NodeOwner = this, LocalId = i, Y = startY + i * stepY });
            //    }
            //}

            for (int i = 0; i < _ports.Count; i++)
            {
                PortPL tempPortPL = BLL_to_PL_mapper.MapPort(_ports[i]);
                tempPortPL.NodeOwner = this;
                tempPortPL.LocalId = i;
                tempPortPL.Y = startY + i * stepY;
                if (!_ports[i].IsLeftSidePort)
                {
                    RightPorts.Add(tempPortPL);
                }
                else
                {
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

//namespace TestGraphClient.Models
//{
//    public class GraphPL : BidirectionalGraph<NodePL, EdgePL>, INotifyPropertyChanged
//    {
//        private int _id;
//        public int Id
//        {
//            get => _id;
//            set { _id = value; OnPropertyChanged(nameof(Id)); }
//        }

//        private string _graphName;
//        public string GraphName
//        {
//            get => _graphName;
//            set { _graphName = value; OnPropertyChanged(nameof(GraphName)); }
//        }

//        public event PropertyChangedEventHandler? PropertyChanged;
//        protected virtual void OnPropertyChanged(string propertyName)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    };

//    public class EdgePL(NodePL source, NodePL target) : IEdge<NodePL>
//    {
//        public int Id { get; set; }
//        public NodePL Source { get; set; } = source;
//        public NodePL Target { get; set; } = target;
//        public PortPL PortSource { get; set; }
//        public PortPL PortTarget { get; set; }
//    }

//    public class NodePL : BaseNotify
//    {
//        public int Id { get; set; }
//        public int PortsNumber { get; set; }

//        private string _nodeName;
//        public string NodeNamePL 
//        {
//          get => _nodeName;
//          set => SetField(ref _nodeName, value);
//        }

//        public NodeDataPL SimpleDataPL { get; set; }
//        public List<PortPL> LeftPorts { get; set; }
//        public List<PortPL> RightPorts { get; set; }

//        private double _x;
//        public double X
//        {
//            get => _x;
//            set => SetField(ref _x, value);
//        }

//        private double _y;
//        public double Y
//        {
//            get => _y;
//            set => SetField(ref _y, value);
//        }



//        public NodePL(string name, int PortCount, int start_Y = 10, int step_Y = 20)
//        {
//            NodeNamePL = name;
//            LeftPorts = new List<PortPL>();
//            RightPorts = new List<PortPL>();



//            for (int i = 0; i < PortCount; i++)
//            {
//                if (i%2 == 0)
//                {
//                    RightPorts.Add(new PortPL { NodeOwner = this, LocalId = i, Y = start_Y + i * step_Y });
//                }
//                else
//                {
//                    LeftPorts.Add(new PortPL { NodeOwner = this, LocalId = i, Y = start_Y + i * step_Y });
//                }

//            }
//        }

//        public event PropertyChangedEventHandler PropertyChanged;
//        protected virtual void OnPropertyChanged(string propertyName)
//        {
//            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
//        }
//    }

//    public class PortPL
//    {
//        public int Id { get; set; }
//        public int LocalId { get; set; }
//        public int InputPortNumber { get; set; }
//        public NodePL NodeOwner { get; set; }
//        public string? InputNodeName { get; set; }
//        public bool IsLeftSidePort { get; set; }

//        public double X { get; set; }
//        public double Y { get; set; }
//    }


//    public class NodeDataPL:BaseNotify
//    {
//        private string _someText;
//        public string SomeText
//        { get => _someText;
//          set => SetField(ref _someText, value);
//        }
//        private int _someValue;
//        public int SomeValue 
//        {
//          get => _someValue;
//          set => SetField(ref _someValue, value);
//        }
//    }

//}
