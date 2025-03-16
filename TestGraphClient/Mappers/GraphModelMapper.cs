using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestGraphClient.Models;
using TestGraphModel;

namespace TestGraphClient.Mappers
{
    public static class BLL_to_PL_mapper
    {
        public static GraphPL MapGraph(Graph _graph)
        {
            GraphPL graphPL = new GraphPL
            {
                Id = _graph.Id,
                GraphName = _graph.GraphName
            };

            //Словарь для хранения  Id узлов
            Dictionary<int, NodePL> _nodeMap = new Dictionary<int, NodePL>();
            foreach (var _node in _graph.Vertices)
            {
                var nodePL = MapNode(_node);
                _nodeMap[_node.Id] = nodePL;
                graphPL.AddVertex(nodePL);
            }

            foreach (var _edge in _graph.Edges)
            {
                EdgePL edgePL = MapEdge(_edge, _nodeMap);
                graphPL.AddEdge(edgePL);
            }
            return graphPL;
        }

        public static NodePL MapNode(Node node)
        {
            var nodePL = new NodePL(node.NodeName, node.Ports)
            {
                Id = node.Id,
                PortsNumber = node.PortsNumber,
                SimpleDataPL = MapNodeData(node.SimpleData),
                X = node.X,
                Y = node.Y
            };

            return nodePL;
        }

        public static EdgePL MapEdge(Edge _edge, Dictionary<int, NodePL> _nodeMap)
        {
            return new EdgePL(_nodeMap[_edge.Source.Id], _nodeMap[_edge.Target.Id])
            {
                Id = _edge.Id,
                PortSource = MapPort(_edge.PortSource),
                PortTarget = MapPort(_edge.PortTarget)
            };
        }

        public static PortPL MapPort(Port _port)
        {
            return new PortPL
            {
                Id = _port.Id,
                LocalId = _port.LocalId,
                InputPortNumber = _port.InputPortNumber,
                InputNodeName = _port.InputNodeName,
                IsLeftSidePort = _port.IsLeftSidePort
            };
        }

        public static NodeDataPL MapNodeData(NodeData _nodeData)
        {
            return new NodeDataPL
            {
                SomeText = _nodeData.SomeText,
                SomeValue = _nodeData.SomeValue
            };
        }
    }

    public static class PL_to_BLL_mapper
    {
        public static Graph MapGraph(GraphPL _graphPL)
        {
            Graph graph = new Graph
            {
                Id = _graphPL.Id,
                GraphName = _graphPL.GraphName
            };

            Dictionary<int, Node> nodeMap = new Dictionary<int, Node>();
            foreach (var nodePL in _graphPL.Vertices)
            {
                Node node = MapNode(nodePL);
                nodeMap[nodePL.Id] = node;
                graph.AddVertex(node);
            }

            foreach (var edgePL in _graphPL.Edges)
            {
                Edge edge = MapEdge(edgePL, nodeMap);
                graph.AddEdge(edge);
            }

            return graph;
        }

        public static Node MapNode(NodePL _nodePL)
        {
            Node node = new Node
            {
                Id = _nodePL.Id,
                PortsNumber = _nodePL.PortsNumber,
                NodeName = _nodePL.NodeNamePL,
                SimpleData = MapNodeData(_nodePL.SimpleDataPL),
                X = _nodePL.X,
                Y = _nodePL.Y
            };

            // Маппирование портов
            foreach (var portPL in _nodePL.LeftPorts)
            {
                Port port = MapPort(portPL);
                node.Ports.Add(port);
            }
            foreach (var portPL in _nodePL.RightPorts)
            {
                Port port = MapPort(portPL);
                node.Ports.Add(port);
            }
            return node;
        }

        public static Edge MapEdge(EdgePL _edgePL, Dictionary<int, Node> _nodeMap)
        {
            return new Edge(_nodeMap[_edgePL.Source.Id], _nodeMap[_edgePL.Target.Id])
            {
                Id = _edgePL.Id,
                PortSource = MapPort(_edgePL.PortSource),
                PortTarget = MapPort(_edgePL.PortTarget)
            };
        }

        public static Port MapPort(PortPL portPL)
        {
            return new Port
            {
                Id = portPL.Id,
                LocalId = portPL.LocalId,
                InputPortNumber = portPL.InputPortNumber,
                InputNodeName = portPL.InputNodeName,
                IsLeftSidePort = portPL.IsLeftSidePort
            };
        }

        public static NodeData MapNodeData(NodeDataPL _nodeDataPL)
        {
            if (_nodeDataPL != null)
            {
                return new NodeData
                {
                    SomeText = _nodeDataPL.SomeText,
                    SomeValue = _nodeDataPL.SomeValue
                };
            }
            else
            {
                return new NodeData
                {
                    SomeText = string.Empty,
                    SomeValue = 0
                };
            }
        }
    }
}
