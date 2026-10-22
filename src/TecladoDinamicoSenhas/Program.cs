using System.Security.Cryptography;
using Spectre.Console;

// Demonstração do teclado dinâmico de senhas. O console faz os dois papéis:
// SERVIDOR (cadastro, sessão, validação) e CLIENTE (o usuário escolhendo os grupos).

const string senhaCadastrada = "1234";

// Servidor: a senha cadastrada fica guardada só como hash + salt.
var salt = RandomNumberGenerator.GetBytes(16);
var hashCadastrado = HashSenha(senhaCadastrada, salt);

// Servidor: abre a sessão, embaralha os dígitos e guarda o mapa token -> grupo.
var grupos = GerarGrupos();
var chaveSessao = GerarChaveSessao();
var tokens = GerarTokens(chaveSessao, grupos);
var mapaSessao = CriarMapaSessao(tokens, grupos);

// Cliente: o usuário monta a senha escolhendo grupos.
var indices = SelecionarGrupos(grupos);
var gruposEscolhidos = indices.Select(i => grupos[i]).ToArray();
var tokensEnviados = indices.Select(i => tokens[i]).ToArray();

// 1. Cadastro
AnsiConsole.Clear();
Etapa("1. Servidor: senha cadastrada (só o hash é guardado)");
var cadastro = new Table().AddColumn("Item").AddColumn("Valor");
cadastro.AddRow("Senha cadastrada (visível só nesta demo)", senhaCadastrada);
cadastro.AddRow("Salt", Convert.ToHexStringLower(salt));
cadastro.AddRow("Hash PBKDF2", Resumo(hashCadastrado));
AnsiConsole.Write(cadastro);
Pausar();

// 2. Sessão
Etapa("2. Servidor: sessão e mapa do teclado");
var sessao = new Table().AddColumn("Grupo").AddColumn("Token (HMAC-SHA256 da sessão)");
for (var i = 0; i < grupos.Length; i++)
    sessao.AddRow(RotuloGrupo(grupos[i]), tokens[i]);
AnsiConsole.Write(sessao);
Pausar();

// 3. O que viaja pela rede
Etapa("3. Cliente: o que é enviado ao servidor");
AnsiConsole.MarkupLine($"Grupos escolhidos: {Markup.Escape(string.Join(" → ", gruposEscolhidos.Select(RotuloGrupo)))}");
AnsiConsole.MarkupLine("Tokens enviados (nenhum dígito sai do cliente):");
foreach (var token in tokensEnviados)
    AnsiConsole.MarkupLine($"  [cyan]{token}[/]");
Pausar();

// 4. Reversão pelo mapa da sessão
Etapa("4. Servidor: reverte os tokens pelo mapa da sessão");
var gruposRevertidos = ReverterTokens(tokensEnviados, mapaSessao);
if (gruposRevertidos is null)
{
    AnsiConsole.MarkupLine("[red]Token desconhecido: tentativa rejeitada.[/]");
    return;
}
for (var i = 0; i < gruposRevertidos.Length; i++)
    AnsiConsole.MarkupLine($"  [cyan]{tokensEnviados[i]}[/] → {Markup.Escape(RotuloGrupo(gruposRevertidos[i]))}");
Pausar();

// 5 e 6. Combinações, hashes e comparação
Etapa("5. Servidor: combinações possíveis e comparação com o hash cadastrado");
var combinacoes = GerarCombinacoes(gruposRevertidos);
AnsiConsole.MarkupLine($"{combinacoes.Count} combinações possíveis. Hash cadastrado: [cyan]{Resumo(hashCadastrado)}[/]");
var resultado = CompararCombinacoes(combinacoes, salt, hashCadastrado);

var comparacao = new Table().AddColumn("#").AddColumn("Combinação").AddColumn("Hash PBKDF2").AddColumn("Confere?");
for (var i = 0; i < resultado.Count; i++)
{
    var (candidata, hash, confere) = resultado[i];
    comparacao.AddRow(
        (i + 1).ToString(),
        candidata,
        Resumo(hash),
        confere ? "[green]SIM[/]" : "[grey]não[/]");
}
AnsiConsole.Write(comparacao);

// O servidor descarta a sessão: o mapa não serve mais para nada.
mapaSessao.Clear();

AnsiConsole.WriteLine();
AnsiConsole.MarkupLine(resultado.Any(r => r.Confere)
    ? "[bold white on green]  SENHA CORRETA  [/]"
    : "[bold white on red]  SENHA INCORRETA  [/]");
Pausar();

// 7. Replay: o atacante reenvia os mesmos tokens em uma nova tentativa de login.
Etapa("6. Replay: mesmos tokens em uma nova sessão");
var novaChave = GerarChaveSessao();
var novosTokens = GerarTokens(novaChave, grupos);
var novoMapa = CriarMapaSessao(novosTokens, grupos);

AnsiConsole.MarkupLine($"Token do grupo {Markup.Escape(RotuloGrupo(gruposEscolhidos[0]))} na sessão original: [cyan]{tokensEnviados[0]}[/]");
AnsiConsole.MarkupLine($"Token do mesmo grupo na nova sessão:    [cyan]{GerarToken(novaChave, gruposEscolhidos[0])}[/]");

var replay = ReverterTokens(tokensEnviados, novoMapa);
AnsiConsole.WriteLine();
AnsiConsole.MarkupLine(replay is null
    ? "[bold black on yellow]  REPLAY BLOQUEADO: os tokens capturados não existem nesta sessão  [/]"
    : "[bold white on red]  ALERTA: replay aceito  [/]");
