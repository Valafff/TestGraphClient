using System.Text;
using System.Text.Json;
using TestGraphModel;

namespace TestGraphClientLogic
{
    public class TestGraphClientLogic
    {
        private HttpClient client;
        private string serverUrl;
        private Graph graph;
        

        public TestGraphClientLogic(string _uri = "http://localhost:3000/api/graph")
        {
            serverUrl = _uri;
            client = new HttpClient();
            graph = new Graph();
        }

        //Проверка соединения
        public async Task<bool> ConnectionTest()
        {
            HttpResponseMessage response = await client.GetAsync($"{serverUrl}/ping");
            return response.IsSuccessStatusCode;
        }

        //Получение модели графа
        public async Task<Graph> GetGraph()
        {
            HttpResponseMessage response = await client.GetAsync($"{serverUrl}/sendgraph");
            if (response.IsSuccessStatusCode)
            {
                string content = await response.Content.ReadAsStringAsync();
                GraphGetting(content, ref graph, out bool _error);
                if (_error)  return null; 
                return graph;
            }
            else
                return null;
        }

        //Создание узла
        public async Task<Graph> CreateNode(Node _newNode)
        {
            //Если ошибка - возвращается null
            if (_newNode == null) return null;
            if (string.IsNullOrEmpty(_newNode.NodeName)) return null; 


            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase, // Используем camelCase для JSON
                WriteIndented = true // Форматировать JSON (опционально) чтобы не было каши в консоли
            };
            string json = JsonSerializer.Serialize(_newNode, options);

            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{serverUrl}/createnode", content);
            if (response.IsSuccessStatusCode)
            {
                GraphGetting(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
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
            Node temp = graph.Nodes.FirstOrDefault(n => n.Id == _newNode.Id);
            if (temp == null) return null;

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string json = JsonSerializer.Serialize(_newNode, options);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{serverUrl}/editnode", content);
            if (response.IsSuccessStatusCode)
            {
                GraphGetting(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            return null;
        }

        //Создание/редактирование ребра.
        public async Task<Graph> CreateOrEditEdge(Edge _edge)
        {

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            string json = JsonSerializer.Serialize(_edge, options);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response = await client.PostAsync($"{serverUrl}/edgeworks", content);
            if (response.IsSuccessStatusCode)
            {
                GraphGetting(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            return null;
        }

        //Удаление ребра
        public async Task<Graph> DeleteEdge(int _edgeId)
        {
            var response = await client.DeleteAsync($"{serverUrl}/deleteedge/{_edgeId}");
            if (response.IsSuccessStatusCode)
            {
                GraphGetting(await response.Content.ReadAsStringAsync(), ref graph, out bool _error);
                if (_error) return null;
                return graph;
            }
            return null;
        }
     
        void GraphGetting(string _content, ref Graph _graph, out bool _error)
        {
            Graph tempGraph = new Graph();
            ////Для правильной сериализации и десериализации  PascalCase 
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            try
            {
                tempGraph = JsonSerializer.Deserialize<Graph>(_content, options);
                _graph = tempGraph;
                _error = false;
            }
            catch (JsonException ex)
            {
                _error = true;
                throw;
            }
        }

    }
}
