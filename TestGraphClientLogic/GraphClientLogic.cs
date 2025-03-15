using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using TestGraphModel;

namespace TestGraphClientLogic
{
    public class GraphClientLogic
    {
        private HttpClient client;
        private string serverUrl;
        private Graph graph;


        public GraphClientLogic(string _uri = "http://localhost:3000/api/graph")
        {
            serverUrl = _uri;
            client = new HttpClient();
            graph = new Graph();
        }

        //Проверка соединения
        public async Task<bool> ConnectionTest()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"{serverUrl}/ping");
                return response.IsSuccessStatusCode;
            }
            catch (Exception)
            {
                return false;
            }
        }

        //Получение модели графа
        public async Task<Graph> GetGraph()
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync($"{serverUrl}/sendgraph");
                string content = await response.Content.ReadAsStringAsync();
                GraphDtoToGrath(content, ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            catch (Exception)
            {
                return null;
            }
        }

        //Отправление модели графа
        public async Task<bool> SetGraph(Graph _graph)
        {
            // Сериализуем объект Graph в JSON
            string json = JsonConvert.SerializeObject(GraphToGraphDto( _graph, out bool _error), Formatting.Indented);
            // Создаем контент для отправки
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            // Отправляем POST-запрос
            HttpResponseMessage response = await client.PostAsync($"{serverUrl}/setgraphstate", content);

            // Проверяем успешность запроса
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Граф успешно отправлен на сервер.");
                return true;
            }
            else
            {
                Console.WriteLine("Ошибка при отправке графа: " + response.StatusCode);
                return false;
            }

            //if (_graph != null)
            //{
            //    GraphDto temp = GraphToGraphDto(_graph, out bool _error);
            //    string json = JsonConvert.SerializeObject(temp, Formatting.Indented);
            //    StringContent content = new StringContent(json, Encoding.UTF8, "application/json");
            //    HttpResponseMessage response = await client.PostAsync($"{serverUrl}/getgraph", content);

            //    Console.WriteLine(response.IsSuccessStatusCode);

            //    if (response.IsSuccessStatusCode)
            //    {
            //        return true;
            //    }
            //    else return false;
            //}
            //else return false;

            return true;
        }

        //Создание узла
        public async Task<Graph> CreateNode(Node _newNode)
        {
            //Если ошибка - возвращается null
            if (_newNode == null) return null;
            if (string.IsNullOrEmpty(_newNode.NodeName)) return null;

            string json = JsonConvert.SerializeObject(_newNode, Formatting.Indented);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{serverUrl}/createnode", content);
            if (response.IsSuccessStatusCode)
            {
                GraphDtoToGrath(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            //Если ошибка - возвращается null если statuscode 200 - возвращается обновленный граф
            return null;
        }

        //Редактирование узла
        public async Task<Graph> EditNode(Node _newNode)
        {
            //Если ошибка - возвращается  null если statuscode 200 - возвращается обновленный граф
            if (_newNode == null) return null;
            if (string.IsNullOrEmpty(_newNode.NodeName)) return null;
            Node temp = graph.Vertices.FirstOrDefault(n => n.Id == _newNode.Id);
            if (temp == null) return null;

            //JsonSerializerOptions options = new JsonSerializerOptions
            //{
            //    PropertyNameCaseInsensitive = true
            //};

            //string json = JsonSerializer.Serialize(_newNode, options);

            string json = JsonConvert.SerializeObject(_newNode, Formatting.Indented);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{serverUrl}/editnode", content);
            if (response.IsSuccessStatusCode)
            {
                GraphDtoToGrath(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            return null;
        }

        //Создание/редактирование ребра.
        public async Task<Graph> CreateOrEditEdge(Edge _edge)
        {
            if (_edge == null) return null;

            if (_edge.Source != null && _edge.Target != null && _edge.PortSource != null && _edge.PortTarget != null)
            {
                EdgeDtoIdOnly edge = new EdgeDtoIdOnly()
                {
                    SourceId = _edge.Source.Id,
                    TargetId = _edge.Target.Id,
                    SourcePortId = _edge.PortSource.Id,
                    TargetPortId = _edge.PortTarget.Id
                };

                string json = JsonConvert.SerializeObject(edge);
                StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

                HttpResponseMessage response = await client.PostAsync($"{serverUrl}/edgeworks", content);
                if (response.IsSuccessStatusCode)
                {
                    GraphDtoToGrath(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                    if (_error) return null;
                    return graph;
                }
            }
            return null;
        }

        //Удаление ребра
        public async Task<Graph> DeleteEdge(int _edgeId)
        {
            var response = await client.DeleteAsync($"{serverUrl}/deleteedge/{_edgeId}");
            if (response.IsSuccessStatusCode)
            {
                GraphDtoToGrath(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            return null;
        }

        static void GraphDtoToGrath(string _content, ref Graph _graph, out bool _error)
        {
            try
            {
                if (_graph != null)
                {
                    _graph.Clear();
                }
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // Игнорировать null-значения
                    MissingMemberHandling = MissingMemberHandling.Ignore, // Игнорировать отсутствующие свойства
                    ContractResolver = new CamelCasePropertyNamesContractResolver() // Использовать camelCase
                };
                var graphDto = JsonConvert.DeserializeObject<GraphDto>(_content);
                // Добавление узлов в граф
                foreach (var nodeDto in graphDto.Vertices)
                {
                    Node node = new Node
                    {
                        Id = nodeDto.Id,
                        PortsNumber = nodeDto.PortsNumber,
                        NodeName = nodeDto.NodeName,
                        X = nodeDto.X,
                        Y = nodeDto.Y,
                        SimpleData = new NodeData(),
                        Ports = nodeDto.Ports.Select(p => new Port
                        {
                            Id = p.Id,
                            LocalId = p.LocalId,
                            InputPortNumber = p.InputPortNumber,
                            IsLeftSidePort = p.IsLeftSidePort
                        }).ToList()
                    };

                    _graph.AddVertex(node);
                }

                // Добавление ребер в граф
                foreach (var edgeDto in graphDto.Edges)
                {
                    var sourceNode = _graph.Vertices.FirstOrDefault(n => n.Id == edgeDto.Source.Id);
                    var targetNode = _graph.Vertices.FirstOrDefault(n => n.Id == edgeDto.Target.Id);

                    if (sourceNode != null && targetNode != null)
                    {
                        var edge = new Edge(sourceNode, targetNode);
                        _graph.AddEdge(edge);
                    }
                }
                _error = false;
            }
            catch (System.Text.Json.JsonException ex)
            {
                _error = true;
                throw;
            }
        }

        public static string GraphToGraphDto(Graph _graph, out bool _error)
        {
            _error = false;

            try
            {
                // Создаем DTO для графа
                var graphDto = new GraphDto
                {
                    Vertices = _graph.Vertices.Select(node => new NodeDto
                    {
                        Id = node.Id,
                        PortsNumber = node.PortsNumber,
                        NodeName = node.NodeName,
                        X = node.X,
                        Y = node.Y,
                        Ports = node.Ports.Select(port => new PortDto
                        {
                            Id = port.Id,
                            LocalId = port.LocalId,
                            InputPortNumber = port.InputPortNumber,
                            IsLeftSidePort = port.IsLeftSidePort
                        }).ToList()
                    }).ToList(),

                    Edges = _graph.Edges.Select(edge => new EdgeDto
                    {
                        Source = new NodeDto
                        {
                            Id = edge.Source.Id,
                            PortsNumber = edge.Source.PortsNumber,
                            NodeName = edge.Source.NodeName
                        },
                        Target = new NodeDto
                        {
                            Id = edge.Target.Id,
                            PortsNumber = edge.Target.PortsNumber,
                            NodeName = edge.Target.NodeName
                        }
                    }).ToList()
                };

                // Настройки сериализации
                var settings = new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore, // Игнорировать null-значения
                    MissingMemberHandling = MissingMemberHandling.Ignore, // Игнорировать отсутствующие свойства
                    ContractResolver = new CamelCasePropertyNamesContractResolver(), // Использовать camelCase
                    Formatting = Formatting.Indented // Красивый вывод с отступами
                };

                // Сериализация в JSON
                string json = JsonConvert.SerializeObject(graphDto, settings);
                return json;
            }
            catch (Exception ex)
            {
                _error = true;
                throw new Exception("Ошибка при преобразовании графа в JSON", ex);
            }
        }

    }
}
