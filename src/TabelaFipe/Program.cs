using TabelaFipe;
var service = new FipeService();

var anoAtual = "32000";
var gasolina = "1";
var etanol = "2";
var diesel = "3";
var tiposCombustivel = new[] { gasolina, etanol, diesel };
var tabelas = await service.ConsultarTabelaDeReferenciaAsync();
var tabela = PromptTabelaReferencia(tabelas);
var ultimoMes = tabela.Codigo;

Console.WriteLine($"A partir de que ano?");
var anoInicial = Console.ReadLine();

var anoFinal = int.Parse(tabela.Mes.Split("/")[1]);

var filtroAnos = new List<string>();
for (int i = int.Parse(anoInicial); i <= anoFinal + 1; i++)
{
    filtroAnos.Add(i.ToString());
}
filtroAnos.Add(anoAtual);

Console.WriteLine($"Buscando dados de modelos entre ano {anoInicial} e {anoFinal}");

var porCodigo = false;

Console.WriteLine($"Buscar por lista de codigos FIPE? S para sim");
porCodigo = Console.ReadKey().Key == ConsoleKey.S;

var batchSize = 100;
if (porCodigo)
{
    List<string> codigos = GetCodigos();
    var combos = new List<(string codigo, string ano, string tipoCombustivel)>();
    List<Carro> carros = new List<Carro>();
    foreach (var codigo in codigos)
    {
        foreach (var ano in filtroAnos)
        {
            foreach (var tipoCombustivel in tiposCombustivel)
            {
                combos.Add((codigo, ano, tipoCombustivel));
            }
        }
    }

    var count = 0;
    var batches = new List<Task>();
    foreach (var item in combos)
    {
        count++;
        batches.Add(Task.Run(async () =>
        {
            //Console.WriteLine($"({item.ano} - {item.codigo} - {item.tipoCombustivel})");
            var carro = await service.ConsultarCarroPorCodigo(
                ultimoMes,
                item.codigo,
                item.ano + "-" + item.tipoCombustivel);

            if (carro != null && carro.Modelo != null)
            {
                //Console.WriteLine($"{carro.Marca} - {carro.Modelo} - {carro.Valor}");
                carros.Add(carro);
            }
        }));

        if (batches.Count == batchSize || combos.IndexOf(item) == (combos.Count - 1))
        {
            Console.WriteLine($"Item {count} de {combos.Count}");
            Task.WaitAll(batches.ToArray());
            batches.Clear();
        }
    }
    WriteOutput(carros);
}
else
{
    Console.WriteLine();
    Console.WriteLine($"Buscando opções por cada ano...");
    var combos = new List<(int marca, string codigo, string ano, string tipoCombustivel)>();
    var marcas = await service.ConsultarMarcas(ultimoMes);
    var batches = new List<Task>();
    foreach (var marca in marcas)
    {
        foreach (var ano in filtroAnos)
        {
            foreach (var tipoCombustivel in tiposCombustivel)
            {
                batches.Add(Task.Run(async () =>
                {
                    var modelos = await service.ConsultarModelosPorAno(ultimoMes, marca.Value, ano + "-" + tipoCombustivel);

                    foreach (var modelo in modelos)
                    {
                        combos.Add((marca.Value, modelo.Value, ano, tipoCombustivel));
                    }
                }));

                if (batches.Count == batchSize)
                {
                    Task.WaitAll(batches.ToArray());
                    batches.Clear();
                }
            }
        }
    }

    if (batches.Any())
    {
        Task.WaitAll(batches.ToArray());
        batches.Clear();
    }
    Console.WriteLine($"Buscando valores...");
    List<Carro> carros = new List<Carro>();
    var count = 0;
    foreach (var item in combos)
    {
        count++;
        batches.Add(Task.Run(async () =>
        {
            //Console.WriteLine($"({item.ano} - {item.codigo} - {item.tipoCombustivel})");
            var carro = await service.ConsultarCarro(
                ultimoMes,
                item.marca,
                item.codigo,
                item.ano + "-" + item.tipoCombustivel);

            if (carro != null && carro.Modelo != null)
            {
                //Console.WriteLine($"{carro.Marca} - {carro.Modelo} - {carro.Valor}");
                carros.Add(carro);
            }
        }));

        if (batches.Count == batchSize || combos.IndexOf(item) == (combos.Count - 1))
        {
            Console.WriteLine($"Item {count} de {combos.Count}");
            Task.WaitAll(batches.ToArray());
            batches.Clear();
        }
    }
    WriteOutput(carros);
}

static void WriteOutput(List<Carro> carros)
{
    var output = new List<string>();
    output.Add($"Tabela de Referencia,Marca,Modelo,Ano,Codigo Fipe,Combustivel,Valor");
    foreach (var carro in carros)
    {
        output.Add($"{carro.MesReferencia},{carro.Marca},\"{carro.Modelo.Replace(",", " ")}\",{(carro.AnoModelo == 32000 ? "Ano atual" : carro.AnoModelo)},{carro.CodigoFipe},{(carro.Combustivel)},\"{carro.Valor.Replace("R$ ", "")}\"");
    }

    File.WriteAllLines("Resultado.csv", output);

}

static List<string> GetCodigos()
{
    var localArquivo = "Codigos.csv";

    Console.WriteLine($"Procurando arquivo na pasta atual com nome {localArquivo}. Digite caminho diferente se necessário");
    var novoCaminho = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(novoCaminho))
    {
        localArquivo = novoCaminho;
    }

    var arquivo = File.ReadAllLines(localArquivo);

    var codigos = new List<string>();
    for (int i = 1; i < arquivo.Length; i++)
    {
        var line = arquivo[i];
        if (line.StartsWith("\""))
        {
            codigos.Add(line.Split("\"").Last().Replace(",", ""));
        }
        else
        {
            codigos.Add(line.Split(",").Last());
        }
    }

    return codigos;
}

static TabelaReferencia PromptTabelaReferencia(List<TabelaReferencia> tabelas)
{
    TabelaReferencia result = null;
    var atual = tabelas.First();
    while (result == null)
    {
        Console.WriteLine($"Qual data de referencia utilizar? (ex: junho/2023) deixe vazio para usar a atual({atual.Mes})");
        var input = Console.ReadLine();
        if (input == "")
        {
            result = atual;
            return atual;
        }

        var tabelaSelecionada = tabelas.FirstOrDefault(x => x.Mes.Trim().Equals(input?.Trim(), StringComparison.CurrentCultureIgnoreCase));

        if (tabelaSelecionada == null)
        {
            Console.WriteLine($"Tabela não encontrada. Essas são as tabelas disponíveis:");
            foreach (var tabela in tabelas)
            {
                Console.WriteLine($"{tabela.Mes}");
            }
        }
        else
        {
            result = tabelaSelecionada;
            return tabelaSelecionada;
        }
    }

    return result;
}