using Spectre.Console;

partial class Program
{
    static string RotuloGrupo(int[] grupo) => $"{grupo[0]} ou {grupo[1]}";

    // Mostra só o início do hash, o suficiente para comparar visualmente.
    static string Resumo(byte[] bytes) => Convert.ToHexStringLower(bytes)[..16] + "…";

    static void Etapa(string titulo)
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[yellow]{Markup.Escape(titulo)}[/]").LeftJustified());
    }

    static void Pausar()
    {
        AnsiConsole.MarkupLine("[grey]Pressione qualquer tecla para continuar...[/]");
        Console.ReadKey(intercept: true);
    }

    // Laço de teclas do "teclado": setas navegam, ENTER acrescenta o grupo,
    // BACKSPACE remove o último e CONFIRMAR envia a seleção.
    // Devolve os índices dos grupos escolhidos, na ordem em que foram digitados.
    static List<int> SelecionarGrupos(int[][] grupos)
    {
        var selecionados = new List<int>();
        var cursor = 0;
        var indiceConfirmar = grupos.Length;
        string? aviso = null;

        while (true)
        {
            DesenharTeclado(grupos, selecionados, cursor, aviso);
            aviso = null;

            switch (Console.ReadKey(intercept: true).Key)
            {
                case ConsoleKey.UpArrow:
                    cursor = Math.Max(0, cursor - 1);
                    break;

                case ConsoleKey.DownArrow:
                    cursor = Math.Min(indiceConfirmar, cursor + 1);
                    break;

                case ConsoleKey.Backspace:
                    if (selecionados.Count > 0)
                        selecionados.RemoveAt(selecionados.Count - 1);
                    break;

                case ConsoleKey.Enter when cursor == indiceConfirmar:
                    if (selecionados.Count == TamanhoSenha)
                        return selecionados;

                    aviso = $"Selecione {TamanhoSenha} grupos antes de confirmar.";
                    break;

                case ConsoleKey.Enter:
                    if (selecionados.Count < TamanhoSenha)
                        selecionados.Add(cursor);
                    else
                        aviso = $"A senha tem {TamanhoSenha} posições. Use BACKSPACE para apagar ou escolha CONFIRMAR.";
                    break;
            }
        }
    }

    static void DesenharTeclado(int[][] grupos, List<int> selecionados, int cursor, string? aviso)
    {
        AnsiConsole.Clear();
        AnsiConsole.Write(new Rule("[yellow]Teclado dinâmico de senhas[/]").LeftJustified());
        AnsiConsole.MarkupLine("[grey]Setas: navegar | ENTER: adicionar | BACKSPACE: apagar o último[/]");
        AnsiConsole.WriteLine();

        for (var i = 0; i < grupos.Length; i++)
        {
            var texto = Markup.Escape(RotuloGrupo(grupos[i]));
            AnsiConsole.MarkupLine(i == cursor ? $"[black on white]  > {texto}  [/]" : $"    {texto}");
        }

        AnsiConsole.MarkupLine(cursor == grupos.Length
            ? "[black on white]  >> CONFIRMAR <<  [/]"
            : "    >> CONFIRMAR <<");

        var posicoes = Enumerable.Range(0, TamanhoSenha)
            .Select(i => i < selecionados.Count ? $"({RotuloGrupo(grupos[selecionados[i]])})" : "( _ )");

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Senha: [green]{Markup.Escape(string.Join(" ", posicoes))}[/]");

        if (aviso is not null)
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(aviso)}[/]");
    }
}
