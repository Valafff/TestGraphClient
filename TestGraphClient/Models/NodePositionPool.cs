using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace TestGraphClient.Models
{
    public  class NodePositionPool
    {
        // Потокобезопасный словарь для хранения позиций узлов
        private  readonly ConcurrentDictionary<int, Point> _nodePositions;

        public  NodePositionPool()
        {
            _nodePositions = new ConcurrentDictionary<int, Point>();
        }


        public  void AddOrUpdateNodePosition(int nodeId, Point position)
        {
            _nodePositions.AddOrUpdate(nodeId, position, (id, oldPosition) => position);
        }

        public  Point? GetNodePosition(int nodeId)
        {
            if (_nodePositions.TryGetValue(nodeId, out Point position))
            {
                return position;
            }
            return null;
        }

        public  bool RemoveNodePosition(int nodeId)
        {
            return _nodePositions.TryRemove(nodeId, out _);
        }

        public  void Clear()
        {
            _nodePositions.Clear();
        }

        public  int Count => _nodePositions.Count;
    }
}
