using System.ComponentModel;
using QuikGraph;

namespace TestGraphClient.Models
{
    public class GraphModel
    {
        public BidirectionalGraph<Node, Edge> Graph { get; }

        public GraphModel()
        {
            Graph = new BidirectionalGraph<Node, Edge>();
        }
    }

    public class Edge : IEdge<Node>
    {
        public Node Source { get; }
        public Node Target { get; }
        

        public Edge(Node source, Node target)
        {
            Source = source;
            Target = target;
        }
    }

    public class Node : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public List<Port> LeftPorts { get; set; }
        public List<Port> RightPorts { get; set; }
        private double _x;
        private double _y;

        public double X
        {
            get => _x;
            set
            {
                _x = value;
                OnPropertyChanged(nameof(X));
            }
        }

        public double Y
        {
            get => _y;
            set
            {
                _y = value;
                OnPropertyChanged(nameof(Y));
            }
        }

        public Node(string name, int leftPortCount, int rightPortCount)
        {
            Name = name;
            LeftPorts = new List<Port>();
            for (int i = 0; i < leftPortCount; i++)
            {
                LeftPorts.Add(new Port { Node = this, Index = i, Y = 10 + i * 20 });
            }

            RightPorts = new List<Port>();
            for (int i = 0; i < rightPortCount; i++)
            {
                RightPorts.Add(new Port { Node = this, Index = i, Y = 10 + i * 20 });
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class Port
    {
        public Node Node { get; set; }
        public int Index { get; set; }
        public double Y { get; set; }
    }

}
