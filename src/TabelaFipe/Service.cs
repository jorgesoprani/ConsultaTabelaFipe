using System.Net.Http.Json;

namespace TabelaFipe
{
    public class FipeService
    {
        private HttpClient _httpClient = new HttpClient()
        {
            BaseAddress = new Uri("https://veiculos.fipe.org.br")
        };

        public async Task<List<TabelaReferencia>> ConsultarTabelaDeReferenciaAsync()
        {
            var response = await _httpClient.PostAsync("/api/veiculos/ConsultarTabelaDeReferencia", new StringContent(""));
            return await response.Content.ReadFromJsonAsync<List<TabelaReferencia>>();
        }

        public async Task<List<Marca>> ConsultarMarcas(int codigoTabela, int tipoVeiculo = 1)
        {
            var response = await _httpClient.PostAsync("/api/veiculos/ConsultarMarcas", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "codigoTabelaReferencia", codigoTabela.ToString() },
                { "codigoTipoVeiculo", tipoVeiculo.ToString() },
            }));
            if (!response.IsSuccessStatusCode)
            {
                return new List<Marca>();
            }
            return await response.Content.ReadFromJsonAsync<List<Marca>>();
        }

        public async Task<PesquisaModelo> ConsultarModelos(int codigoTabela, int marca, int tipoVeiculo = 1)
        {
            var contentData = new Dictionary<string, string>
            {
                { "codigoTabelaReferencia", codigoTabela.ToString() },
                { "codigoTipoVeiculo", tipoVeiculo.ToString() },
                { "codigoMarca", marca.ToString() },
            };

            var content = new FormUrlEncodedContent(contentData);
            var response = await _httpClient.PostAsync("/api/veiculos/ConsultarModelos", content);
            if (!response.IsSuccessStatusCode)
            {
                return new PesquisaModelo { Anos = new List<Ano>(), Modelos = new List<Modelo>() };
            }
            return await response.Content.ReadFromJsonAsync<PesquisaModelo>();
        }

        public async Task<List<ModeloAno>> ConsultarModelosPorAno(int codigoTabela, int marca, string ano, int tipoVeiculo = 1)
        {
            try
            {
                var contentData = new Dictionary<string, string>
            {
                { "codigoTabelaReferencia", codigoTabela.ToString() },
                { "codigoTipoVeiculo", tipoVeiculo.ToString() },
                { "codigoMarca", marca.ToString() },
                { "ano", ano },
                { "anoModelo", ano.Split("-")[0] },
                { "codigoTipoCombustivel", ano.Split("-")[1] },
            };

                var content = new FormUrlEncodedContent(contentData);
                var response = await _httpClient.PostAsync("/api/veiculos/ConsultarModelosAtravesDoAno", content);
                var json = await response.Content.ReadAsStringAsync();
                if (json.Contains("nadaencontrado"))
                {
                    return new List<ModeloAno>();
                }
                if (!response.IsSuccessStatusCode)
                {
                    return new List<ModeloAno>();
                }
                return await response.Content.ReadFromJsonAsync<List<ModeloAno>>();

            }
            catch (Exception ex)
            {
                return new List<ModeloAno>();
            }
        }

        public async Task<Carro> ConsultarCarro(int codigoTabela, int marca, string codigoModelo, string anoModelo, int tipoVeiculo = 1)
        {
            try
            {
                var contentData = new Dictionary<string, string>
                {
                    { "codigoTabelaReferencia", codigoTabela.ToString() },
                    { "codigoTipoVeiculo", tipoVeiculo.ToString() },
                    { "codigoMarca", marca.ToString() },
                    { "codigoModelo", codigoModelo },
                    { "anoModelo", anoModelo.Split("-")[0] },
                    { "codigoTipoCombustivel", anoModelo.Split("-")[1] },
                    { "tipoVeiculo", "carro" },
                    { "tipoConsulta", "tradicional" },
                };

                var content = new FormUrlEncodedContent(contentData);
                var response = await _httpClient.PostAsync("/api/veiculos/ConsultarValorComTodosParametros", content);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }

                var result = await response.Content.ReadFromJsonAsync<Carro>();
                if (result.Codigo == "0")
                {
                    return null;
                }
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<Carro> ConsultarCarroPorCodigo(int codigoTabela, string codigoFipe, string anoModelo, int tipoVeiculo = 1)
        {
            try
            {
                var contentData = new Dictionary<string, string>
                {
                    { "codigoTabelaReferencia", codigoTabela.ToString() },
                    { "codigoTipoVeiculo", tipoVeiculo.ToString() },
                    { "anoModelo", anoModelo.Split("-")[0] },
                    { "codigoTipoCombustivel", anoModelo.Split("-")[1] },
                    { "tipoVeiculo", "carro" },
                    { "tipoConsulta", "codigo" },
                    { "modeloCodigoExterno", codigoFipe }
                };

                var content = new FormUrlEncodedContent(contentData);
                var response = await _httpClient.PostAsync("/api/veiculos/ConsultarValorComTodosParametros", content);
                if (!response.IsSuccessStatusCode)
                {
                    return null;
                }
                var result = await response.Content.ReadFromJsonAsync<Carro>();
                if (result.Codigo == "0")
                {
                    return null;
                }
                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
        }
    }

    public class TabelaReferencia
    {
        public int Codigo { get; set; }
        public string Mes { get; set; }
    }

    public class Marca
    {
        public int Value { get; set; }
        public string Label { get; set; }
    }

    public class PesquisaModelo
    {
        public List<Ano> Anos { get; set; }
        public List<Modelo> Modelos { get; set; }
    }

    public class Ano
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class Modelo
    {
        public string Label { get; set; }
        public int Value { get; set; }
    }

    public class ModeloAno
    {
        public string Label { get; set; }
        public string Value { get; set; }
    }

    public class Carro
    {
        public string Codigo { get; set; }
        public int AnoModelo { get; set; }
        public string CodigoFipe { get; set; }
        public string Combustivel { get; set; }
        public string Marca { get; set; }
        public string MesReferencia { get; set; }
        public string Modelo { get; set; }
        public string Valor { get; set; }
    }
}
