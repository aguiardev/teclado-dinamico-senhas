using System.Security.Cryptography;
using System.Text;

// O "partial class Program" é só um contêiner para os métodos: em C# um método precisa
// morar dentro de um tipo. Não há estado nem objetos aqui, apenas funções.
partial class Program
{
    const int TamanhoSenha = 4;
    const int IteracoesPbkdf2 = 100_000;

    /* Embaralha os dígitos de 0 a 9 (Fisher-Yates) e divide em 5 grupos de 2.

    1. Percorra o array do fim para o começo.
    2. Em cada posição i, sorteie um índice j entre 0 e i e troque os dois elementos.
    3. A posição i fica definitiva e o sorteio continua só no que sobrou.

    Este algoritmo garante que o teclado de cada sessão seja uma permutação aleatória sem favorecer nenhuma ordem.

    */
    static int[][] GerarGrupos()
    {
        var digitos = Enumerable.Range(0, 10).ToArray();

        for (var i = digitos.Length - 1; i > 0; i--)
        {
            var j = RandomNumberGenerator.GetInt32(i + 1);
            (digitos[i], digitos[j]) = (digitos[j], digitos[i]);
        }
        
        /* Transforma lista numérica em lista de pares e ordena cada par. Ex:

        - [9, 4, 2, 7, 0, 5, 1, 8, 6, 3] <- lista original
        - [9,4], [2,7], [0,5], [1,8], [6,3] <- lista de listas com dois números
        - [4,9], [2,7], [0,5], [1,8], [3,6] <- ordena os números de cada lista */

        return digitos.Chunk(2).Select(grupo => grupo.Order().ToArray()).ToArray();
    }

    // Chave aleatória que vale só para esta sessão.
    static byte[] GerarChaveSessao() => RandomNumberGenerator.GetBytes(32);

    // Token do grupo: HMAC-SHA256 do grupo com a chave da sessão.
    // Sem a chave, não dá para descobrir quais dígitos estão por trás do token.
    static string GerarToken(byte[] chaveSessao, int[] grupo)
    {
        var dados = Encoding.UTF8.GetBytes(string.Join(",", grupo));
        return Convert.ToHexStringLower(HMACSHA256.HashData(chaveSessao, dados))[..16];
    }

    static string[] GerarTokens(byte[] chaveSessao, int[][] grupos) =>
        grupos.Select(grupo => GerarToken(chaveSessao, grupo)).ToArray();

    /* O mapa fica guardado no servidor, associado à sessão: token -> grupo. Detalhes do Zip:
    
        - Chaves precisam ser únicas. Se dois tokens fossem iguais, o ToDictionary 
        lançaria exceção. Na prática isso não acontece, porque cada token é um HMAC 
        de um grupo diferente (e no teste que rodei os 5 tokens saíram distintos).

        - O Zip para no array mais curto. Se tokens e grupos tivessem tamanhos diferentes, 
        os elementos sobrando seriam ignorados sem erro. Aqui os dois têm sempre 5 itens.
    */
    static Dictionary<string, int[]> CriarMapaSessao(string[] tokens, int[][] grupos) =>
        tokens.Zip(grupos).ToDictionary(par => par.First, par => par.Second);

    // Converte os tokens recebidos de volta em grupos. Devolve null se algum token
    // não existir no mapa (sessão expirada, outra sessão ou token forjado).
    static int[][]? ReverterTokens(string[] tokens, Dictionary<string, int[]> mapaSessao)
    {
        var grupos = new int[tokens.Length][];

        for (var i = 0; i < tokens.Length; i++)
        {
            if (!mapaSessao.TryGetValue(tokens[i], out var grupo))
                return null;

            grupos[i] = grupo;
        }

        return grupos;
    }

    // Produto cartesiano: todas as senhas possíveis a partir dos grupos recebidos.
    // Ex.: [1,2] e [3,9] -> 13, 19, 23, 29.
    static List<string> GerarCombinacoes(int[][] grupos)
    {
        var candidatas = new List<string> { "" };

        foreach (var grupo in grupos)
        {
            candidatas = candidatas
                .SelectMany(prefixo => grupo.Select(digito => prefixo + digito))
                .ToList();
        }

        return candidatas;
    }

    static byte[] HashSenha(string senha, byte[] salt) =>
        Rfc2898DeriveBytes.Pbkdf2(senha, salt, IteracoesPbkdf2, HashAlgorithmName.SHA256, 32);

    // Compara cada combinação com a senha cadastrada (hash + salt).
    // Todas são avaliadas, para a demonstração mostrar a tabela completa.
    static List<(string Candidata, byte[] Hash, bool Confere)> CompararCombinacoes(
        List<string> combinacoes, byte[] salt, byte[] hashCadastrado)
    {
        return combinacoes
            .Select(candidata =>
            {
                var hash = HashSenha(candidata, salt);
                return (candidata, hash, CryptographicOperations.FixedTimeEquals(hash, hashCadastrado));
            })
            .ToList();
    }
}
